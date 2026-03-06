using AdminSite.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdminSite.Pages;

//[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public int PackageCount { get; set; }
    public int ActiveTenantCount { get; set; }
    public int TotalTenantCount { get; set; }
    public int AdminUserCount { get; set; }
    public List<RecentTenantDto> RecentTenants { get; set; } = [];
    public List<RecentUserDto> RecentUsers { get; set; } = [];

    public record RecentTenantDto(string Name, string? Package, bool Active, DateTime? CreatedAt);
    public record RecentUserDto(string Name, string? Email, string? Tenant, DateTime? CreatedAt);

    public async Task OnGetAsync()
    {
        PackageCount       = await db.Packages.CountAsync();
        TotalTenantCount   = await db.Tenants.CountAsync();
        ActiveTenantCount  = await db.Tenants.CountAsync(t => t.active);
        AdminUserCount     = await db.Users.CountAsync(u => u.TenantId != null);

        RecentTenants = await db.Tenants
            .Include(t => t.Package)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentTenantDto(t.Name, t.Package != null ? t.Package.Name : null, t.active, t.CreatedAt))
            .ToListAsync();

        RecentUsers = await db.Users
            .Include(u => u.Tenant)
            .Where(u => u.TenantId != null)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new RecentUserDto(
                u.Name ?? u.Username,
                u.Email,
                u.Tenant != null ? u.Tenant.Name : null,
                u.CreatedAt))
            .ToListAsync();
    }
}
