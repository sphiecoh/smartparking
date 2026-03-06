using System.ComponentModel.DataAnnotations;
using AdminSite.Infrastructure.Data;
using AdminSite.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════
// INDEX
// ═══════════════════════════════════════════════════
namespace AdminSite.Pages.Packages;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<PackageRow> Packages { get; set; } = [];

    public record PackageRow(int Id, string Name, int TenantCount, DateTime? CreatedAt);

    public async Task OnGetAsync()
    {
        Packages = await db.Packages
            .OrderBy(p => p.Name)
            .Select(p => new PackageRow(
                p.Id, p.Name,
                p.Tenants.Count,
                p.CreatedAt))
            .ToListAsync();
    }
}

// ═══════════════════════════════════════════════════
// CREATE
// ═══════════════════════════════════════════════════

[Authorize]
public class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty] public PackageInput Input { get; set; } = new();

    public class PackageInput
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = "";
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var pkg = new Package { Name = Input.Name, CreatedAt = DateTime.UtcNow };
        db.Packages.Add(pkg);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Package \"{pkg.Name}\" created.";
        return RedirectToPage("Index");
    }
}

// ═══════════════════════════════════════════════════
// EDIT
// ═══════════════════════════════════════════════════


[Authorize]
public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty] public PackageInput Input { get; set; } = new();
    public int PackageId { get; set; }

    public class PackageInput
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var pkg = await db.Packages.FindAsync(id);
        if (pkg is null) return NotFound();
        PackageId = id;
        Input.Name = pkg.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid) { PackageId = id; return Page(); }

        var pkg = await db.Packages.FindAsync(id);
        if (pkg is null) return NotFound();

        pkg.Name         = Input.Name;
        pkg.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Package \"{pkg.Name}\" updated.";
        return RedirectToPage("Index");
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
        var pkg = await db.Packages.Include(p => p.Tenants).FirstOrDefaultAsync(p => p.Id == id);
        if (pkg is null) return NotFound();

        if (pkg.Tenants.Any())
        {
            TempData["Error"] = $"Cannot delete \"{pkg.Name}\" — {pkg.Tenants.Count} tenant(s) are using it.";
            return RedirectToPage("Index");
        }

        db.Packages.Remove(pkg);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Package \"{pkg.Name}\" deleted.";
        return RedirectToPage("Index");
    }
}
