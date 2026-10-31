using Microsoft.EntityFrameworkCore;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed default Admin and Staff user if none exists
        User? adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@ticketshow.vn");
        if (adminUser == null)
        {
            adminUser = new User
            {
                Email = "admin@ticketshow.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123@"),
                FullName = "Quản Trị Viên Hệ Thống",
                PhoneNumber = "0901234567",
                Role = UserRole.Admin,
                IsActive = true
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        User? staffUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "staff@ticketshow.vn");
        if (staffUser == null)
        {
            staffUser = new User
            {
                Email = "staff@ticketshow.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("StaffPassword123@"),
                FullName = "Nhân Viên Soát Vé",
                PhoneNumber = "0909888999",
                Role = UserRole.Staff,
                IsActive = true
            };
            context.Users.Add(staffUser);
            await context.SaveChangesAsync();
        }

        // 2. Seed RBAC (Roles, Permissions, Menus, Mappings)
        await SeedRbacAsync(context, adminUser, staffUser);

        // 2. Seed Sample Event if no events exist
        if (!await context.Events.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var sampleEvent = new Event
            {
                OrganizerId = adminUser.Id,
                Title = "Live Concert Hà Nội - Những Bản Tình Ca Mùa Thu 2026",
                Slug = "live-concert-ha-noi-2026",
                Description = "Đêm nhạc đặc biệt quy tụ dàn nghệ sĩ hàng đầu Việt Nam. Trải nghiệm âm thanh vòm đỉnh cao và không gian trình diễn hiện đại bậc nhất.",
                BannerUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=1200&auto=format&fit=crop&q=80",
                VenueName = "Trung Tâm Hội Nghị Quốc Gia, Hà Nội",
                VenueAddress = "Đại lộ Thăng Long, Mễ Trì, Nam Từ Liêm, Hà Nội",
                StartTime = now.AddDays(15),
                EndTime = now.AddDays(15).AddHours(3),
                SalesStartTime = now.AddDays(-1),
                SalesEndTime = now.AddDays(14),
                Status = EventStatus.Published
            };

            context.Events.Add(sampleEvent);
            await context.SaveChangesAsync();

            // 3. Ticket Tiers
            var vipTier = new TicketTier
            {
                EventId = sampleEvent.Id,
                Name = "VIP Zone",
                Price = 1500000,
                TotalQuantity = 20,
                SoldQuantity = 2,
                ColorHex = "#a855f7" // Purple
            };

            var standardTier = new TicketTier
            {
                EventId = sampleEvent.Id,
                Name = "Standard Zone",
                Price = 800000,
                TotalQuantity = 40,
                SoldQuantity = 1,
                ColorHex = "#3b82f6" // Blue
            };

            context.TicketTiers.AddRange(vipTier, standardTier);
            await context.SaveChangesAsync();

            // 4. Seats (Rows A, B are VIP; C, D, E, F are Standard)
            var rows = new[] { "A", "B", "C", "D", "E", "F" };
            for (int r = 0; r < rows.Length; r++)
            {
                var rowName = rows[r];
                var tier = (r < 2) ? vipTier : standardTier;

                for (int s = 1; s <= 10; s++)
                {
                    var seatNumber = s.ToString("D2");
                    var status = SeatStatus.Available;

                    // Set 2 sample seats as already sold for realistic demo visualization
                    if ((rowName == "A" && seatNumber == "05") || (rowName == "B" && seatNumber == "06"))
                    {
                        status = SeatStatus.Sold;
                    }

                    context.Seats.Add(new Seat
                    {
                        EventId = sampleEvent.Id,
                        TicketTierId = tier.Id,
                        RowName = rowName,
                        SeatNumber = seatNumber,
                        Status = status
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedRbacAsync(AppDbContext context, User adminUser, User staffUser)
    {
        // 1. Seed Roles
        var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "SUPER_ADMIN");
        if (superAdminRole == null)
        {
            superAdminRole = new Role
            {
                Code = "SUPER_ADMIN",
                Name = "Quản trị viên cấp cao",
                Description = "Toàn quyền quản trị tài khoản, vai trò, menu và toàn bộ hệ thống",
                IsSystem = true
            };
            context.Roles.Add(superAdminRole);
        }

        var organizerRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "ORGANIZER");
        if (organizerRole == null)
        {
            organizerRole = new Role
            {
                Code = "ORGANIZER",
                Name = "Ban tổ chức sự kiện",
                Description = "Quản lý và tạo show diễn, quản lý sơ đồ ghế và theo dõi doanh thu",
                IsSystem = true
            };
            context.Roles.Add(organizerRole);
        }

        var staffRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "STAFF");
        if (staffRole == null)
        {
            staffRole = new Role
            {
                Code = "STAFF",
                Name = "Nhân viên soát vé",
                Description = "Thực hiện soát vé E-ticket tại cổng vào trực tuyến và ngoại tuyến",
                IsSystem = true
            };
            context.Roles.Add(staffRole);
        }

        var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "CUSTOMER");
        if (customerRole == null)
        {
            customerRole = new Role
            {
                Code = "CUSTOMER",
                Name = "Khách hàng",
                Description = "Người dùng đặt vé và tham dự các sự kiện",
                IsSystem = true
            };
            context.Roles.Add(customerRole);
        }

        await context.SaveChangesAsync();

        // 2. Seed Permissions
        var permissionsList = new List<(string Code, string Name, string Module)>
        {
            ("dashboard.view", "Xem bảng điều khiển tổng quan", "Dashboard"),
            ("events.view", "Xem danh sách show diễn", "Events"),
            ("events.create", "Tạo sự kiện & sơ đồ ghế mới", "Events"),
            ("events.update", "Chỉnh sửa thông tin sự kiện", "Events"),
            ("events.close", "Đóng / Mở cổng bán vé show diễn", "Events"),
            ("orders.view", "Xem danh sách đơn đặt vé", "Orders"),
            ("orders.cancel", "Hủy đơn hàng giữ chỗ", "Orders"),
            ("checkin.scan", "Quét vé trực tiếp tại cổng", "CheckIn"),
            ("checkin.manifest", "Tải danh sách vé chạy Offline", "CheckIn"),
            ("checkin.sync", "Đồng bộ vé ngoại tuyến lên máy chủ", "CheckIn"),
            ("checkin.stats", "Xem thống kê check-in thời gian thực", "CheckIn"),
            ("users.view", "Xem danh sách tài khoản", "Users"),
            ("users.manage", "Khóa/Mở và phân quyền tài khoản", "Users"),
            ("roles.view", "Xem danh mục vai trò", "Roles"),
            ("roles.manage", "Tạo và cấu hình ma trận phân quyền vai trò", "Roles"),
            ("menus.view", "Xem cấu trúc cây menu", "Menus"),
            ("menus.manage", "Tạo và gán menu hiển thị cho vai trò", "Menus")
        };

        var allPermissions = new List<Permission>();
        foreach (var p in permissionsList)
        {
            var existing = await context.Permissions.FirstOrDefaultAsync(x => x.Code == p.Code);
            if (existing == null)
            {
                existing = new Permission
                {
                    Code = p.Code,
                    Name = p.Name,
                    Module = p.Module
                };
                context.Permissions.Add(existing);
            }
            allPermissions.Add(existing);
        }
        await context.SaveChangesAsync();

        // 3. Assign Permissions
        var existingRolePerms = await context.RolePermissions.Where(rp => rp.RoleId == superAdminRole.Id).ToListAsync();
        foreach (var perm in allPermissions)
        {
            if (!existingRolePerms.Any(rp => rp.PermissionId == perm.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = superAdminRole.Id,
                    PermissionId = perm.Id
                });
            }
        }

        var organizerCodes = new[] { "dashboard.view", "events.view", "events.create", "events.update", "events.close", "orders.view", "orders.cancel", "checkin.stats" };
        var existingOrgPerms = await context.RolePermissions.Where(rp => rp.RoleId == organizerRole.Id).ToListAsync();
        foreach (var perm in allPermissions.Where(p => organizerCodes.Contains(p.Code)))
        {
            if (!existingOrgPerms.Any(rp => rp.PermissionId == perm.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = organizerRole.Id,
                    PermissionId = perm.Id
                });
            }
        }

        var staffCodes = new[] { "checkin.scan", "checkin.manifest", "checkin.sync", "checkin.stats" };
        var existingStaffPerms = await context.RolePermissions.Where(rp => rp.RoleId == staffRole.Id).ToListAsync();
        foreach (var perm in allPermissions.Where(p => staffCodes.Contains(p.Code)))
        {
            if (!existingStaffPerms.Any(rp => rp.PermissionId == perm.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = staffRole.Id,
                    PermissionId = perm.Id
                });
            }
        }

        await context.SaveChangesAsync();

        // 4. Seed Menus
        var menusList = new List<(string Title, string RoutePath, string Icon, int SortOrder, string? PermissionCode)>
        {
            ("Tổng quan", "/admin", "📊", 1, "dashboard.view"),
            ("Quản lý Show diễn", "/admin/events", "🎪", 2, "events.view"),
            ("Tạo Show diễn mới", "/admin/events/create", "➕", 3, "events.create"),
            ("Đơn đặt vé", "/admin/orders", "💳", 4, "orders.view"),
            ("Cổng soát vé QR", "/admin/scanner", "📷", 5, "checkin.scan"),
            ("Quản lý Tài khoản", "/admin/users", "👥", 6, "users.view"),
            ("Vai trò & Phân quyền", "/admin/roles", "🛡️", 7, "roles.view"),
            ("Cấu hình Menu", "/admin/menus", "📑", 8, "menus.view")
        };

        var allMenus = new List<Menu>();
        foreach (var m in menusList)
        {
            var existing = await context.Menus.FirstOrDefaultAsync(x => x.RoutePath == m.RoutePath);
            if (existing == null)
            {
                existing = new Menu
                {
                    Title = m.Title,
                    RoutePath = m.RoutePath,
                    Icon = m.Icon,
                    SortOrder = m.SortOrder,
                    PermissionCode = m.PermissionCode,
                    IsActive = true
                };
                context.Menus.Add(existing);
            }
            allMenus.Add(existing);
        }
        await context.SaveChangesAsync();

        // Assign Menus to Roles
        var existingAdminMenus = await context.RoleMenus.Where(rm => rm.RoleId == superAdminRole.Id).ToListAsync();
        foreach (var menu in allMenus)
        {
            if (!existingAdminMenus.Any(rm => rm.MenuId == menu.Id))
            {
                context.RoleMenus.Add(new RoleMenu
                {
                    RoleId = superAdminRole.Id,
                    MenuId = menu.Id
                });
            }
        }

        var orgMenuPaths = new[] { "/admin", "/admin/events", "/admin/events/create", "/admin/orders" };
        var existingOrgMenus = await context.RoleMenus.Where(rm => rm.RoleId == organizerRole.Id).ToListAsync();
        foreach (var menu in allMenus.Where(m => orgMenuPaths.Contains(m.RoutePath)))
        {
            if (!existingOrgMenus.Any(rm => rm.MenuId == menu.Id))
            {
                context.RoleMenus.Add(new RoleMenu
                {
                    RoleId = organizerRole.Id,
                    MenuId = menu.Id
                });
            }
        }

        var staffMenuPaths = new[] { "/admin/scanner" };
        var existingStaffMenus = await context.RoleMenus.Where(rm => rm.RoleId == staffRole.Id).ToListAsync();
        foreach (var menu in allMenus.Where(m => staffMenuPaths.Contains(m.RoutePath)))
        {
            if (!existingStaffMenus.Any(rm => rm.MenuId == menu.Id))
            {
                context.RoleMenus.Add(new RoleMenu
                {
                    RoleId = staffRole.Id,
                    MenuId = menu.Id
                });
            }
        }

        // 5. Assign User Roles
        var adminUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == superAdminRole.Id);
        if (adminUserRole == null)
        {
            context.UserRoles.Add(new UserRoleMapping
            {
                UserId = adminUser.Id,
                RoleId = superAdminRole.Id
            });
        }

        var staffUserRole = await context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == staffUser.Id && ur.RoleId == staffRole.Id);
        if (staffUserRole == null)
        {
            context.UserRoles.Add(new UserRoleMapping
            {
                UserId = staffUser.Id,
                RoleId = staffRole.Id
            });
        }

        await context.SaveChangesAsync();
    }
}
