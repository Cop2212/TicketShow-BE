using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Orders.Commands;

public record ReserveSeatsCommand : IRequest<ApiResponse<ReservationResultDto>>
{
    public Guid EventId { get; init; }
    public List<Guid> SeatIds { get; init; } = new();
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
    string Label, // e.g. A01
    string TierName,
    decimal Price
);

public class ReserveSeatsCommandValidator : AbstractValidator<ReserveSeatsCommand>
{
    public ReserveSeatsCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.SeatIds).NotEmpty().WithMessage("Vui lòng chọn ít nhất 1 ghế.");
        RuleFor(x => x.SeatIds.Count).LessThanOrEqualTo(10).WithMessage("Mỗi lần đặt chỉ được chọn tối đa 10 ghế.");
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
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev == null)
            throw new NotFoundException("Sự kiện", request.EventId);

        if (ev.Status == EventStatus.Closed || ev.Status == EventStatus.Finished)
            throw new AppException("Sự kiện này đã đóng cổng bán vé.", 400);

        var now = DateTime.UtcNow;
        if (ev.SalesEndTime != DateTime.MinValue && now > ev.SalesEndTime)
            throw new AppException("Đã quá thời gian mở bán vé của sự kiện này.", 400);

        var requestedSeats = ev.Seats
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToList();

        if (requestedSeats.Count != request.SeatIds.Count)
            throw new AppException("Một số ghế yêu cầu không tồn tại trong sự kiện.", 400);

        var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var lockOwnerId = orderCode;
        var lockedSuccessKeys = new List<Guid>();
        try
        {
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
            decimal totalAmount = requestedSeats.Sum(s => s.TicketTier?.Price ?? 0);
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            var customerId = _currentUser.UserId;
            if (!customerId.HasValue)
            {
                // Find or create customer by email
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.CustomerEmail.ToLower(), cancellationToken);
                customerId = existingUser?.Id ?? (await _context.Users.Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken) ?? Guid.NewGuid());
            }

            var order = new Order
            {
                OrderCode = orderCode,
                CustomerId = customerId.Value,
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim().ToLower(),
                CustomerPhone = request.CustomerPhone?.Trim(),
                TotalAmount = totalAmount,
                PaymentStatus = PaymentStatus.Pending,
                ExpiresAt = expiresAt
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

            var resultSeats = requestedSeats.Select(s => new ReservedSeatItemDto(
                s.Id,
                $"{s.RowName}{s.SeatNumber}",
                s.TicketTier?.Name ?? "General",
                s.TicketTier?.Price ?? 0
            )).ToList();

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
            throw;
        }
    }
}
