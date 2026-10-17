using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Domain.Enums;

namespace TicketShow.Infrastructure.Services;

public class EventSalesAutoCloserWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventSalesAutoCloserWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public EventSalesAutoCloserWorker(IServiceProvider serviceProvider, ILogger<EventSalesAutoCloserWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventSalesAutoCloserWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAutoCloseAndExpiredLocksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing EventSalesAutoCloserWorker.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("EventSalesAutoCloserWorker is stopping.");
    }

    private async Task ProcessAutoCloseAndExpiredLocksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var lockService = scope.ServiceProvider.GetRequiredService<ILockService>();

        var now = DateTime.UtcNow;

        // 1. Auto-close events 1 day prior to show start OR when sales_end_time reached
        var eventsToClose = await context.Events
            .Where(e => e.Status == EventStatus.Published &&
                       ((e.SalesEndTime != DateTime.MinValue && e.SalesEndTime <= now) ||
                        (e.StartTime <= now.AddDays(1))))
            .ToListAsync(cancellationToken);

        if (eventsToClose.Count > 0)
        {
            foreach (var ev in eventsToClose)
            {
                ev.Status = EventStatus.Closed;
                _logger.LogInformation("Auto-closed ticket sales for event {EventTitle} (Id: {EventId})", ev.Title, ev.Id);
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Housekeeping: Release expired locked seats where customer abandoned reservation
        var expiredLockedSeats = await context.Seats
            .Where(s => s.Status == SeatStatus.Locked && s.LockedUntil.HasValue && s.LockedUntil.Value < now)
            .ToListAsync(cancellationToken);

        if (expiredLockedSeats.Count > 0)
        {
            foreach (var seat in expiredLockedSeats)
            {
                var owner = seat.LockedBySessionId ?? "SYSTEM_HOUSEKEEPING";
                await lockService.ReleaseSeatLockAsync(seat.EventId, seat.Id, owner);
                seat.Status = SeatStatus.Available;
                seat.LockedUntil = null;
                seat.LockedBySessionId = null;
            }
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} abandoned seat locks and returned them to available.", expiredLockedSeats.Count);
        }
    }
}
