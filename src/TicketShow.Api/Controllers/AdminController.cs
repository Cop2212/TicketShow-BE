using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Admin.Commands;
using TicketShow.Application.Features.Admin.Queries;

namespace TicketShow.Api.Controllers;

[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    // ==========================================
    // 1. DASHBOARD & NAVIGATION
    // ==========================================

    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<ApiResponse<AdminDashboardStatsDto>>> GetDashboardStats()
    {
        var result = await Mediator.Send(new GetAdminDashboardStatsQuery());
        return Ok(result);
    }

    [Authorize]
    [HttpGet("navigation")]
    public async Task<ActionResult<ApiResponse<UserNavigationDto>>> GetUserNavigation()
    {
        var result = await Mediator.Send(new GetUserNavigationQuery());
        return Ok(result);
    }

    // ==========================================
    // 2. USER MANAGEMENT
    // ==========================================

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<UserAdminDto>>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? roleCode)
    {
        var result = await Mediator.Send(new GetUsersQuery(search, roleCode));
        return Ok(result);
    }

    [HttpPut("users/{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleUserStatus(Guid id, [FromBody] ToggleStatusRequest request)
    {
        var result = await Mediator.Send(new ToggleUserStatusCommand(id, request.IsActive));
        return Ok(result);
    }

    [HttpPut("users/{id}/roles")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignUserRoles(Guid id, [FromBody] AssignRolesRequest request)
    {
        var result = await Mediator.Send(new AssignUserRolesCommand(id, request.RoleIds));
        return Ok(result);
    }

    // ==========================================
    // 3. ROLE & PERMISSION MANAGEMENT
    // ==========================================

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
    {
        var result = await Mediator.Send(new GetRolesQuery());
        return Ok(result);
    }

    [HttpPost("roles")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("roles/{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await Mediator.Send(new UpdateRoleCommand(id, request.Name, request.Description));
        return Ok(result);
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<List<ModulePermissionsDto>>>> GetPermissions()
    {
        var result = await Mediator.Send(new GetPermissionsQuery());
        return Ok(result);
    }

    [HttpGet("roles/{id}/permissions")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GetRolePermissions(Guid id)
    {
        var result = await Mediator.Send(new GetRolePermissionsQuery(id));
        return Ok(result);
    }

    [HttpPut("roles/{id}/permissions")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateRolePermissions(Guid id, [FromBody] UpdatePermissionsRequest request)
    {
        var result = await Mediator.Send(new UpdateRolePermissionsCommand(id, request.PermissionIds));
        return Ok(result);
    }

    // ==========================================
    // 4. MENU MANAGEMENT
    // ==========================================

    [HttpGet("menus")]
    public async Task<ActionResult<ApiResponse<List<MenuItemDto>>>> GetMenus()
    {
        var result = await Mediator.Send(new GetMenusQuery());
        return Ok(result);
    }

    [HttpPost("menus")]
    public async Task<ActionResult<ApiResponse<MenuItemDto>>> CreateMenu([FromBody] CreateMenuCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("menus/{id}")]
    public async Task<ActionResult<ApiResponse<MenuItemDto>>> UpdateMenu(Guid id, [FromBody] UpdateMenuRequest request)
    {
        var result = await Mediator.Send(new UpdateMenuCommand(
            id,
            request.Title,
            request.RoutePath,
            request.Icon,
            request.SortOrder,
            request.PermissionCode,
            request.IsActive
        ));
        return Ok(result);
    }

    [HttpGet("roles/{id}/menus")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GetRoleMenus(Guid id)
    {
        var result = await Mediator.Send(new GetRoleMenusQuery(id));
        return Ok(result);
    }

    [HttpPut("roles/{id}/menus")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateRoleMenus(Guid id, [FromBody] UpdateRoleMenusRequest request)
    {
        var result = await Mediator.Send(new UpdateRoleMenusCommand(id, request.MenuIds));
        return Ok(result);
    }
}

public record ToggleStatusRequest(bool IsActive);
public record AssignRolesRequest(List<Guid> RoleIds);
public record UpdateRoleRequest(string Name, string? Description);
public record UpdatePermissionsRequest(List<Guid> PermissionIds);
public record UpdateMenuRequest(string Title, string RoutePath, string Icon, int SortOrder, string? PermissionCode, bool IsActive);
public record UpdateRoleMenusRequest(List<Guid> MenuIds);
