using BookingApi.Common;
using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using BookingApi.Infrastructure.Data.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════
// CREATE CUSTOMER
// ═══════════════════════════════════════════════════════════════
namespace BookingApi.Features.Customers;

public sealed record CreateCustomerRequest(string Name, string? Email);

public sealed record CreateCustomerResponse(int Id, string Name, string? Email);

public sealed record CreateCustomerCommand(int TenantId, CreateCustomerRequest Request)
    : IRequest<CreateCustomerResponse>;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Email).EmailAddress().When(x => x.Request.Email is not null);
    }
}

public sealed class CreateCustomerHandler(AppDbContext db)
    : IRequestHandler<CreateCustomerCommand, CreateCustomerResponse>
{
    public async Task<CreateCustomerResponse> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = new Customer
        {
            Name      = cmd.Request.Name,
            Email     = cmd.Request.Email,
            TenantId  = cmd.TenantId,
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return new CreateCustomerResponse(customer.Id, customer.Name, customer.Email);
    }
}

public sealed class CreateCustomerEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/customers", async (
            CreateCustomerRequest request,
            ITenantContext tenant,
            IMediator mediator,
            IValidator<CreateCustomerCommand> validator,
            CancellationToken ct) =>
        {
            var cmd = new CreateCustomerCommand(tenant.TenantId, request);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/customers/{result.Id}", result);
        })
        .WithName("CreateCustomer").WithTags("Customers")
        .Produces<CreateCustomerResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}

// ═══════════════════════════════════════════════════════════════
// GET CUSTOMER
// ═══════════════════════════════════════════════════════════════


public sealed record CustomerDetailResponse(
    int Id, string Name, string? Email,
    List<VehicleBrief> Vehicles);
public sealed record VehicleBrief(int Id, string Reg, string? Make, string? Model, bool Active);

public sealed record GetCustomerQuery(int TenantId, int CustomerId)
    : IRequest<CustomerDetailResponse?>;

public sealed class GetCustomerHandler(AppDbContext db)
    : IRequestHandler<GetCustomerQuery, CustomerDetailResponse?>
{
    public async Task<CustomerDetailResponse?> Handle(GetCustomerQuery q, CancellationToken ct)
    {
        var c = await db.Customers
            .AsNoTracking()
            .Include(x => x.Vehicles)
            .Where(x => x.Id == q.CustomerId && x.TenantId == q.TenantId)
            .FirstOrDefaultAsync(ct);

        return c is null ? null : new CustomerDetailResponse(
            c.Id, c.Name, c.Email,
            c.Vehicles.Select(v => new VehicleBrief(v.Id, v.Reg, v.Make, v.Model, v.Active)).ToList());
    }
}

public sealed class GetCustomerEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/customers/{id:int}", async (
            int id, ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCustomerQuery(tenant.TenantId, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetCustomer").WithTags("Customers")
        .Produces<CustomerDetailResponse>().Produces(404);
    }
}

// ═══════════════════════════════════════════════════════════════
// LIST CUSTOMERS
// ═══════════════════════════════════════════════════════════════


public sealed record CustomerSummaryResponse(int Id, string Name, string? Email);

public sealed record ListCustomersQuery(int TenantId, string? Search, int Page, int PageSize)
    : IRequest<PagedResult<CustomerSummaryResponse>>;

public sealed class ListCustomersHandler(AppDbContext db)
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerSummaryResponse>>
{
    public async Task<PagedResult<CustomerSummaryResponse>> Handle(ListCustomersQuery q, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().Where(c => c.TenantId == q.TenantId);

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(c => c.Name.Contains(q.Search) || (c.Email != null && c.Email.Contains(q.Search)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(c => new CustomerSummaryResponse(c.Id, c.Name, c.Email))
            .ToListAsync(ct);

        return new PagedResult<CustomerSummaryResponse>(items, total, q.Page, q.PageSize);
    }
}

public sealed class ListCustomersEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/customers", async (
            ITenantContext tenant, IMediator mediator, CancellationToken ct,
            string? search = null, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(
                new ListCustomersQuery(tenant.TenantId, search, page, Math.Min(pageSize, 100)), ct)))
        .WithName("ListCustomers").WithTags("Customers")
        .Produces<PagedResult<CustomerSummaryResponse>>();
    }
}
