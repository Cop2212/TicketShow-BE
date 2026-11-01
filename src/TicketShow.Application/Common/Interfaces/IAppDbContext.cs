using Microsoft.EntityFrameworkCore;
using TicketShow.Domain.Entities;

namespace TicketShow.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Event> Events { get; }
    DbSet<TicketTier> TicketTiers { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Order> Orders { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<CheckInLog> CheckInLogs { get; }
    DbSet<Venue> Venues { get; }
    DbSet<VenueBooking> VenueBookings { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Menu> Menus { get; }
    DbSet<RoleMenu> RoleMenus { get; }
    DbSet<UserRoleMapping> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
