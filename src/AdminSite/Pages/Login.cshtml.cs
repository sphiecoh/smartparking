using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AdminSite.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdminSite.Pages;

public class LoginModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required] public string Username { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Find a superadmin user (no tenant) matching the credentials
        var user = await db.Users
            .Include(u => u.Role)
            .Where(u => u.Username == Input.Username && u.TenantId == null)
            .FirstOrDefaultAsync();

        // Verify BCrypt password hash; allow a plain fallback for seeded dev passwords
        bool valid = user != null &&
            (BCrypt.Net.BCrypt.Verify(Input.Password, user.Password)
             || user.Password == Input.Password); // dev seed plain-text fallback

        if (!valid)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user!.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("FullName", user.Name ?? user.Username),
            new(ClaimTypes.Role, "SuperAdmin")
        };

        var identity  = new ClaimsIdentity(claims, "AdminCookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AdminCookie", principal,
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToPage("/Index");
    }
}
