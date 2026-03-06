namespace BookingApi.Common.Interfaces;

/// <summary>
/// Resolved from the X-Tenant-Id header on every request.
/// Every query/command uses this to scope DB access to one tenant.
/// </summary>
public interface ITenantContext
{
    int TenantId { get; }
}
