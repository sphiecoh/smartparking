using BookingApi.Common;
using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using BookingApi.Infrastructure.Data.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════
// CREATE VEHICLE
// ═══════════════════════════════════════════════════════════════
namespace BookingApi.Features.Vehicles;

public sealed record CreateVehicleRequest(
    string Reg, string? Make, string? Model, int? OwnerId);

public sealed record CreateVehicleResponse(
    int Id, string Reg, string? Make, string? Model, bool Active);

public sealed record CreateVehicleCommand(int TenantId, CreateVehicleRequest Request)
    : IRequest<CreateVehicleResponse>;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.Request.Reg).NotEmpty().MaximumLength(20);
    }
}

public sealed class CreateVehicleHandler(AppDbContext db)
    : IRequestHandler<CreateVehicleCommand, CreateVehicleResponse>
{
    public async Task<CreateVehicleResponse> Handle(CreateVehicleCommand cmd, CancellationToken ct)
    {
        var v = new Vehicle
        {
            Reg       = cmd.Request.Reg,
            Make      = cmd.Request.Make,
            Model     = cmd.Request.Model,
            OwnerId   = cmd.Request.OwnerId,
            TenantId  = cmd.TenantId,
            Active    = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Vehicles.Add(v);
        await db.SaveChangesAsync(ct);
        return new CreateVehicleResponse(v.Id, v.Reg, v.Make, v.Model, v.Active);
    }
}

public sealed class CreateVehicleEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/vehicles", async (
            CreateVehicleRequest request, ITenantContext tenant,
            IMediator mediator, IValidator<CreateVehicleCommand> validator, CancellationToken ct) =>
        {
            var cmd = new CreateVehicleCommand(tenant.TenantId, request);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/vehicles/{result.Id}", result);
        })
        .WithName("CreateVehicle").WithTags("Vehicles")
        .Produces<CreateVehicleResponse>(201).ProducesValidationProblem();
    }
}

// ═══════════════════════════════════════════════════════════════
// GET VEHICLE
// ═══════════════════════════════════════════════════════════════


public sealed record VehicleDetailResponse(
    int Id, string Reg, string? Make, string? Model,
    bool Active, string? OwnerName);

public sealed record GetVehicleQuery(int TenantId, int VehicleId)
    : IRequest<VehicleDetailResponse?>;

public sealed class GetVehicleHandler(AppDbContext db)
    : IRequestHandler<GetVehicleQuery, VehicleDetailResponse?>
{
    public async Task<VehicleDetailResponse?> Handle(GetVehicleQuery q, CancellationToken ct)
    {
        var v = await db.Vehicles
            .AsNoTracking()
            .Include(x => x.Owner)
            .Where(x => x.Id == q.VehicleId && x.TenantId == q.TenantId)
            .FirstOrDefaultAsync(ct);

        return v is null ? null
            : new VehicleDetailResponse(v.Id, v.Reg, v.Make, v.Model, v.Active, v.Owner?.Name);
    }
}

public sealed class GetVehicleEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/vehicles/{id:int}", async (
            int id, ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVehicleQuery(tenant.TenantId, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetVehicle").WithTags("Vehicles")
        .Produces<VehicleDetailResponse>().Produces(404);
    }
}

// ═══════════════════════════════════════════════════════════════
// LIST VEHICLES
// ═══════════════════════════════════════════════════════════════

public sealed record VehicleSummaryResponse(
    int Id, string Reg, string? Make, string? Model, bool Active, int? OwnerId);

public sealed record ListVehiclesQuery(int TenantId, bool? ActiveOnly, int Page, int PageSize)
    : IRequest<PagedResult<VehicleSummaryResponse>>;

public sealed class ListVehiclesHandler(AppDbContext db)
    : IRequestHandler<ListVehiclesQuery, PagedResult<VehicleSummaryResponse>>
{
    public async Task<PagedResult<VehicleSummaryResponse>> Handle(ListVehiclesQuery q, CancellationToken ct)
    {
        var query = db.Vehicles.AsNoTracking().Where(v => v.TenantId == q.TenantId);

        if (q.ActiveOnly == true)
            query = query.Where(v => v.Active);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.Reg)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(v => new VehicleSummaryResponse(v.Id, v.Reg, v.Make, v.Model, v.Active, v.OwnerId))
            .ToListAsync(ct);

        return new PagedResult<VehicleSummaryResponse>(items, total, q.Page, q.PageSize);
    }
}

public sealed class ListVehiclesEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/vehicles", async (
            ITenantContext tenant, IMediator mediator, CancellationToken ct,
            bool? activeOnly = null, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(
                new ListVehiclesQuery(tenant.TenantId, activeOnly, page, Math.Min(pageSize, 100)), ct)))
        .WithName("ListVehicles").WithTags("Vehicles")
        .Produces<PagedResult<VehicleSummaryResponse>>();
    }
}
