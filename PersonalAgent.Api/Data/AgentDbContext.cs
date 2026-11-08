using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Api.Data;

public class AgentDbContext : IdentityDbContext<ApplicationUser>
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Preference> Preferences => Set<Preference>();
    public DbSet<WatchRule> WatchRules => Set<WatchRule>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Interest>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Category).IsRequired().HasMaxLength(50);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Interests)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Preference>(entity =>
        {
            entity.Property(x => x.Key).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Value).IsRequired().HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Preferences)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Interest)
                .WithMany(x => x.Preferences)
                .HasForeignKey(x => x.InterestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WatchRule>(entity =>
        {
            entity.Property(x => x.Title).IsRequired().HasMaxLength(100);
            entity.Property(x => x.SearchTerm).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.MaxPrice).HasPrecision(18, 2);

            entity.HasOne(x => x.User)
                .WithMany(x => x.WatchRules)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Interest)
                .WithMany(x => x.WatchRules)
                .HasForeignKey(x => x.InterestId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Interest
                && entry.Entity is not Preference
                && entry.Entity is not WatchRule)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = now;
                entry.Property("UpdatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("CreatedAt").IsModified = false;
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}