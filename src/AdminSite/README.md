# Booking Platform — Admin Site

ASP.NET Core 8 Razor Pages admin portal for managing packages, tenants, and tenant administrator accounts.

---

## Features

| Section | Capabilities |
|---|---|
| **Dashboard** | Platform stats, recent tenants & users at a glance |
| **Packages** | Create, rename, delete subscription packages |
| **Tenants** | Create, edit (name, URL, package, active toggle), delete tenants |
| **Admin Users** | Create, edit, reset password, delete per-tenant admin users |

**UX highlights:**
- Live client-side search on every list page (no page reload)
- Tenant filter on the admin users list
- Confirm-delete modal with context-aware warning messages
- Toggle switches for boolean fields (active/inactive)
- Password strength meter on user create/edit
- Animated toast alerts (auto-dismiss after 4s)
- BCrypt password hashing via BCrypt.Net-Next

---

## Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 8 Razor Pages |
| ORM | Entity Framework Core 8 + Npgsql |
| Auth | Cookie authentication (no ASP.NET Identity overhead) |
| Password hashing | BCrypt.Net-Next |
| Fonts | Syne + DM Mono (Google Fonts) |
| CSS | Handwritten design system (no Bootstrap dependency) |

---

## Setup

### 1. Prerequisites
- .NET 8 SDK
- PostgreSQL with the booking schema already applied (shared DB with the API)

### 2. Configure connection string
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=bookingdb;Username=postgres;Password=yourpassword"
  }
}
```

### 3. Seed a superadmin user
The login page authenticates users from the `users` table where `tenant_id IS NULL`.
Insert a superadmin before first use:

```sql
INSERT INTO users (username, name, email, password_hash, tenant_id, created_at)
VALUES (
    'admin',
    'Super Admin',
    'admin@platform.com',
    '$2a$11$...bcrypt_hash_of_your_password...',
    NULL,
    NOW()
);
```

To generate a hash in C#:
```csharp
BCrypt.Net.BCrypt.HashPassword("YourSecurePassword123!");
```

### 4. Run
```bash
dotnet run --project src/AdminSite
```

Navigate to: `https://localhost:5002/Login`

---

## Project Structure

```
src/AdminSite/
├── Program.cs                          # DI, middleware, cookie auth
├── appsettings.json
├── Infrastructure/
│   └── Data/
│       ├── AppDbContext.cs             # EF Core context (packages, tenants, users, roles)
│       └── Entities/Entities.cs       # Domain models
├── Pages/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml             # Sidebar shell layout
│   │   ├── _LoginLayout.cshtml        # Minimal layout for login
│   │   └── _DeleteModal.cshtml        # Reusable confirm-delete modal
│   ├── Index.cshtml[.cs]              # Dashboard
│   ├── Login.cshtml[.cs]              # Auth
│   ├── Logout.cshtml[.cs]
│   ├── Packages/
│   │   ├── Index, Create, Edit, Delete
│   ├── Tenants/
│   │   ├── Index, Create, Edit, Delete
│   └── TenantUsers/
│       ├── Index, Create, Edit, Delete
└── wwwroot/
    ├── css/admin.css                   # Full design system
    └── js/admin.js                     # Modal, live search, pw strength
```
