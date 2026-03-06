using BookingApi.Common.Interfaces;

namespace BookingApi.Common.Middleware;

/// <summary>
/// Reads X-Tenant-Id from every request header and makes it
/// available as ITenantContext for the lifetime of the request.
/// Returns 400 if the header is missing or invalid.
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var raw)
            || !int.TryParse(raw, out var tenantId))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-Tenant-Id header." });
            return;
        }

        ctx.Items["TenantId"] = tenantId;
        await next(ctx);
    }
}

/// <summary>
/// Scoped implementation — resolved from HttpContext.Items by DI.
/// </summary>
public class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public int TenantId =>
        accessor.HttpContext?.Items["TenantId"] is int id
            ? id
            : throw new InvalidOperationException("TenantId not set on current request.");
}
