using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Orders.Commands;

public record CancelReservationCommand(string OrderCode) : IRequest<ApiResponse<bool>>;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;
    private readonly ILockService _lockService;

    public CancelReservationCommandHandler(IAppDbContext context, ILockService lockService)
    {
        _context = context;
        _lockService = lockService;
    }

    public async Task<ApiResponse<bool>> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderCode == request.OrderCode, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Đơn hàng", request.OrderCode);
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            throw new AppException("Đơn hàng này đã được thanh toán thành công, không thể hủy giữ chỗ.", 400);
        }

        if (order.PaymentStatus == PaymentStatus.Failed)
        {
            return ApiResponse<bool>.Ok(true, "Đơn hàng đã được giải phóng trước đó.");
        }

        order.PaymentStatus = PaymentStatus.Failed;

        // 1. Giải phóng ghế ngồi (Seated)
        var lockedSeats = await _context.Seats
            .Where(s => s.LockedBySessionId == order.OrderCode && s.Status == SeatStatus.Locked)
            .ToListAsync(cancellationToken);

        foreach (var seat in lockedSeats)
        {
            seat.Status = SeatStatus.Available;
            seat.LockedUntil = null;
            seat.LockedBySessionId = null;
            await _lockService.ReleaseSeatLockAsync(seat.EventId, seat.Id, order.OrderCode);
        }

        // 2. Hoàn trả số lượng vé đứng đã giữ chỗ (Standing / General Admission)
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
                    var tiers = await _context.TicketTiers
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
                // Ignore parse errors on cancel
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã hủy giữ chỗ thành công và giải phóng toàn bộ vị trí cho người khác.");
    }
}
