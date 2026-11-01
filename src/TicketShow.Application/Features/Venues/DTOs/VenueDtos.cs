using FluentValidation;
using MediatR;
using TicketShow.Application.Common.Models;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Venues.DTOs;

public record VenueSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Address,
    string City,
    string? ImageUrl,
    int TotalCapacity,
    VenueStatus Status,
    Guid OwnerId,
    string OwnerName
);

public record VenueDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Address,
    string City,
    string? ImageUrl,
    int TotalCapacity,
    string MasterLayoutJson,
    VenueStatus Status,
    Guid OwnerId,
    string OwnerName
);

public record CreateVenueCommand : IRequest<ApiResponse<VenueDetailDto>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int TotalCapacity { get; init; }
    public string MasterLayoutJson { get; init; } = "{}";
}

public class CreateVenueCommandValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Tên địa điểm không được để trống.");
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500).WithMessage("Địa chỉ không được để trống.");
        RuleFor(x => x.City).NotEmpty().MaximumLength(100).WithMessage("Tỉnh/Thành phố không được để trống.");
        RuleFor(x => x.TotalCapacity).GreaterThan(0).WithMessage("Sức chứa phải lớn hơn 0.");
        RuleFor(x => x.MasterLayoutJson).NotEmpty().WithMessage("Cấu hình sơ đồ ghế Master không được để trống.");
    }
}

public record UpdateVenueCommand : IRequest<ApiResponse<VenueDetailDto>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int TotalCapacity { get; init; }
    public string MasterLayoutJson { get; init; } = "{}";
}

public record ApproveVenueCommand(Guid VenueId, bool IsApproved, string? Reason = null) : IRequest<ApiResponse<bool>>;
