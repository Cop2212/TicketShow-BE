using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Events.Commands;
using TicketShow.Application.Features.Events.DTOs;
using TicketShow.Domain.Enums;

namespace TicketShow.Api.Controllers;

public class EventsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EventSummaryDto>>>> GetEvents(
        [FromQuery] string? search,
        [FromQuery] EventStatus? status)
    {
        var result = await Mediator.Send(new GetEventsQuery(search, status));
        return Ok(result);
    }

    [HttpGet("{idOrSlug}")]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> GetEventDetail(string idOrSlug)
    {
        var result = await Mediator.Send(new GetEventDetailQuery(idOrSlug));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> CreateEvent([FromBody] CreateEventCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetEventDetail), new { idOrSlug = result.Data?.Id.ToString() }, result);
    }
}
