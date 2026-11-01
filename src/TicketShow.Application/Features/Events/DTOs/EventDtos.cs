using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Events.DTOs;

public record TicketTierDto(
    Guid Id,
    string Name,
    decimal Price,
    int TotalQuantity,
    int SoldQuantity,
    string ColorHex
);

public record SeatDto(
    Guid Id,
    Guid TicketTierId,
    string TierName,
    string RowName,
    string SeatNumber,
    string Status, // Available, Locked, Sold
    decimal Price,
    string ColorHex
);

public record EventSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? BannerUrl,
    string VenueName,
    string? VenueAddress,
    DateTime StartTime,
    DateTime EndTime,
    DateTime SalesStartTime,
    DateTime SalesEndTime,
    EventStatus Status,
    List<TicketTierDto> TicketTiers,
    Guid? VenueId = null,
    Guid? VenueBookingId = null,
    bool IsSeated = true,
    Guid OrganizerId = default,
    string OrganizerName = "N/A"
);

public record EventDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? BannerUrl,
    string VenueName,
    string? VenueAddress,
    DateTime StartTime,
    DateTime EndTime,
    DateTime SalesStartTime,
    DateTime SalesEndTime,
    EventStatus Status,
    List<TicketTierDto> TicketTiers,
    List<SeatDto> Seats,
    Guid? VenueId = null,
    Guid? VenueBookingId = null,
    bool IsSeated = true,
    Guid OrganizerId = default,
    string OrganizerName = "N/A"
);

public record CreateTicketTierInput(
    string Name,
    decimal Price,
    int TotalQuantity,
    string ColorHex
);

public record ZonePricingInput(
    string ZoneName,
    decimal Price,
    string? ColorHex = null
);

public record CreateEventCommand : IRequest<ApiResponse<EventDetailDto>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BannerUrl { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string? VenueAddress { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime SalesStartTime { get; init; }
    public DateTime SalesEndTime { get; init; }
    public List<CreateTicketTierInput> TicketTiers { get; init; } = new();
    public int? GenerateRowsCount { get; init; }
    public int? GenerateSeatsPerRow { get; init; }
    public Guid? VenueId { get; init; }
    public Guid? VenueBookingId { get; init; }
    public bool IsSeated { get; init; } = true;
    public EventStatus InitialStatus { get; init; } = EventStatus.Published;
}

public record CreateEventFromBookingCommand : IRequest<ApiResponse<EventDetailDto>>
{
    public Guid VenueBookingId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BannerUrl { get; init; }
    public DateTime SalesStartTime { get; init; }
    public DateTime SalesEndTime { get; init; }
    public List<ZonePricingInput> ZonePricings { get; init; } = new();
    public bool SubmitForApproval { get; init; } = false;
}

public record SubmitEventForApprovalCommand(Guid EventId) : IRequest<ApiResponse<bool>>;

public record ApproveEventCommand(Guid EventId, bool Approved, string? Reason = null) : IRequest<ApiResponse<bool>>;

public record CloseEventSalesCommand(Guid EventId) : IRequest<ApiResponse<bool>>;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.VenueName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.StartTime).GreaterThan(DateTime.UtcNow.AddMinutes(-5));
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
        RuleFor(x => x.SalesEndTime).LessThanOrEqualTo(x => x.StartTime);
        RuleFor(x => x.TicketTiers).NotEmpty().WithMessage("Phải có ít nhất 1 hạng vé.");
    }
}
