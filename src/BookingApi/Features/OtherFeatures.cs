using BookingApi.Common;
using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using BookingApi.Infrastructure.Data.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════
// SERVICES
// ═══════════════════════════════════════════════════════════════
namespace BookingApi.Features.Services;

public sealed record CreateServiceRequest(string Name);
public sealed record CreateServiceResponse(int Id, string Name);
public sealed record CreateServiceCommand(int TenantId, CreateServiceRequest Request)
    : IRequest<CreateServiceResponse>;

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceValidator() =>
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreateServiceHandler(AppDbContext db)
    : IRequestHandler<CreateServiceCommand, CreateServiceResponse>
{
    public async Task<CreateServiceResponse> Handle(CreateServiceCommand cmd, CancellationToken ct)
    {
        var s = new Service { Name = cmd.Request.Name, TenantId = cmd.TenantId, CreatedAt = DateTime.UtcNow };
        db.Services.Add(s);
        await db.SaveChangesAsync(ct);
        return new CreateServiceResponse(s.Id, s.Name);
    }
}

public sealed class CreateServiceEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/services", async (
            CreateServiceRequest request, ITenantContext tenant,
            IMediator mediator, IValidator<CreateServiceCommand> validator, CancellationToken ct) =>
        {
            var cmd = new CreateServiceCommand(tenant.TenantId, request);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/services/{result.Id}", result);
        })
        .WithName("CreateService").WithTags("Services")
        .Produces<CreateServiceResponse>(201).ProducesValidationProblem();
    }
}


public sealed record ServiceSummaryResponse(int Id, string Name, bool HasActivePricing);
public sealed record ListServicesQuery(int TenantId) : IRequest<List<ServiceSummaryResponse>>;

public sealed class ListServicesHandler(AppDbContext db)
    : IRequestHandler<ListServicesQuery, List<ServiceSummaryResponse>>
{
    public async Task<List<ServiceSummaryResponse>> Handle(ListServicesQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.Services.AsNoTracking()
            .Where(s => s.TenantId == q.TenantId)
            .Select(s => new ServiceSummaryResponse(
                s.Id, s.Name,
                s.Pricings.Any(p => p.EffectiveFrom <= now)))
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }
}

public sealed class ListServicesEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/services", async (ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListServicesQuery(tenant.TenantId), ct)))
        .WithName("ListServices").WithTags("Services")
        .Produces<List<ServiceSummaryResponse>>();
    }
}

// ═══════════════════════════════════════════════════════════════
// AGENCIES
// ═══════════════════════════════════════════════════════════════


public sealed record CreateAgencyRequest(
    string Name, string? AddressLine1, string? AddressLine2,
    string? AddressLine3, string? AddressLine4, decimal? Rate);

public sealed record CreateAgencyResponse(int Id, string Name, bool Active);
public sealed record CreateAgencyCommand(int TenantId, CreateAgencyRequest Request)
    : IRequest<CreateAgencyResponse>;

public sealed class CreateAgencyValidator : AbstractValidator<CreateAgencyCommand>
{
    public CreateAgencyValidator() =>
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreateAgencyHandler(AppDbContext db)
    : IRequestHandler<CreateAgencyCommand, CreateAgencyResponse>
{
    public async Task<CreateAgencyResponse> Handle(CreateAgencyCommand cmd, CancellationToken ct)
    {
        var a = new Agency
        {
            Name         = cmd.Request.Name,
            TenantId     = cmd.TenantId,
            AddressLine1 = cmd.Request.AddressLine1,
            AddressLine2 = cmd.Request.AddressLine2,
            AddressLine3 = cmd.Request.AddressLine3,
            AddressLine4 = cmd.Request.AddressLine4,
            Rate         = cmd.Request.Rate,
            Active       = true,
            Status       = "Active",
            CreatedAt    = DateTime.UtcNow
        };
        db.Agencies.Add(a);
        await db.SaveChangesAsync(ct);
        return new CreateAgencyResponse(a.Id, a.Name, a.Active);
    }
}

public sealed class CreateAgencyEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/agencies", async (
            CreateAgencyRequest request, ITenantContext tenant,
            IMediator mediator, IValidator<CreateAgencyCommand> validator, CancellationToken ct) =>
        {
            var cmd = new CreateAgencyCommand(tenant.TenantId, request);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/agencies/{result.Id}", result);
        })
        .WithName("CreateAgency").WithTags("Agencies")
        .Produces<CreateAgencyResponse>(201).ProducesValidationProblem();
    }
}



public sealed record AgencySummaryResponse(
    int Id, string Name, bool Active, decimal? Credits, string? Status);

public sealed record ListAgenciesQuery(int TenantId, bool? ActiveOnly)
    : IRequest<List<AgencySummaryResponse>>;

public sealed class ListAgenciesHandler(AppDbContext db)
    : IRequestHandler<ListAgenciesQuery, List<AgencySummaryResponse>>
{
    public async Task<List<AgencySummaryResponse>> Handle(ListAgenciesQuery q, CancellationToken ct)
    {
        var query = db.Agencies.AsNoTracking().Where(a => a.TenantId == q.TenantId);
        if (q.ActiveOnly == true) query = query.Where(a => a.Active);
        return await query
            .OrderBy(a => a.Name)
            .Select(a => new AgencySummaryResponse(a.Id, a.Name, a.Active, a.Credits, a.Status))
            .ToListAsync(ct);
    }
}

public sealed class ListAgenciesEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/agencies", async (
            ITenantContext tenant, IMediator mediator, CancellationToken ct, bool? activeOnly = null) =>
            Results.Ok(await mediator.Send(new ListAgenciesQuery(tenant.TenantId, activeOnly), ct)))
        .WithName("ListAgencies").WithTags("Agencies")
        .Produces<List<AgencySummaryResponse>>();
    }
}

// ═══════════════════════════════════════════════════════════════
// PAYMENTS
// ═══════════════════════════════════════════════════════════════


public sealed record CreatePaymentRequest(decimal Amount);
public sealed record CreatePaymentResponse(int Id, decimal Amount, DateTime CreatedAt);
public sealed record CreatePaymentCommand(int TenantId, int BookingId, decimal Amount)
    : IRequest<CreatePaymentResponse>;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator() =>
        RuleFor(x => x.Amount).GreaterThan(0);
}

public sealed class CreatePaymentHandler(AppDbContext db)
    : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        var bookingExists = await db.Bookings
            .AnyAsync(b => b.Id == cmd.BookingId && b.TenantId == cmd.TenantId, ct);

        if (!bookingExists)
            throw new KeyNotFoundException($"Booking {cmd.BookingId} not found.");

        var payment = new Payment
        {
            BookingId = cmd.BookingId,
            TenantId  = cmd.TenantId,
            Amount    = cmd.Amount,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return new CreatePaymentResponse(payment.Id, payment.Amount!, payment.CreatedAt!.Value);
    }
}

public sealed class CreatePaymentEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/bookings/{bookingId:int}/payments", async (
            int bookingId, CreatePaymentRequest request, ITenantContext tenant,
            IMediator mediator, IValidator<CreatePaymentCommand> validator, CancellationToken ct) =>
        {
            var cmd = new CreatePaymentCommand(tenant.TenantId, bookingId, request.Amount);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            try
            {
                var result = await mediator.Send(cmd, ct);
                return Results.Created($"/api/bookings/{bookingId}/payments/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        })
        .WithName("CreatePayment").WithTags("Payments")
        .Produces<CreatePaymentResponse>(201).ProducesValidationProblem().Produces(404);
    }
}

public sealed record PaymentResponse(int Id, decimal? Amount, DateTime? CreatedAt);
public sealed record BookingPaymentSummary(
    List<PaymentResponse> Payments, decimal TotalPaid, decimal TotalCharged, decimal Balance);

public sealed record GetBookingPaymentsQuery(int TenantId, int BookingId)
    : IRequest<BookingPaymentSummary?>;

public sealed class GetBookingPaymentsHandler(AppDbContext db)
    : IRequestHandler<GetBookingPaymentsQuery, BookingPaymentSummary?>
{
    public async Task<BookingPaymentSummary?> Handle(GetBookingPaymentsQuery q, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.Payments)
            .Include(b => b.BookingServices)
            .Where(b => b.Id == q.BookingId && b.TenantId == q.TenantId)
            .FirstOrDefaultAsync(ct);

        if (booking is null) return null;

        var totalPaid = booking.Payments.Sum(p => p.Amount);
        var totalCharged = booking.BookingServices.Sum(bs => bs.PriceCharged);

        return new BookingPaymentSummary(
            booking.Payments.Select(p => new PaymentResponse(p.Id, p.Amount, p.CreatedAt)).ToList(),
            totalPaid, totalCharged, totalCharged - totalPaid);
    }
}

public sealed class GetBookingPaymentsEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/bookings/{bookingId:int}/payments", async (
            int bookingId, ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBookingPaymentsQuery(tenant.TenantId, bookingId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetBookingPayments").WithTags("Payments")
        .Produces<BookingPaymentSummary>().Produces(404);
    }
}

// ═══════════════════════════════════════════════════════════════
// VAT PROFILES
// ═══════════════════════════════════════════════════════════════


public sealed record CreateVatProfileRequest(string Name, decimal Rate);
public sealed record CreateVatProfileResponse(int Id, string Name, decimal Rate, bool Active);
public sealed record CreateVatProfileCommand(int TenantId, CreateVatProfileRequest Request)
    : IRequest<CreateVatProfileResponse>;

public sealed class CreateVatProfileValidator : AbstractValidator<CreateVatProfileCommand>
{
    public CreateVatProfileValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty();
        RuleFor(x => x.Request.Rate).InclusiveBetween(0, 1)
            .WithMessage("Rate must be between 0 and 1 (e.g. 0.15 for 15%).");
    }
}

public sealed class CreateVatProfileHandler(AppDbContext db)
    : IRequestHandler<CreateVatProfileCommand, CreateVatProfileResponse>
{
    public async Task<CreateVatProfileResponse> Handle(CreateVatProfileCommand cmd, CancellationToken ct)
    {
        var vp = new VatProfile
        {
            TenantId  = cmd.TenantId,
            Name      = cmd.Request.Name,
            Rate      = cmd.Request.Rate,
            Active    = true,
            CreatedAt = DateTime.UtcNow
        };
        db.VatProfiles.Add(vp);
        await db.SaveChangesAsync(ct);
        return new CreateVatProfileResponse(vp.Id, vp.Name, vp.Rate, vp.Active);
    }
}

public sealed class CreateVatProfileEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/vat-profiles", async (
            CreateVatProfileRequest request, ITenantContext tenant,
            IMediator mediator, IValidator<CreateVatProfileCommand> validator, CancellationToken ct) =>
        {
            var cmd = new CreateVatProfileCommand(tenant.TenantId, request);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/vat-profiles/{result.Id}", result);
        })
        .WithName("CreateVatProfile").WithTags("VAT Profiles")
        .Produces<CreateVatProfileResponse>(201).ProducesValidationProblem();
    }
}


public sealed record VatProfileResponse(int Id, string Name, decimal Rate, bool Active);
public sealed record ListVatProfilesQuery(int TenantId) : IRequest<List<VatProfileResponse>>;

public sealed class ListVatProfilesHandler(AppDbContext db)
    : IRequestHandler<ListVatProfilesQuery, List<VatProfileResponse>>
{
    public async Task<List<VatProfileResponse>> Handle(ListVatProfilesQuery q, CancellationToken ct) =>
        await db.VatProfiles.AsNoTracking()
            .Where(vp => vp.TenantId == q.TenantId)
            .OrderBy(vp => vp.Name)
            .Select(vp => new VatProfileResponse(vp.Id, vp.Name, vp.Rate, vp.Active))
            .ToListAsync(ct);
}

public sealed class ListVatProfilesEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/vat-profiles", async (ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListVatProfilesQuery(tenant.TenantId), ct)))
        .WithName("ListVatProfiles").WithTags("VAT Profiles")
        .Produces<List<VatProfileResponse>>();
    }
}

// ═══════════════════════════════════════════════════════════════
// INVOICES
// ═══════════════════════════════════════════════════════════════

public sealed record InvoiceItemRequest(string Description, decimal Price);

public sealed record CreateInvoiceRequest(
    int VatProfileId,
    string Number,
    List<InvoiceItemRequest> Items);

public sealed record CreateInvoiceResponse(
    int Id, string Number,
    decimal Subtotal, decimal VatAmount, decimal Total,
    string VatProfileName, decimal VatRate);

public sealed record CreateInvoiceCommand(int TenantId, CreateInvoiceRequest Request)
    : IRequest<CreateInvoiceResponse>;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Request.VatProfileId).GreaterThan(0);
        RuleFor(x => x.Request.Number).NotEmpty();
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty();
            item.RuleFor(i => i.Price).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateInvoiceHandler(AppDbContext db)
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResponse>
{
    public async Task<CreateInvoiceResponse> Handle(CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var vatProfile = await db.VatProfiles
            .Where(vp => vp.Id == cmd.Request.VatProfileId && vp.TenantId == cmd.TenantId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"VatProfile {cmd.Request.VatProfileId} not found.");

        var subtotal  = cmd.Request.Items.Sum(i => i.Price);
        var vatAmount = Math.Round(subtotal * vatProfile.Rate, 2);
        var total     = subtotal + vatAmount;

        var invoice = new Invoice
        {
            TenantId     = cmd.TenantId,
            VatProfileId = vatProfile.Id,
            Number       = cmd.Request.Number,
            Total        = total,
            VatAmount    = vatAmount,
            CreatedAt    = DateTime.UtcNow,
            Items = cmd.Request.Items.Select(i => new InvoiceItem
            {
                Description = i.Description,
                Price       = i.Price
            }).ToList()
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return new CreateInvoiceResponse(
            invoice.Id, invoice.Number!,
            subtotal, vatAmount, total,
            vatProfile.Name, vatProfile.Rate);
    }
}

public sealed class CreateInvoiceEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPost("/api/invoices", async (
            CreateInvoiceRequest request, ITenantContext tenant,
            IMediator mediator, IValidator<CreateInvoiceCommand> validator, CancellationToken ct) =>
        {
            var cmd = new CreateInvoiceCommand(tenant.TenantId, request);
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            try
            {
                var result = await mediator.Send(cmd, ct);
                return Results.Created($"/api/invoices/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        })
        .WithName("CreateInvoice").WithTags("Invoices")
        .Produces<CreateInvoiceResponse>(201).ProducesValidationProblem().Produces(404);
    }
}


public sealed record InvoiceItemResponse(int Id, string? Description, decimal? Price);
public sealed record InvoiceDetailResponse(
    int Id, string? Number, decimal? Total, decimal? VatAmount,
    string VatProfileName, decimal VatRate, List<InvoiceItemResponse> Items);

public sealed record GetInvoiceQuery(int TenantId, int InvoiceId) : IRequest<InvoiceDetailResponse?>;

public sealed class GetInvoiceHandler(AppDbContext db)
    : IRequestHandler<GetInvoiceQuery, InvoiceDetailResponse?>
{
    public async Task<InvoiceDetailResponse?> Handle(GetInvoiceQuery q, CancellationToken ct)
    {
        var inv = await db.Invoices.AsNoTracking()
            .Include(i => i.VatProfile)
            .Include(i => i.Items)
            .Where(i => i.Id == q.InvoiceId && i.TenantId == q.TenantId)
            .FirstOrDefaultAsync(ct);

        return inv is null ? null : new InvoiceDetailResponse(
            inv.Id, inv.Number, inv.Total, inv.VatAmount,
            inv.VatProfile.Name, inv.VatProfile.Rate,
            inv.Items.Select(i => new InvoiceItemResponse(i.Id, i.Description, i.Price)).ToList());
    }
}

public sealed class GetInvoiceEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/invoices/{id:int}", async (
            int id, ITenantContext tenant, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInvoiceQuery(tenant.TenantId, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetInvoice").WithTags("Invoices")
        .Produces<InvoiceDetailResponse>().Produces(404);
    }
}
