using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Business entity DbSets
        public DbSet<EcoMission> EcoMissions { get; set; }
        public DbSet<UserMission> UserMissions { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Team> Teams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity configuration must be called first

            // Configure the relationship between ApplicationUser and Team
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(u => u.TeamId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure composite index for UserMission (to improve query performance)
            modelBuilder.Entity<UserMission>()
                .HasIndex(um => new { um.UserId, um.Status });

            // Seed data: Initial Badges
            modelBuilder.Entity<Badge>().HasData(
                new Badge { Id = 1, Name = "🌱 Green Sprout", Description = "Complete your first mission", UnlockXP = 10 },
                new Badge { Id = 2, Name = "🌿 Eco Guardian", Description = "Reach a total of 100 XP", UnlockXP = 100 },
                new Badge { Id = 3, Name = "♻️ Recycling Master", Description = "Reach a total of 500 XP", UnlockXP = 500 },
                new Badge { Id = 4, Name = "🌟 Protector", Description = "Reach a total of 1000 XP", UnlockXP = 1000 },
                new Badge { Id = 5, Name = "🔥 Combo King", Description = "Complete missions for 7 consecutive days", UnlockXP = 500 } // Special logic to be handled later
            );
        }
    }
}
