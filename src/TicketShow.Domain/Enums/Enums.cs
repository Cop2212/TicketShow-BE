namespace TicketShow.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Organizer = 2,
    Staff = 3,
    Customer = 4
}

public enum EventStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3,
    Finished = 4
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
