# Booking API

.NET 8 Minimal API using **Vertical Slice Architecture**.

---

## Architecture

Each feature is a self-contained vertical slice. No shared service layers or fat controllers.

```
src/BookingApi/
├── Program.cs                          # Entry point, DI, middleware, endpoint discovery
├── Common/
│   ├── Interfaces/ITenantContext.cs    # Tenant scoping contract
│   ├── Middleware/TenantMiddleware.cs  # Reads X-Tenant-Id header
│   ├── Extensions/EndpointExtensions.cs # Auto-discovers & maps all IEndpointDefinition
│   └── PagedResult.cs                 # Shared pagination envelope
├── Infrastructure/
│   └── Data/
│       ├── AppDbContext.cs             # EF Core context + full model config
│       └── Entities/Entities.cs       # All domain entities
└── Features/
    ├── Bookings/
    │   ├── CreateBooking/              # POST /api/bookings
    │   ├── GetBooking/                 # GET  /api/bookings/{id}
    │   ├── ListBookings/               # GET  /api/bookings
    │   └── UpdateBookingStatus/        # PATCH /api/bookings/{id}/status
    ├── Customers/                      # CRUD — GET, POST, LIST
    ├── Vehicles/                       # CRUD — GET, POST, LIST
    ├── Services/                       # POST, LIST
    ├── ServicePricing/
    │   ├── CreatePricing/              # POST /api/services/{id}/pricing
    │   ├── GetActivePricing/           # GET  /api/services/{id}/pricing/active
    │   └── GetPricingHistory/          # GET  /api/services/{id}/pricing/history
    ├── Agencies/                       # POST, LIST
    ├── Payments/                       # POST & GET per booking
    ├── Invoices/                       # POST, GET
    └── VatProfiles/                    # POST, LIST
```

Each slice contains in one file:
- **Request/Response DTOs**
- **Command/Query** (MediatR `IRequest<T>`)
- **Validator** (FluentValidation)
- **Handler** (`IRequestHandler<TRequest, TResponse>`)
- **Endpoint** (`IEndpointDefinition` → auto-registered at startup)

---

## Stack

| Concern | Library |
|---|---|
| Framework | ASP.NET Core 8 Minimal APIs |
| ORM | Entity Framework Core 8 + Npgsql |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Docs | Swashbuckle (Swagger UI) |
| Database | PostgreSQL |

---

## Setup

### 1. Prerequisites
- .NET 8 SDK
- PostgreSQL instance

### 2. Configure database
Edit `src/BookingApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=bookingdb;Username=postgres;Password=yourpassword"
  }
}
```

### 3. Run migrations
```bash
cd src/BookingApi
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the API
```bash
dotnet run --project src/BookingApi
```

Swagger UI: `https://localhost:5001/swagger`

---

## Multi-Tenancy

Every request requires an `X-Tenant-Id` header:
```
X-Tenant-Id: 1
```

The `TenantMiddleware` enforces this before any endpoint is reached.
All queries are automatically scoped to the resolved tenant — no cross-tenant data leakage is possible.

---

## Endpoint Reference

### Bookings
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/bookings` | Create booking; auto-resolves tier pricing |
| `GET` | `/api/bookings` | List with filters: status, customerId, agencyId, date range |
| `GET` | `/api/bookings/{id}` | Detail with services, payments, totals |
| `PATCH` | `/api/bookings/{id}/status` | Transition status |
| `POST` | `/api/bookings/{id}/payments` | Record a payment |
| `GET` | `/api/bookings/{id}/payments` | Payment summary with balance |

### Services & Pricing
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/services` | Create service |
| `GET` | `/api/services` | List services (with active pricing flag) |
| `POST` | `/api/services/{id}/pricing` | Create new pricing schedule with tiers |
| `GET` | `/api/services/{id}/pricing/active` | Active schedule + tiers (supports `?asOf=`) |
| `GET` | `/api/services/{id}/pricing/history` | Full pricing history |

### Supporting Resources
| Method | Route | Description |
|---|---|---|
| `POST/GET` | `/api/customers` | Customers |
| `POST/GET` | `/api/vehicles` | Vehicles |
| `POST/GET` | `/api/agencies` | Agencies |
| `POST/GET` | `/api/invoices` | Invoices (VAT auto-calculated) |
| `POST/GET` | `/api/vat-profiles` | VAT rate profiles |

---

## Pricing Resolution Flow

When a booking is created with services:

1. Find the active `service_pricing` schedule for each service
   (most recent `effective_from <= NOW()` for the tenant)
2. Find the matching `service_pricing_tier` for the booking duration in days
   (`day_from <= days <= day_to`, or `day_from <= days` when `day_to IS NULL`)
3. Snapshot `service_pricing_id`, `service_pricing_tier_id`, `price_per_day`, and calculated `price_charged` onto `booking_services`
4. Historical bookings are never affected by future pricing changes

---

## Adding a New Feature Slice

1. Create `Features/MyFeature/MyFeatureSlice.cs`
2. Define your request/response records, command/query, validator, handler, and endpoint
3. Implement `IEndpointDefinition` on the endpoint class
4. That's it — auto-discovered at startup, no `Program.cs` changes needed
