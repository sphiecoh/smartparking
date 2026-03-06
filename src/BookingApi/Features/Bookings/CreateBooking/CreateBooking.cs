using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using BookingApi.Infrastructure.Data.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Features.Bookings.CreateBooking;

// ── DTOs ────────────────────────────────────────────────────────

public sealed record CreateBookingRequest(
    int CustomerId,
    int VehicleId,
    DateTime StartDate,
    DateTime EndDate,
    int? AgencyId,
    string? Voucher,
    List<int> ServiceIds   // services to attach; pricing resolved automatically
);

public sealed record CreateBookingResponse(
    int Id,
    string Status,
    decimal TotalServiceCharges,
    List<BookingServiceLineResponse> Lines
);

public sealed record BookingServiceLineResponse(
    int ServiceId,
    string ServiceName,
    int Days,
    decimal PricePerDay,
    decimal PriceCharged
);

// ── Command ──────────────────────────────────────────────────────

public sealed record CreateBookingCommand(
    int TenantId,
    CreateBookingRequest Request
) : IRequest<CreateBookingResponse>;

// ── Validator ────────────────────────────────────────────────────

public sealed class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.Request.CustomerId).GreaterThan(0);
        RuleFor(x => x.Request.VehicleId).GreaterThan(0);
        RuleFor(x => x.Request.StartDate).NotEmpty();
        RuleFor(x => x.Request.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.Request.StartDate)
            .WithMessage("EndDate must be after StartDate.");
        RuleFor(x => x.Request.ServiceIds)
            .NotEmpty().WithMessage("At least one service is required.");
    }
}

// ── Handler ──────────────────────────────────────────────────────

public sealed class CreateBookingHandler(AppDbContext db, ITenantContext tenant)
    : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<CreateBookingResponse> Handle(
        CreateBookingCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var days = (int)Math.Ceiling((req.EndDate - req.StartDate).TotalDays);

        // Resolve active pricing tiers for each requested service
        var lines = new List<BookingService>();

        foreach (var serviceId in req.ServiceIds)
        {
            // Find the active pricing schedule (most recent effective_from <= now)
            var pricing = await db.ServicePricings
                .Where(sp => sp.ServiceId == serviceId
                          && sp.TenantId == tenant.TenantId
                          && sp.EffectiveFrom <= DateTime.UtcNow)
                .OrderByDescending(sp => sp.EffectiveFrom)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException(
                    $"No active pricing found for service {serviceId} in tenant {tenant.TenantId}.");

            // Find the matching day-range tier
            var tier = await db.ServicePricingTiers
                .Where(t => t.ServicePricingId == pricing.Id
                         && t.DayFrom <= days
                         && (t.DayTo == null || t.DayTo >= days))
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException(
                    $"No pricing tier covers {days} days for service {serviceId}.");

            lines.Add(new BookingService
            {
                ServiceId             = serviceId,
                ServicePricingId      = pricing.Id,
                ServicePricingTierId  = tier.Id,
                Days                  = days,
                PricePerDay           = tier.PricePerDay,
                PriceCharged          = tier.PricePerDay * days
            });
        }

        var booking = new Booking
        {
            TenantId   = tenant.TenantId,
            CustomerId = req.CustomerId,
            VehicleId  = req.VehicleId,
            StartDate  = req.StartDate,
            EndDate    = req.EndDate,
            AgencyId   = req.AgencyId,
            Voucher    = req.Voucher,
            Status     = "Pending",
            CreatedAt  = DateTime.UtcNow,
            BookingServices = lines
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        // Load service names for the response
        var serviceIds = lines.Select(l => l.ServiceId).ToList();
        var serviceNames = await db.Services
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        return new CreateBookingResponse(
            booking.Id,
            booking.Status,
            lines.Sum(l => l.PriceCharged),
            lines.Select(l => new BookingServiceLineResponse(
                l.ServiceId,
                serviceNames.GetValueOrDefault(l.ServiceId, "Unknown"),
                l.Days,
                l.PricePerDay,
                l.PriceCharged)).ToList()
        );
    }
}

// ── Endpoint ─────────────────────────────────────────────────────

public sealed class CreateBookingEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/bookings", async (
            CreateBookingRequest request,
            ITenantContext tenant,
            IMediator mediator,
            IValidator<CreateBookingCommand> validator,
            CancellationToken ct) =>
        {
            var cmd = new CreateBookingCommand(tenant.TenantId, request);

            var validation = await validator.ValidateAsync(cmd, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(cmd, ct);
                return Results.Created($"/api/bookings/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateBooking")
        .WithTags("Bookings")
        .WithSummary("Create a new booking with automatic tier-based pricing resolution")
        .Produces<CreateBookingResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
