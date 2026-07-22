using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KaitiakiQuest.API.Tests.Services
{
    public class BadgeServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly BadgeService _service;

        public BadgeServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _service = new BadgeService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        // ====================================================================
        // Auxiliary method
        // ====================================================================

        private async Task<Badge> CreateTestBadgeAsync(
            string name = "Test Badge",
            string description = "Test Description",
            int unlockXP = 100,
            string? iconUrl = null,
            bool isActive = true)
        {
            var badge = new Badge
            {
                Name = name,
                Description = description,
                UnlockXP = unlockXP,
                IconUrl = iconUrl,
                IsActive = isActive
            };

            _context.Badges.Add(badge);
            await _context.SaveChangesAsync();
            return badge;
        }

        private async Task<UserBadge> AwardBadgeToUserAsync(string userId, int badgeId)
        {
            var userBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                AwardedDate = DateTime.UtcNow
            };

            _context.UserBadges.Add(userBadge);
            await _context.SaveChangesAsync();
            return userBadge;
        }

        private async Task<ApplicationUser> CreateTestUserAsync(string userId)
        {
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = $"user_{userId}",
                Email = $"{userId}@test.com",
                TotalXP = 0,
                Level = 1,
                CurrentStreak = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // ====================================================================
        // GetAllBadgesAsync
        // ====================================================================

        [Fact]
        public async Task GetAllBadgesAsync_WhenBadgesExist_ReturnsAllActiveBadges()
        {
            // Arrange
            await CreateTestBadgeAsync("Badge 1", "Desc 1", 100);
            await CreateTestBadgeAsync("Badge 2", "Desc 2", 200);
            await CreateTestBadgeAsync("Badge 3", "Desc 3", 300, isActive: false);

            // Act
            var result = await _service.GetAllBadgesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Retrieve Badges successfully.");
            result.Data.Should().HaveCount(2);
            result.Data.Should().NotContain(b => b.Name == "Badge 3");
        }

        [Fact]
        public async Task GetAllBadgesAsync_WhenNoBadgesExist_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetAllBadgesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllBadgesAsync_OnlyReturnsActiveBadges()
        {
            // Arrange
            await CreateTestBadgeAsync("Active Badge", "Active", 100, isActive: true);
            await CreateTestBadgeAsync("Inactive Badge", "Inactive", 200, isActive: false);
            await CreateTestBadgeAsync("Another Active", "Active 2", 300, isActive: true);

            // Act
            var result = await _service.GetAllBadgesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(b => b.IsActive == true);
            result.Data.Should().NotContain(b => b.Name == "Inactive Badge");
        }

        [Fact]
        public async Task GetAllBadgesAsync_ReturnsBadgesWithCorrectFields()
        {
            // Arrange
            await CreateTestBadgeAsync(
                name: "Golden Badge",
                description: "The best badge",
                unlockXP: 1000,
                iconUrl: "https://example.com/gold.png",
                isActive: true);

            // Act
            var result = await _service.GetAllBadgesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            var badge = result.Data!.First();
            badge.Name.Should().Be("Golden Badge");
            badge.Description.Should().Be("The best badge");
            badge.UnlockXP.Should().Be(1000);
            badge.IconUrl.Should().Be("https://example.com/gold.png");
            badge.IsActive.Should().BeTrue();
            badge.Id.Should().BeGreaterThan(0);
        }

        // ====================================================================
        // GetUserBadgesAsync
        // ====================================================================

        [Fact]
        public async Task GetUserBadgesAsync_WhenUserHasBadges_ReturnsUserBadges()
        {
            // Arrange
            var userId = "user-1";
            await CreateTestUserAsync(userId);

            var badge1 = await CreateTestBadgeAsync("Badge 1", "Desc 1", 100);
            var badge2 = await CreateTestBadgeAsync("Badge 2", "Desc 2", 200);

            await AwardBadgeToUserAsync(userId, badge1.Id);
            await AwardBadgeToUserAsync(userId, badge2.Id);

            // Act
            var result = await _service.GetUserBadgesAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(ub => ub.UserId == userId);
            result.Data.Select(ub => ub.BadgeId).Should().Contain(new[] { badge1.Id, badge2.Id });
        }

        [Fact]
        public async Task GetUserBadgesAsync_WhenUserHasNoBadges_ReturnsEmptyList()
        {
            // Arrange
            var userId = "user-2";
            await CreateTestUserAsync(userId);

            // Act
            var result = await _service.GetUserBadgesAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBadgesAsync_WhenUserIdIsEmpty_ReturnsFailure()
        {
            // Act
            var result = await _service.GetUserBadgesAsync(string.Empty);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("User not found!");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetUserBadgesAsync_WhenUserIdIsNull_ReturnsFailure()
        {
            // Act
            var result = await _service.GetUserBadgesAsync(null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("User not found!");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetUserBadgesAsync_WhenUserDoesNotExist_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetUserBadgesAsync("non-existent-user");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBadgesAsync_ReturnsBadgesOrderedByAwardedDateDescending()
        {
            // Arrange
            var userId = "user-3";
            await CreateTestUserAsync(userId);

            var badge1 = await CreateTestBadgeAsync("Old Badge", "Old", 100);
            var badge2 = await CreateTestBadgeAsync("New Badge", "New", 200);

            var oldBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badge1.Id,
                AwardedDate = DateTime.UtcNow.AddDays(-5)
            };
            _context.UserBadges.Add(oldBadge);

            var newBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badge2.Id,
                AwardedDate = DateTime.UtcNow
            };
            _context.UserBadges.Add(newBadge);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserBadgesAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.First().BadgeId.Should().Be(badge2.Id); // 最新的先返回
            result.Data.Last().BadgeId.Should().Be(badge1.Id);
        }

        [Fact]
        public async Task GetUserBadgesAsync_ReturnsBadgeWithNestedBadgeData()
        {
            // Arrange
            var userId = "user-4";
            await CreateTestUserAsync(userId);

            var badge = await CreateTestBadgeAsync(
                name: "Nested Badge",
                description: "Nested Description",
                unlockXP: 500,
                iconUrl: "https://example.com/nested.png");

            await AwardBadgeToUserAsync(userId, badge.Id);

            // Act
            var result = await _service.GetUserBadgesAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var userBadge = result.Data!.First();
            userBadge.Badge.Should().NotBeNull();
            userBadge.Badge!.Id.Should().Be(badge.Id);
            userBadge.Badge.Name.Should().Be("Nested Badge");
            userBadge.Badge.Description.Should().Be("Nested Description");
            userBadge.Badge.UnlockXP.Should().Be(500);
            userBadge.Badge.IconUrl.Should().Be("https://example.com/nested.png");
            userBadge.Badge.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserBadgesAsync_OnlyReturnsBadgesBelongingToSpecifiedUser()
        {
            // Arrange
            var userId1 = "user-5";
            var userId2 = "user-6";
            await CreateTestUserAsync(userId1);
            await CreateTestUserAsync(userId2);

            var badge1 = await CreateTestBadgeAsync("Badge 1", "Desc", 100);
            var badge2 = await CreateTestBadgeAsync("Badge 2", "Desc", 200);

            await AwardBadgeToUserAsync(userId1, badge1.Id);
            await AwardBadgeToUserAsync(userId2, badge2.Id);

            // Act
            var result = await _service.GetUserBadgesAsync(userId1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().BadgeId.Should().Be(badge1.Id);
            result.Data.Should().NotContain(ub => ub.BadgeId == badge2.Id);
        }

        [Fact]
        public async Task GetUserBadgesAsync_WhenUserHasMultipleBadges_ReturnsAllWithCorrectCount()
        {
            // Arrange
            var userId = "user-7";
            await CreateTestUserAsync(userId);

            var badge1 = await CreateTestBadgeAsync("Badge A", "Desc", 100);
            var badge2 = await CreateTestBadgeAsync("Badge B", "Desc", 200);
            var badge3 = await CreateTestBadgeAsync("Badge C", "Desc", 300);

            await AwardBadgeToUserAsync(userId, badge1.Id);
            await AwardBadgeToUserAsync(userId, badge2.Id);
            await AwardBadgeToUserAsync(userId, badge3.Id);

            // Act
            var result = await _service.GetUserBadgesAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Select(ub => ub.BadgeId).Should().Contain(new[] { badge1.Id, badge2.Id, badge3.Id });
        }
    }
}
