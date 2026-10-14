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
    List<TicketTierDto> TicketTiers
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
    List<SeatDto> Seats
);

public record CreateTicketTierInput(
    string Name,
    decimal Price,
    int TotalQuantity,
    string ColorHex
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
    public int? GenerateRowsCount { get; init; } // e.g. 5 rows (A..E)
    public int? GenerateSeatsPerRow { get; init; } // e.g. 10 seats (1..10)
}

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
