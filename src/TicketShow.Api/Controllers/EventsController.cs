using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Events.Commands;
using TicketShow.Application.Features.Events.DTOs;
using TicketShow.Domain.Enums;

namespace TicketShow.Api.Controllers;

public class EventsController : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<EventSummaryDto>>>> GetEvents(
        [FromQuery] string? search,
        [FromQuery] EventStatus? status)
    {
        var result = await Mediator.Send(new GetEventsQuery(search, status));
        return Ok(result);
    }

    [HttpGet("{idOrSlug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> GetEventDetail(string idOrSlug)
    {
        var result = await Mediator.Send(new GetEventDetailQuery(idOrSlug));
        return Ok(result);
    }

    /// <summary>
    /// Tạo sự kiện thủ công (Standalone Show)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> CreateEvent([FromBody] CreateEventCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetEventDetail), new { idOrSlug = result.Data?.Id.ToString() }, result);
    }

    /// <summary>
    /// Tạo sự kiện từ Đơn thuê địa điểm đã xác nhận (Tự động thừa hưởng Sơ đồ ghế Master từ Venue)
    /// </summary>
    [HttpPost("from-booking")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> CreateEventFromBooking([FromBody] CreateEventFromBookingCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetEventDetail), new { idOrSlug = result.Data?.Id.ToString() }, result);
    }

    /// <summary>
    /// Ban tổ chức gửi yêu cầu phê duyệt show diễn lên Ban quản trị sàn
    /// </summary>
    [HttpPatch("{id}/submit")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitForApproval(Guid id)
    {
        var result = await Mediator.Send(new SubmitEventForApprovalCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Ban quản trị sàn phê duyệt hoặc từ chối xuất bản show diễn lên Trang chủ
    /// </summary>
    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveEvent(Guid id, [FromBody] ApproveEventRequest request)
    {
        var result = await Mediator.Send(new ApproveEventCommand(id, request.Approved, request.Reason));
        return Ok(result);
    }

    /// <summary>
    /// Đóng cổng bán vé cho sự kiện
    /// </summary>
    [HttpPatch("{id}/close-sales")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> CloseEventSales(Guid id)
    {
        var result = await Mediator.Send(new CloseEventSalesCommand(id));
        return Ok(result);
    }
}

public record ApproveEventRequest(bool Approved, string? Reason = null);
