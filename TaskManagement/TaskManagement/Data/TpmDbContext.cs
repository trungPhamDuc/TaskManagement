using Microsoft.EntityFrameworkCore;
using TaskManagement.Models;

namespace TaskManagement.Data;

public class TpmDbContext(DbContextOptions<TpmDbContext> options) : DbContext(options)
{
    public DbSet<TpmUser> Users => Set<TpmUser>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();
    public DbSet<TaskNotification> Notifices => Set<TaskNotification>();
    public DbSet<TaskImage> TaskImages => Set<TaskImage>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TpmUser>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Username).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(24);
            entity.Property(user => user.Group).HasConversion<string>().HasMaxLength(24);
            entity.Property(user => user.Area).HasConversion<string>().HasMaxLength(24);
        });

        modelBuilder.Entity<WorkTask>(entity =>
        {
            entity.HasIndex(task => task.Key).IsUnique();
            entity.Property(task => task.Key).HasMaxLength(20).IsRequired();
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Group).HasConversion<string>().HasMaxLength(24);
            entity.Property(task => task.Area).HasConversion<string>().HasMaxLength(24);
            entity.Property(task => task.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(task => task.Priority).HasConversion<string>().HasMaxLength(24);
            entity.HasOne(task => task.CreatedBy).WithMany(user => user.CreatedTasks)
                .HasForeignKey(task => task.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(task => task.Assignee).WithMany(user => user.AssignedTasks)
                .HasForeignKey(task => task.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });


        modelBuilder.Entity<TaskNotification>(entity =>
        {
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead });
            entity.HasOne(notification => notification.User).WithMany()
                .HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(notification => notification.WorkTask).WithMany()
                .HasForeignKey(notification => notification.WorkTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskImage>(entity =>
        {
            entity.Property(image => image.FileName).HasMaxLength(255).IsRequired();
            entity.Property(image => image.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(image => image.Kind).HasConversion<string>().HasMaxLength(24);
            entity.Property(image => image.Data).HasColumnType("varbinary(max)").IsRequired();
            entity.HasOne(image => image.WorkTask).WithMany(task => task.Images)
                .HasForeignKey(image => image.WorkTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PushSubscription>(entity =>
        {
            entity.Property(subscription => subscription.Endpoint).HasMaxLength(2048).IsRequired();
            entity.Property(subscription => subscription.EndpointHash).HasMaxLength(64).IsRequired();
            entity.Property(subscription => subscription.P256dh).HasMaxLength(200).IsRequired();
            entity.Property(subscription => subscription.Auth).HasMaxLength(200).IsRequired();
            entity.HasIndex(subscription => subscription.EndpointHash).IsUnique();
            entity.HasIndex(subscription => subscription.UserId);
            entity.HasOne(subscription => subscription.User).WithMany()
                .HasForeignKey(subscription => subscription.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
