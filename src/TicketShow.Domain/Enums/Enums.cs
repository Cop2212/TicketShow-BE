namespace TicketShow.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Organizer = 2,
    Staff = 3,
    Customer = 4,
    VenueOwner = 5
}

public enum EventStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3,
    Finished = 4,
    PendingApproval = 5,
    Cancelled = 6
}

public enum VenueStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Suspended = 4
}

public enum VenueBookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum SeatStatus
{
    Available = 1,
    Locked = 2,
    Sold = 3
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum TicketStatus
{
    Valid = 1,
    CheckedIn = 2,
    Cancelled = 3
}

public enum CheckInStatus
{
    Success = 1,
    AlreadyUsed = 2,
    Invalid = 3,
    WrongEvent = 4
}
