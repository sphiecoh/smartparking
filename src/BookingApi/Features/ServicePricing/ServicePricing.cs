using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using BookingApi.Infrastructure.Data.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════
// CREATE SERVICE PRICING SCHEDULE (with tiers)
// ═══════════════════════════════════════════════════════════════
namespace BookingApi.Features.ServicePricing;

public sealed record PricingTierRequest(
    int DayFrom,
    int? DayTo,        // null = open-ended
    decimal PricePerDay
);

public sealed record CreatePricingRequest(
    int ServiceId,
    DateTime EffectiveFrom,
    string Currency,
    string? Note,
    List<PricingTierRequest> Tiers
);

public sealed record PricingTierResponse(
    int Id, int DayFrom, int? DayTo, decimal PricePerDay);

public sealed record CreatePricingResponse(
    int Id, int ServiceId, DateTime EffectiveFrom,
    string Currency, string? Note,
    List<PricingTierResponse> Tiers);

public sealed record CreatePricingCommand(int TenantId, int UserId, CreatePricingRequest Request)
    : IRequest<CreatePricingResponse>;

// ── Validator ────────────────────────────────────────────────────

public sealed class CreatePricingValidator : AbstractValidator<CreatePricingCommand>
{
    public CreatePricingValidator()
    {
        RuleFor(x => x.Request.ServiceId).GreaterThan(0);
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Request.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Request.Tiers).NotEmpty()
            .WithMessage("At least one pricing tier is required.");

        RuleForEach(x => x.Request.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.DayFrom).GreaterThan(0);
            tier.RuleFor(t => t.PricePerDay).GreaterThan(0);
            tier.RuleFor(t => t.DayTo)
                .GreaterThan(t => t.DayFrom)
                .When(t => t.DayTo.HasValue)
                .WithMessage("DayTo must be greater than DayFrom.");
        });

        // Tiers must not have duplicate DayFrom values
        RuleFor(x => x.Request.Tiers)
            .Must(tiers => tiers.Select(t => t.DayFrom).Distinct().Count() == tiers.Count)
            .WithMessage("Tier DayFrom values must be unique.");

        // Only one open-ended tier (DayTo = null) is allowed
        RuleFor(x => x.Request.Tiers)
            .Must(tiers => tiers.Count(t => t.DayTo == null) <= 1)
            .WithMessage("Only one open-ended tier (DayTo = null) is allowed.");
    }
}

// ── Handler ──────────────────────────────────────────────────────

public sealed class CreatePricingHandler(AppDbContext db)
    : IRequestHandler<CreatePricingCommand, CreatePricingResponse>
{
    public async Task<CreatePricingResponse> Handle(CreatePricingCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        var pricing = new Infrastructure.Data.Entities.ServicePricing
        {
            ServiceId     = req.ServiceId,
            TenantId      = cmd.TenantId,
            Currency      = req.Currency.ToUpper(),
            EffectiveFrom = req.EffectiveFrom,
            Note          = req.Note,
            CreatedAt     = DateTime.UtcNow,
            CreatedBy     = cmd.UserId,
            Tiers = req.Tiers
                .OrderBy(t => t.DayFrom)
                .Select(t => new ServicePricingTier
                {
                    DayFrom      = t.DayFrom,
                    DayTo        = t.DayTo,
                    PricePerDay  = t.PricePerDay,
                    CreatedAt    = DateTime.UtcNow,
                    CreatedBy    = cmd.UserId
                }).ToList()
        };

        db.ServicePricings.Add(pricing);
        await db.SaveChangesAsync(ct);

        return new CreatePricingResponse(
            pricing.Id, pricing.ServiceId, pricing.EffectiveFrom,
            pricing.Currency, pricing.Note,
            pricing.Tiers.Select(t => new PricingTierResponse(
                t.Id, t.DayFrom, t.DayTo, t.PricePerDay)).ToList());
    }
}

// ── Endpoint ─────────────────────────────────────────────────────

public sealed class CreatePricingEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/services/{serviceId:int}/pricing", async (
            int serviceId,
            CreatePricingRequest request,
            ITenantContext tenant,
            IMediator mediator,
            IValidator<CreatePricingCommand> validator,
            CancellationToken ct) =>
        {
            // Ensure the serviceId in the route matches the body
            var reqWithId = request with { ServiceId = serviceId };
            var cmd = new CreatePricingCommand(tenant.TenantId, 0, reqWithId);

            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());

            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/services/{serviceId}/pricing/{result.Id}", result);
        })
        .WithName("CreateServicePricing")
        .WithTags("Service Pricing")
        .WithSummary("Create a new pricing schedule with day-range tiers for a service. Becomes active on EffectiveFrom.")
        .Produces<CreatePricingResponse>(201)
        .ProducesValidationProblem();
    }
}

// ═══════════════════════════════════════════════════════════════
// GET ACTIVE PRICING for a service
// ═══════════════════════════════════════════════════════════════


public sealed record ActivePricingResponse(
    int Id, int ServiceId, string ServiceName,
    DateTime EffectiveFrom, string Currency, string? Note,
    List<TierDetail> Tiers);

public sealed record TierDetail(
    int Id, int DayFrom, int? DayTo, decimal PricePerDay,
    string Label  // e.g. "1-9 days" or "30+ days"
);

public sealed record GetActivePricingQuery(int TenantId, int ServiceId, DateTime? AsOf)
    : IRequest<ActivePricingResponse?>;

public sealed class GetActivePricingHandler(AppDbContext db)
    : IRequestHandler<GetActivePricingQuery, ActivePricingResponse?>
{
    public async Task<ActivePricingResponse?> Handle(GetActivePricingQuery q, CancellationToken ct)
    {
        var asOf = q.AsOf ?? DateTime.UtcNow;

        var pricing = await db.ServicePricings
            .AsNoTracking()
            .Include(p => p.Service)
            .Include(p => p.Tiers.OrderBy(t => t.DayFrom))
            .Where(p => p.ServiceId == q.ServiceId
                     && p.TenantId == q.TenantId
                     && p.EffectiveFrom <= asOf)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (pricing is null) return null;

        return new ActivePricingResponse(
            pricing.Id, pricing.ServiceId, pricing.Service.Name,
            pricing.EffectiveFrom, pricing.Currency, pricing.Note,
            pricing.Tiers.Select(t => new TierDetail(
                t.Id, t.DayFrom, t.DayTo, t.PricePerDay,
                t.DayTo.HasValue
                    ? $"{t.DayFrom}–{t.DayTo} days"
                    : $"{t.DayFrom}+ days"
            )).ToList());
    }
}

public sealed class GetActivePricingEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/services/{serviceId:int}/pricing/active", async (
            int serviceId, ITenantContext tenant, IMediator mediator,
            CancellationToken ct, DateTime? asOf = null) =>
        {
            var result = await mediator.Send(
                new GetActivePricingQuery(tenant.TenantId, serviceId, asOf), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetActiveServicePricing")
        .WithTags("Service Pricing")
        .WithSummary("Get the currently active pricing schedule and tiers for a service. Pass asOf to query historical state.")
        .Produces<ActivePricingResponse>().Produces(404);
    }
}

// ═══════════════════════════════════════════════════════════════
// GET PRICING HISTORY for a service
// ═══════════════════════════════════════════════════════════════


public sealed record PricingHistoryEntry(
    int Id, DateTime EffectiveFrom, string Currency,
    string? Note, int TierCount, DateTime? CreatedAt);

public sealed record GetPricingHistoryQuery(int TenantId, int ServiceId)
    : IRequest<List<PricingHistoryEntry>>;

public sealed class GetPricingHistoryHandler(AppDbContext db)
    : IRequestHandler<GetPricingHistoryQuery, List<PricingHistoryEntry>>
{
    public async Task<List<PricingHistoryEntry>> Handle(GetPricingHistoryQuery q, CancellationToken ct) =>
        await db.ServicePricings
            .AsNoTracking()
            .Include(p => p.Tiers)
            .Where(p => p.ServiceId == q.ServiceId && p.TenantId == q.TenantId)
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => new PricingHistoryEntry(
                p.Id, p.EffectiveFrom, p.Currency,
                p.Note, p.Tiers.Count, p.CreatedAt))
            .ToListAsync(ct);
}

public sealed class GetPricingHistoryEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/services/{serviceId:int}/pricing/history", async (
            int serviceId, ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(
                new GetPricingHistoryQuery(tenant.TenantId, serviceId), ct)))
        .WithName("GetServicePricingHistory")
        .WithTags("Service Pricing")
        .WithSummary("Full pricing change history for a service, newest first")
        .Produces<List<PricingHistoryEntry>>();
    }
}
