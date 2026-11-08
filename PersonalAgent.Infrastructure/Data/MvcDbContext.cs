using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Core.Entities;
using PersonalAgent.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace PersonalAgent.Infrastructure.Data;

public class MvcDbContext : IdentityDbContext<MvcApplicationUser>
{
    public MvcDbContext(DbContextOptions<MvcDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(x => x.LoginProvider)
                .HasMaxLength(128);

            entity.Property(x => x.ProviderKey)
                .HasMaxLength(128);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(x => x.LoginProvider)
                .HasMaxLength(128);

            entity.Property(x => x.Name)
                .HasMaxLength(128);
        });


        builder.Entity<Project>()
            .HasMany(p => p.Issues)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Issue>()
            .HasOne<MvcApplicationUser>()
            .WithMany(u => u.AssignedIssues)
            .HasForeignKey(i => i.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<IssueComment>()
            .HasOne(c => c.Issue)
            .WithMany(i => i.Comments)
            .HasForeignKey(c => c.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IssueComment>()
            .HasOne<MvcApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}