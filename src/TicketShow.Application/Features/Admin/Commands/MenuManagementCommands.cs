using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;

namespace TicketShow.Application.Features.Admin.Commands;

public record GetMenusQuery : IRequest<ApiResponse<List<MenuItemDto>>>;

public record MenuItemDto(
    Guid Id,
    string Title,
    string RoutePath,
    string Icon,
    Guid? ParentId,
    int SortOrder,
    string? PermissionCode,
    bool IsActive
);

public record CreateMenuCommand(
    string Title,
    string RoutePath,
    string Icon,
    Guid? ParentId,
    int SortOrder,
    string? PermissionCode
) : IRequest<ApiResponse<MenuItemDto>>;

public record UpdateMenuCommand(
    Guid Id,
    string Title,
    string RoutePath,
    string Icon,
    int SortOrder,
    string? PermissionCode,
    bool IsActive
) : IRequest<ApiResponse<MenuItemDto>>;

public record GetRoleMenusQuery(Guid RoleId) : IRequest<ApiResponse<List<Guid>>>;

public record UpdateRoleMenusCommand(Guid RoleId, List<Guid> MenuIds) : IRequest<ApiResponse<bool>>;

public class MenuManagementHandlers :
    IRequestHandler<GetMenusQuery, ApiResponse<List<MenuItemDto>>>,
    IRequestHandler<CreateMenuCommand, ApiResponse<MenuItemDto>>,
    IRequestHandler<UpdateMenuCommand, ApiResponse<MenuItemDto>>,
    IRequestHandler<GetRoleMenusQuery, ApiResponse<List<Guid>>>,
    IRequestHandler<UpdateRoleMenusCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public MenuManagementHandlers(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<MenuItemDto>>> Handle(GetMenusQuery request, CancellationToken cancellationToken)
    {
        var menus = await _context.Menus
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Title)
            .Select(m => new MenuItemDto(
                m.Id,
                m.Title,
                m.RoutePath,
                m.Icon,
                m.ParentId,
                m.SortOrder,
                m.PermissionCode,
                m.IsActive
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<MenuItemDto>>.Ok(menus, "Lấy danh mục menu thành công.");
    }

    public async Task<ApiResponse<MenuItemDto>> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = new Menu
        {
            Title = request.Title.Trim(),
            RoutePath = request.RoutePath.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "📑" : request.Icon.Trim(),
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            PermissionCode = string.IsNullOrWhiteSpace(request.PermissionCode) ? null : request.PermissionCode.Trim(),
            IsActive = true
        };

        _context.Menus.Add(menu);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<MenuItemDto>.Ok(new MenuItemDto(menu.Id, menu.Title, menu.RoutePath, menu.Icon, menu.ParentId, menu.SortOrder, menu.PermissionCode, menu.IsActive), "Thêm mục menu thành công.");
    }

    public async Task<ApiResponse<MenuItemDto>> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (menu == null)
            throw new NotFoundException("Mục menu", request.Id);

        menu.Title = request.Title.Trim();
        menu.RoutePath = request.RoutePath.Trim();
        menu.Icon = string.IsNullOrWhiteSpace(request.Icon) ? "📑" : request.Icon.Trim();
        menu.SortOrder = request.SortOrder;
        menu.PermissionCode = string.IsNullOrWhiteSpace(request.PermissionCode) ? null : request.PermissionCode.Trim();
        menu.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<MenuItemDto>.Ok(new MenuItemDto(menu.Id, menu.Title, menu.RoutePath, menu.Icon, menu.ParentId, menu.SortOrder, menu.PermissionCode, menu.IsActive), "Cập nhật menu thành công.");
    }

    public async Task<ApiResponse<List<Guid>>> Handle(GetRoleMenusQuery request, CancellationToken cancellationToken)
    {
        var menuIds = await _context.RoleMenus
            .Where(rm => rm.RoleId == request.RoleId)
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Guid>>.Ok(menuIds, "Lấy danh mục menu của vai trò thành công.");
    }

    public async Task<ApiResponse<bool>> Handle(UpdateRoleMenusCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role == null)
            throw new NotFoundException("Vai trò", request.RoleId);

        var currentRoleMenus = await _context.RoleMenus
            .Where(rm => rm.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        _context.RoleMenus.RemoveRange(currentRoleMenus);

        foreach (var mId in request.MenuIds)
        {
            _context.RoleMenus.Add(new RoleMenu
            {
                RoleId = request.RoleId,
                MenuId = mId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true, "Cập nhật phân quyền menu cho vai trò thành công.");
    }
}
