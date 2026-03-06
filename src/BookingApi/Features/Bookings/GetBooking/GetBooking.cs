using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Features.Bookings.GetBooking;

// ── Response DTO ─────────────────────────────────────────────────

public sealed record BookingDetailResponse(
    int Id,
    string? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Voucher,
    CustomerSummary? Customer,
    VehicleSummary? Vehicle,
    AgencySummary? Agency,
    List<BookingServiceDetail> Services,
    List<PaymentSummary> Payments,
    decimal TotalCharged,
    decimal TotalPaid
);

public sealed record CustomerSummary(int Id, string Name, string? Email);
public sealed record VehicleSummary(int Id, string Reg, string? Make, string? Model);
public sealed record AgencySummary(int Id, string Name);
public sealed record BookingServiceDetail(
    int ServiceId, string ServiceName,
    int Days, decimal PricePerDay, decimal PriceCharged,
    string Currency);
public sealed record PaymentSummary(int Id, decimal? Amount, DateTime? CreatedAt);

// ── Query ────────────────────────────────────────────────────────

public sealed record GetBookingQuery(int TenantId, int BookingId)
    : IRequest<BookingDetailResponse?>;

// ── Handler ──────────────────────────────────────────────────────

public sealed class GetBookingHandler(AppDbContext db)
    : IRequestHandler<GetBookingQuery, BookingDetailResponse?>
{
    public async Task<BookingDetailResponse?> Handle(
        GetBookingQuery query, CancellationToken ct)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Vehicle)
            .Include(b => b.Agency)
            .Include(b => b.Payments)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.ServicePricing)
            .Where(b => b.Id == query.BookingId && b.TenantId == query.TenantId)
            .FirstOrDefaultAsync(ct);

        if (booking is null) return null;

        return new BookingDetailResponse(
            booking.Id,
            booking.Status,
            booking.StartDate,
            booking.EndDate,
            booking.Voucher,
            booking.Customer is null ? null
                : new CustomerSummary(booking.Customer.Id, booking.Customer.Name, booking.Customer.Email),
            booking.Vehicle is null ? null
                : new VehicleSummary(booking.Vehicle.Id, booking.Vehicle.Reg, booking.Vehicle.Make, booking.Vehicle.Model),
            booking.Agency is null ? null
                : new AgencySummary(booking.Agency.Id, booking.Agency.Name),
            booking.BookingServices.Select(bs => new BookingServiceDetail(
                bs.ServiceId,
                bs.Service.Name,
                bs.Days,
                bs.PricePerDay,
                bs.PriceCharged,
                bs.ServicePricing.Currency)).ToList(),
            booking.Payments.Select(p => new PaymentSummary(p.Id, p.Amount, p.CreatedAt)).ToList(),
            booking.BookingServices.Sum(bs => bs.PriceCharged),
            booking.Payments.Sum(p => p.Amount)
        );
    }
}

// ── Endpoint ─────────────────────────────────────────────────────

public sealed class GetBookingEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/bookings/{id:int}", async (
            int id,
            ITenantContext tenant,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBookingQuery(tenant.TenantId, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetBooking")
        .WithTags("Bookings")
        .WithSummary("Get a booking by ID including services, payments and totals")
        .Produces<BookingDetailResponse>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
