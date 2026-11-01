using TicketShow.Domain.Common;
using TicketShow.Domain.Enums;

namespace TicketShow.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public ICollection<UserRoleMapping> UserRoles { get; set; } = new List<UserRoleMapping>();
    public ICollection<Venue> OwnedVenues { get; set; } = new List<Venue>();
    public ICollection<VenueBooking> VenueBookings { get; set; } = new List<VenueBooking>();
}
