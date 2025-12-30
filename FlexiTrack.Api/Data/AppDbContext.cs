using FlexiTrack.Api.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskLog> TaskLogs => Set<TaskLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Created).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(u => u.WeeklyHoursTarget).HasPrecision(5, 2);

            entity.HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        builder.Entity<TaskLog>(entity =>
        {
            entity.Property(t => t.Description).HasMaxLength(500).IsRequired();
            entity.Property(t => t.Client).HasMaxLength(200);
            entity.Property(t => t.Created).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.TaskItem)
                .WithMany(ti => ti.TaskLogs)
                .HasForeignKey(t => t.TaskItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TaskItem>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.Created).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(t => t.AssignedUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
