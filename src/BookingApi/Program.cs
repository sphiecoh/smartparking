using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Common.Middleware;
using BookingApi.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── MediatR — scans this assembly for all handlers ───────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ── FluentValidation — scans this assembly for all validators ────
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ── Tenant context ────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

// ── OpenAPI / Swagger ─────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Booking API", Version = "v1" });
    c.AddSecurityDefinition("TenantId", new()
    {
        Name = "X-Tenant-Id",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "Tenant identifier — required on every request"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "TenantId" } },
            []
        }
    });
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Resolve tenant from X-Tenant-Id header before hitting any endpoint
app.UseMiddleware<TenantMiddleware>();

// ── Register all feature endpoints (vertical slice auto-discovery) ─
app.MapAllEndpoints();

// ── Health check ──────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithTags("Health")
   .ExcludeFromDescription();

app.Run();

// Needed for integration test projects to reference WebApplicationFactory<Program>
public partial class Program { }
