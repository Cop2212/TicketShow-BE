using FluentValidation;
using MediatR;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.VenueBookings.DTOs;

public record VenueBookingSummaryDto(
    Guid Id,
    string BookingCode,
    Guid VenueId,
    string VenueName,
    string VenueAddress,
    string VenueCity,
    Guid OrganizerId,
    string OrganizerName,
    DateTime StartTime,
    DateTime EndTime,
    decimal PriceQuote,
    string? Note,
    string? RejectionReason,
    VenueBookingStatus Status,
    Guid? EventId,
    string? EventTitle
);

public record SlotAvailabilityResultDto(
    bool IsAvailable,
    string Message,
    DateTime StartTime,
    DateTime EndTime,
    string? ConflictingBookingCode = null
);

public record CheckSlotAvailabilityQuery(
    Guid VenueId,
    DateTime StartTime,
    DateTime EndTime
) : IRequest<ApiResponse<SlotAvailabilityResultDto>>;

public record RequestVenueBookingCommand : IRequest<ApiResponse<VenueBookingSummaryDto>>
{
    public Guid VenueId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public decimal PriceQuote { get; init; }
    public string? Note { get; init; }
}

public class RequestVenueBookingCommandValidator : AbstractValidator<RequestVenueBookingCommand>
{
    public RequestVenueBookingCommandValidator()
    {
        RuleFor(x => x.VenueId).NotEmpty().WithMessage("Địa điểm không được để trống.");
        RuleFor(x => x.StartTime).GreaterThan(DateTime.UtcNow).WithMessage("Thời gian bắt đầu thuê phải ở tương lai.");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        RuleFor(x => x.PriceQuote).GreaterThanOrEqualTo(0).WithMessage("Giá đề xuất không hợp lệ.");
    }
}

public record RespondVenueBookingCommand(
    Guid BookingId,
    bool Accept,
    string? Reason = null
) : IRequest<ApiResponse<VenueBookingSummaryDto>>;
