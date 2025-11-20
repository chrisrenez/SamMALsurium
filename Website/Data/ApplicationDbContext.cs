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
    public DbSet<EventType> EventTypes { get; set; }
    public DbSet<EventMedia> EventMedia { get; set; }
    public DbSet<EventAttendee> EventAttendees { get; set; }
    public DbSet<EventArtwork> EventArtworks { get; set; }
    public DbSet<EventAnnouncement> EventAnnouncements { get; set; }
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
            .HasOne(e => e.Organizer)
            .WithMany()
            .HasForeignKey(e => e.OrganizedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Event>()
            .HasOne(e => e.EventType)
            .WithMany(et => et.Events)
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for Event entity
        builder.Entity<Event>()
            .HasIndex(e => e.CreatedById);

        builder.Entity<Event>()
            .HasIndex(e => e.StartDate);

        builder.Entity<Event>()
            .HasIndex(e => e.IsPublic);

        builder.Entity<Event>()
            .HasIndex(e => e.EventTypeId);

        builder.Entity<Event>()
            .HasIndex(e => e.OrganizedBy);

        builder.Entity<Event>()
            .HasIndex(e => e.IsActive);

        // Composite indexes for common query patterns
        builder.Entity<Event>()
            .HasIndex(e => new { e.IsPublic, e.StartDate });

        builder.Entity<Event>()
            .HasIndex(e => new { e.IsActive, e.StartDate });

        // Configure EventMedia relationships
        builder.Entity<EventMedia>()
            .HasOne(em => em.Event)
            .WithMany(e => e.EventMedia)
            .HasForeignKey(em => em.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventMedia>()
            .HasIndex(em => em.EventId);

        builder.Entity<EventMedia>()
            .HasIndex(em => new { em.EventId, em.DisplayOrder });

        // Configure EventAttendee relationships
        builder.Entity<EventAttendee>()
            .HasOne(ea => ea.Event)
            .WithMany(e => e.Attendees)
            .HasForeignKey(ea => ea.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventAttendee>()
            .HasOne(ea => ea.User)
            .WithMany()
            .HasForeignKey(ea => ea.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uniqueness constraint: Each user can only have one RSVP per event
        builder.Entity<EventAttendee>()
            .HasIndex(ea => new { ea.EventId, ea.UserId })
            .IsUnique();

        builder.Entity<EventAttendee>()
            .HasIndex(ea => ea.RsvpStatus);

        // Configure EventArtwork relationships
        builder.Entity<EventArtwork>()
            .HasOne(ea => ea.Event)
            .WithMany(e => e.EventArtworks)
            .HasForeignKey(ea => ea.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventArtwork>()
            .HasIndex(ea => ea.EventId);

        builder.Entity<EventArtwork>()
            .HasIndex(ea => ea.ArtworkId);

        // Uniqueness constraint: Each artwork can only be linked to an event once
        builder.Entity<EventArtwork>()
            .HasIndex(ea => new { ea.EventId, ea.ArtworkId })
            .IsUnique();

        // Configure EventAnnouncement relationships
        builder.Entity<EventAnnouncement>()
            .HasOne(ea => ea.Event)
            .WithMany(e => e.Announcements)
            .HasForeignKey(ea => ea.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EventAnnouncement>()
            .HasOne(ea => ea.Sender)
            .WithMany()
            .HasForeignKey(ea => ea.SentBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EventAnnouncement>()
            .HasIndex(ea => ea.EventId);

        builder.Entity<EventAnnouncement>()
            .HasIndex(ea => ea.SentAt);

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

        builder.Entity<Poll>()
            .HasIndex(p => p.EndDate);

        // Composite indexes for common query patterns
        builder.Entity<Poll>()
            .HasIndex(p => new { p.Status, p.EndDate });

        builder.Entity<Poll>()
            .HasIndex(p => new { p.EventId, p.Status });

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

        // Seed EventTypes
        builder.Entity<EventType>().HasData(
            new EventType
            {
                Id = 1,
                Name = "Workshop",
                Description = "Hands-on learning sessions focused on specific artistic techniques or skills"
            },
            new EventType
            {
                Id = 2,
                Name = "Exhibition",
                Description = "Art shows and gallery exhibitions showcasing member artwork"
            },
            new EventType
            {
                Id = 3,
                Name = "Meetup",
                Description = "Informal gatherings for artists to connect and socialize"
            },
            new EventType
            {
                Id = 4,
                Name = "Online Class",
                Description = "Virtual learning sessions and webinars"
            },
            new EventType
            {
                Id = 5,
                Name = "Critique Session",
                Description = "Constructive feedback sessions for artwork and creative projects"
            },
            new EventType
            {
                Id = 6,
                Name = "Other",
                Description = "Other types of events and gatherings"
            }
        );
    }
}
