using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;

namespace TicketShow.Application.Features.Admin.Queries;

public record GetUserNavigationQuery : IRequest<ApiResponse<UserNavigationDto>>;

public record UserNavigationDto(
    string FullName,
    string Email,
    List<string> Roles,
    List<string> Permissions,
    List<UserNavMenuItemDto> Menus
);

public record UserNavMenuItemDto(
    Guid Id,
    string Title,
    string RoutePath,
    string Icon,
    int SortOrder,
    string? PermissionCode
);

public class GetUserNavigationQueryHandler : IRequestHandler<GetUserNavigationQuery, ApiResponse<UserNavigationDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUserNavigationQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<UserNavigationDto>> Handle(GetUserNavigationQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            throw new AppException("Vui lòng đăng nhập để truy cập tài nguyên.", 401);
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RoleMenus)
                        .ThenInclude(rm => rm.Menu)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("Tài khoản người dùng", userId.Value);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Code)
            .Distinct()
            .ToList();

        // If no explicit user_roles yet, fallback to enum role
        if (roles.Count == 0)
        {
            roles.Add(user.Role.ToString().ToUpperInvariant());
        }

        // Permissions collection
        var permissions = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToList();

        // Menus collection
        var menuItems = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RoleMenus)
            .Where(rm => rm.Menu != null && rm.Menu.IsActive)
            .Select(rm => rm.Menu!)
            .DistinctBy(m => m.Id)
            .OrderBy(m => m.SortOrder)
            .Select(m => new UserNavMenuItemDto(
                m.Id,
                m.Title,
                m.RoutePath,
                m.Icon,
                m.SortOrder,
                m.PermissionCode
            ))
            .ToList();

        // If Super Admin, guarantee all active menus if list is empty
        if (roles.Contains("SUPER_ADMIN") || roles.Contains("ADMIN"))
        {
            if (menuItems.Count == 0)
            {
                menuItems = await _context.Menus
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new UserNavMenuItemDto(
                        m.Id,
                        m.Title,
                        m.RoutePath,
                        m.Icon,
                        m.SortOrder,
                        m.PermissionCode
                    ))
                    .ToListAsync(cancellationToken);
            }
        }

        var result = new UserNavigationDto(
            FullName: user.FullName,
            Email: user.Email,
            Roles: roles,
            Permissions: permissions,
            Menus: menuItems
        );

        return ApiResponse<UserNavigationDto>.Ok(result, "Lấy danh mục menu và quyền thành công.");
    }
}
