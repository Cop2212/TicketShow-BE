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
    IRequestHandler<CreateEventCommand, ApiResponse<EventDetailDto>>
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
                e.TicketTiers.Select(t => new TicketTierDto(t.Id, t.Name, t.Price, t.TotalQuantity, t.SoldQuantity, t.ColorHex)).ToList()
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
            ev.TicketTiers.Select(t => new TicketTierDto(t.Id, t.Name, t.Price, t.TotalQuantity, t.SoldQuantity, t.ColorHex)).ToList(),
            seatDtos
        );

        return ApiResponse<EventDetailDto>.Ok(detail, "Lấy chi tiết sự kiện thành công.");
    }

    public async Task<ApiResponse<EventDetailDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var organizerId = _currentUser.UserId;
        if (!organizerId.HasValue)
        {
            // Fallback to first user in system if running in dev without token
            var defaultUser = await _context.Users.FirstOrDefaultAsync(cancellationToken);
            organizerId = defaultUser?.Id ?? Guid.NewGuid();
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
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description,
            BannerUrl = request.BannerUrl,
            VenueName = request.VenueName.Trim(),
            VenueAddress = request.VenueAddress?.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SalesStartTime = request.SalesStartTime,
            SalesEndTime = request.SalesEndTime,
            Status = EventStatus.Published
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
                ColorHex = string.IsNullOrWhiteSpace(tierInput.ColorHex) ? "#8b5cf6" : tierInput.ColorHex
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Generate Seat map if requested
        if (request.GenerateRowsCount.HasValue && request.GenerateRowsCount > 0 &&
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
                // Front 2 rows can be VIP, rest Standard
                var assignedTier = (r < 2 && tiers.Count > 1) ? primaryTier : secondaryTier;

                for (int s = 1; s <= seatsPerRow; s++)
                {
                    var seatNumber = s.ToString("D2");
                    _context.Seats.Add(new Seat
                    {
                        EventId = ev.Id,
                        TicketTierId = assignedTier.Id,
                        RowName = rowName,
                        SeatNumber = seatNumber,
                        Status = SeatStatus.Available
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return await Handle(new GetEventDetailQuery(ev.Id.ToString()), cancellationToken);
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
        return string.IsNullOrWhiteSpace(str) ? $"event-{Guid.NewGuid():N}" : str;
    }
}
