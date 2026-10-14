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
}
