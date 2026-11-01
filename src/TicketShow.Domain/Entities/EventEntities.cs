using TicketShow.Domain.Common;
using TicketShow.Domain.Enums;

namespace TicketShow.Domain.Entities;

public class Event : BaseEntity
{
    public Guid OrganizerId { get; set; }
    public User? Organizer { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BannerUrl { get; set; }
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public Guid? VenueBookingId { get; set; }
    public VenueBooking? VenueBooking { get; set; }

    public bool IsSeated { get; set; } = true;

    public string VenueName { get; set; } = string.Empty;
    public string? VenueAddress { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime SalesStartTime { get; set; }
    public DateTime SalesEndTime { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    public ICollection<TicketTier> TicketTiers { get; set; } = new List<TicketTier>();
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class TicketTier : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public string Name { get; set; } = string.Empty; // e.g. VIP, Standard, Zone A
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; } = 0;
    public string ColorHex { get; set; } = "#8b5cf6"; // For SVG Seat map rendering

    public uint Version { get; set; } // Concurrency token xmin

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Seat : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid TicketTierId { get; set; }
    public TicketTier? TicketTier { get; set; }

    public string RowName { get; set; } = string.Empty; // e.g. A, B, C...
    public string SeatNumber { get; set; } = string.Empty; // e.g. 01, 02...
    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public DateTime? LockedUntil { get; set; }
    public Guid? LockedByUserId { get; set; }
    public string? LockedBySessionId { get; set; }

    public Ticket? Ticket { get; set; }
}

public class Order : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty; // e.g. ORD-20261011-XXXX
    public Guid CustomerId { get; set; }
    public User? Customer { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentGateway { get; set; } // VNPAY, MOMO, QRPAY
    public string? TransactionId { get; set; }
    public DateTime ExpiresAt { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Ticket : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid TicketTierId { get; set; }
    public TicketTier? TicketTier { get; set; }

    public Guid? SeatId { get; set; }
    public Seat? Seat { get; set; }

    public string TicketCode { get; set; } = string.Empty; // Unique code e.g. TICK-17285...
    public string QrCodeSignature { get; set; } = string.Empty; // HMAC-SHA256 signature
    public TicketStatus Status { get; set; } = TicketStatus.Valid;

    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedByStaffId { get; set; }
    public User? CheckedByStaff { get; set; }

    public ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();
}

public class CheckInLog : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public Guid? ScannedByUserId { get; set; }
    public User? ScannedByUser { get; set; }

    public CheckInStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
