using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Venues.Commands;
using TicketShow.Application.Features.Venues.DTOs;
using TicketShow.Domain.Enums;

namespace TicketShow.Api.Controllers;

[Route("api/v1/venues")]
public class VenuesController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách địa điểm (Catalog công khai hoặc theo bộ lọc)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<VenueSummaryDto>>>> GetVenues(
        [FromQuery] VenueStatus? status = VenueStatus.Approved,
        [FromQuery] string? city = null,
        [FromQuery] string? search = null,
        [FromQuery] bool myVenuesOnly = false)
    {
        var result = await Mediator.Send(new GetVenuesQuery(status, city, search, myVenuesOnly));
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết thông tin và sơ đồ ghế Master của địa điểm
    /// </summary>
    [HttpGet("{idOrSlug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VenueDetailDto>>> GetVenueDetail(string idOrSlug)
    {
        var result = await Mediator.Send(new GetVenueDetailQuery(idOrSlug));
        return Ok(result);
    }

    /// <summary>
    /// Chủ địa điểm đăng ký mặt bằng cơ sở mới lên sàn
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "VenueOwner,Admin")]
    public async Task<ActionResult<ApiResponse<VenueDetailDto>>> CreateVenue([FromBody] CreateVenueCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetVenueDetail), new { idOrSlug = result.Data?.Id.ToString() }, result);
    }

    /// <summary>
    /// Chủ địa điểm cập nhật thông tin mặt bằng hoặc sơ đồ ghế Master
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "VenueOwner,Admin")]
    public async Task<ActionResult<ApiResponse<VenueDetailDto>>> UpdateVenue(Guid id, [FromBody] UpdateVenueCommand command)
    {
        if (id != command.Id)
            return BadRequest(ApiResponse<VenueDetailDto>.Fail("ID không khớp."));

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Ban quản trị sàn phê duyệt hoặc từ chối địa điểm
    /// </summary>
    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveVenue(Guid id, [FromBody] ApproveVenueRequest request)
    {
        var result = await Mediator.Send(new ApproveVenueCommand(id, request.IsApproved, request.Reason));
        return Ok(result);
    }
}

public record ApproveVenueRequest(bool IsApproved, string? Reason = null);
