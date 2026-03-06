using BookingApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServicePricing> ServicePricings => Set<ServicePricing>();
    public DbSet<ServicePricingTier> ServicePricingTiers => Set<ServicePricingTier>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<VatProfile> VatProfiles => Set<VatProfile>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<AgencyCustomer> AgencyCustomers => Set<AgencyCustomer>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ── Package ──────────────────────────────────────────────
        b.Entity<Package>(e =>
        {
            e.ToTable("package");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");
        });

        // ── Tenant ───────────────────────────────────────────────
        b.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Url).HasColumnName("url");
            e.Property(x => x.PackageId).HasColumnName("packageid");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Package)
             .WithMany(p => p.Tenants)
             .HasForeignKey(x => x.PackageId);
        });

        // ── Role ─────────────────────────────────────────────────
        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant)
             .WithMany(t => t.Roles)
             .HasForeignKey(x => x.TenantId);
        });

        // ── User ─────────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Username).HasColumnName("username").IsRequired();
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);
        });

        // ── Agency ───────────────────────────────────────────────
        b.Entity<Agency>(e =>
        {
            e.ToTable("agencies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.RechargeLevel).HasColumnName("recharge_level").HasPrecision(12, 2);
            e.Property(x => x.Credits).HasColumnName("credits").HasPrecision(12, 2);
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
            e.Property(x => x.AddressLine1).HasColumnName("address_line1");
            e.Property(x => x.AddressLine2).HasColumnName("address_line2");
            e.Property(x => x.AddressLine3).HasColumnName("address_line3");
            e.Property(x => x.AddressLine4).HasColumnName("address_line4");
            e.Property(x => x.Rate).HasColumnName("rate").HasPrecision(10, 4);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant).WithMany(t => t.Agencies).HasForeignKey(x => x.TenantId);
        });

        // ── Customer ─────────────────────────────────────────────
        b.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant).WithMany(t => t.Customers).HasForeignKey(x => x.TenantId);
        });

        // ── Vehicle ──────────────────────────────────────────────
        b.Entity<Vehicle>(e =>
        {
            e.ToTable("vehicles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Reg).HasColumnName("reg").IsRequired();
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
            e.Property(x => x.OwnerId).HasColumnName("owner_id");
            e.Property(x => x.Make).HasColumnName("make");
            e.Property(x => x.Model).HasColumnName("model");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Owner).WithMany(c => c.Vehicles).HasForeignKey(x => x.OwnerId);
            e.HasOne(x => x.Tenant).WithMany(t => t.Vehicles).HasForeignKey(x => x.TenantId);
        });

        // ── Service ──────────────────────────────────────────────
        b.Entity<Service>(e =>
        {
            e.ToTable("services");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant).WithMany(t => t.Services).HasForeignKey(x => x.TenantId);
        });

        // ── ServicePricing ───────────────────────────────────────
        b.Entity<ServicePricing>(e =>
        {
            e.ToTable("service_pricing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ServiceId).HasColumnName("service_id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("ZAR");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            e.Property(x => x.Note).HasColumnName("note");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasIndex(x => new { x.ServiceId, x.TenantId, x.EffectiveFrom }).IsUnique();

            e.HasOne(x => x.Service).WithMany(s => s.Pricings).HasForeignKey(x => x.ServiceId);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
        });

        // ── ServicePricingTier ───────────────────────────────────
        b.Entity<ServicePricingTier>(e =>
        {
            e.ToTable("service_pricing_tier");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ServicePricingId).HasColumnName("service_pricing_id");
            e.Property(x => x.DayFrom).HasColumnName("day_from");
            e.Property(x => x.DayTo).HasColumnName("day_to");
            e.Property(x => x.PricePerDay).HasColumnName("price_per_day").HasPrecision(12, 2);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");

            e.HasIndex(x => new { x.ServicePricingId, x.DayFrom }).IsUnique();

            e.HasOne(x => x.ServicePricing)
             .WithMany(sp => sp.Tiers)
             .HasForeignKey(x => x.ServicePricingId);
        });

        // ── Booking ──────────────────────────────────────────────
        b.Entity<Booking>(e =>
        {
            e.ToTable("bookings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.VehicleId).HasColumnName("vehicle_id");
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.Voucher).HasColumnName("voucher");
            e.Property(x => x.AgencyId).HasColumnName("agency_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasIndex(x => new { x.TenantId, x.Voucher }).IsUnique();

            e.HasOne(x => x.Tenant).WithMany(t => t.Bookings).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.Customer).WithMany(c => c.Bookings).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Vehicle).WithMany(v => v.Bookings).HasForeignKey(x => x.VehicleId);
            e.HasOne(x => x.Agency).WithMany(a => a.Bookings).HasForeignKey(x => x.AgencyId);
        });

        // ── BookingService ───────────────────────────────────────
        b.Entity<BookingService>(e =>
        {
            e.ToTable("booking_services");
            e.HasKey(x => new { x.BookingId, x.ServiceId });
            e.Property(x => x.BookingId).HasColumnName("booking_id");
            e.Property(x => x.ServiceId).HasColumnName("service_id");
            e.Property(x => x.ServicePricingId).HasColumnName("service_pricing_id");
            e.Property(x => x.ServicePricingTierId).HasColumnName("service_pricing_tier_id");
            e.Property(x => x.Days).HasColumnName("days");
            e.Property(x => x.PricePerDay).HasColumnName("price_per_day").HasPrecision(12, 2);
            e.Property(x => x.PriceCharged).HasColumnName("price_charged").HasPrecision(12, 2);

            e.HasOne(x => x.Booking).WithMany(b => b.BookingServices).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.Service).WithMany(s => s.BookingServices).HasForeignKey(x => x.ServiceId);
            e.HasOne(x => x.ServicePricing).WithMany(sp => sp.BookingServices).HasForeignKey(x => x.ServicePricingId);
        });

        // ── Payment ──────────────────────────────────────────────
        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(12, 2);
            e.Property(x => x.BookingId).HasColumnName("booking_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Booking).WithMany(b => b.Payments).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
        });

        // ── VatProfile ───────────────────────────────────────────
        b.Entity<VatProfile>(e =>
        {
            e.ToTable("vat_profile");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Rate).HasColumnName("rate").HasPrecision(5, 4);
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant).WithMany(t => t.VatProfiles).HasForeignKey(x => x.TenantId);
        });

        // ── Invoice ──────────────────────────────────────────────
        b.Entity<Invoice>(e =>
        {
            e.ToTable("invoice");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Total).HasColumnName("total").HasPrecision(12, 2);
            e.Property(x => x.VatProfileId).HasColumnName("vat_profile_id");
            e.Property(x => x.VatAmount).HasColumnName("vat_amount").HasPrecision(12, 2);
            e.Property(x => x.Number).HasColumnName("number");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");

            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.VatProfile).WithMany(v => v.Invoices).HasForeignKey(x => x.VatProfileId);
        });

        // ── InvoiceItem ──────────────────────────────────────────
        b.Entity<InvoiceItem>(e =>
        {
            e.ToTable("invoice_item");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(12, 2);

            e.HasOne(x => x.Invoice).WithMany(i => i.Items).HasForeignKey(x => x.InvoiceId);
        });

        // ── AgencyCustomer ───────────────────────────────────────
        b.Entity<AgencyCustomer>(e =>
        {
            e.ToTable("agency_customers");
            e.HasKey(x => new { x.AgencyId, x.CustomerId });
            e.Property(x => x.AgencyId).HasColumnName("agency_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");

            e.HasOne(x => x.Agency).WithMany(a => a.AgencyCustomers).HasForeignKey(x => x.AgencyId);
            e.HasOne(x => x.Customer).WithMany(c => c.AgencyCustomers).HasForeignKey(x => x.CustomerId);
        });

        // ── TenantUser ───────────────────────────────────────────
        b.Entity<TenantUser>(e =>
        {
            e.ToTable("tenant_users");
            e.HasKey(x => new { x.TenantId, x.UserId });
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.UserId).HasColumnName("user_id");

            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
    }
}
