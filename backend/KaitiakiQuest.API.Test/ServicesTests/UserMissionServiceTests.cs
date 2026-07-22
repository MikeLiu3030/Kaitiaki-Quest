using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Hubs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;




namespace KaitiakiQuest.API.Tests.Services
{
    internal class LeaderboardEntry
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalXP { get; set; }
        public int Level { get; set; }
        public int CurrentStreak { get; set; }
    }

    public class UserMissionServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IGamificationService> _gamificationServiceMock;
        private readonly Mock<IHubContext<TeamHub>> _hubContextMock;
        private readonly Mock<IHubClients> _clientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<UserMissionService>> _loggerMock;
        private readonly UserMissionService _service;

        public UserMissionServiceTests()
        {

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();


            _gamificationServiceMock = new Mock<IGamificationService>();
            _loggerMock = new Mock<ILogger<UserMissionService>>();


            _hubContextMock = new Mock<IHubContext<TeamHub>>();
            _clientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
            _clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _clientsMock.Setup(x => x.GroupExcept(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()))
                .Returns(_clientProxyMock.Object);


            _cache = new MemoryCache(new MemoryCacheOptions());


            _service = new UserMissionService(
                _context,
                _gamificationServiceMock.Object,
                _cache,
                _loggerMock.Object,
                _hubContextMock.Object
            );
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
            _cache.Dispose();
        }

        // ====================================================================
        // Auxiliary method
        // ====================================================================

        private async Task<ApplicationUser> CreateTestUserAsync(
            string userId,
            string userName = "testuser",
            int totalXP = 0,
            int level = 1,
            int streak = 0)
        {
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = userName,
                Email = $"{userId}@test.com",
                TotalXP = totalXP,
                Level = level,
                CurrentStreak = streak
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<EcoMission> CreateTestMissionAsync(
            int id,
            string title = "Test Mission",
            string description = "Test Description",
            int basePoints = 30,
            string category = "Recycling",
            bool isDaily = false,
            bool isActive = true)
        {
            var mission = new EcoMission
            {
                Id = id,
                Title = title,
                Description = description,
                BasePoints = basePoints,
                Category = category,
                IsDaily = isDaily,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();
            return mission;
        }

        private async Task<UserMission> CreateUserMissionAsync(
            string userId,
            int missionId,
            MissionStatus status = MissionStatus.Pending,
            int earnedXP = 0)
        {
            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = missionId,
                Status = status,
                EarnedXP = earnedXP,
                AcceptedDate = DateTime.UtcNow
            };

            _context.UserMissions.Add(userMission);
            await _context.SaveChangesAsync();
            return userMission;
        }

        // ====================================================================
        // GetMyMissionsAsync
        // ====================================================================

        [Fact]
        public async Task GetMyMissionsAsync_WhenUserHasMissions_ReturnsMissions()
        {
            // Arrange
            var userId = "user-1";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1);
            await CreateUserMissionAsync(userId, mission.Id);

            // Act
            var result = await _service.GetMyMissionsAsync(userId, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().MissionTitle.Should().Be("Test Mission");
        }

        [Fact]
        public async Task GetMyMissionsAsync_WhenUserHasNoMissions_ReturnsEmptyList()
        {
            // Arrange
            var userId = "user-2";
            await CreateTestUserAsync(userId);

            // Act
            var result = await _service.GetMyMissionsAsync(userId, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyMissionsAsync_WhenInvalidStatus_ReturnsFailure()
        {
            // Arrange
            var userId = "user-3";
            await CreateTestUserAsync(userId);

            // Act
            var result = await _service.GetMyMissionsAsync(userId, "InvalidStatus");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Invalid mission status parameter.");
        }

        [Fact]
        public async Task GetMyMissionsAsync_WhenFilterByStatus_ReturnsOnlyMatchingStatus()
        {
            // Arrange
            var userId = "user-4";
            await CreateTestUserAsync(userId);
            var mission1 = await CreateTestMissionAsync(1);
            var mission2 = await CreateTestMissionAsync(2);

            await CreateUserMissionAsync(userId, mission1.Id, MissionStatus.Pending);
            await CreateUserMissionAsync(userId, mission2.Id, MissionStatus.Completed);

            // Act
            var result = await _service.GetMyMissionsAsync(userId, "Completed");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().Status.Should().Be("Completed");
        }

        // ====================================================================
        // GetMyStatsAsync
        // ====================================================================

        [Fact]
        public async Task GetMyStatsAsync_WhenUserHasData_ReturnsStats()
        {
            // Arrange
            var userId = "user-5";
            var user = await CreateTestUserAsync(userId, "statsuser", totalXP: 150, level: 2, streak: 3);
            var mission = await CreateTestMissionAsync(1);
            var userMission = await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Completed, 30);
            userMission.CompletedDate = DateTime.UtcNow; 
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetMyStatsAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var dataType = result.Data!.GetType();
            var totalMissions = (int)dataType.GetProperty("TotalMissions")!.GetValue(result.Data)!;
            var totalXP = (int)dataType.GetProperty("TotalXP")!.GetValue(result.Data)!;
            var currentStreak = (int)dataType.GetProperty("CurrentStreak")!.GetValue(result.Data)!;
            var weeklyMissions = (int)dataType.GetProperty("WeeklyMissions")!.GetValue(result.Data)!;
            var level = (int)dataType.GetProperty("Level")!.GetValue(result.Data)!;

            totalMissions.Should().Be(1);
            totalXP.Should().Be(30);
            currentStreak.Should().Be(3);
            weeklyMissions.Should().Be(1); 
            level.Should().Be(2);
        }
        [Fact]
        public async Task GetMyStatsAsync_WhenUserHasNoData_ReturnsZeroStats()
        {
            // Arrange
            var userId = "user-6";
            await CreateTestUserAsync(userId);

            // Act
            var result = await _service.GetMyStatsAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var dataType = result.Data!.GetType();
            ((int)dataType.GetProperty("TotalMissions")!.GetValue(result.Data)!).Should().Be(0);
            ((int)dataType.GetProperty("TotalXP")!.GetValue(result.Data)!).Should().Be(0);
            ((int)dataType.GetProperty("CurrentStreak")!.GetValue(result.Data)!).Should().Be(0);
            ((int)dataType.GetProperty("WeeklyMissions")!.GetValue(result.Data)!).Should().Be(0);
            ((int)dataType.GetProperty("Level")!.GetValue(result.Data)!).Should().Be(1);
        }

        // ====================================================================
        // AcceptMissionAsync
        // ====================================================================

        [Fact]
        public async Task AcceptMissionAsync_WhenMissionAvailable_AcceptsMission()
        {
            // Arrange
            var userId = "user-7";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1);

            var dto = new AcceptMissionDto { EcoMissionId = mission.Id };

            // Act
            var result = await _service.AcceptMissionAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Mission accepted successfully");
            result.Data.Should().NotBeNull();
            result.Data!.EcoMissionId.Should().Be(mission.Id);
            result.Data.Status.Should().Be("Pending");

            
            var saved = await _context.UserMissions.FirstOrDefaultAsync(um => um.UserId == userId);
            saved.Should().NotBeNull();
            saved!.Status.Should().Be(MissionStatus.Pending);
        }

        [Fact]
        public async Task AcceptMissionAsync_WhenMissionNotActive_ReturnsFailure()
        {
            // Arrange
            var userId = "user-8";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1, isActive: false);

            var dto = new AcceptMissionDto { EcoMissionId = mission.Id };

            // Act
            var result = await _service.AcceptMissionAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not available");
        }

        [Fact]
        public async Task AcceptMissionAsync_WhenAlreadyAccepted_ReturnsFailure()
        {
            // Arrange
            var userId = "user-9";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1);
            await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Pending);

            var dto = new AcceptMissionDto { EcoMissionId = mission.Id };

            // Act
            var result = await _service.AcceptMissionAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("You already accepted this mission");
        }

        // ====================================================================
        // CompleteMissionAsync
        // ====================================================================

        [Fact]
        public async Task CompleteMissionAsync_WhenValid_CompletesMission()
        {
            // Arrange
            var userId = "user-10";
            var user = await CreateTestUserAsync(userId, "completeuser");
            var mission = await CreateTestMissionAsync(1, "Complete Mission", basePoints: 30);
            var userMission = await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Pending);

            _gamificationServiceMock
                .Setup(x => x.ProcessMissionCompletion(userId, It.IsAny<UserMission>()))
                .ReturnsAsync(30);

            _gamificationServiceMock
                .Setup(x => x.UpdateStreak(userId))
                .Returns(Task.CompletedTask);

            _gamificationServiceMock
                .Setup(x => x.CheckAndAwardBadges(userId))
                .Returns(Task.CompletedTask);

            var dto = new CompleteMissionDto { EvidenceImageUrl = "https://example.com/evidence.jpg" };

            // Act
            var result = await _service.CompleteMissionAsync(userId, userMission.Id, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Mission completed! 🎉");
            result.Data.Should().NotBeNull();
            result.Data!.EarnedXP.Should().Be(30);
            result.Data.Status.Should().Be("Completed");
            result.Data.EvidenceImageUrl.Should().Be("https://example.com/evidence.jpg");


            var saved = await _context.UserMissions.FindAsync(userMission.Id);
            saved!.Status.Should().Be(MissionStatus.Completed);
            saved.CompletedDate.Should().NotBeNull();
            saved.EarnedXP.Should().Be(30);

            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser!.TotalXP.Should().Be(30);
            updatedUser.Level.Should().Be(1);
        }

        [Fact]
        public async Task CompleteMissionAsync_WhenMissionNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = "user-11";
            await CreateTestUserAsync(userId);

            // Act
            var result = await _service.CompleteMissionAsync(userId, 999, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
        }

        [Fact]
        public async Task CompleteMissionAsync_WhenMissionAlreadyCompleted_ReturnsFailure()
        {
            // Arrange
            var userId = "user-12";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1);
            var userMission = await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Completed);

            // Act
            var result = await _service.CompleteMissionAsync(userId, userMission.Id, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission already completed or failed");
        }

        [Fact]
        public async Task CompleteMissionAsync_WhenUserInTeam_BroadcastsSignalR()
        {
            // Arrange
            var userId = "user-13";
            var team = new Team
            {
                Name = "Test Team",
                InviteCode = "TEAM123",
                TotalTeamXP = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.Teams.Add(team);

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = "teamuser",
                Email = "team@test.com",
                TeamId = team.Id,
                Team = team
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mission = await CreateTestMissionAsync(2, "Team Mission", basePoints: 50);
            var userMission = await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Pending);

            _gamificationServiceMock
                .Setup(x => x.ProcessMissionCompletion(userId, It.IsAny<UserMission>()))
                .ReturnsAsync(50);

            _gamificationServiceMock
                .Setup(x => x.UpdateStreak(userId))
                .Returns(Task.CompletedTask);

            _gamificationServiceMock
                .Setup(x => x.CheckAndAwardBadges(userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CompleteMissionAsync(userId, userMission.Id, null);

            // Assert
            result.IsSuccess.Should().BeTrue();


            _clientProxyMock.Verify(
                x => x.SendCoreAsync(
                    "TeamXPUpdated",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.AtLeastOnce
            );


            var updatedTeam = await _context.Teams.FindAsync(team.Id);
            updatedTeam!.TotalTeamXP.Should().Be(50);
        }

        // ====================================================================
        // AbandonMissionAsync
        // ====================================================================

        [Fact]
        public async Task AbandonMissionAsync_WhenPending_AbandonsMission()
        {
            // Arrange
            var userId = "user-14";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1);
            var userMission = await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Pending);

            // Act
            var result = await _service.AbandonMissionAsync(userId, userMission.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Mission abandoned");

            var saved = await _context.UserMissions.FindAsync(userMission.Id);
            saved!.Status.Should().Be(MissionStatus.Failed);
            saved.FailedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task AbandonMissionAsync_WhenMissionNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = "user-15";
            await CreateTestUserAsync(userId);

            // Act
            var result = await _service.AbandonMissionAsync(userId, 999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
        }

        [Fact]
        public async Task AbandonMissionAsync_WhenMissionAlreadyCompleted_ReturnsFailure()
        {
            // Arrange
            var userId = "user-16";
            await CreateTestUserAsync(userId);
            var mission = await CreateTestMissionAsync(1);
            var userMission = await CreateUserMissionAsync(userId, mission.Id, MissionStatus.Completed);

            // Act
            var result = await _service.AbandonMissionAsync(userId, userMission.Id);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Cannot abandon completed mission");
        }

        // ====================================================================
        // GetLeaderboardAsync
        // ====================================================================

        [Fact]
        public async Task GetLeaderboardAsync_WhenUsersExist_ReturnsLeaderboard()
        {
            // Arrange
            await CreateTestUserAsync("user-17", "topuser", totalXP: 1000);
            await CreateTestUserAsync("user-18", "seconduser", totalXP: 500);
            await CreateTestUserAsync("user-19", "thirduser", totalXP: 100);

            // Act
            var result = await _service.GetLeaderboardAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            
            var entries = result.Data
                .Select(x => new LeaderboardEntry
                {
                    UserName = (string)x.GetType().GetProperty("UserName")!.GetValue(x)!,
                    TotalXP = (int)x.GetType().GetProperty("TotalXP")!.GetValue(x)!,
                    Level = (int)x.GetType().GetProperty("Level")!.GetValue(x)!,
                    CurrentStreak = (int)x.GetType().GetProperty("CurrentStreak")!.GetValue(x)!
                })
                .ToList();

            entries.Should().BeInDescendingOrder(e => e.TotalXP);
        }

        [Fact]
        public async Task GetLeaderboardAsync_WhenNoUsers_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetLeaderboardAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLeaderboardAsync_ReturnsCachedData()
        {
            // Arrange 
            await CreateTestUserAsync("user-20", "cacheduser", totalXP: 500);

            // Act 
            var result1 = await _service.GetLeaderboardAsync();


            var result2 = await _service.GetLeaderboardAsync();

            // Assert
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeTrue();

            result1.Data.Should().BeEquivalentTo(result2.Data);
        }

        [Fact]
        public async Task GetLeaderboardAsync_TakesTop10()
        {
            // Arrange 
            for (int i = 1; i <= 15; i++)
            {
                await CreateTestUserAsync($"user-{20 + i}", $"user{i}", totalXP: i * 100);
            }

            // Act
            var result = await _service.GetLeaderboardAsync();

            // Assert 
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(10);
        }
    }
}