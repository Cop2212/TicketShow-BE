using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Admin.Queries;

namespace TicketShow.Api.Controllers;

[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    /// <summary>
    /// Lấy thống kê tổng quan toàn bộ hệ thống cho Dashboard Quản trị viên
    /// </summary>
    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<ApiResponse<AdminDashboardStatsDto>>> GetDashboardStats()
    {
        var result = await Mediator.Send(new GetAdminDashboardStatsQuery());
        return Ok(result);
    }
}
