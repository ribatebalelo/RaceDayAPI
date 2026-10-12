using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')");
                t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Draft','Published','Cancelled','Completed')");
            });
            e.HasKey(x => x.EventId);
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories", t => t.HasCheckConstraint("CK_Categories_Type", "[CategoryType] IN ('Age','Distance')"));
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.Price).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Pending','Confirmed','Cancelled')"));
            e.HasKey(x => x.EnrolmentId);
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
        });

        b.Entity<RaceResult>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished','DNF','DSQ')"));
            e.HasKey(x => x.ResultId);
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<RaceResult>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
        });
    }
}
