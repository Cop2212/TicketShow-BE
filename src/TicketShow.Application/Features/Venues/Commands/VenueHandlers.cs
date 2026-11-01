using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Interfaces;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Venues.DTOs;
using TicketShow.Domain.Entities;
using TicketShow.Domain.Enums;

namespace TicketShow.Application.Features.Venues.Commands;

public record GetVenuesQuery(
    VenueStatus? Status = null,
    string? City = null,
    string? Search = null,
    bool MyVenuesOnly = false
) : IRequest<ApiResponse<List<VenueSummaryDto>>>;

public record GetVenueDetailQuery(string IdOrSlug) : IRequest<ApiResponse<VenueDetailDto>>;

public class VenueHandlers :
    IRequestHandler<GetVenuesQuery, ApiResponse<List<VenueSummaryDto>>>,
    IRequestHandler<GetVenueDetailQuery, ApiResponse<VenueDetailDto>>,
    IRequestHandler<CreateVenueCommand, ApiResponse<VenueDetailDto>>,
    IRequestHandler<UpdateVenueCommand, ApiResponse<VenueDetailDto>>,
    IRequestHandler<ApproveVenueCommand, ApiResponse<bool>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public VenueHandlers(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<VenueSummaryDto>>> Handle(GetVenuesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Venues
            .Include(v => v.Owner)
            .AsNoTracking()
            .AsQueryable();

        if (request.MyVenuesOnly && _currentUser.UserId.HasValue)
        {
            query = query.Where(v => v.OwnerId == _currentUser.UserId.Value);
        }
        else if (request.Status.HasValue)
        {
            query = query.Where(v => v.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(v => v.City.ToLower().Contains(request.City.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(search) || v.Address.ToLower().Contains(search));
        }

        var venues = await query
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VenueSummaryDto(
                v.Id,
                v.Name,
                v.Slug,
                v.Description,
                v.Address,
                v.City,
                v.ImageUrl,
                v.TotalCapacity,
                v.Status,
                v.OwnerId,
                v.Owner != null ? v.Owner.FullName : "N/A"
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<VenueSummaryDto>>.Ok(venues, "Lấy danh sách địa điểm thành công.");
    }

    public async Task<ApiResponse<VenueDetailDto>> Handle(GetVenueDetailQuery request, CancellationToken cancellationToken)
    {
        var isGuid = Guid.TryParse(request.IdOrSlug, out var venueId);

        var v = await _context.Venues
            .Include(x => x.Owner)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => (isGuid && x.Id == venueId) || x.Slug == request.IdOrSlug, cancellationToken);

        if (v == null)
            throw new NotFoundException("Địa điểm", request.IdOrSlug);

        var dto = new VenueDetailDto(
            v.Id,
            v.Name,
            v.Slug,
            v.Description,
            v.Address,
            v.City,
            v.ImageUrl,
            v.TotalCapacity,
            v.MasterLayoutJson,
            v.Status,
            v.OwnerId,
            v.Owner != null ? v.Owner.FullName : "N/A"
        );

        return ApiResponse<VenueDetailDto>.Ok(dto, "Lấy chi tiết địa điểm thành công.");
    }

    public async Task<ApiResponse<VenueDetailDto>> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId;
        if (!ownerId.HasValue)
        {
            var fallbackUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.VenueOwner || u.Role == UserRole.Admin, cancellationToken);
            ownerId = fallbackUser?.Id ?? Guid.NewGuid();
        }

        var baseSlug = GenerateSlug(request.Name);
        var slug = baseSlug;
        var count = 1;
        while (await _context.Venues.AnyAsync(v => v.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{count++}";
        }

        var venue = new Venue
        {
            OwnerId = ownerId.Value,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            TotalCapacity = request.TotalCapacity,
            MasterLayoutJson = string.IsNullOrWhiteSpace(request.MasterLayoutJson) ? "{}" : request.MasterLayoutJson,
            Status = VenueStatus.PendingApproval
        };

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync(cancellationToken);

        return await Handle(new GetVenueDetailQuery(venue.Id.ToString()), cancellationToken);
    }

    public async Task<ApiResponse<VenueDetailDto>> Handle(UpdateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);
        if (venue == null)
            throw new NotFoundException("Địa điểm", request.Id);

        venue.Name = request.Name.Trim();
        venue.Description = request.Description?.Trim();
        venue.Address = request.Address.Trim();
        venue.City = request.City.Trim();
        venue.ImageUrl = request.ImageUrl?.Trim();
        venue.TotalCapacity = request.TotalCapacity;
        if (!string.IsNullOrWhiteSpace(request.MasterLayoutJson))
        {
            venue.MasterLayoutJson = request.MasterLayoutJson;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await Handle(new GetVenueDetailQuery(venue.Id.ToString()), cancellationToken);
    }

    public async Task<ApiResponse<bool>> Handle(ApproveVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId, cancellationToken);
        if (venue == null)
            throw new NotFoundException("Địa điểm", request.VenueId);

        venue.Status = request.IsApproved ? VenueStatus.Approved : VenueStatus.Suspended;
        await _context.SaveChangesAsync(cancellationToken);

        var msg = request.IsApproved ? "Phê duyệt địa điểm lên sàn thành công." : "Đã tạm dừng / từ chối địa điểm.";
        return ApiResponse<bool>.Ok(true, msg);
    }

    private static string GenerateSlug(string name)
    {
        var str = name.ToLowerInvariant();
        str = Regex.Replace(str, @"[áàảãạâấầẩẫậăắằẳẵặ]", "a");
        str = Regex.Replace(str, @"[éèẻẽẹêếềểễệ]", "e");
        str = Regex.Replace(str, @"[íìỉĩị]", "i");
        str = Regex.Replace(str, @"[óòỏõọôốồổỗộơớờởỡợ]", "o");
        str = Regex.Replace(str, @"[úùủũụưứừửữự]", "u");
        str = Regex.Replace(str, @"[ýỳỷỹỵ]", "y");
        str = Regex.Replace(str, @"[đ]", "d");
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = Regex.Replace(str, @"\s+", " ").Trim();
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }
}
