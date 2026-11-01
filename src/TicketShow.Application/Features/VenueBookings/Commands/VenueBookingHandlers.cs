using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.VenueBookings.DTOs;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.VenueBookings.Commands;

public record GetVenueBookingsQuery(
    Guid? VenueId = null,
    Guid? OrganizerId = null,
    VenueBookingStatus? Status = null
) : IRequest<ApiResponse<List<VenueBookingSummaryDto>>>;

public class VenueBookingHandlers :
    IRequestHandler<GetVenueBookingsQuery, ApiResponse<List<VenueBookingSummaryDto>>>,
    IRequestHandler<CheckSlotAvailabilityQuery, ApiResponse<SlotAvailabilityResultDto>>,
    IRequestHandler<RequestVenueBookingCommand, ApiResponse<VenueBookingSummaryDto>>,
    IRequestHandler<RespondVenueBookingCommand, ApiResponse<VenueBookingSummaryDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILockService _lockService;

    public VenueBookingHandlers(IAppDbContext context, ICurrentUserService currentUser, ILockService lockService)
    {
        _context = context;
        _currentUser = currentUser;
        _lockService = lockService;
    }

    public async Task<ApiResponse<List<VenueBookingSummaryDto>>> Handle(GetVenueBookingsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VenueBookings
            .Include(b => b.Venue)
            .Include(b => b.Organizer)
            .Include(b => b.Event)
            .AsNoTracking()
            .AsQueryable();

        if (request.VenueId.HasValue)
        {
            query = query.Where(b => b.VenueId == request.VenueId.Value);
        }

        if (request.OrganizerId.HasValue)
        {
            query = query.Where(b => b.OrganizerId == request.OrganizerId.Value);
        }
        else if (_currentUser.UserId.HasValue)
        {
            // If current user is a VenueOwner: show bookings for venues they own
            // If Organizer: show bookings they created
            var userId = _currentUser.UserId.Value;
            var isOwner = await _context.Venues.AnyAsync(v => v.OwnerId == userId, cancellationToken);
            if (isOwner)
            {
                query = query.Where(b => b.Venue != null && b.Venue.OwnerId == userId);
            }
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        var list = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new VenueBookingSummaryDto(
                b.Id,
                b.BookingCode,
                b.VenueId,
                b.Venue != null ? b.Venue.Name : "N/A",
                b.Venue != null ? b.Venue.Address : "N/A",
                b.Venue != null ? b.Venue.City : "N/A",
                b.OrganizerId,
                b.Organizer != null ? b.Organizer.FullName : "N/A",
                b.StartTime,
                b.EndTime,
                b.PriceQuote,
                b.Note,
                b.RejectionReason,
                b.Status,
                b.EventId,
                b.Event != null ? b.Event.Title : null
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<VenueBookingSummaryDto>>.Ok(list, "Lấy danh sách yêu cầu thuê địa điểm thành công.");
    }

    public async Task<ApiResponse<SlotAvailabilityResultDto>> Handle(CheckSlotAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var startTimeUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        var endTimeUtc = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);

        var conflict = await _context.VenueBookings
            .AsNoTracking()
            .Where(b => b.VenueId == request.VenueId &&
                        (b.Status == VenueBookingStatus.Confirmed || b.Status == VenueBookingStatus.Pending) &&
                        startTimeUtc < b.EndTime &&
                        endTimeUtc > b.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (conflict != null)
        {
            return ApiResponse<SlotAvailabilityResultDto>.Ok(new SlotAvailabilityResultDto(
                IsAvailable: false,
                Message: $"Khung giờ này đã bị trùng với đơn đặt lịch {conflict.BookingCode} ({conflict.StartTime:dd/MM/yyyy HH:mm} - {conflict.EndTime:HH:mm}).",
                StartTime: request.StartTime,
                EndTime: request.EndTime,
                ConflictingBookingCode: conflict.BookingCode
            ));
        }

        return ApiResponse<SlotAvailabilityResultDto>.Ok(new SlotAvailabilityResultDto(
            IsAvailable: true,
            Message: "Khung giờ hoàn toàn trống và sẵn sàng để thuê.",
            StartTime: request.StartTime,
            EndTime: request.EndTime
        ));
    }

    public async Task<ApiResponse<VenueBookingSummaryDto>> Handle(RequestVenueBookingCommand request, CancellationToken cancellationToken)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId, cancellationToken);
        if (venue == null)
            throw new NotFoundException("Địa điểm", request.VenueId);

        if (venue.Status != VenueStatus.Approved)
            throw new AppException("Địa điểm này chưa được phê duyệt hoặc đang tạm dừng cho thuê.", 400);

        var organizerId = _currentUser.UserId;
        if (!organizerId.HasValue)
        {
            var defaultUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Organizer || u.Role == UserRole.Admin, cancellationToken);
            organizerId = defaultUser?.Id ?? Guid.NewGuid();
        }

        var startTimeUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        var endTimeUtc = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);

        // Anti-collision check: prevent overlapping confirmed/pending bookings
        var conflict = await _context.VenueBookings
            .AnyAsync(b => b.VenueId == request.VenueId &&
                           (b.Status == VenueBookingStatus.Confirmed || b.Status == VenueBookingStatus.Pending) &&
                           startTimeUtc < b.EndTime &&
                           endTimeUtc > b.StartTime,
                      cancellationToken);

        if (conflict)
        {
            throw new AppException("Rất tiếc! Khung giờ bạn chọn đã có đơn đặt giữ chỗ hoặc đã được xác nhận cho show khác.", 409);
        }

        var bookingCode = $"VBK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var booking = new VenueBooking
        {
            VenueId = request.VenueId,
            OrganizerId = organizerId.Value,
            BookingCode = bookingCode,
            StartTime = startTimeUtc,
            EndTime = endTimeUtc,
            PriceQuote = request.PriceQuote,
            Note = request.Note?.Trim(),
            Status = VenueBookingStatus.Pending
        };

        _context.VenueBookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetBookingSummaryAsync(booking.Id, cancellationToken);
    }

    public async Task<ApiResponse<VenueBookingSummaryDto>> Handle(RespondVenueBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.VenueBookings
            .Include(b => b.Venue)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            throw new NotFoundException("Yêu cầu thuê địa điểm", request.BookingId);

        if (booking.Status != VenueBookingStatus.Pending)
            throw new AppException("Yêu cầu này đã được xử lý trước đó.", 400);

        if (request.Accept)
        {
            booking.Status = VenueBookingStatus.Confirmed;
            booking.RejectionReason = null;

            // Exclusive slot resolution: Auto-reject any other pending bookings that collide with this confirmed window
            var overlappingPending = await _context.VenueBookings
                .Where(b => b.Id != booking.Id &&
                            b.VenueId == booking.VenueId &&
                            b.Status == VenueBookingStatus.Pending &&
                            booking.StartTime < b.EndTime &&
                            booking.EndTime > b.StartTime)
                .ToListAsync(cancellationToken);

            foreach (var other in overlappingPending)
            {
                other.Status = VenueBookingStatus.Rejected;
                other.RejectionReason = $"Tự động từ chối do địa điểm đã xác nhận cho đơn {booking.BookingCode}.";
            }
        }
        else
        {
            booking.Status = VenueBookingStatus.Rejected;
            booking.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? "Chủ địa điểm từ chối yêu cầu." : request.Reason.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetBookingSummaryAsync(booking.Id, cancellationToken);
    }

    private async Task<ApiResponse<VenueBookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var b = await _context.VenueBookings
            .Include(x => x.Venue)
            .Include(x => x.Organizer)
            .Include(x => x.Event)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);

        if (b == null)
            throw new NotFoundException("Yêu cầu thuê địa điểm", bookingId);

        var dto = new VenueBookingSummaryDto(
            b.Id,
            b.BookingCode,
            b.VenueId,
            b.Venue != null ? b.Venue.Name : "N/A",
            b.Venue != null ? b.Venue.Address : "N/A",
            b.Venue != null ? b.Venue.City : "N/A",
            b.OrganizerId,
            b.Organizer != null ? b.Organizer.FullName : "N/A",
            b.StartTime,
            b.EndTime,
            b.PriceQuote,
            b.Note,
            b.RejectionReason,
            b.Status,
            b.EventId,
            b.Event != null ? b.Event.Title : null
        );

        return ApiResponse<VenueBookingSummaryDto>.Ok(dto, "Xử lý yêu cầu thuê địa điểm thành công.");
    }
}
