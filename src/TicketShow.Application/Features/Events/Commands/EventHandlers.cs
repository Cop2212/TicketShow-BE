using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Events.DTOs;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Events.Commands;

public record GetEventsQuery(string? Search, EventStatus? Status) : IRequest<ApiResponse<List<EventSummaryDto>>>;

public record GetEventDetailQuery(string IdOrSlug) : IRequest<ApiResponse<EventDetailDto>>;

public class EventHandlers :
    IRequestHandler<GetEventsQuery, ApiResponse<List<EventSummaryDto>>>,
    IRequestHandler<GetEventDetailQuery, ApiResponse<EventDetailDto>>,
    IRequestHandler<CreateEventCommand, ApiResponse<EventDetailDto>>,
    IRequestHandler<CreateEventFromBookingCommand, ApiResponse<EventDetailDto>>,
    IRequestHandler<SubmitEventForApprovalCommand, ApiResponse<bool>>,
    IRequestHandler<ApproveEventCommand, ApiResponse<bool>>,
    IRequestHandler<CloseEventSalesCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILockService _lockService;

    public EventHandlers(IAppDbContext context, ICurrentUserService currentUser, ILockService lockService)
    {
        _context = context;
        _currentUser = currentUser;
        _lockService = lockService;
    }

    public async Task<ApiResponse<List<EventSummaryDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Events
            .Include(e => e.TicketTiers)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(search) || e.VenueName.ToLower().Contains(search));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        var events = await query
            .OrderBy(e => e.StartTime)
            .Select(e => new EventSummaryDto(
                e.Id,
                e.Title,
                e.Slug,
                e.Description,
                e.BannerUrl,
                e.VenueName,
                e.VenueAddress,
                e.StartTime,
                e.EndTime,
                e.SalesStartTime,
                e.SalesEndTime,
                e.Status,
                e.TicketTiers.Select(t => new TicketTierDto(t.Id, t.Name, t.Price, t.TotalQuantity, t.SoldQuantity, t.ColorHex, t.IsStanding)).ToList(),
                e.VenueId,
                e.VenueBookingId,
                e.IsSeated,
                e.TicketingMode,
                e.OrganizerId,
                e.Organizer != null ? e.Organizer.FullName : "N/A"
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<EventSummaryDto>>.Ok(events, "Lấy danh sách sự kiện thành công.");
    }

    public async Task<ApiResponse<EventDetailDto>> Handle(GetEventDetailQuery request, CancellationToken cancellationToken)
    {
        var isGuid = Guid.TryParse(request.IdOrSlug, out var eventId);

        var ev = await _context.Events
            .Include(e => e.TicketTiers)
            .Include(e => e.Seats)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => (isGuid && e.Id == eventId) || e.Slug == request.IdOrSlug, cancellationToken);

        if (ev == null)
        {
            throw new NotFoundException("Sự kiện", request.IdOrSlug);
        }

        var tierMap = ev.TicketTiers.ToDictionary(t => t.Id);

        var seatDtos = new List<SeatDto>();
        foreach (var seat in ev.Seats.OrderBy(s => s.RowName).ThenBy(s => s.SeatNumber))
        {
            var isLocked = seat.Status == SeatStatus.Locked || await _lockService.IsSeatLockedAsync(ev.Id, seat.Id);
            var statusStr = seat.Status == SeatStatus.Sold ? "Sold" : (isLocked ? "Locked" : "Available");
            var tier = tierMap.TryGetValue(seat.TicketTierId, out var t) ? t : null;

            seatDtos.Add(new SeatDto(
                seat.Id,
                seat.TicketTierId,
                tier?.Name ?? "General",
                seat.RowName,
                seat.SeatNumber,
                statusStr,
                tier?.Price ?? 0,
                tier?.ColorHex ?? "#8b5cf6"
            ));
        }

        var detail = new EventDetailDto(
            ev.Id,
            ev.Title,
            ev.Slug,
            ev.Description,
            ev.BannerUrl,
            ev.VenueName,
            ev.VenueAddress,
            ev.StartTime,
            ev.EndTime,
            ev.SalesStartTime,
            ev.SalesEndTime,
            ev.Status,
            ev.TicketTiers.Select(t => new TicketTierDto(t.Id, t.Name, t.Price, t.TotalQuantity, t.SoldQuantity, t.ColorHex, t.IsStanding)).ToList(),
            seatDtos,
            ev.VenueId,
            ev.VenueBookingId,
            ev.IsSeated,
            ev.TicketingMode,
            ev.OrganizerId,
            ev.Organizer != null ? ev.Organizer.FullName : "N/A"
        );

        return ApiResponse<EventDetailDto>.Ok(detail, "Lấy chi tiết sự kiện thành công.");
    }

    public async Task<ApiResponse<EventDetailDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var organizerId = _currentUser.UserId;
        if (!organizerId.HasValue)
        {
            var defaultUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Organizer || u.Role == UserRole.Admin, cancellationToken);
            organizerId = defaultUser?.Id ?? Guid.NewGuid();
        }
        // Ceiling Guard & Venue Type Compatibility Validation
        if (request.VenueId.HasValue)
        {
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId.Value, cancellationToken);
            if (venue != null)
            {
                if (venue.VenueType == VenueType.SeatedOnly && request.TicketingMode != TicketingMode.SeatedOnly)
                {
                    throw new AppException($"Địa điểm '{venue.Name}' là cơ sở thuần ghế ngồi, không hỗ trợ tổ chức show có vé đứng.", 400);
                }
                if (venue.VenueType == VenueType.StandingOnly && request.TicketingMode != TicketingMode.StandingOnly)
                {
                    throw new AppException($"Địa điểm '{venue.Name}' là khu vực thuần vé đứng ngoài trời, không hỗ trợ tổ chức show có ghế ngồi.", 400);
                }

                int totalStandingRequested = request.TicketTiers
                    .Where(t => t.IsStanding || request.TicketingMode == TicketingMode.StandingOnly)
                    .Sum(t => t.TotalQuantity);

                if (venue.StandingCapacity > 0 && totalStandingRequested > venue.StandingCapacity)
                {
                    throw new AppException($"Tổng số vé đứng phát hành ({totalStandingRequested:N0} vé) vượt quá hạn mức tối đa cho phép của địa điểm ({venue.StandingCapacity:N0} vé).", 400);
                }

                int totalSeatedRequested = request.TicketTiers
                    .Where(t => !t.IsStanding && request.TicketingMode != TicketingMode.StandingOnly)
                    .Sum(t => t.TotalQuantity);

                if (venue.SeatedCapacity > 0 && totalSeatedRequested > venue.SeatedCapacity)
                {
                    throw new AppException($"Tổng số ghế phát hành ({totalSeatedRequested:N0} ghế) vượt quá số ghế thiết kế của địa điểm ({venue.SeatedCapacity:N0} ghế).", 400);
                }
            }
        }

        var baseSlug = GenerateSlug(request.Title);
        var slug = baseSlug;
        var count = 1;
        while (await _context.Events.AnyAsync(e => e.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{count++}";
        }

        var ev = new Event
        {
            OrganizerId = organizerId.Value,
            VenueId = request.VenueId,
            VenueBookingId = request.VenueBookingId,
            IsSeated = request.TicketingMode != TicketingMode.StandingOnly,
            TicketingMode = request.TicketingMode,
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description,
            BannerUrl = request.BannerUrl,
            VenueName = request.VenueName.Trim(),
            VenueAddress = request.VenueAddress?.Trim(),
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc),
            SalesStartTime = DateTime.SpecifyKind(request.SalesStartTime, DateTimeKind.Utc),
            SalesEndTime = DateTime.SpecifyKind(request.SalesEndTime, DateTimeKind.Utc),
            Status = request.InitialStatus
        };

        _context.Events.Add(ev);

        // Add Ticket Tiers
        foreach (var tierInput in request.TicketTiers)
        {
            ev.TicketTiers.Add(new TicketTier
            {
                EventId = ev.Id,
                Name = tierInput.Name,
                Price = tierInput.Price,
                TotalQuantity = tierInput.TotalQuantity,
                SoldQuantity = 0,
                ColorHex = string.IsNullOrWhiteSpace(tierInput.ColorHex) ? "#8b5cf6" : tierInput.ColorHex,
                IsStanding = tierInput.IsStanding || request.TicketingMode == TicketingMode.StandingOnly
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Generate Seat map ONLY if not StandingOnly
        if (request.TicketingMode != TicketingMode.StandingOnly && request.IsSeated)
        {
            bool layoutGenerated = false;
            // Try inheriting exact seat matrix from Venue if available
            if (request.VenueId.HasValue)
            {
                var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId.Value, cancellationToken);
                if (venue != null && !string.IsNullOrWhiteSpace(venue.MasterLayoutJson))
                {
                    try
                    {
                        var layout = JsonSerializer.Deserialize<VenueLayoutModel>(venue.MasterLayoutJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (layout?.Seats != null && layout.Seats.Count > 0)
                        {
                            var validSeats = layout.Seats.Where(s => !s.Zone.Equals("Space", StringComparison.OrdinalIgnoreCase)).ToList();
                            var tierMap = ev.TicketTiers.ToDictionary(t => t.Name.ToLower().Trim(), t => t);

                            foreach (var s in validSeats)
                            {
                                tierMap.TryGetValue(s.Zone.ToLower().Trim(), out var assignedTier);
                                var targetTier = assignedTier ?? ev.TicketTiers.First();

                                _context.Seats.Add(new Seat
                                {
                                    EventId = ev.Id,
                                    TicketTierId = targetTier.Id,
                                    RowName = s.Row,
                                    SeatNumber = s.Number,
                                    Status = SeatStatus.Available
                                });
                            }

                            await _context.SaveChangesAsync(cancellationToken);
                            layoutGenerated = true;
                        }
                    }
                    catch
                    {
                        // fallback to standard generation
                    }
                }
            }

            if (!layoutGenerated && request.GenerateRowsCount.HasValue && request.GenerateRowsCount > 0 &&
                request.GenerateSeatsPerRow.HasValue && request.GenerateSeatsPerRow > 0)
            {
                var tiers = ev.TicketTiers.ToList();
                var primaryTier = tiers.First();
                var secondaryTier = tiers.Count > 1 ? tiers[1] : primaryTier;

                var rows = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var rowCount = Math.Min(request.GenerateRowsCount.Value, 10);
                var seatsPerRow = Math.Min(request.GenerateSeatsPerRow.Value, 20);

                for (int r = 0; r < rowCount; r++)
                {
                    var rowName = rows[r].ToString();
                    var assignedTier = (r < 2 && tiers.Count > 1) ? primaryTier : secondaryTier;

                    for (int s = 1; s <= seatsPerRow; s++)
                    {
                        _context.Seats.Add(new Seat
                        {
                            EventId = ev.Id,
                            TicketTierId = assignedTier.Id,
                            RowName = rowName,
                            SeatNumber = s.ToString("D2"),
                            Status = SeatStatus.Available
                        });
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        return await Handle(new GetEventDetailQuery(ev.Id.ToString()), cancellationToken);
    }

    public async Task<ApiResponse<EventDetailDto>> Handle(CreateEventFromBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.VenueBookings
            .Include(b => b.Venue)
            .FirstOrDefaultAsync(b => b.Id == request.VenueBookingId, cancellationToken);

        if (booking == null)
            throw new NotFoundException("Đơn đặt thuê địa điểm", request.VenueBookingId);

        if (booking.Status != VenueBookingStatus.Confirmed)
            throw new AppException("Chỉ có thể tạo show diễn từ đơn thuê địa điểm đã được Chủ địa điểm XÁC NHẬN (Confirmed).", 400);

        if (booking.EventId.HasValue)
            throw new AppException("Đơn đặt địa điểm này đã được liên kết với một show diễn khác.", 400);

        var venue = booking.Venue ?? throw new AppException("Không tìm thấy thông tin địa điểm liên kết.", 404);

        var baseSlug = GenerateSlug(request.Title);
        var slug = baseSlug;
        var count = 1;
        while (await _context.Events.AnyAsync(e => e.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{count++}";
        }

        var ev = new Event
        {
            OrganizerId = booking.OrganizerId,
            VenueId = venue.Id,
            VenueBookingId = booking.Id,
            IsSeated = request.TicketingMode != TicketingMode.StandingOnly,
            TicketingMode = request.TicketingMode,
            Slug = slug,
            Description = request.Description?.Trim(),
            BannerUrl = request.BannerUrl?.Trim(),
            VenueName = venue.Name,
            VenueAddress = $"{venue.Address}, {venue.City}",
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            SalesStartTime = DateTime.SpecifyKind(request.SalesStartTime, DateTimeKind.Utc),
            SalesEndTime = DateTime.SpecifyKind(request.SalesEndTime, DateTimeKind.Utc),
            Status = request.SubmitForApproval ? EventStatus.PendingApproval : EventStatus.Draft
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync(cancellationToken);

        // Link event back to booking
        booking.EventId = ev.Id;

        // Parse MasterLayoutJson from Venue and generate TicketTiers & Seats
        try
        {
            var layout = JsonSerializer.Deserialize<VenueLayoutModel>(venue.MasterLayoutJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (layout != null && layout.Seats != null && layout.Seats.Count > 0)
            {
                var validSeats = layout.Seats.Where(s => !s.Zone.Equals("Space", StringComparison.OrdinalIgnoreCase)).ToList();
                var pricingMap = request.ZonePricings.ToDictionary(z => z.ZoneName.ToLower().Trim(), z => z);
                var groups = validSeats.GroupBy(s => s.Zone).ToList();

                foreach (var group in groups)
                {
                    pricingMap.TryGetValue(group.Key.ToLower().Trim(), out var customPricing);
                    var price = customPricing?.Price ?? 500000m;
                    var color = !string.IsNullOrWhiteSpace(customPricing?.ColorHex) ? customPricing.ColorHex : "#8b5cf6";

                    var tier = new TicketTier
                    {
                        EventId = ev.Id,
                        Name = group.Key,
                        Price = price,
                        TotalQuantity = group.Count(),
                        SoldQuantity = 0,
                        ColorHex = color
                    };

                    _context.TicketTiers.Add(tier);
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var s in group)
                    {
                        _context.Seats.Add(new Seat
                        {
                            EventId = ev.Id,
                            TicketTierId = tier.Id,
                            RowName = s.Row,
                            SeatNumber = s.Number,
                            Status = SeatStatus.Available
                        });
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            else if (layout != null && layout.Zones != null && layout.Zones.Count > 0)
            {
                var seatsPerRow = layout.SeatsPerRow > 0 ? layout.SeatsPerRow : 10;
                var pricingMap = request.ZonePricings.ToDictionary(z => z.ZoneName.ToLower().Trim(), z => z);

                foreach (var zone in layout.Zones)
                {
                    pricingMap.TryGetValue(zone.Name.ToLower().Trim(), out var customPricing);
                    var price = customPricing?.Price ?? 500000m;
                    var color = !string.IsNullOrWhiteSpace(customPricing?.ColorHex) ? customPricing.ColorHex : (!string.IsNullOrWhiteSpace(zone.ColorHex) ? zone.ColorHex : "#8b5cf6");
                    var totalZoneSeats = zone.Rows.Count * seatsPerRow;

                    var tier = new TicketTier
                    {
                        EventId = ev.Id,
                        Name = zone.Name,
                        Price = price,
                        TotalQuantity = totalZoneSeats,
                        SoldQuantity = 0,
                        ColorHex = color
                    };

                    _context.TicketTiers.Add(tier);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Clone Seats for each row in this zone
                    foreach (var rowName in zone.Rows)
                    {
                        for (int s = 1; s <= seatsPerRow; s++)
                        {
                            _context.Seats.Add(new Seat
                            {
                                EventId = ev.Id,
                                TicketTierId = tier.Id,
                                RowName = rowName,
                                SeatNumber = s.ToString("D2"),
                                Status = SeatStatus.Available
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch
        {
            // Fallback default tier if json layout parsing fails
            var fallbackTier = new TicketTier
            {
                EventId = ev.Id,
                Name = "General Admission",
                Price = 500000,
                TotalQuantity = venue.TotalCapacity,
                SoldQuantity = 0,
                ColorHex = "#8b5cf6"
            };
            _context.TicketTiers.Add(fallbackTier);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await Handle(new GetEventDetailQuery(ev.Id.ToString()), cancellationToken);
    }

    public async Task<ApiResponse<bool>> Handle(SubmitEventForApprovalCommand request, CancellationToken cancellationToken)
    {
        var ev = await _context.Events
            .Include(e => e.TicketTiers)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev == null)
            throw new NotFoundException("Sự kiện", request.EventId);

        if (ev.TicketTiers.Count == 0)
            throw new AppException("Sự kiện phải có ít nhất 1 hạng vé trước khi gửi phê duyệt.", 400);

        ev.Status = EventStatus.PendingApproval;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã gửi yêu cầu phê duyệt show diễn lên Ban quản trị sàn.");
    }

    public async Task<ApiResponse<bool>> Handle(ApproveEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Sự kiện", request.EventId);

        ev.Status = request.Approved ? EventStatus.Published : EventStatus.Draft;
        await _context.SaveChangesAsync(cancellationToken);

        var msg = request.Approved
            ? "Phê duyệt show diễn thành công. Show đã chính thức xuất hiện trên Trang chủ để mở bán vé!"
            : $"Đã từ chối duyệt show diễn. Lý do: {request.Reason ?? "Cần bổ sung thông tin"}";

        return ApiResponse<bool>.Ok(true, msg);
    }

    public async Task<ApiResponse<bool>> Handle(CloseEventSalesCommand request, CancellationToken cancellationToken)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Sự kiện", request.EventId);

        ev.Status = EventStatus.Closed;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã đóng cổng bán vé cho sự kiện này.");
    }

    private static string GenerateSlug(string title)
    {
        var str = title.ToLowerInvariant();
        str = Regex.Replace(str, @"[áàảãạâấầẩẫậăắằẳẵặ]", "a");
        str = Regex.Replace(str, @"[éèẻẽẹêếềểễệ]", "e");
        str = Regex.Replace(str, @"[íìỉĩị]", "i");
        str = Regex.Replace(str, @"[óòỏõọôốồổỗộơớờởỡợ]", "o");
        str = Regex.Replace(str, @"[úùủũụưứừửữự]", "u");
        str = Regex.Replace(str, @"[ýỳỷỹỵ]", "y");
        str = Regex.Replace(str, @"[đ]", "d");
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = Regex.Replace(str, @"\s+", " ").Trim();
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }

    private class VenueLayoutModel
    {
        public List<string> Rows { get; set; } = new();
        public int SeatsPerRow { get; set; }
        public List<VenueZoneModel> Zones { get; set; } = new();
        public List<VenueSeatItemModel> Seats { get; set; } = new();
    }

    private class VenueSeatItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
    }

    private class VenueZoneModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Rows { get; set; } = new();
        public string? ColorHex { get; set; }
    }
}
