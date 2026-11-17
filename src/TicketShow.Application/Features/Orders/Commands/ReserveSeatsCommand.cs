using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Orders.Commands;

public record StandingTicketOrderInput(Guid TicketTierId, int Quantity);

public record ReserveSeatsCommand : IRequest<ApiResponse<ReservationResultDto>>
{
    public Guid EventId { get; init; }
    public List<Guid> SeatIds { get; init; } = new();
    public List<StandingTicketOrderInput> StandingTickets { get; init; } = new();
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
}

public record ReservationResultDto(
    Guid OrderId,
    string OrderCode,
    decimal TotalAmount,
    DateTime ExpiresAt,
    int RemainingSeconds,
    List<ReservedSeatItemDto> Seats
);

public record ReservedSeatItemDto(
    Guid SeatId,
    string Label, // e.g. A01 or "Fanzone (Vé đứng x2)"
    string TierName,
    decimal Price
);

public class ReserveSeatsCommandValidator : AbstractValidator<ReserveSeatsCommand>
{
    public ReserveSeatsCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x)
            .Must(x => (x.SeatIds != null && x.SeatIds.Count > 0) || (x.StandingTickets != null && x.StandingTickets.Count > 0))
            .WithMessage("Vui lòng chọn ít nhất 1 vị trí ghế ngồi hoặc 1 vé đứng.");

        RuleFor(x => (x.SeatIds != null ? x.SeatIds.Count : 0) + (x.StandingTickets != null ? x.StandingTickets.Sum(s => s.Quantity) : 0))
            .InclusiveBetween(1, 10)
            .WithMessage("Mỗi lần đặt chỉ được chọn tối đa từ 1 đến 10 vé để chống đầu cơ vé.");

        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(150);
    }
}

public class ReserveSeatsCommandHandler : IRequestHandler<ReserveSeatsCommand, ApiResponse<ReservationResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILockService _lockService;

    public ReserveSeatsCommandHandler(IAppDbContext context, ICurrentUserService currentUser, ILockService lockService)
    {
        _context = context;
        _currentUser = currentUser;
        _lockService = lockService;
    }

    public async Task<ApiResponse<ReservationResultDto>> Handle(ReserveSeatsCommand request, CancellationToken cancellationToken)
    {
        var ev = await _context.Events
            .Include(e => e.Seats)
                .ThenInclude(s => s.TicketTier)
            .Include(e => e.TicketTiers)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev == null)
            throw new NotFoundException("Sự kiện", request.EventId);

        if (ev.Status == EventStatus.Closed || ev.Status == EventStatus.Finished)
            throw new AppException("Sự kiện này đã đóng cổng bán vé.", 400);

        var now = DateTime.UtcNow;
        if (ev.SalesEndTime != DateTime.MinValue && now > ev.SalesEndTime)
            throw new AppException("Đã quá thời gian mở bán vé của sự kiện này.", 400);

        // Mode-specific validation
        if (ev.TicketingMode == TicketingMode.SeatedOnly && (request.SeatIds == null || request.SeatIds.Count == 0))
        {
            throw new AppException("Sự kiện này là show thuần ghế ngồi. Vui lòng chọn ghế trên sơ đồ 2D.", 400);
        }
        if (ev.TicketingMode == TicketingMode.StandingOnly && (request.StandingTickets == null || request.StandingTickets.Count == 0))
        {
            throw new AppException("Sự kiện này là show thuần vé đứng tự do. Vui lòng chọn số lượng vé cần đặt.", 400);
        }
        // 1. Process Seated Tickets (if any)
        var requestedSeats = new List<Seat>();
        if (request.SeatIds != null && request.SeatIds.Count > 0)
        {
            requestedSeats = ev.Seats
                .Where(s => request.SeatIds.Contains(s.Id))
                .ToList();

            if (requestedSeats.Count != request.SeatIds.Count)
                throw new AppException("Một số ghế yêu cầu không tồn tại trong sự kiện.", 400);
        }

        // 2. Process Standing Tickets (if any)
        var standingSnapshot = new List<StandingHoldSnapshot>();
        decimal standingTotal = 0;
        if (request.StandingTickets != null && request.StandingTickets.Count > 0)
        {
            foreach (var st in request.StandingTickets)
            {
                if (st.Quantity <= 0) continue;

                var tier = ev.TicketTiers.FirstOrDefault(t => t.Id == st.TicketTierId);
                if (tier == null)
                    throw new AppException("Hạng vé đứng yêu cầu không tồn tại.", 400);

                var availableStock = tier.TotalQuantity - (tier.SoldQuantity + tier.HoldQuantity);
                if (availableStock < st.Quantity)
                {
                    throw new AppException($"Hạng vé '{tier.Name}' chỉ còn {Math.Max(0, availableStock)} vé khả dụng, không đủ số lượng yêu cầu.", 409);
                }

                // Tăng số lượng giữ chỗ tạm thời (chống overselling)
                tier.HoldQuantity += st.Quantity;

                standingTotal += tier.Price * st.Quantity;
                standingSnapshot.Add(new StandingHoldSnapshot(
                    TierId: tier.Id,
                    TierName: tier.Name,
                    Quantity: st.Quantity,
                    UnitPrice: tier.Price
                ));
            }
        }

        var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var lockOwnerId = orderCode;
        var lockedSuccessKeys = new List<Guid>();

        try
        {
            // Acquire locks on specific seats
            foreach (var seat in requestedSeats)
            {
                if (seat.Status == SeatStatus.Sold)
                {
                    throw new AppException($"Ghế {seat.RowName}{seat.SeatNumber} đã được bán.", 409);
                }

                // Check Redis lock (SETNX with TTL 300s)
                var acquired = await _lockService.LockSeatAsync(ev.Id, seat.Id, lockOwnerId, TimeSpan.FromMinutes(5));
                if (!acquired)
                {
                    throw new AppException($"Ghế {seat.RowName}{seat.SeatNumber} đang có người khác chọn giữ chỗ. Vui lòng chọn ghế khác.", 409);
                }

                lockedSuccessKeys.Add(seat.Id);
            }

            // Calculate total amount
            decimal seatsTotal = requestedSeats.Sum(s => s.TicketTier?.Price ?? 0);
            decimal totalAmount = seatsTotal + standingTotal;
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            var customerId = _currentUser.UserId;
            if (!customerId.HasValue)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.CustomerEmail.ToLower(), cancellationToken);
                customerId = existingUser?.Id ?? (await _context.Users.Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken) ?? Guid.NewGuid());
            }

            var order = new Order
            {
                EventId = ev.Id,
                OrderCode = orderCode,
                CustomerId = customerId.Value,
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim().ToLower(),
                CustomerPhone = request.CustomerPhone?.Trim(),
                TotalAmount = totalAmount,
                PaymentStatus = PaymentStatus.Pending,
                ExpiresAt = expiresAt,
                ReservedItemsJson = standingSnapshot.Count > 0 ? JsonSerializer.Serialize(standingSnapshot) : null
            };

            _context.Orders.Add(order);

            // Mark seats as Locked
            foreach (var seat in requestedSeats)
            {
                seat.Status = SeatStatus.Locked;
                seat.LockedUntil = expiresAt;
                seat.LockedBySessionId = orderCode;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var resultSeats = new List<ReservedSeatItemDto>();

            // Add seated tickets to response
            foreach (var s in requestedSeats)
            {
                resultSeats.Add(new ReservedSeatItemDto(
                    s.Id,
                    $"{s.RowName}{s.SeatNumber}",
                    s.TicketTier?.Name ?? "General",
                    s.TicketTier?.Price ?? 0
                ));
            }

            // Add standing tickets to response
            foreach (var st in standingSnapshot)
            {
                resultSeats.Add(new ReservedSeatItemDto(
                    st.TierId,
                    $"{st.TierName} (Vé đứng x{st.Quantity})",
                    st.TierName,
                    st.UnitPrice * st.Quantity
                ));
            }

            var result = new ReservationResultDto(
                order.Id,
                order.OrderCode,
                order.TotalAmount,
                expiresAt,
                300,
                resultSeats
            );

            return ApiResponse<ReservationResultDto>.Ok(result, "Giữ chỗ thành công. Vui lòng thanh toán trong vòng 5 phút.");
        }
        catch
        {
            // Compensation: release all acquired locks if any seat failed
            foreach (var seatId in lockedSuccessKeys)
            {
                await _lockService.ReleaseSeatLockAsync(ev.Id, seatId, lockOwnerId);
            }

            // Hoàn lại số lượng vé đứng đã giữ chỗ tạm nếu xảy ra lỗi giữa chừng
            foreach (var st in standingSnapshot)
            {
                var tier = ev.TicketTiers.FirstOrDefault(t => t.Id == st.TierId);
                if (tier != null)
                {
                    tier.HoldQuantity = Math.Max(0, tier.HoldQuantity - st.Quantity);
                }
            }

            throw;
        }
    }
}

public record StandingHoldSnapshot(Guid TierId, string TierName, int Quantity, decimal UnitPrice);
