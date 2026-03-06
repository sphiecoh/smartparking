using AdminSite.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminSite.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       optionsBuilder.LogTo(Console.WriteLine);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
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

        b.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Url).HasColumnName("url");
            e.Property(x => x.active).HasColumnName("active").HasDefaultValue(true);
            e.Property(x => x.PackageId).HasColumnName("packageid");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x => x.ModifiedDate).HasColumnName("modified_date");
            e.HasOne(x => x.Package).WithMany(p => p.Tenants).HasForeignKey(x => x.PackageId);
        });

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
            e.HasOne(x => x.Tenant).WithMany(t => t.Roles).HasForeignKey(x => x.TenantId);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Username).HasColumnName("username").IsRequired();
            e.Property(x => x.Password).HasColumnName("password");
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
    }
}
