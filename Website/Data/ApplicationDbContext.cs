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
    public DbSet<Image> Images { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollOption> PollOptions { get; set; }
    public DbSet<PollVote_MultipleChoice> PollVotes_MultipleChoice { get; set; }
    public DbSet<PollVote_RankedChoice> PollVotes_RankedChoice { get; set; }
    public DbSet<PollVote_ScoreVoting> PollVotes_ScoreVoting { get; set; }
    public DbSet<PollVote_AvailabilityGrid> PollVotes_AvailabilityGrid { get; set; }
    public DbSet<PollVoteHistory> PollVoteHistories { get; set; }

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

        // Configure Image relationships
        builder.Entity<Image>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Create indexes for Image entity
        builder.Entity<Image>()
            .HasIndex(i => i.UserId);

        builder.Entity<Image>()
            .HasIndex(i => i.UploadedAt);

        // Configure Event relationships
        builder.Entity<Event>()
            .HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Event>()
            .HasIndex(e => e.CreatedById);

        builder.Entity<Event>()
            .HasIndex(e => e.StartDate);

        // Configure Poll relationships
        builder.Entity<Poll>()
            .HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Poll>()
            .HasOne(p => p.Event)
            .WithMany(e => e.Polls)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for Poll
        builder.Entity<Poll>()
            .HasIndex(p => p.EventId);

        builder.Entity<Poll>()
            .HasIndex(p => p.Status);

        builder.Entity<Poll>()
            .HasIndex(p => p.CreatedById);

        builder.Entity<Poll>()
            .HasIndex(p => p.CreatedAt);

        // Configure PollOption relationships
        builder.Entity<PollOption>()
            .HasOne(po => po.Poll)
            .WithMany(p => p.Options)
            .HasForeignKey(po => po.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PollOption>()
            .HasIndex(po => po.PollId);

        // Configure PollVote_MultipleChoice
        builder.Entity<PollVote_MultipleChoice>()
            .HasOne(v => v.Poll)
            .WithMany()
            .HasForeignKey(v => v.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PollVote_MultipleChoice>()
            .HasOne(v => v.Option)
            .WithMany()
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PollVote_MultipleChoice>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uniqueness constraint for single-select multiple choice (PollId + UserId)
        // Note: This will be conditionally enforced in application logic based on IsMultiSelect
        builder.Entity<PollVote_MultipleChoice>()
            .HasIndex(v => new { v.PollId, v.UserId, v.OptionId })
            .IsUnique();

        // Configure PollVote_RankedChoice
        builder.Entity<PollVote_RankedChoice>()
            .HasOne(v => v.Poll)
            .WithMany()
            .HasForeignKey(v => v.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PollVote_RankedChoice>()
            .HasOne(v => v.Option)
            .WithMany()
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PollVote_RankedChoice>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uniqueness constraint: Each user can rank each option only once
        builder.Entity<PollVote_RankedChoice>()
            .HasIndex(v => new { v.PollId, v.UserId, v.OptionId })
            .IsUnique();

        // Uniqueness constraint: Each user can assign each rank only once per poll
        builder.Entity<PollVote_RankedChoice>()
            .HasIndex(v => new { v.PollId, v.UserId, v.Rank })
            .IsUnique();

        // Configure PollVote_ScoreVoting
        builder.Entity<PollVote_ScoreVoting>()
            .HasOne(v => v.Poll)
            .WithMany()
            .HasForeignKey(v => v.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PollVote_ScoreVoting>()
            .HasOne(v => v.Option)
            .WithMany()
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PollVote_ScoreVoting>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uniqueness constraint: Each user can score each option only once
        builder.Entity<PollVote_ScoreVoting>()
            .HasIndex(v => new { v.PollId, v.UserId, v.OptionId })
            .IsUnique();

        // Configure PollVote_AvailabilityGrid
        builder.Entity<PollVote_AvailabilityGrid>()
            .HasOne(v => v.Poll)
            .WithMany()
            .HasForeignKey(v => v.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PollVote_AvailabilityGrid>()
            .HasOne(v => v.Option)
            .WithMany()
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PollVote_AvailabilityGrid>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uniqueness constraint: Each user can vote on each time slot only once
        builder.Entity<PollVote_AvailabilityGrid>()
            .HasIndex(v => new { v.PollId, v.UserId, v.OptionId })
            .IsUnique();

        // Configure PollVoteHistory
        builder.Entity<PollVoteHistory>()
            .HasOne(h => h.Poll)
            .WithMany()
            .HasForeignKey(h => h.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PollVoteHistory>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PollVoteHistory>()
            .HasIndex(h => new { h.PollId, h.UserId, h.ChangedAt });

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
