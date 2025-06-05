using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SonicPoints.Models;

namespace SonicPoints.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<IdentityUserClaim<string>> UserClaims { get; set; }
        public DbSet<IdentityUserRole<string>> UserRoles { get; set; }
        public DbSet<IdentityUserLogin<string>> UserLogins { get; set; }
        public DbSet<IdentityRoleClaim<string>> RoleClaims { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }


        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectUser> ProjectUsers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Leaderboard> Leaderboards { get; set; }
        public DbSet<RedeemableItem> RedeemableItems { get; set; }
        public DbSet<RedeemHistory> RedeemHistory { get; set; }
        public DbSet<ProjectUserPoints> ProjectUserPoints { get; set; }

        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TaskItem ↔ Project & User
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project ↔ Admin
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Admin)
                .WithMany()
                .HasForeignKey(p => p.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // RedeemHistory ↔ User, Project, Item
            modelBuilder.Entity<RedeemHistory>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RedeemHistory>()
                .HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RedeemHistory>()
                .HasOne(r => r.RedeemableItem)
                .WithMany()
                .HasForeignKey(r => r.RedeemableItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Leaderboard ↔ User & Task
            modelBuilder.Entity<Leaderboard>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<Leaderboard>()
                .HasOne(l => l.User)
                .WithMany(u => u.TaskCompletions)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Leaderboard>()
                .HasOne(l => l.Task)
                .WithMany(t => t.Leaderboards)
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProjectUser ↔ Project & User
            modelBuilder.Entity<ProjectUser>()
                .HasOne(pu => pu.Project)
                .WithMany(p => p.ProjectUsers)
                .HasForeignKey(pu => pu.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectUser>()
                .HasOne(pu => pu.User)
                .WithMany(u => u.ProjectUsers)
                .HasForeignKey(pu => pu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
    .HasIndex(u => u.Email)
    .IsUnique();

        }
    }
}
