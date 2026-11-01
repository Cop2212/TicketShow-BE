using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.VenueBookings.Commands;
using TicketShow.Application.Features.VenueBookings.DTOs;
using TicketShow.Domain.Enums;

namespace TicketShow.Api.Controllers;

[Route("api/v1/venue-bookings")]
public class VenueBookingsController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách yêu cầu thuê địa điểm (Organizer xem đơn của mình, VenueOwner xem đơn gửi đến địa điểm của mình)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<VenueBookingSummaryDto>>>> GetBookings(
        [FromQuery] Guid? venueId = null,
        [FromQuery] Guid? organizerId = null,
        [FromQuery] VenueBookingStatus? status = null)
    {
        var result = await Mediator.Send(new GetVenueBookingsQuery(venueId, organizerId, status));
        return Ok(result);
    }

    /// <summary>
    /// Kiểm tra khung giờ thuê có bị xung đột / trùng lịch với show khác không (Anti-collision Check)
    /// </summary>
    [HttpGet("check-slot")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SlotAvailabilityResultDto>>> CheckSlot(
        [FromQuery] Guid venueId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        var result = await Mediator.Send(new CheckSlotAvailabilityQuery(venueId, startTime, endTime));
        return Ok(result);
    }

    /// <summary>
    /// Ban tổ chức gửi yêu cầu giữ chỗ / thuê địa điểm theo khung giờ
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<ApiResponse<VenueBookingSummaryDto>>> RequestBooking([FromBody] RequestVenueBookingCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Chủ địa điểm chấp thuận (Confirmed) hoặc từ chối (Rejected) yêu cầu thuê mặt bằng
    /// </summary>
    [HttpPatch("{id}/respond")]
    [Authorize(Roles = "VenueOwner,Admin")]
    public async Task<ActionResult<ApiResponse<VenueBookingSummaryDto>>> RespondBooking(Guid id, [FromBody] RespondBookingRequest request)
    {
        var result = await Mediator.Send(new RespondVenueBookingCommand(id, request.Accept, request.Reason));
        return Ok(result);
    }
}

public record RespondBookingRequest(bool Accept, string? Reason = null);
