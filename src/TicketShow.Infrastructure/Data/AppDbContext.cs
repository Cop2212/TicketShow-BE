using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Domain.Entities;

namespace TicketShow.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketTier> TicketTiers => Set<TicketTier>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<CheckInLog> CheckInLogs => Set<CheckInLog>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<UserRoleMapping> UserRoles => Set<UserRoleMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
        });

        // Event
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
            entity.Property(e => e.VenueName).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TicketTier
        modelBuilder.Entity<TicketTier>(entity =>
        {
            entity.ToTable("ticket_types");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Price).HasPrecision(18, 2);
            entity.Property(t => t.ColorHex).HasMaxLength(20);

            entity.Property(t => t.Version).IsRowVersion();

            entity.HasOne(t => t.Event)
                .WithMany(e => e.TicketTiers)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seat
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("seats");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.EventId, s.RowName, s.SeatNumber }).IsUnique();
            entity.HasIndex(s => new { s.EventId, s.Status });
            entity.Property(s => s.RowName).IsRequired().HasMaxLength(10);
            entity.Property(s => s.SeatNumber).IsRequired().HasMaxLength(10);

            entity.HasOne(s => s.Event)
                .WithMany(e => e.Seats)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.TicketTier)
                .WithMany(t => t.Seats)
                .HasForeignKey(s => s.TicketTierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.HasIndex(o => new { o.CustomerId, o.PaymentStatus });
            entity.Property(o => o.OrderCode).IsRequired().HasMaxLength(50);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(150);

            entity.HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Ticket
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TicketCode).IsUnique();
            entity.HasIndex(t => new { t.EventId, t.Status });
            entity.HasIndex(t => t.OrderId);
            entity.Property(t => t.TicketCode).IsRequired().HasMaxLength(50);
            entity.Property(t => t.QrCodeSignature).IsRequired();

            entity.HasOne(t => t.Order)
                .WithMany(o => o.Tickets)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Event)
                .WithMany(e => e.Tickets)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.TicketTier)
                .WithMany(tt => tt.Tickets)
                .HasForeignKey(t => t.TicketTierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Seat)
                .WithOne(s => s.Ticket)
                .HasForeignKey<Ticket>(t => t.SeatId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.CheckedByStaff)
                .WithMany()
                .HasForeignKey(t => t.CheckedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CheckInLog
        modelBuilder.Entity<CheckInLog>(entity =>
        {
            entity.ToTable("check_in_logs");
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.TicketId);

            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.CheckInLogs)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ScannedByUser)
                .WithMany()
                .HasForeignKey(c => c.ScannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.Code).IsUnique();
            entity.Property(r => r.Code).IsRequired().HasMaxLength(50);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(255);
        });

        // Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Code).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Module).IsRequired().HasMaxLength(50);
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(rp => rp.Id);
            entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Menu
        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("menus");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(100);
            entity.Property(m => m.RoutePath).IsRequired().HasMaxLength(150);
            entity.Property(m => m.Icon).HasMaxLength(50);
            entity.Property(m => m.PermissionCode).HasMaxLength(100);

            entity.HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RoleMenu
        modelBuilder.Entity<RoleMenu>(entity =>
        {
            entity.ToTable("role_menus");
            entity.HasKey(rm => rm.Id);
            entity.HasIndex(rm => new { rm.RoleId, rm.MenuId }).IsUnique();

            entity.HasOne(rm => rm.Role)
                .WithMany(r => r.RoleMenus)
                .HasForeignKey(rm => rm.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rm => rm.Menu)
                .WithMany(m => m.RoleMenus)
                .HasForeignKey(rm => rm.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRoleMapping
        modelBuilder.Entity<UserRoleMapping>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(ur => ur.Id);
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<TicketShow.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
