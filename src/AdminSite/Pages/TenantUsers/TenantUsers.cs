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
namespace AdminSite.Pages.TenantUsers;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<UserRow> Users { get; set; } = [];
    public List<TenantOption> AllTenants { get; set; } = [];
    public string? FilterTenant { get; set; }
    public int? FilterTenantId { get; set; }

    public record UserRow(
        int Id, string Username, string? FullName, string? Email,
        int? TenantId, string? TenantName, string? RoleName, DateTime? CreatedAt);
    public record TenantOption(int Id, string Name);

    public async Task OnGetAsync(int? tenantId = null)
    {
        FilterTenantId = tenantId;

        AllTenants = await db.Tenants.OrderBy(t => t.Name)
            .Select(t => new TenantOption(t.Id, t.Name)).ToListAsync();

        if (tenantId.HasValue)
            FilterTenant = AllTenants.FirstOrDefault(t => t.Id == tenantId)?.Name;

        var query = db.Users
            .Include(u => u.Tenant)
            .Include(u => u.Role)
            .Where(u => u.TenantId != null); // only tenant users, not superadmins

        if (tenantId.HasValue)
            query = query.Where(u => u.TenantId == tenantId);

        Users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserRow(
                u.Id, u.Username, u.Name, u.Email,
                u.TenantId,
                u.Tenant != null ? u.Tenant.Name : null,
                u.Role != null ? u.Role.Name : null,
                u.CreatedAt))
            .ToListAsync();
    }
}

// ═══════════════════════════════════════════════════
// CREATE
// ═══════════════════════════════════════════════════

[Authorize]
public class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty] public UserInput Input { get; set; } = new();
    public List<SelectListItem> Tenants { get; set; } = [];
    public List<SelectListItem> Roles   { get; set; } = [];

    public class UserInput
    {
        [Required, MaxLength(100)]
        public string Username { get; set; } = "";

        [MaxLength(200)]
        public string? FullName { get; set; }

        [EmailAddress, MaxLength(200)]
        public string? Email { get; set; }

        [Required]
        public int TenantId { get; set; }

        public int? RoleId { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; } = "";

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task OnGetAsync(int? tenantId = null)
    {
        if (tenantId.HasValue) Input.TenantId = tenantId.Value;
        await LoadSelectsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadSelectsAsync(); return Page(); }

        // Check username uniqueness within tenant
        var exists = await db.Users
            .AnyAsync(u => u.Username == Input.Username && u.TenantId == Input.TenantId);
        if (exists)
        {
            ModelState.AddModelError("Input.Username", "Username already exists for this tenant.");
            await LoadSelectsAsync();
            return Page();
        }

        var user = new User
        {
            Username     = Input.Username,
            Name         = Input.FullName,
            Email        = Input.Email,
            TenantId     = Input.TenantId == 0 ? null : Input.TenantId,
            RoleId       = Input.RoleId,
            Password = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            CreatedAt    = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Admin user \"{user.Username}\" created for tenant.";
        return RedirectToPage("Index", new { tenantId = Input.TenantId });
    }

    private async Task LoadSelectsAsync()
    {
        Tenants = (await db.Tenants.OrderBy(t => t.Name).ToListAsync())
            .Select(t => new SelectListItem(t.Name, t.Id.ToString()))
            .ToList();

        // Roles scoped to selected tenant (or all if none selected yet)
        var roleQuery = db.Roles.AsQueryable();
        if (Input.TenantId > 0)
            roleQuery = roleQuery.Where(r => r.TenantId == Input.TenantId);

        Roles = (await roleQuery.OrderBy(r => r.Name).ToListAsync())
            .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
            .ToList();
        Roles.Insert(0, new SelectListItem("— No role —", ""));
    }
}

// ═══════════════════════════════════════════════════
// EDIT
// ═══════════════════════════════════════════════════


[Authorize]
public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty] public UserInput Input { get; set; } = new();
    public int UserId { get; set; }
    public List<SelectListItem> Tenants { get; set; } = [];
    public List<SelectListItem> Roles   { get; set; } = [];

    public class UserInput
    {
        [Required, MaxLength(100)] public string Username { get; set; } = "";
        [MaxLength(200)]           public string? FullName { get; set; }
        [EmailAddress, MaxLength(200)] public string? Email { get; set; }
        [Required]                 public int TenantId { get; set; }
        public int? RoleId { get; set; }

        // Password is optional on edit — blank = no change
        [MinLength(8)]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var u = await db.Users.FindAsync(id);
        if (u is null) return NotFound();
        UserId           = id;
        Input.Username   = u.Username;
        Input.FullName   = u.Name;
        Input.Email      = u.Email;
        Input.TenantId   = u.TenantId ?? 0;
        Input.RoleId     = u.RoleId;
        await LoadSelectsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Password fields are optional on edit
        ModelState.Remove("Input.NewPassword");
        ModelState.Remove("Input.ConfirmPassword");

        if (!string.IsNullOrEmpty(Input.NewPassword) && Input.NewPassword.Length < 8)
            ModelState.AddModelError("Input.NewPassword", "Password must be at least 8 characters.");

        if (!string.IsNullOrEmpty(Input.NewPassword)
            && Input.NewPassword != Input.ConfirmPassword)
            ModelState.AddModelError("Input.ConfirmPassword", "Passwords do not match.");

        if (!ModelState.IsValid) { UserId = id; await LoadSelectsAsync(); return Page(); }

        var u = await db.Users.FindAsync(id);
        if (u is null) return NotFound();

        u.Username     = Input.Username;
        u.Name         = Input.FullName;
        u.Email        = Input.Email;
        u.TenantId     = Input.TenantId;
        u.RoleId       = Input.RoleId;
        u.ModifiedDate = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(Input.NewPassword))
            u.Password = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);

        await db.SaveChangesAsync();
        TempData["Success"] = $"User \"{u.Username}\" updated.";
        return RedirectToPage("Index");
    }

    private async Task LoadSelectsAsync()
    {
        Tenants = (await db.Tenants.OrderBy(t => t.Name).ToListAsync())
            .Select(t => new SelectListItem(t.Name, t.Id.ToString()))
            .ToList();

        Roles = (await db.Roles
            .Where(r => r.TenantId == Input.TenantId)
            .OrderBy(r => r.Name)
            .ToListAsync())
            .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
            .ToList();
        Roles.Insert(0, new SelectListItem("— No role —", ""));
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
        var u = await db.Users.FindAsync(id);
        if (u is null) return NotFound();
        var tenantId = u.TenantId;
        db.Users.Remove(u);
        await db.SaveChangesAsync();
        TempData["Success"] = $"User \"{u.Username}\" deleted.";
        return RedirectToPage("Index", tenantId.HasValue ? new { tenantId } : null);
    }
}
