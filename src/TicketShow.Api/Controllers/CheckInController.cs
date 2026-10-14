using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.CheckIn.Commands;
using TicketShow.Domain.Enums;

namespace TicketShow.Api.Controllers;

[Route("api/v1/check-in")]
public class CheckInController : BaseApiController
{
    private readonly IAppDbContext _context;

    public CheckInController(IAppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Quét mã QR trực tiếp tại cổng soát vé (Real-time Scan)
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<ApiResponse<CheckInResultDto>>> ScanTicket([FromBody] VerifyCheckInCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Đồng bộ danh sách vé trước giờ diễn để chạy chế độ Offline
    /// </summary>
    [HttpGet("manifest/{eventId}")]
    public async Task<ActionResult<ApiResponse<List<OfflineTicketManifestDto>>>> GetOfflineManifest(Guid eventId)
    {
        var result = await Mediator.Send(new GetOfflineManifestQuery(eventId));
        return Ok(result);
    }

    /// <summary>
    /// Thống kê soát vé thời gian thực của sự kiện
    /// </summary>
    [HttpGet("stats/{eventId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetCheckInStats(Guid eventId)
    {
        var totalTickets = await _context.Tickets.CountAsync(t => t.EventId == eventId && t.Status != TicketStatus.Cancelled);
        var checkedInCount = await _context.Tickets.CountAsync(t => t.EventId == eventId && t.Status == TicketStatus.CheckedIn);
        var remainingCount = totalTickets - checkedInCount;
        var checkInRate = totalTickets > 0 ? Math.Round((double)checkedInCount / totalTickets * 100, 1) : 0;

        var stats = new
        {
            TotalTickets = totalTickets,
            CheckedInCount = checkedInCount,
            RemainingCount = remainingCount,
            CheckInRate = checkInRate
        };

        return Ok(ApiResponse<object>.Ok(stats, "Lấy thống kê soát vé thành công."));
    }
}
