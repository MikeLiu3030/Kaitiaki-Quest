using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Migrate database (Ensure tables have been created)
            await context.Database.MigrateAsync();

            // 2. Create roles
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

            // 3. Create Admin account
            var adminEmail = "admin@kaitiaki.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    TotalXP = 0,
                    Level = 1,
                    CurrentStreak = 0
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 4. Create a test user
            var userEmail = "user@kaitiaki.com";
            var testUser = await userManager.FindByEmailAsync(userEmail);
            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true,
                    TotalXP = 0,
                    Level = 1,
                    CurrentStreak = 0
                };

                var result = await userManager.CreateAsync(testUser, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
            }

            // 5. Seed or Update EcoMissions
            var missionsToSeed = new[]
            {
                new
                {
                    Title = "♻️ Recycle 10 plastic bottles",
                    Description = "Collect and properly sort 10 plastic bottles for recycling. Upload a photo as proof.",
                    BasePoints = 30,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1532996128854-2c5b7d5aee6d?w=400",
                    IsDaily = true
                },
                new
                {
                    Title = "🚶 Walk or Bike 2 km",
                    Description = "Choose to walk or bike instead of driving for at least 2 km.",
                    BasePoints = 25,
                    Category = "Transport",
                    ImageUrl = "https://images.unsplash.com/photo-1571068316344-75bc76f77890?w=400",
                    IsDaily = false
                },
                new
                {
                    Title = "💡 Save Energy for 1 Hour",
                    Description = "Turn off all unnecessary lights and electronics for 1 hour.",
                    BasePoints = 20,
                    Category = "Energy",
                    ImageUrl = "https://images.unsplash.com/photo-1581091226033-d5c48150dbaa?w=400",
                    IsDaily = true
                },
                new
                {
                    Title = "🌳 Plant a Tree or Plant",
                    Description = "Plant a native tree or plant to contribute to New Zealand's green ecosystem.",
                    BasePoints = 50,
                    Category = "Planting",
                    ImageUrl = "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=400",
                    IsDaily = false
                },
                new
                {
                    Title = "🛍️ Use a Reusable Shopping Bag",
                    Description = "Use a reusable cloth bag for shopping and refuse plastic bags.",
                    BasePoints = 15,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=400",
                    IsDaily = false
                }
            };

            // Iterate through each mission to check if it exists
            foreach (var missionData in missionsToSeed)
            {
                // Use Title as the unique identifier to find the mission
                var existingMission = await context.EcoMissions.FirstOrDefaultAsync(m => m.Title == missionData.Title);

                if (existingMission == null)
                {
                    // If the mission does not exist, create a new one
                    var newMission = new EcoMission
                    {
                        Title = missionData.Title,
                        Description = missionData.Description,
                        BasePoints = missionData.BasePoints,
                        Category = missionData.Category,
                        ImageUrl = missionData.ImageUrl,
                        IsDaily = missionData.IsDaily,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.EcoMissions.Add(newMission);
                }
                else
                {
                    // If the mission already exists, update its mutable fields
                    existingMission.Description = missionData.Description;
                    existingMission.BasePoints = missionData.BasePoints;
                    existingMission.ImageUrl = missionData.ImageUrl;
                    existingMission.IsDaily = missionData.IsDaily;
                }
            }

            // 6. Seed or Update Badges
            var badgesToSeed = new[]
            {
                new { Name = "🌱 Green Sprout", Description = "Complete your first task", UnlockXP = 10 },
                new { Name = "🌿 Eco Defender", Description = "Earn a total of 100 XP", UnlockXP = 100 },
                new { Name = "♻️ Recycling Master", Description = "Earn a total of 500 XP", UnlockXP = 500 },
                new { Name = "🌟 The Guardian", Description = "Earn a total of 1000 XP", UnlockXP = 1000 },
                new { Name = "🔥 Combo King", Description = "Complete tasks for 7 consecutive days", UnlockXP = 700 },
                new { Name = "🌏 Eco Legend", Description = "Earn a total of 2000 XP", UnlockXP = 2000 },
                new { Name = "♻️ Recycling Hero", Description = "Earn a total of 3000 XP", UnlockXP = 3000 },
                new { Name = "💡 Energy Saver", Description = "Earn a total of 4000 XP", UnlockXP = 4000 },
                new { Name = "🌳 Tree Planter", Description = "Earn a total of 5000 XP", UnlockXP = 5000 },
                new { Name = "⚔️ Eco Warrior", Description = "Earn a total of 7500 XP", UnlockXP = 7500 }
            };

            // Iterate through each badge to check if it exists
            foreach (var badgeData in badgesToSeed)
            {
                // Use Name as the unique identifier to find the badge
                var existingBadge = await context.Badges.FirstOrDefaultAsync(b => b.Name == badgeData.Name);

                if (existingBadge == null)
                {
                    // If the badge does not exist, create a new one
                    var newBadge = new Badge
                    {
                        Name = badgeData.Name,
                        Description = badgeData.Description,
                        UnlockXP = badgeData.UnlockXP,
                        IsActive = true
                    };
                    context.Badges.Add(newBadge);
                }
                else
                {
                    // If the badge already exists, update its mutable fields
                    existingBadge.Description = badgeData.Description;
                    existingBadge.UnlockXP = badgeData.UnlockXP;
                }
            }

            // Save all changes to the database in a single transaction
            await context.SaveChangesAsync();
        }
    }
}