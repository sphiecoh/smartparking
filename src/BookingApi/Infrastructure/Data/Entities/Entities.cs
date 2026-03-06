namespace BookingApi.Infrastructure.Data.Entities;

// -------------------------------------------------------
// Audit base — shared by every entity
// -------------------------------------------------------
public abstract class AuditableEntity
{
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    public bool active { get; set; } = true;
}

public interface IHasTenantId
{
    int? TenantId { get; set; }
}

// -------------------------------------------------------
// Package
// -------------------------------------------------------
public class Package : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Tenant> Tenants { get; set; } = [];
}

// -------------------------------------------------------
// Tenant
// -------------------------------------------------------
public class Tenant : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Url { get; set; }
    public int? PackageId { get; set; }

    public Package? Package { get; set; }
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Agency> Agencies { get; set; } = [];
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<VatProfile> VatProfiles { get; set; } = [];
}

// -------------------------------------------------------
// Role
// -------------------------------------------------------
public class Role : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<User> Users { get; set; } = [];
}

// -------------------------------------------------------
// User
// -------------------------------------------------------
public class User : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public int? RoleId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public int? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    public Role? Role { get; set; }
    public string Password { get; set; }
}

// -------------------------------------------------------
// Agency
// -------------------------------------------------------
public class Agency : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? TenantId { get; set; }
    public string? Status { get; set; }
    public decimal? RechargeLevel { get; set; }
    public decimal? Credits { get; set; }
    public bool Active { get; set; } = true;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public decimal? Rate { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<AgencyCustomer> AgencyCustomers { get; set; } = [];
}

// -------------------------------------------------------
// Customer
// -------------------------------------------------------
public class Customer : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public int? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<AgencyCustomer> AgencyCustomers { get; set; } = [];
}

// -------------------------------------------------------
// Vehicle
// -------------------------------------------------------
public class Vehicle : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public string Reg { get; set; } = null!;
    public bool Active { get; set; } = true;
    public int? OwnerId { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? TenantId { get; set; }

    public Customer? Owner { get; set; }
    public Tenant? Tenant { get; set; }
    public ICollection<Booking> Bookings { get; set; } = [];
}

// -------------------------------------------------------
// Service
// -------------------------------------------------------
public class Service : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<ServicePricing> Pricings { get; set; } = [];
    public ICollection<BookingService> BookingServices { get; set; } = [];
}

// -------------------------------------------------------
// ServicePricing — one row per pricing schedule version
// -------------------------------------------------------
public class ServicePricing : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int? TenantId { get; set; }
    public string Currency { get; set; } = "ZAR";
    public DateTime EffectiveFrom { get; set; }
    public string? Note { get; set; }

    public Service Service { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<ServicePricingTier> Tiers { get; set; } = [];
    public ICollection<BookingService> BookingServices { get; set; } = [];
}

// -------------------------------------------------------
// ServicePricingTier — day-range bands within a schedule
// -------------------------------------------------------
public class ServicePricingTier:IHasTenantId
{
    public int Id { get; set; }
    public int ServicePricingId { get; set; }
    public int DayFrom { get; set; }
    public int? DayTo { get; set; }             // null = open-ended (DayFrom+)
    public decimal PricePerDay { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    public ServicePricing ServicePricing { get; set; } = null!;
    public int? TenantId { get; set; }
}

// -------------------------------------------------------
// Booking
// -------------------------------------------------------
public class Booking : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public int? CustomerId { get; set; }
    public int? VehicleId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Voucher { get; set; }
    public int? AgencyId { get; set; }
    public string? Status { get; set; }

    public Tenant? Tenant { get; set; }
    public Customer? Customer { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Agency? Agency { get; set; }
    public ICollection<BookingService> BookingServices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

// -------------------------------------------------------
// BookingService — junction with pricing snapshot
// -------------------------------------------------------
public class BookingService
{
    public int BookingId { get; set; }
    public int ServiceId { get; set; }
    public int ServicePricingId { get; set; }
    public int ServicePricingTierId { get; set; }
    public int Days { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal PriceCharged { get; set; }

    public Booking Booking { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public ServicePricing ServicePricing { get; set; } = null!;
    public ServicePricingTier ServicePricingTier { get; set; } = null!;
}

// -------------------------------------------------------
// Payment
// -------------------------------------------------------
public class Payment : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public decimal Amount { get; set; }
    public int BookingId { get; set; }

    public Tenant? Tenant { get; set; }
    public Booking? Booking { get; set; }
}

// -------------------------------------------------------
// VatProfile
// -------------------------------------------------------
public class VatProfile : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Rate { get; set; }
    public bool Active { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = [];
}

// -------------------------------------------------------
// Invoice
// -------------------------------------------------------
public class Invoice : AuditableEntity,IHasTenantId
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public decimal? Total { get; set; }
    public int VatProfileId { get; set; }
    public decimal? VatAmount { get; set; }
    public string? Number { get; set; }

    public Tenant? Tenant { get; set; }
    public VatProfile VatProfile { get; set; } = null!;
    public ICollection<InvoiceItem> Items { get; set; } = [];
}

// -------------------------------------------------------
// InvoiceItem
// -------------------------------------------------------
public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }

    public Invoice Invoice { get; set; } = null!;
}

// -------------------------------------------------------
// Junction: AgencyCustomer
// -------------------------------------------------------
public class AgencyCustomer
{
    public int AgencyId { get; set; }
    public int CustomerId { get; set; }

    public Agency Agency { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}

// -------------------------------------------------------
// Junction: TenantUser
// -------------------------------------------------------
public class TenantUser
{
    public int TenantId { get; set; }
    public int UserId { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
}
