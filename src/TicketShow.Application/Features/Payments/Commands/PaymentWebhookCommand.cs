using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Payments.Commands;

public record PaymentWebhookCommand : IRequest<ApiResponse<PaymentProcessResultDto>>
{
    public Guid? OrderId { get; init; }
    public string? OrderCode { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string? PaymentGateway { get; init; } = "QRPAY";
    public string Status { get; init; } = "SUCCESS"; // SUCCESS, FAILED
}

public record PaymentProcessResultDto(
    string OrderCode,
    string Status,
    string TransactionId,
    List<GeneratedTicketDto> Tickets
);

public record GeneratedTicketDto(
    Guid TicketId,
    string TicketCode,
    string TierName,
    string SeatLabel,
    string QrData,
    string QrCodeBase64Image
);

public class PaymentWebhookCommandHandler : IRequestHandler<PaymentWebhookCommand, ApiResponse<PaymentProcessResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly IQrSignatureService _qrService;
    private readonly ILockService _lockService;

    public PaymentWebhookCommandHandler(IAppDbContext context, IQrSignatureService qrService, ILockService lockService)
    {
        _context = context;
        _qrService = qrService;
        _lockService = lockService;
    }

    public async Task<ApiResponse<PaymentProcessResultDto>> Handle(PaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(o => o.Tickets)
                .ThenInclude(t => t.Seat)
            .Include(o => o.Tickets)
                .ThenInclude(t => t.TicketTier)
            .AsQueryable();

        Order? order = null;
        if (request.OrderId.HasValue)
        {
            order = await query.FirstOrDefaultAsync(o => o.Id == request.OrderId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.OrderCode))
        {
            order = await query.FirstOrDefaultAsync(o => o.OrderCode == request.OrderCode, cancellationToken);
        }

        if (order == null)
        {
            throw new NotFoundException("Đơn hàng", request.OrderId?.ToString() ?? request.OrderCode ?? "Unknown");
        }

        // Idempotency: If already paid, return existing tickets without re-issuing
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            var existingTickets = order.Tickets.Select(t => new GeneratedTicketDto(
                t.Id,
                t.TicketCode,
                t.TicketTier?.Name ?? "General",
                t.Seat != null ? $"{t.Seat.RowName}{t.Seat.SeatNumber}" : "GA",
                t.QrCodeSignature,
                _qrService.GenerateQrBase64Image(t.QrCodeSignature)
            )).ToList();

            return ApiResponse<PaymentProcessResultDto>.Ok(
                new PaymentProcessResultDto(order.OrderCode, "PAID", order.TransactionId ?? string.Empty, existingTickets),
                "Đơn hàng đã được xác nhận thanh toán trước đó (Idempotent)."
            );
        }

        var isSuccess = request.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                        request.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase);

        if (!isSuccess)
        {
            order.PaymentStatus = PaymentStatus.Failed;

            // Revert locked seats
            var seatsToRevert = await _context.Seats
                .Where(s => s.LockedBySessionId == order.OrderCode && s.Status == SeatStatus.Locked)
                .ToListAsync(cancellationToken);

            foreach (var seat in seatsToRevert)
            {
                seat.Status = SeatStatus.Available;
                seat.LockedUntil = null;
                seat.LockedBySessionId = null;
                await _lockService.ReleaseSeatLockAsync(seat.EventId, seat.Id, order.OrderCode);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<PaymentProcessResultDto>.Ok(
                new PaymentProcessResultDto(order.OrderCode, "FAILED", request.TransactionId, new()),
                "Đã ghi nhận giao dịch thất bại và giải phóng ghế đã giữ."
            );
        }

        // Process successful payment
        order.PaymentStatus = PaymentStatus.Paid;
        order.TransactionId = string.IsNullOrWhiteSpace(request.TransactionId) ? $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.TransactionId;
        order.PaymentGateway = request.PaymentGateway ?? "QRPAY";

        // Find locked seats for this order session
        var lockedSeats = await _context.Seats
            .Include(s => s.TicketTier)
            .Include(s => s.Event)
            .Where(s => s.LockedBySessionId == order.OrderCode)
            .ToListAsync(cancellationToken);

        var generatedTickets = new List<GeneratedTicketDto>();

        foreach (var seat in lockedSeats)
        {
            seat.Status = SeatStatus.Sold;
            seat.LockedUntil = null;

            var ticketCode = $"TICK-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(100, 999)}";
            var qrSignedData = _qrService.GenerateQRSignedData(ticketCode, seat.EventId, order.CustomerEmail);

            var ticket = new Ticket
            {
                OrderId = order.Id,
                EventId = seat.EventId,
                TicketTierId = seat.TicketTierId,
                SeatId = seat.Id,
                TicketCode = ticketCode,
                QrCodeSignature = qrSignedData,
                Status = TicketStatus.Valid
            };

            _context.Tickets.Add(ticket);

            if (seat.TicketTier != null)
            {
                seat.TicketTier.SoldQuantity += 1;
            }

            // Generate Base64 QR image
            var qrBase64 = _qrService.GenerateQrBase64Image(qrSignedData);

            generatedTickets.Add(new GeneratedTicketDto(
                ticket.Id,
                ticket.TicketCode,
                seat.TicketTier?.Name ?? "General",
                $"{seat.RowName}{seat.SeatNumber}",
                qrSignedData,
                qrBase64
            ));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<PaymentProcessResultDto>.Ok(
            new PaymentProcessResultDto(order.OrderCode, "PAID", order.TransactionId, generatedTickets),
            "Xử lý thanh toán thành công và đã xuất E-ticket gắn mã QR bảo mật."
        );
    }
}
