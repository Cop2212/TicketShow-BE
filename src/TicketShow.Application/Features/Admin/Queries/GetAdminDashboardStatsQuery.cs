using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Admin.Queries;

public record GetAdminDashboardStatsQuery : IRequest<ApiResponse<AdminDashboardStatsDto>>;

public record AdminDashboardStatsDto(
    int TotalEvents,
    int PublishedEvents,
    int TotalSeats,
    int TotalTicketsSold,
    decimal TotalRevenue,
    double OverallCheckInRate,
    int TotalCheckedInTickets,
    List<AdminRecentEventItemDto> RecentEvents,
    List<AdminRecentOrderItemDto> RecentOrders
);

public record AdminRecentEventItemDto(
    Guid Id,
    string Title,
    string Slug,
    string VenueName,
    DateTime StartTime,
    int Status,
    int TotalSeats,
    int SoldSeats,
    decimal Revenue
);

public record AdminRecentOrderItemDto(
    Guid OrderId,
    string OrderCode,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string PaymentStatus,
    DateTime CreatedAt,
    int TicketCount
);

public class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, ApiResponse<AdminDashboardStatsDto>>
{
    private readonly IAppDbContext _context;

    public GetAdminDashboardStatsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<AdminDashboardStatsDto>> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        // 1. Event Counts
        var totalEvents = await _context.Events.CountAsync(cancellationToken);
        var publishedEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Published, cancellationToken);

        // 2. Total Seats & Tickets Sold
        var totalSeats = await _context.Seats.CountAsync(cancellationToken);
        var totalTicketsSold = await _context.Tickets.CountAsync(t => t.Status != TicketStatus.Cancelled, cancellationToken);
        var totalCheckedIn = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.CheckedIn, cancellationToken);

        var checkInRate = totalTicketsSold > 0
            ? Math.Round((double)totalCheckedIn / totalTicketsSold * 100, 1)
            : 0;

        // 3. Revenue from Paid orders
        var totalRevenue = await _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        // 4. Recent Events with stats
        var recentEvents = await _context.Events
            .Include(e => e.Seats)
            .Include(e => e.Tickets)
                .ThenInclude(t => t.Order)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new AdminRecentEventItemDto(
                e.Id,
                e.Title,
                e.Slug,
                e.VenueName,
                e.StartTime,
                (int)e.Status,
                e.Seats.Count,
                e.Seats.Count(s => s.Status == SeatStatus.Sold),
                e.Tickets
                    .Where(t => t.Order != null && t.Order.PaymentStatus == PaymentStatus.Paid)
                    .Sum(t => t.TicketTier != null ? t.TicketTier.Price : 0)
            ))
            .ToListAsync(cancellationToken);

        // 5. Recent Orders
        var recentOrders = await _context.Orders
            .Include(o => o.Tickets)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new AdminRecentOrderItemDto(
                o.Id,
                o.OrderCode,
                o.CustomerName,
                o.CustomerEmail,
                o.TotalAmount,
                o.PaymentStatus.ToString(),
                o.CreatedAt,
                o.Tickets.Count
            ))
            .ToListAsync(cancellationToken);

        var stats = new AdminDashboardStatsDto(
            TotalEvents: totalEvents,
            PublishedEvents: publishedEvents,
            TotalSeats: totalSeats,
            TotalTicketsSold: totalTicketsSold,
            TotalRevenue: totalRevenue,
            OverallCheckInRate: checkInRate,
            TotalCheckedInTickets: totalCheckedIn,
            RecentEvents: recentEvents,
            RecentOrders: recentOrders
        );

        return ApiResponse<AdminDashboardStatsDto>.Ok(stats, "Lấy thống kê quản trị thành công.");
    }
}
