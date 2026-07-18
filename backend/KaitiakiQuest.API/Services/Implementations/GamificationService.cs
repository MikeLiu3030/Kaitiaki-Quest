using Microsoft.EntityFrameworkCore;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Interfaces;

namespace KaitiakiQuest.API.Services.Implementations
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationService> _logger;

        public GamificationService(
            ApplicationDbContext context,
            ILogger<GamificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Handle task completion: calculate final XP (including combo bonus)
        /// </summary>
        public async Task<int> ProcessMissionCompletion(string userId, UserMission userMission)
        {
            if (userMission.EcoMission == null)
                return 10; // Guaranteed Points

            var basePoints = userMission.EcoMission.BasePoints;
            var bonusMultiplier = 1.0;

            // 1. Check combo bonus: 1.5x points if streak >= 7 days
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentStreak >= 7)
            {
                bonusMultiplier = 1.5;
                _logger.LogInformation($"User {userId} has streak {user.CurrentStreak}, applying 1.5x bonus!");
            }

            // 2. Check if daily task is completed (extra +5 XP)
            var isDailyBonus = userMission.EcoMission.IsDaily ? 5 : 0;

            // 3. Calculate the final XP.
            var earnedXP = (int)(basePoints * bonusMultiplier) + isDailyBonus;

            // 4. save to userMission (the caller is responsible for SaveChanges)
            userMission.EarnedXP = earnedXP;

            _logger.LogInformation($"User {userId} earned {earnedXP} XP for mission {userMission.EcoMission.Title}");

            return earnedXP;
        }

        /// <summary>
        /// Update the user's streak status
        /// </summary>
        public async Task UpdateStreak(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var today = DateTime.UtcNow.Date;
            var lastCompleted = user.LastMissionCompleteDate?.Date;

            if (lastCompleted == null)
            {
                // First-time task completion
                user.CurrentStreak = 1;
            }
            else if (lastCompleted == today)
            {
                // Task already completed today, do not calculate again (prevent duplicate triggers)
                return;
            }
            else if (lastCompleted == today.AddDays(-1))
            {
                // Task completed yesterday, continue the streak
                user.CurrentStreak += 1;
            }
            else
            {
                // Task not completed for over 1 day, reset streak
                user.CurrentStreak = 1;
            }

            user.LastMissionCompleteDate = today;

            _logger.LogInformation($"User {userId} streak updated to {user.CurrentStreak}");
        }

        /// <summary>
        /// Check and award new badges
        /// </summary>
        public async Task CheckAndAwardBadges(string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return;
            await _context.Entry(user).Collection(u => u.UserBadges).LoadAsync();

            // Get list of badge IDs owned by the user
            var existingBadgeIds = user.UserBadges.Select(ub => ub.BadgeId).ToHashSet();

            // Get all unlockable badges (sorted by UnlockXP in ascending order)
            var allBadges = await _context.Badges
                .Where(b => b.IsActive)
                .OrderBy(b => b.UnlockXP)
                .ToListAsync();

            var newlyAwardedBadges = new List<UserBadge>();

            foreach (var badge in allBadges)
            {
                // Skip if badge is already owned
                if (existingBadgeIds.Contains(badge.Id))
                    continue;

                // Check if user's total XP meets the unlock requirement
                if (user.TotalXP >= badge.UnlockXP)
                {
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id,
                        AwardedDate = DateTime.UtcNow
                    };

                    newlyAwardedBadges.Add(userBadge);
                    existingBadgeIds.Add(badge.Id); // Prevent duplicate awards within the same batch

                    _logger.LogInformation($"🎖️ User {userId} unlocked badge: {badge.Name}");
                }
            }

            // Batch add new badges
            if (newlyAwardedBadges.Any())
            {
                _context.UserBadges.AddRange(newlyAwardedBadges);

            }
        }

        /// <summary>
        /// Get user's current level (based on TotalXP)
        /// </summary>
        public int CalculateLevel(int totalXP)
        {
            // Level formula: 1 level per 100 XP, starting at level 1
            return totalXP / 100 + 1;
        }

        /// <summary>
        /// Get progress for the next badge (for frontend display)
        /// </summary>
        public async Task<object?> GetNextBadgeProgress(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var existingBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToHashSetAsync();

            var nextBadge = await _context.Badges
                .Where(b => b.IsActive && !existingBadgeIds.Contains(b.Id))
                .OrderBy(b => b.UnlockXP)
                .FirstOrDefaultAsync();

            if (nextBadge == null)
                return new { HasNextBadge = false };

            return new
            {
                HasNextBadge = true,
                nextBadge.Name,
                nextBadge.UnlockXP,
                CurrentXP = user.TotalXP,
                Progress = Math.Min(100, (double)user.TotalXP / nextBadge.UnlockXP * 100)
            };
        }
    }
}