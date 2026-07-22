using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() { }
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
        }
    }
}
