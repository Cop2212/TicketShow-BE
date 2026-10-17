using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.CheckIn.Commands;

public record VerifyCheckInCommand(string QrCodeString, Guid? EventId = null) : IRequest<ApiResponse<CheckInResultDto>>;

public record CheckInResultDto(
    string Status, // SUCCESS, ALREADY_USED, INVALID, WRONG_EVENT
    string Message,
    string? TicketCode,
    string? CustomerName,
    string? TierName,
    string? SeatLabel,
    DateTime? CheckedInAt
);

public record GetOfflineManifestQuery(Guid EventId) : IRequest<ApiResponse<List<OfflineTicketManifestDto>>>;

public record OfflineTicketManifestDto(
    string TicketCode,
    string QrCodeSignature,
    string TierName,
    string SeatLabel,
    string CustomerName,
    bool IsCheckedIn
);

public record OfflineCheckInItem(
    string TicketCode,
    DateTime ScannedAt,
    string? Notes = null
);

public record BatchSyncCheckInCommand(
    Guid EventId,
    List<OfflineCheckInItem> Items
) : IRequest<ApiResponse<BatchSyncResultDto>>;

public record BatchSyncResultDto(
    int TotalSubmitted,
    int SuccessfullySynced,
    int AlreadyCheckedInCount,
    int NotFoundCount
);

public class CheckInHandlers :
    IRequestHandler<VerifyCheckInCommand, ApiResponse<CheckInResultDto>>,
    IRequestHandler<GetOfflineManifestQuery, ApiResponse<List<OfflineTicketManifestDto>>>,
    IRequestHandler<BatchSyncCheckInCommand, ApiResponse<BatchSyncResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly IQrSignatureService _qrService;
    private readonly ICurrentUserService _currentUser;

    public CheckInHandlers(IAppDbContext context, IQrSignatureService qrService, ICurrentUserService currentUser)
    {
        _context = context;
        _qrService = qrService;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<CheckInResultDto>> Handle(VerifyCheckInCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify HMAC Signature directly
        var isSignatureValid = _qrService.VerifyQRSignedData(request.QrCodeString);
        if (!isSignatureValid)
        {
            return ApiResponse<CheckInResultDto>.Ok(new CheckInResultDto(
                Status: "INVALID",
                Message: "MÃ QR KHÔNG HỢP LỆ (Chữ ký HMAC không khớp hoặc mã đã bị can thiệp)",
                TicketCode: null,
                CustomerName: null,
                TierName: null,
                SeatLabel: null,
                CheckedInAt: null
            ), "Mã QR giả mạo hoặc không hợp lệ.");
        }

        var payload = _qrService.ParseQrPayload(request.QrCodeString);
        if (payload == null)
        {
            return ApiResponse<CheckInResultDto>.Ok(new CheckInResultDto(
                Status: "INVALID",
                Message: "Không thể đọc dữ liệu trong mã QR",
                TicketCode: null,
                CustomerName: null,
                TierName: null,
                SeatLabel: null,
                CheckedInAt: null
            ), "Dữ liệu QR không đúng cấu trúc.");
        }

        // 2. Query ticket from database
        var ticket = await _context.Tickets
            .Include(t => t.Order)
            .Include(t => t.Seat)
            .Include(t => t.TicketTier)
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.TicketCode == payload.TicketCode, cancellationToken);

        if (ticket == null)
        {
            return ApiResponse<CheckInResultDto>.Ok(new CheckInResultDto(
                Status: "INVALID",
                Message: "Vé không tồn tại trong hệ thống máy chủ",
                TicketCode: payload.TicketCode,
                CustomerName: payload.Email,
                TierName: null,
                SeatLabel: null,
                CheckedInAt: null
            ), "Vé không tồn tại.");
        }

        // Check if event matches
        if (request.EventId.HasValue && ticket.EventId != request.EventId.Value)
        {
            return ApiResponse<CheckInResultDto>.Ok(new CheckInResultDto(
                Status: "WRONG_EVENT",
                Message: $"VÉ KHÔNG THUỘC SỰ KIỆN NÀY (Vé áp dụng cho: {ticket.Event?.Title})",
                TicketCode: ticket.TicketCode,
                CustomerName: ticket.Order?.CustomerName,
                TierName: ticket.TicketTier?.Name,
                SeatLabel: ticket.Seat != null ? $"{ticket.Seat.RowName}{ticket.Seat.SeatNumber}" : "GA",
                CheckedInAt: null
            ), "Sai sự kiện.");
        }

        var staffUserId = _currentUser.UserId;
        var seatLabel = ticket.Seat != null ? $"{ticket.Seat.RowName}{ticket.Seat.SeatNumber}" : "GA";

        // 3. Check duplicate check-in (Anti-fraud)
        if (ticket.Status == TicketStatus.CheckedIn)
        {
            // Log attempt
            _context.CheckInLogs.Add(new CheckInLog
            {
                TicketId = ticket.Id,
                ScannedByUserId = staffUserId,
                Status = CheckInStatus.AlreadyUsed,
                Message = "Phát hiện quét lại vé đã sử dụng trước đó.",
                ScannedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<CheckInResultDto>.Ok(new CheckInResultDto(
                Status: "ALREADY_USED",
                Message: $"CẢNH BÁO GIAN LẬN: Vé này đã được check-in lúc {ticket.CheckedInAt:HH:mm:ss dd/MM/yyyy}!",
                TicketCode: ticket.TicketCode,
                CustomerName: ticket.Order?.CustomerName,
                TierName: ticket.TicketTier?.Name,
                SeatLabel: seatLabel,
                CheckedInAt: ticket.CheckedInAt
            ), "Vé đã sử dụng trước đó.");
        }

        // 4. Valid check-in
        ticket.Status = TicketStatus.CheckedIn;
        ticket.CheckedInAt = DateTime.UtcNow;
        ticket.CheckedByStaffId = staffUserId;

        _context.CheckInLogs.Add(new CheckInLog
        {
            TicketId = ticket.Id,
            ScannedByUserId = staffUserId,
            Status = CheckInStatus.Success,
            Message = "Soát vé thành công.",
            ScannedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CheckInResultDto>.Ok(new CheckInResultDto(
            Status: "SUCCESS",
            Message: "VÉ HỢP LỆ - Cho phép qua cổng!",
            TicketCode: ticket.TicketCode,
            CustomerName: ticket.Order?.CustomerName,
            TierName: ticket.TicketTier?.Name,
            SeatLabel: seatLabel,
            CheckedInAt: ticket.CheckedInAt
        ), "Soát vé thành công.");
    }

    public async Task<ApiResponse<List<OfflineTicketManifestDto>>> Handle(GetOfflineManifestQuery request, CancellationToken cancellationToken)
    {
        var ev = await _context.Events.FindAsync(new object[] { request.EventId }, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Sự kiện", request.EventId);

        var tickets = await _context.Tickets
            .Include(t => t.Order)
            .Include(t => t.Seat)
            .Include(t => t.TicketTier)
            .Where(t => t.EventId == request.EventId && t.Status != TicketStatus.Cancelled)
            .AsNoTracking()
            .Select(t => new OfflineTicketManifestDto(
                t.TicketCode,
                t.QrCodeSignature,
                t.TicketTier != null ? t.TicketTier.Name : "General",
                t.Seat != null ? $"{t.Seat.RowName}{t.Seat.SeatNumber}" : "GA",
                t.Order != null ? t.Order.CustomerName : "Khách",
                t.Status == TicketStatus.CheckedIn
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<OfflineTicketManifestDto>>.Ok(tickets, "Đã đồng bộ danh sách vé sự kiện thành công.");
    }

    public async Task<ApiResponse<BatchSyncResultDto>> Handle(BatchSyncCheckInCommand request, CancellationToken cancellationToken)
    {
        var staffUserId = _currentUser.UserId;
        var ticketCodes = request.Items.Select(i => i.TicketCode).Distinct().ToList();

        var tickets = await _context.Tickets
            .Where(t => t.EventId == request.EventId && ticketCodes.Contains(t.TicketCode))
            .ToListAsync(cancellationToken);

        int success = 0;
        int already = 0;
        int notFound = 0;

        foreach (var item in request.Items)
        {
            var ticket = tickets.FirstOrDefault(t => t.TicketCode == item.TicketCode);
            if (ticket == null)
            {
                notFound++;
                continue;
            }

            if (ticket.Status == TicketStatus.CheckedIn)
            {
                already++;
                _context.CheckInLogs.Add(new CheckInLog
                {
                    TicketId = ticket.Id,
                    ScannedByUserId = staffUserId,
                    Status = CheckInStatus.AlreadyUsed,
                    Message = $"Offline Batch Sync: vé đã được check-in trước đó. Quét offline lúc {item.ScannedAt:HH:mm:ss}",
                    ScannedAt = item.ScannedAt
                });
                continue;
            }

            ticket.Status = TicketStatus.CheckedIn;
            ticket.CheckedInAt = item.ScannedAt;
            ticket.CheckedByStaffId = staffUserId;

            _context.CheckInLogs.Add(new CheckInLog
            {
                TicketId = ticket.Id,
                ScannedByUserId = staffUserId,
                Status = CheckInStatus.Success,
                Message = $"Offline Batch Sync thành công. Quét offline lúc {item.ScannedAt:HH:mm:ss}",
                ScannedAt = item.ScannedAt
            });

            success++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var result = new BatchSyncResultDto(
            TotalSubmitted: request.Items.Count,
            SuccessfullySynced: success,
            AlreadyCheckedInCount: already,
            NotFoundCount: notFound
        );

        return ApiResponse<BatchSyncResultDto>.Ok(result, $"Đồng bộ thành công {success}/{request.Items.Count} vé quét ngoại tuyến.");
    }
}
