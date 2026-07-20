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
            // 5. Seed or Update EcoMissions
            var missionsToSeed = new[]
             {
                // ===== (Recycling) =====
                new EcoMission
                {
                    Title = "♻️ Recycle 10 plastic bottles",
                    Description = "Collect and properly sort 10 plastic bottles for recycling. Upload a photo as proof.",
                    BasePoints = 30,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1532996128854-2c5b7d5aee6d?w=400",
                    IsDaily = true
                },
                new EcoMission
                {
                    Title = "♻️ Recycle 20 glass jars",
                    Description = "Collect and recycle 20 glass jars. Glass can be recycled infinitely!",
                    BasePoints = 40,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1585515321534-0841b24b4ea2?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "♻️ Sort your waste for a week",
                    Description = "Separate your waste into recycling, compost, and landfill for 7 consecutive days.",
                    BasePoints = 60,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1604187351574-c75ca79f2f0b?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🛍️ Use a Reusable Shopping Bag",
                    Description = "Use a reusable cloth bag for shopping and refuse plastic bags.",
                    BasePoints = 15,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "♻️ Recycle 20 aluminium cans",
                    Description = "Collect and recycle 20 aluminium cans. Aluminium can be recycled forever!",
                    BasePoints = 35,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1564554686612-ff3992d3b268?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "♻️ Upcycle old clothes into shopping bags",
                    Description = "Transform old t-shirts into reusable shopping bags. Zero waste!",
                    BasePoints = 45,
                    Category = "Recycling",
                    ImageUrl = "https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=400",
                    IsDaily = false
                },

                // =====  (Transport) =====
                new EcoMission
                {
                    Title = "🚶 Walk or Bike 2 km",
                    Description = "Choose to walk or bike instead of driving for at least 2 km.",
                    BasePoints = 25,
                    Category = "Transport",
                    ImageUrl = "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🚶 Walk or Bike 5 km",
                    Description = "Choose to walk or bike instead of driving for at least 5 km.",
                    BasePoints = 40,
                    Category = "Transport",
                    ImageUrl = "https://images.unsplash.com/photo-1571068316344-75bc76f77890?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🚌 Take public transport 5 days in a row",
                    Description = "Use public transport (bus, train, ferry) for 5 consecutive days instead of driving.",
                    BasePoints = 50,
                    Category = "Transport",
                    ImageUrl = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🛴 Use an e-scooter for a week",
                    Description = "Use an e-scooter or electric bike for your daily commute for 7 days.",
                    BasePoints = 55,
                    Category = "Transport",
                    ImageUrl = "https://images.unsplash.com/photo-1590362895031-4bb7c3f2c25a?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🚗 Carpool with a colleague",
                    Description = "Arrange a carpool and share a ride with at least one other person.",
                    BasePoints = 30,
                    Category = "Transport",
                    ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=400", 
                    IsDaily = false
                },

                // =====  (Energy) =====
                new EcoMission
                {
                    Title = "💡 Save Energy for 1 Hour",
                    Description = "Turn off all unnecessary lights and electronics for 1 hour.",
                    BasePoints = 20,
                    Category = "Energy",
                    ImageUrl = "https://images.unsplash.com/photo-1581091226033-d5c48150dbaa?w=400",
                    IsDaily = true
                },
                new EcoMission
                {
                    Title = "💡 Save Energy for 3 Hours",
                    Description = "Turn off all unnecessary lights and electronics for 3 hours.",
                    BasePoints = 40,
                    Category = "Energy",
                    ImageUrl = "https://images.unsplash.com/photo-1581091226033-d5c48150dbaa?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "💡 Replace 5 light bulbs with LEDs",
                    Description = "Replace 5 traditional light bulbs with energy-efficient LEDs in your home.",
                    BasePoints = 35,
                    Category = "Energy",
                    ImageUrl = "https://images.unsplash.com/photo-1497366216548-37526070297c?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "💡 Unplug idle electronics for a day",
                    Description = "Unplug all electronics that are not in use for 24 hours (phone chargers, TVs, etc.).",
                    BasePoints = 25,
                    Category = "Energy",
                    ImageUrl = "https://images.unsplash.com/photo-1590496793929-7e2e5b2f0c6c?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "💡 Use natural light all day",
                    Description = "Don't turn on any lights during daylight hours. Use natural light instead.",
                    BasePoints = 30,
                    Category = "Energy",
                    ImageUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=400", 
                    IsDaily = false
                },

                // ===== (Planting) =====
                new EcoMission
                {
                    Title = "🌳 Plant a Tree or Plant",
                    Description = "Plant a native tree or plant to contribute to New Zealand's green ecosystem.",
                    BasePoints = 50,
                    Category = "Planting",
                    ImageUrl = "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🌱 Start a herb garden",
                    Description = "Plant a small herb garden with basil, mint, or parsley in your kitchen or balcony.",
                    BasePoints = 35,
                    Category = "Planting",
                    ImageUrl = "https://images.unsplash.com/photo-1585320806297-9794b3e4eeae?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🌳 Plant 5 native trees",
                    Description = "Plant 5 native trees in your local community area.",
                    BasePoints = 80,
                    Category = "Planting",
                    ImageUrl = "https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🌿 Create a composting system",
                    Description = "Set up a composting system for your kitchen waste. Reduce landfill waste!",
                    BasePoints = 45,
                    Category = "Planting",
                    ImageUrl = "https://images.unsplash.com/photo-1586105251261-72a756497a11?w=400", 
                    IsDaily = false
                },

                // ===== (Water) =====
                new EcoMission
                {
                    Title = "💧 Reduce shower time to 5 minutes",
                    Description = "Take a shower of 5 minutes or less. Save water and energy!",
                    BasePoints = 20,
                    Category = "Water",
                    ImageUrl = "https://images.unsplash.com/photo-1541040998045-7cb75fcb141a?w=400",
                    IsDaily = true
                },
                new EcoMission
                {
                    Title = "💧 Fix a leaking tap",
                    Description = "Fix a dripping tap. A leaking tap can waste up to 15 litres of water per day!",
                    BasePoints = 30,
                    Category = "Water",
                    ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85f94a?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "💧 Install a water-saving shower head",
                    Description = "Install a water-efficient shower head to reduce water consumption.",
                    BasePoints = 40,
                    Category = "Water",
                    ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85f94a?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "💧 Collect rainwater for plants",
                    Description = "Set up a rainwater collection system and use it to water your plants.",
                    BasePoints = 35,
                    Category = "Water",
                    ImageUrl = "https://images.unsplash.com/photo-1563299796-17596ed6b017?w=400", 
                    IsDaily = false
                },

                // ===== (Education) =====
                new EcoMission
                {
                    Title = "📚 Watch a documentary about climate change",
                    Description = "Watch a documentary about climate change and share one key insight.",
                    BasePoints = 25,
                    Category = "Education",
                    ImageUrl = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "📚 Read an article about New Zealand's native wildlife",
                    Description = "Read an article about conservation and New Zealand's native wildlife.",
                    BasePoints = 20,
                    Category = "Education",
                    ImageUrl = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "📚 Teach someone about recycling",
                    Description = "Share recycling knowledge with a friend, family member, or colleague.",
                    BasePoints = 15,
                    Category = "Education",
                    ImageUrl = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "📚 Learn 5 Māori words for nature",
                    Description = "Learn and use 5 Māori words related to nature and the environment.",
                    BasePoints = 15,
                    Category = "Education",
                    ImageUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=400", 
                    IsDaily = false
                },

                // ===== (Community) =====
                new EcoMission
                {
                    Title = "🧹 Join a community clean-up event",
                    Description = "Participate in a local beach or park clean-up event in your community.",
                    BasePoints = 60,
                    Category = "Community",
                    ImageUrl = "https://images.unsplash.com/photo-1523741543316-beb7f4f7d4f4?w=400", 
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🧹 Organize a community clean-up",
                    Description = "Organize a local clean-up event and invite friends and neighbours to join.",
                    BasePoints = 80,
                    Category = "Community",
                    ImageUrl = "https://images.unsplash.com/photo-1523741543316-beb7f4f7d4f4?w=400",
                    IsDaily = false
                },
                new EcoMission
                {
                    Title = "🧹 Adopt a street to keep clean",
                    Description = "Adopt a street or park in your area and keep it clean for a month.",
                    BasePoints = 70,
                    Category = "Community",
                    ImageUrl = "https://images.unsplash.com/photo-1449157291148-3d40066a5b4c?w=400", // 街道
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