using BookingApi.Common.Extensions;
using BookingApi.Common.Interfaces;
using BookingApi.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Features.Bookings.UpdateBookingStatus;

public static class BookingStatus
{
    public const string Pending    = "Pending";
    public const string Confirmed  = "Confirmed";
    public const string InProgress = "InProgress";
    public const string Completed  = "Completed";
    public const string Cancelled  = "Cancelled";

    public static readonly string[] All =
        [Pending, Confirmed, InProgress, Completed, Cancelled];
}

// ── Request / Command ─────────────────────────────────────────────

public sealed record UpdateBookingStatusRequest(string Status);

public sealed record UpdateBookingStatusCommand(
    int TenantId, int BookingId, string Status
) : IRequest<bool>;

// ── Validator ────────────────────────────────────────────────────

public sealed class UpdateBookingStatusValidator : AbstractValidator<UpdateBookingStatusCommand>
{
    public UpdateBookingStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => BookingStatus.All.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", BookingStatus.All)}");
    }
}

// ── Handler ──────────────────────────────────────────────────────

public sealed class UpdateBookingStatusHandler(AppDbContext db, ITenantContext tenant)
    : IRequestHandler<UpdateBookingStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateBookingStatusCommand cmd, CancellationToken ct)
    {
        var rows = await db.Bookings
            .Where(b => b.Id == cmd.BookingId && b.TenantId == cmd.TenantId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, cmd.Status)
                .SetProperty(b => b.ModifiedDate, DateTime.UtcNow), ct);

        return rows > 0;
    }
}

// ── Endpoint ─────────────────────────────────────────────────────

public sealed class UpdateBookingStatusEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapPatch("/api/bookings/{id:int}/status", async (
            int id,
            UpdateBookingStatusRequest request,
            ITenantContext tenant,
            IMediator mediator,
            IValidator<UpdateBookingStatusCommand> validator,
            CancellationToken ct) =>
        {
            var cmd = new UpdateBookingStatusCommand(tenant.TenantId, id, request.Status);

            var validation = await validator.ValidateAsync(cmd, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var updated = await mediator.Send(cmd, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateBookingStatus")
        .WithTags("Bookings")
        .WithSummary("Update the status of a booking (Pending → Confirmed → InProgress → Completed | Cancelled)")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();
    }
}
