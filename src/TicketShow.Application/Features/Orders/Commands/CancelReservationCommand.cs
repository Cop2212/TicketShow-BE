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
            throw new NotFoundException("Đơn hàng", request.OrderCode);

        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new AppException("Đơn hàng đã thanh toán thành công, không thể hủy giữ chỗ.", 400);

        // Find all seats locked by this order
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

        order.PaymentStatus = PaymentStatus.Failed;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã hủy giữ chỗ và giải phóng ghế thành công.");
    }
}
