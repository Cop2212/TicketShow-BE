using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Orders.Commands;
using TicketShow.Application.Features.Payments.Commands;

namespace TicketShow.Api.Controllers;

public class OrdersController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IQrSignatureService _qrService;

    public OrdersController(IAppDbContext context, ICurrentUserService currentUser, IQrSignatureService qrService)
    {
        _context = context;
        _currentUser = currentUser;
        _qrService = qrService;
    }

    [HttpPost("reserve-seats")]
    public async Task<ActionResult<ApiResponse<ReservationResultDto>>> ReserveSeats([FromBody] ReserveSeatsCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{orderCode}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelReservation(string orderCode)
    {
        var result = await Mediator.Send(new CancelReservationCommand(orderCode));
        return Ok(result);
    }

    [HttpGet("{orderCode}")]
    public async Task<ActionResult<ApiResponse<object>>> GetOrderByCode(string orderCode)
    {
        var order = await _context.Orders
            .Include(o => o.Tickets)
                .ThenInclude(t => t.Seat)
            .Include(o => o.Tickets)
                .ThenInclude(t => t.TicketTier)
            .Include(o => o.Tickets)
                .ThenInclude(t => t.Event)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null)
            throw new NotFoundException("Đơn hàng", orderCode);

        var tickets = order.Tickets.Select(t => new
        {
            t.Id,
            t.TicketCode,
            EventTitle = t.Event?.Title,
            VenueName = t.Event?.VenueName,
            StartTime = t.Event?.StartTime,
            TierName = t.TicketTier?.Name,
            SeatLabel = t.Seat != null ? $"{t.Seat.RowName}{t.Seat.SeatNumber}" : "GA",
            Status = t.Status.ToString(),
            QrCodeImage = _qrService.GenerateQrBase64Image(t.QrCodeSignature)
        }).ToList();

        var data = new
        {
            order.Id,
            order.OrderCode,
            order.CustomerName,
            order.CustomerEmail,
            order.TotalAmount,
            PaymentStatus = order.PaymentStatus.ToString(),
            order.ExpiresAt,
            Tickets = tickets
        };

        return Ok(ApiResponse<object>.Ok(data, "Lấy thông tin đơn hàng thành công."));
    }

    [HttpGet("my-tickets")]
    public async Task<ActionResult<ApiResponse<object>>> GetMyTickets([FromQuery] string? email)
    {
        var userEmail = _currentUser.Email ?? email;
        var userId = _currentUser.UserId;

        var query = _context.Tickets
            .Include(t => t.Order)
            .Include(t => t.Seat)
            .Include(t => t.TicketTier)
            .Include(t => t.Event)
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(t => t.Order != null && (t.Order.CustomerId == userId.Value || (userEmail != null && t.Order.CustomerEmail == userEmail)));
        }
        else if (!string.IsNullOrWhiteSpace(userEmail))
        {
            query = query.Where(t => t.Order != null && t.Order.CustomerEmail.ToLower() == userEmail.ToLower());
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.TicketCode,
                EventId = t.EventId,
                EventTitle = t.Event != null ? t.Event.Title : string.Empty,
                VenueName = t.Event != null ? t.Event.VenueName : string.Empty,
                StartTime = t.Event != null ? t.Event.StartTime : DateTime.MinValue,
                EndTime = t.Event != null ? t.Event.EndTime : DateTime.MinValue,
                TierName = t.TicketTier != null ? t.TicketTier.Name : "General",
                Price = t.TicketTier != null ? t.TicketTier.Price : 0,
                SeatLabel = t.Seat != null ? $"{t.Seat.RowName}{t.Seat.SeatNumber}" : "GA",
                Status = t.Status.ToString(),
                t.CheckedInAt,
                QrData = t.QrCodeSignature,
                QrCodeImage = _qrService.GenerateQrBase64Image(t.QrCodeSignature)
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(tickets, "Lấy danh sách vé thành công."));
    }
}
