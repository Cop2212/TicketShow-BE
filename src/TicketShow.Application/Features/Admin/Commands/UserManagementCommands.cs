using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;

namespace TicketShow.Application.Features.Admin.Commands;

// 1. Get Users
public record GetUsersQuery(string? Search = null, string? RoleCode = null) : IRequest<ApiResponse<List<UserAdminDto>>>;

public record UserAdminDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt,
    List<string> RoleNames,
    List<Guid> RoleIds
);

// 2. Toggle Status
public record ToggleUserStatusCommand(Guid UserId, bool IsActive) : IRequest<ApiResponse<bool>>;

// 3. Assign Roles
public record AssignUserRolesCommand(Guid UserId, List<Guid> RoleIds) : IRequest<ApiResponse<bool>>;

public class UserManagementHandlers :
    IRequestHandler<GetUsersQuery, ApiResponse<List<UserAdminDto>>>,
    IRequestHandler<ToggleUserStatusCommand, ApiResponse<bool>>,
    IRequestHandler<AssignUserRolesCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;

    public UserManagementHandlers(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<UserAdminDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(s) || u.FullName.ToLower().Contains(s) || (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(request.RoleCode))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Code == request.RoleCode));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAdminDto(
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.IsActive,
                u.CreatedAt,
                u.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList(),
                u.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.RoleId).ToList()
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<UserAdminDto>>.Ok(users, "Lấy danh sách người dùng thành công.");
    }

    public async Task<ApiResponse<bool>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new NotFoundException("Tài khoản người dùng", request.UserId);

        user.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var msg = request.IsActive ? "Đã mở khóa tài khoản thành công." : "Đã khóa tài khoản thành công.";
        return ApiResponse<bool>.Ok(true, msg);
    }

    public async Task<ApiResponse<bool>> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new NotFoundException("Tài khoản người dùng", request.UserId);

        // Remove old roles
        var currentRoles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync(cancellationToken);

        _context.UserRoles.RemoveRange(currentRoles);

        // Add selected roles
        foreach (var roleId in request.RoleIds)
        {
            _context.UserRoles.Add(new UserRoleMapping
            {
                UserId = user.Id,
                RoleId = roleId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true, "Cập nhật vai trò tài khoản thành công.");
    }
}
