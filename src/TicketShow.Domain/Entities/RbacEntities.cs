using TicketShow.Domain.Common;

namespace TicketShow.Domain.Entities;

public class Role : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. SUPER_ADMIN, ORGANIZER, GATE_STAFF, CUSTOMER
    public string Name { get; set; } = string.Empty; // e.g. Quản trị viên cấp cao
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false; // Protected from deletion

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
    public ICollection<UserRoleMapping> UserRoles { get; set; } = new List<UserRoleMapping>();
}

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. events.view, events.create, users.manage
    public string Name { get; set; } = string.Empty; // e.g. Xem danh sách sự kiện
    public string Module { get; set; } = string.Empty; // Grouping: Events, Orders, Users, Roles, Menus, Scanner
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

public class Menu : BaseEntity
{
    public string Title { get; set; } = string.Empty; // e.g. Tổng quan, Quản lý Show, Quản lý User
    public string RoutePath { get; set; } = string.Empty; // e.g. /admin, /admin/events, /admin/users
    public string Icon { get; set; } = "📑"; // Emoji or icon name
    public Guid? ParentId { get; set; }
    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = new List<Menu>();

    public int SortOrder { get; set; } = 0;
    public string? PermissionCode { get; set; } // Required permission code to view
    public bool IsActive { get; set; } = true;

    public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
}

public class RoleMenu : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid MenuId { get; set; }
    public Menu? Menu { get; set; }
}

public class UserRoleMapping : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}
