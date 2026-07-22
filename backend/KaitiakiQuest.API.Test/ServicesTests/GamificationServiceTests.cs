using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KaitiakiQuest.API.Tests.Services
{
    public class GamificationServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<GamificationService>> _loggerMock;
        private readonly GamificationService _service;

        public GamificationServiceTests()
        {
            // 1. Create SQLite in-member connection.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // 2. setup DbContext using SQLite
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);

            // 3. ensure the database schema has been created.
            _context.Database.EnsureCreated();

            // 4. Simulate ILogger
            _loggerMock = new Mock<ILogger<GamificationService>>();

            // 5. create a test service instance
            _service = new GamificationService(_context, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        // ====================================================================
        // ProcessMissionCompletion
        // ====================================================================

        [Fact]
        public async Task ProcessMissionCompletion_WhenStreakLessThan7_ReturnsBasePoints()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 3,
                TotalXP = 50
            };
            _context.Users.Add(user);

            var mission = new EcoMission
            {
                Id = 1,
                BasePoints = 30,
                IsDaily = false,
                Title = "Test Mission"
            };
            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();

            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = mission.Id,
                EcoMission = mission
            };

            // Act
            var result = await _service.ProcessMissionCompletion(userId, userMission);

            // Assert
            result.Should().Be(30);
            userMission.EarnedXP.Should().Be(30);
        }

        [Fact]
        public async Task ProcessMissionCompletion_WhenStreakGreaterThanOrEqualTo7_Applies1_5xBonus()
        {
            // Arrange
            var userId = "user-2";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 7,
                TotalXP = 100
            };
            _context.Users.Add(user);

            var mission = new EcoMission
            {
                Id = 2,
                BasePoints = 30,
                IsDaily = false,
                Title = "Test Mission 2"
            };
            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();

            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = mission.Id,
                EcoMission = mission
            };

            // Act
            var result = await _service.ProcessMissionCompletion(userId, userMission);

            // Assert
            result.Should().Be(45); // 30 * 1.5
        }

        [Fact]
        public async Task ProcessMissionCompletion_WhenDailyMission_Adds5BonusXP()
        {
            // Arrange
            var userId = "user-3";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 3,
                TotalXP = 50
            };
            _context.Users.Add(user);

            var mission = new EcoMission
            {
                Id = 3,
                BasePoints = 20,
                IsDaily = true,
                Title = "Daily Mission"
            };
            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();

            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = mission.Id,
                EcoMission = mission
            };

            // Act
            var result = await _service.ProcessMissionCompletion(userId, userMission);

            // Assert
            result.Should().Be(25); // 20 + 5
        }

        [Fact]
        public async Task ProcessMissionCompletion_WhenDailyMissionAndStreakGreaterThan7_CombinesBonuses()
        {
            // Arrange
            var userId = "user-4";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 10,
                TotalXP = 200
            };
            _context.Users.Add(user);

            var mission = new EcoMission
            {
                Id = 4,
                BasePoints = 20,
                IsDaily = true,
                Title = "Daily & Streak"
            };
            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();

            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = mission.Id,
                EcoMission = mission
            };

            // Act
            var result = await _service.ProcessMissionCompletion(userId, userMission);

            // Assert
            result.Should().Be(35); // (20 * 1.5) + 5
        }

        [Fact]
        public async Task ProcessMissionCompletion_WhenEcoMissionIsNull_ReturnsGuaranteed10XP()
        {
            // Arrange
            var userId = "user-5";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 0,
                TotalXP = 0
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userMission = new UserMission
            {
                UserId = userId,
                EcoMission = null // simulate the mission does't existe
            };

            // Act
            var result = await _service.ProcessMissionCompletion(userId, userMission);

            // Assert
            result.Should().Be(10);
        }

        // ====================================================================
        //  UpdateStreak
        // ====================================================================

        [Fact]
        public async Task UpdateStreak_WhenFirstTimeCompletion_SetsStreakTo1()
        {
            // Arrange
            var userId = "user-6";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 0,
                LastMissionCompleteDate = null
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdateStreak(userId);

            // Assert
            user.CurrentStreak.Should().Be(1);
            user.LastMissionCompleteDate.Should().Be(DateTime.UtcNow.Date);
        }

        [Fact]
        public async Task UpdateStreak_WhenAlreadyCompletedToday_DoesNotUpdateStreak()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var userId = "user-7";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 5,
                LastMissionCompleteDate = today
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdateStreak(userId);

            // Assert
            user.CurrentStreak.Should().Be(5);
            user.LastMissionCompleteDate.Should().Be(today);
        }

        [Fact]
        public async Task UpdateStreak_WhenCompletedYesterday_IncrementsStreak()
        {
            // Arrange
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var userId = "user-8";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 2,
                LastMissionCompleteDate = yesterday
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdateStreak(userId);

            // Assert
            user.CurrentStreak.Should().Be(3);
            user.LastMissionCompleteDate.Should().Be(DateTime.UtcNow.Date);
        }

        [Fact]
        public async Task UpdateStreak_WhenCompletedMoreThanOneDayAgo_ResetsStreakTo1()
        {
            // Arrange
            var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2);
            var userId = "user-9";
            var user = new ApplicationUser
            {
                Id = userId,
                CurrentStreak = 10,
                LastMissionCompleteDate = twoDaysAgo
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdateStreak(userId);

            // Assert
            user.CurrentStreak.Should().Be(1);
            user.LastMissionCompleteDate.Should().Be(DateTime.UtcNow.Date);
        }

        // ====================================================================
        // CheckAndAwardBadges
        // ====================================================================

        [Fact]
        public async Task CheckAndAwardBadges_WhenUserXPMeetsCondition_AwardsBadge()
        {
            // Arrange
            var userId = "user-10";
            var user = new ApplicationUser
            {
                Id = userId,
                TotalXP = 100,
                UserBadges = new List<UserBadge>()
            };
            _context.Users.Add(user);

            var badge1 = new Badge { Id = 1, Name = "Green Sprout", UnlockXP = 10, IsActive = true };
            var badge2 = new Badge { Id = 2, Name = "Eco Guardian", UnlockXP = 100, IsActive = true };
            _context.Badges.AddRange(badge1, badge2);
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndAwardBadges(userId);
            await _context.SaveChangesAsync();

            // Assert
            var awardedBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .ToListAsync();
            awardedBadges.Should().HaveCount(2);
            awardedBadges.Select(ub => ub.BadgeId).Should().Contain(new[] { 1, 2 });
        }

        [Fact]
        public async Task CheckAndAwardBadges_WhenUserXPDoesNotMeetCondition_AwardsNoBadges()
        {
            // Arrange
            var userId = "user-11";
            var user = new ApplicationUser
            {
                Id = userId,
                TotalXP = 50,
                UserBadges = new List<UserBadge>()
            };
            _context.Users.Add(user);

            var badge1 = new Badge { Id = 3, Name = "Recycling Master", UnlockXP = 500, IsActive = true };
            _context.Badges.Add(badge1);
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndAwardBadges(userId);

            // Assert
            var awardedBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .ToListAsync();
            awardedBadges.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckAndAwardBadges_WhenUserAlreadyHasBadge_DoesNotDuplicate()
        {
            // Arrange
            var userId = "user-12";
            var existingBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = 1,
                AwardedDate = DateTime.UtcNow
            };
            var user = new ApplicationUser
            {
                Id = userId,
                TotalXP = 500,
                UserBadges = new List<UserBadge> { existingBadge }
            };
            _context.Users.Add(user);

            var badge1 = new Badge { Id = 1, Name = "Green Sprout", UnlockXP = 10, IsActive = true };
            var badge2 = new Badge { Id = 2, Name = "Eco Guardian", UnlockXP = 100, IsActive = true };
            _context.Badges.AddRange(badge1, badge2);
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndAwardBadges(userId);
            await _context.SaveChangesAsync();


            var awardedBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .ToListAsync();
            awardedBadges.Should().HaveCount(2); // 已有1个 + 新增1个
            awardedBadges.Select(ub => ub.BadgeId).Should().Contain(new[] { 1, 2 });
        }

        // ====================================================================
        // CalculateLevel 
        // ====================================================================

        [Theory]
        [InlineData(0, 1)]
        [InlineData(50, 1)]
        [InlineData(99, 1)]
        [InlineData(100, 2)]
        [InlineData(150, 2)]
        [InlineData(250, 3)]
        [InlineData(1000, 11)]
        public void CalculateLevel_ReturnsCorrectLevel(int totalXP, int expectedLevel)
        {
            // Act
            var result = _service.CalculateLevel(totalXP);

            // Assert
            result.Should().Be(expectedLevel);
        }

        // ====================================================================
        //  GetNextBadgeProgress 
        // ====================================================================

        [Fact]
        public async Task GetNextBadgeProgress_WhenUserExistsAndHasNextBadge_ReturnsProgress()
        {
            // Arrange
            var userId = "user-13";
            var user = new ApplicationUser
            {
                Id = userId,
                TotalXP = 250,
                UserBadges = new List<UserBadge>
                {
                    new UserBadge {BadgeId = 10}
                }
            };
            _context.Users.Add(user);

            var badge1 = new Badge { Id = 10, Name = "Bronze", UnlockXP = 100, IsActive = true };
            var badge2 = new Badge { Id = 11, Name = "Silver", UnlockXP = 500, IsActive = true };
            _context.Badges.AddRange(badge1, badge2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetNextBadgeProgress(userId);


            // Assert
            result.Should().NotBeNull();
            var progress = result as dynamic;
            ((bool)progress.HasNextBadge).Should().BeTrue();
            ((string)progress.Name).Should().Be("Silver");
            ((int)progress.UnlockXP).Should().Be(500);
            ((int)progress.CurrentXP).Should().Be(250);
            ((double)progress.Progress).Should().Be(50.0);
        }

        [Fact]
        public async Task GetNextBadgeProgress_WhenAllBadgesUnlocked_ReturnsNoNextBadge()
        {
            // Arrange
            var userId = "user-14";
            var user = new ApplicationUser
            {
                Id = userId,
                TotalXP = 1000,
                UserBadges = new List<UserBadge>
                {
                    new UserBadge { BadgeId = 20 },
                    new UserBadge { BadgeId = 21 }
                }
            };
            _context.Users.Add(user);

            var badge1 = new Badge { Id = 20, Name = "Silver", UnlockXP = 100, IsActive = true };
            var badge2 = new Badge { Id = 21, Name = "Gold", UnlockXP = 500, IsActive = true };
            _context.Badges.AddRange(badge1, badge2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetNextBadgeProgress(userId);
            await _context.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            var progress = result as dynamic;
            ((bool)progress.HasNextBadge).Should().BeFalse();
        }

        [Fact]
        public async Task GetNextBadgeProgress_WhenUserNotFound_ReturnsNull()
        {
  

            // Act
            var result = await _service.GetNextBadgeProgress("non-existing-user");

            // Assert
            result.Should().BeNull();
        }
    }
}