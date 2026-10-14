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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
        });

        // Event
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
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
            entity.ToTable("TicketTiers");
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
            entity.ToTable("Seats");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.EventId, s.RowName, s.SeatNumber }).IsUnique();
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
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OrderCode).IsUnique();
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
            entity.ToTable("Tickets");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TicketCode).IsUnique();
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
            entity.ToTable("CheckInLogs");
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.CheckInLogs)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ScannedByUser)
                .WithMany()
                .HasForeignKey(c => c.ScannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
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
