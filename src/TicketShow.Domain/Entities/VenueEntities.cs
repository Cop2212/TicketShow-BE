using TicketShow.Domain.Common;
using TicketShow.Domain.Enums;

namespace TicketShow.Domain.Entities;

public class Venue : BaseEntity
{
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public int TotalCapacity { get; set; }

    /// <summary>
    /// JSON mô tả sơ đồ ghế gốc của địa điểm: các khu vực (Zones), số hàng (Rows), số ghế/hàng (SeatsPerRow)
    /// </summary>
    public string MasterLayoutJson { get; set; } = "{}";

    public VenueStatus Status { get; set; } = VenueStatus.Draft;

    public ICollection<VenueBooking> Bookings { get; set; } = new List<VenueBooking>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

public class VenueBooking : BaseEntity
{
    public Guid VenueId { get; set; }
    public Venue? Venue { get; set; }

    public Guid OrganizerId { get; set; }
    public User? Organizer { get; set; }

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    public string BookingCode { get; set; } = string.Empty; // e.g. VBK-20261031-XXXX
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public decimal PriceQuote { get; set; }
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }

    public VenueBookingStatus Status { get; set; } = VenueBookingStatus.Pending;
}
