using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Auth.Commands.Login;
using TicketShow.Application.Features.Auth.Commands.Register;
using TicketShow.Application.Features.Auth.DTOs;

namespace TicketShow.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly ICurrentUserService _currentUserService;

    public AuthController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Role = _currentUserService.Role
        }, "Lấy thông tin người dùng thành công."));
    }
}
