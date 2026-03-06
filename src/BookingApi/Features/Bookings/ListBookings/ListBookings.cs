using BookingApi.Common;
using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Features.Bookings.ListBookings;

// ── Response DTO ─────────────────────────────────────────────────

public sealed record BookingSummaryResponse(
    int Id,
    string? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    string? CustomerName,
    string? VehicleReg,
    decimal TotalCharged
);

// ── Query ────────────────────────────────────────────────────────

public sealed record ListBookingsQuery(
    int TenantId,
    string? Status,
    int? CustomerId,
    int? AgencyId,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page,
    int PageSize
) : IRequest<PagedResult<BookingSummaryResponse>>;

// ── Handler ──────────────────────────────────────────────────────

public sealed class ListBookingsHandler(AppDbContext db)
    : IRequestHandler<ListBookingsQuery, PagedResult<BookingSummaryResponse>>
{
    public async Task<PagedResult<BookingSummaryResponse>> Handle(
        ListBookingsQuery q, CancellationToken ct)
    {
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
            .Include(b => b.BookingServices)
            .Where(b => b.TenantId == q.TenantId);

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(b => b.Status == q.Status);

        if (q.CustomerId.HasValue)
            query = query.Where(b => b.CustomerId == q.CustomerId);

        if (q.AgencyId.HasValue)
            query = query.Where(b => b.AgencyId == q.AgencyId);

        if (q.FromDate.HasValue)
            query = query.Where(b => b.StartDate >= q.FromDate);

        if (q.ToDate.HasValue)
            query = query.Where(b => b.EndDate <= q.ToDate);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(b => new BookingSummaryResponse(
                b.Id,
                b.Status,
                b.StartDate,
                b.EndDate,
                b.Customer != null ? b.Customer.Name : null,
                b.Vehicle != null ? b.Vehicle.Reg : null,
                b.BookingServices.Sum(bs => bs.PriceCharged)
            ))
            .ToListAsync(ct);

        return new PagedResult<BookingSummaryResponse>(items, total, q.Page, q.PageSize);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────

public sealed class ListBookingsEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/bookings", async (
            ITenantContext tenant,
            IMediator mediator,
            CancellationToken ct,
            string? status = null,
            int? customerId = null,
            int? agencyId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await mediator.Send(
                new ListBookingsQuery(tenant.TenantId, status, customerId,
                    agencyId, fromDate, toDate, page, Math.Min(pageSize, 100)), ct);

            return Results.Ok(result);
        })
        .WithName("ListBookings")
        .WithTags("Bookings")
        .WithSummary("List bookings with optional filters for status, customer, agency and date range")
        .Produces<PagedResult<BookingSummaryResponse>>();
    }
}
