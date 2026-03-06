namespace AdminSite.Infrastructure.Data.Entities;

public abstract class AuditableEntity
{
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class Package : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Tenant> Tenants { get; set; } = [];
}

public class Tenant : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Url { get; set; }
    public bool active { get; set; } = true;
    public int? PackageId { get; set; }
    public Package? Package { get; set; }
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
}

public class Role : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public ICollection<User> Users { get; set; } = [];
}

public class User : AuditableEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string? Password { get; set; }
    public int? RoleId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public int? TenantId { get; set; } 
    public Tenant? Tenant { get; set; }
    public Role? Role { get; set; }
    public bool active { get; set; } = true;
}
