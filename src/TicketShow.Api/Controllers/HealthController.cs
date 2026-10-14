using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;

namespace TicketShow.Api.Controllers;

public class HealthController : BaseApiController
{
    private readonly IAppDbContext _context;

    public HealthController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> CheckHealth()
    {
        var dbCanConnect = false;
        try
        {
            if (_context is DbContext dbContext)
            {
                dbCanConnect = await dbContext.Database.CanConnectAsync();
            }
        }
        catch
        {
            dbCanConnect = false;
        }

        var payload = new
        {
            Status = "Healthy",
            Database = dbCanConnect ? "Connected" : "Disconnected",
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(payload, "Hệ thống hoạt động bình thường."));
    }
}
