using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SamMALsurium.Models;

namespace SamMALsurium.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IConfiguration _configuration;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure AdminAuditLog relationships
        builder.Entity<AdminAuditLog>()
            .HasOne(a => a.AdminUser)
            .WithMany()
            .HasForeignKey(a => a.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AdminAuditLog>()
            .HasOne(a => a.TargetUser)
            .WithMany()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed roles
        var adminRoleId = "1";
        var memberRoleId = "2";

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = adminRoleId
            },
            new IdentityRole
            {
                Id = memberRoleId,
                Name = "Member",
                NormalizedName = "MEMBER",
                ConcurrencyStamp = memberRoleId
            }
        );

        // Seed admin user
        var adminUserId = "1";
        var adminEmail = _configuration["AdminSeedData:AdminEmail"] ?? "admin@sammalsurium.local";
        var adminFirstName = _configuration["AdminSeedData:AdminFirstName"] ?? "Admin";
        var adminLastName = _configuration["AdminSeedData:AdminLastName"] ?? "User";

        // Pre-computed hash for password "Admin@123456!" using PasswordHasher
        var adminPasswordHash = "AQAAAAIAAYagAAAAEDXSxNvlbUhsE/RmXu59+JHIcgkGrUgePiyDq55+S6szKxMkCsqSxTPw05tCKMGIMg==";

        builder.Entity<ApplicationUser>().HasData(new ApplicationUser
        {
            Id = adminUserId,
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpper(),
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpper(),
            EmailConfirmed = true,
            FirstName = adminFirstName,
            LastName = adminLastName,
            SecurityStamp = "admin-seed-security-stamp",
            PasswordHash = adminPasswordHash,
            IsApproved = true,
            AccountStatus = Models.Enums.AccountStatus.Active
        });

        // Assign admin user to Admin role
        builder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            }
        );
    }
}
