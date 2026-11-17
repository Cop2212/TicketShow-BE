using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Features.Orders.Commands;
using TicketShow.Domain.Enums;
using TicketShow.Infrastructure.Data;

namespace TicketShow.Infrastructure.BackgroundServices;

/// <summary>
/// Worker chạy ngầm định kỳ quét các đơn đặt vé hết hạn thanh toán (5 phút),
/// tự động giải phóng ghế ngồi, hoàn trả số lượng vé đứng và gỡ khóa phân tán trên Redis.
/// </summary>
public class SeatHoldExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatHoldExpirationWorker> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(20)); // Chu kỳ quét mỗi 20 giây

    public SeatHoldExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<SeatHoldExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SeatHoldExpirationWorker đã khởi động thành công (Quét định kỳ mỗi 20s).");

        while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepExpiredSeatHoldsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét giải phóng ghế hết hạn.");
            }
        }
    }

    private async Task SweepExpiredSeatHoldsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lockService = scope.ServiceProvider.GetRequiredService<ILockService>();

        var now = DateTime.UtcNow;

        // Tìm tối đa 50 đơn hàng Pending đã quá hạn
        var expiredOrders = await context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Pending && o.ExpiresAt <= now)
            .OrderBy(o => o.ExpiresAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (!expiredOrders.Any())
        {
            return;
        }

        _logger.LogInformation("Phát hiện {Count} đơn hàng giữ chỗ đã hết hạn cần giải phóng.", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                order.PaymentStatus = PaymentStatus.Failed;

                // 1. Giải phóng ghế ngồi (Seated)
                var lockedSeats = await context.Seats
                    .Where(s => s.LockedBySessionId == order.OrderCode && s.Status == SeatStatus.Locked)
                    .ToListAsync(cancellationToken);

                foreach (var seat in lockedSeats)
                {
                    seat.Status = SeatStatus.Available;
                    seat.LockedUntil = null;
                    seat.LockedBySessionId = null;
                    await lockService.ReleaseSeatLockAsync(seat.EventId, seat.Id, order.OrderCode);
                }

                // 2. Hoàn lại số lượng vé đứng (Standing Holds)
                if (!string.IsNullOrWhiteSpace(order.ReservedItemsJson))
                {
                    try
                    {
                        var standingHolds = JsonSerializer.Deserialize<List<StandingHoldSnapshot>>(order.ReservedItemsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (standingHolds != null && standingHolds.Count > 0)
                        {
                            var tierIds = standingHolds.Select(h => h.TierId).Distinct().ToList();
                            var tiers = await context.TicketTiers
                                .Where(t => tierIds.Contains(t.Id))
                                .ToListAsync(cancellationToken);

                            foreach (var hold in standingHolds)
                            {
                                var tier = tiers.FirstOrDefault(t => t.Id == hold.TierId);
                                if (tier != null)
                                {
                                    tier.HoldQuantity = Math.Max(0, tier.HoldQuantity - hold.Quantity);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi deserialize
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Đã tự động giải phóng đơn hàng hết hạn: {OrderCode} ({SeatCount} ghế ngồi)", 
                    order.OrderCode, lockedSeats.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi xử lý giải phóng đơn hàng hết hạn: {OrderCode}", order.OrderCode);
            }
        }
    }
}
