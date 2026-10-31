using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;

namespace TicketShow.Application.Features.Admin.Commands;

public record GetRolesQuery : IRequest<ApiResponse<List<RoleDto>>>;

public record RoleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    int UserCount,
    int PermissionCount
);

public record CreateRoleCommand(string Code, string Name, string? Description) : IRequest<ApiResponse<RoleDto>>;

public record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest<ApiResponse<RoleDto>>;

public record GetPermissionsQuery : IRequest<ApiResponse<List<ModulePermissionsDto>>>;

public record ModulePermissionsDto(
    string Module,
    List<PermissionItemDto> Permissions
);

public record PermissionItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description
);

public record GetRolePermissionsQuery(Guid RoleId) : IRequest<ApiResponse<List<Guid>>>;

public record UpdateRolePermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<ApiResponse<bool>>;

public class RoleManagementHandlers :
    IRequestHandler<GetRolesQuery, ApiResponse<List<RoleDto>>>,
    IRequestHandler<CreateRoleCommand, ApiResponse<RoleDto>>,
    IRequestHandler<UpdateRoleCommand, ApiResponse<RoleDto>>,
    IRequestHandler<GetPermissionsQuery, ApiResponse<List<ModulePermissionsDto>>>,
    IRequestHandler<GetRolePermissionsQuery, ApiResponse<List<Guid>>>,
    IRequestHandler<UpdateRolePermissionsCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public RoleManagementHandlers(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
            .AsNoTracking()
            .OrderBy(r => r.IsSystem ? 0 : 1)
            .ThenBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                r.IsSystem,
                r.UserRoles.Count,
                r.RolePermissions.Count
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<RoleDto>>.Ok(roles, "Lấy danh sách vai trò thành công.");
    }

    public async Task<ApiResponse<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.Roles.AnyAsync(r => r.Code == code, cancellationToken))
            throw new AppException($"Mã vai trò '{code}' đã tồn tại trong hệ thống.", 400);

        var role = new Role
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsSystem = false
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<RoleDto>.Ok(new RoleDto(role.Id, role.Code, role.Name, role.Description, false, 0, 0), "Tạo vai trò mới thành công.");
    }

    public async Task<ApiResponse<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
            throw new NotFoundException("Vai trò", request.Id);

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<RoleDto>.Ok(new RoleDto(role.Id, role.Code, role.Name, role.Description, role.IsSystem, role.UserRoles.Count, role.RolePermissions.Count), "Cập nhật vai trò thành công.");
    }

    public async Task<ApiResponse<List<ModulePermissionsDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        var grouped = permissions
            .GroupBy(p => p.Module)
            .Select(g => new ModulePermissionsDto(
                Module: g.Key,
                Permissions: g.Select(p => new PermissionItemDto(p.Id, p.Code, p.Name, p.Description)).ToList()
            ))
            .ToList();

        return ApiResponse<List<ModulePermissionsDto>>.Ok(grouped, "Lấy danh mục phân quyền thành công.");
    }

    public async Task<ApiResponse<List<Guid>>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissionIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Guid>>.Ok(permissionIds, "Lấy quyền của vai trò thành công.");
    }

    public async Task<ApiResponse<bool>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role == null)
            throw new NotFoundException("Vai trò", request.RoleId);

        var currentRolePerms = await _context.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        _context.RolePermissions.RemoveRange(currentRolePerms);

        foreach (var pId in request.PermissionIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = request.RoleId,
                PermissionId = pId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true, "Cập nhật ma trận phân quyền cho vai trò thành công.");
    }
}
