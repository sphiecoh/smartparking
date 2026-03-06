using System.ComponentModel.DataAnnotations;
using AdminSite.Infrastructure.Data;
using AdminSite.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════
// INDEX
// ═══════════════════════════════════════════════════
namespace AdminSite.Pages.Tenants;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<TenantRow> Tenants { get; set; } = [];

    public record TenantRow(
        int Id, string Name, string? Url, string? PackageName,
        int UserCount, bool Active, DateTime? CreatedAt);

    public async Task OnGetAsync()
    {
        Tenants = await db.Tenants
            .Include(t => t.Package)
            .Include(t => t.Users)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantRow(
                t.Id, t.Name, t.Url,
                t.Package != null ? t.Package.Name : null,
                t.Users.Count, t.active, t.CreatedAt))
            .ToListAsync();
    }
}

// ═══════════════════════════════════════════════════
// CREATE
// ═══════════════════════════════════════════════════


[Authorize]
public class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty] public TenantInput Input { get; set; } = new();
    public List<SelectListItem> Packages { get; set; } = [];

    public class TenantInput
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [Url, MaxLength(500)]
        public string? Url { get; set; }

        public int? PackageId { get; set; }
        public bool Active { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        await LoadPackagesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadPackagesAsync(); return Page(); }

        var tenant = new Tenant
        {
            Name      = Input.Name,
            Url       = Input.Url,
            PackageId = Input.PackageId,
            active    = Input.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Tenant \"{tenant.Name}\" created successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadPackagesAsync()
    {
        var pkgs = await db.Packages.OrderBy(p => p.Name).ToListAsync();
        Packages = pkgs.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        Packages.Insert(0, new SelectListItem("— No package —", ""));
    }
}

// ═══════════════════════════════════════════════════
// EDIT
// ═══════════════════════════════════════════════════

[Authorize]
public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty] public TenantInput Input { get; set; } = new();
    public int TenantId { get; set; }
    public List<SelectListItem> Packages { get; set; } = [];

    public class TenantInput
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [Url, MaxLength(500)]
        public string? Url { get; set; }

        public int? PackageId { get; set; }
        public bool Active { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var t = await db.Tenants.FindAsync(id);
        if (t is null) return NotFound();
        TenantId     = id;
        Input.Name      = t.Name;
        Input.Url       = t.Url;
        Input.PackageId = t.PackageId;
        Input.Active    = t.active;
        await LoadPackagesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid) { TenantId = id; await LoadPackagesAsync(); return Page(); }

        var t = await db.Tenants.FindAsync(id);
        if (t is null) return NotFound();

        t.Name         = Input.Name;
        t.Url          = Input.Url;
        t.PackageId    = Input.PackageId;
        t.active       = Input.Active;
        t.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Tenant \"{t.Name}\" updated.";
        return RedirectToPage("Index");
    }

    private async Task LoadPackagesAsync()
    {
        var pkgs = await db.Packages.OrderBy(p => p.Name).ToListAsync();
        Packages = pkgs.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        Packages.Insert(0, new SelectListItem("— No package —", ""));
    }
}

// ═══════════════════════════════════════════════════
// DELETE
// ═══════════════════════════════════════════════════

[Authorize]
public class DeleteModel(AppDbContext db) : PageModel
{
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var t = await db.Tenants.FindAsync(id);
        if (t is null) return NotFound();
        db.Tenants.Remove(t);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Tenant \"{t.Name}\" deleted.";
        return RedirectToPage("Index");
    }
}
