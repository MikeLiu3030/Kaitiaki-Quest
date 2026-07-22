using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;


namespace KaitiakiQuest.API.Tests.Services
{
    public class EcoMissionServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly EcoMissionService _service;

        public EcoMissionServiceTests()
        {
            // 1. Create SQLite in-member connection.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // 2. setup DbContext using SQLite
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // 3. create a test service instance
            _service = new EcoMissionService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        // ====================================================================
        // Auxiliary method: Create test badges
        // ====================================================================

        private async Task<EcoMission> CreateTestMissionAsync(
            string title = "Test Mission",
            string description = "Test Description",
            int basePoints = 30,
            string category = "Recycling",
            string? imageUrl = null,
            bool isDaily = false,
            bool isActive = true)
        {
            var mission = new EcoMission
            {
                Title = title,
                Description = description,
                BasePoints = basePoints,
                Category = category,
                ImageUrl = imageUrl,
                IsDaily = isDaily,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();
            return mission;
        }

        // ====================================================================
        // Part 1: GetAllMissionsAsync
        // ====================================================================

        [Fact]
        public async Task GetAllMissionsAsync_WhenMissionsExist_ReturnsAllActiveMissions()
        {
            // Arrange
            await CreateTestMissionAsync("Mission 1", "Desc 1", 30, "Recycling");
            await CreateTestMissionAsync("Mission 2", "Desc 2", 40, "Energy");
            await CreateTestMissionAsync("Mission 3", "Desc 3", 50, "Transport", isActive: false);

            // Act
            var result = await _service.GetAllMissionsAsync(null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2); // return only active mission
            result.Data.Should().OnlyContain(m => m.Title != "Mission 3"); // do not include inactive
        }

        [Fact]
        public async Task GetAllMissionsAsync_WhenNoMissionsExist_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetAllMissionsAsync(null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllMissionsAsync_WhenFilterByCategory_ReturnsOnlyMatchingCategory()
        {
            // Arrange
            await CreateTestMissionAsync("Recycling Mission", "Desc", 30, "Recycling");
            await CreateTestMissionAsync("Energy Mission", "Desc", 40, "Energy");
            await CreateTestMissionAsync("Transport Mission", "Desc", 50, "Transport");

            // Act
            var result = await _service.GetAllMissionsAsync("Energy", null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().Category.Should().Be("Energy");
        }

        [Fact]
        public async Task GetAllMissionsAsync_WhenFilterByCategoryAndNoMatch_ReturnsEmptyList()
        {
            // Arrange
            await CreateTestMissionAsync("Mission 1", "Desc", 30, "Recycling");

            // Act
            var result = await _service.GetAllMissionsAsync("NonExistentCategory", null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllMissionsAsync_WhenFilterByIsDaily_ReturnsOnlyDailyMissions()
        {
            // Arrange
            await CreateTestMissionAsync("Daily Mission", "Desc", 30, "Recycling", isDaily: true);
            await CreateTestMissionAsync("Weekly Mission", "Desc", 40, "Energy", isDaily: false);

            // Act
            var result = await _service.GetAllMissionsAsync(null, true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().IsDaily.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllMissionsAsync_WhenFilterByIsDailyFalse_ReturnsOnlyNonDailyMissions()
        {
            // Arrange
            await CreateTestMissionAsync("Daily Mission", "Desc", 30, "Recycling", isDaily: true);
            await CreateTestMissionAsync("Weekly Mission", "Desc", 40, "Energy", isDaily: false);

            // Act
            var result = await _service.GetAllMissionsAsync(null, false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().IsDaily.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllMissionsAsync_WhenFilterByCategoryAndIsDaily_CombinesBothFilters()
        {
            // Arrange
            await CreateTestMissionAsync("Daily Recycling", "Desc", 30, "Recycling", isDaily: true);
            await CreateTestMissionAsync("Daily Energy", "Desc", 40, "Energy", isDaily: true);
            await CreateTestMissionAsync("Weekly Recycling", "Desc", 50, "Recycling", isDaily: false);

            // Act
            var result = await _service.GetAllMissionsAsync("Recycling", true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().Title.Should().Be("Daily Recycling");
        }

        // ====================================================================
        // Part 2: GetMissionByIdAsync
        // ====================================================================

        [Fact]
        public async Task GetMissionByIdAsync_WhenMissionExists_ReturnsMission()
        {
            // Arrange
            var mission = await CreateTestMissionAsync("Test Mission", "Test Desc", 30, "Recycling");

            // Act
            var result = await _service.GetMissionByIdAsync(mission.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(mission.Id);
            result.Data.Title.Should().Be("Test Mission");
            result.Data.BasePoints.Should().Be(30);
        }

        [Fact]
        public async Task GetMissionByIdAsync_WhenMissionDoesNotExist_ReturnsFailure()
        {
            // Act
            var result = await _service.GetMissionByIdAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetMissionByIdAsync_WhenMissionIsInactive_ReturnsFailure()
        {
            // Arrange
            var mission = await CreateTestMissionAsync("Inactive Mission", "Desc", 30, "Recycling", isActive: false);

            // Act
            var result = await _service.GetMissionByIdAsync(mission.Id);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
        }

        // ====================================================================
        // Part 3: CreateMissionAsync
        // ====================================================================

        [Fact]
        public async Task CreateMissionAsync_WithValidData_CreatesMission()
        {
            // Arrange
            var dto = new CreateEcoMissionDto
            {
                Title = "New Mission",
                Description = "New Description",
                BasePoints = 50,
                Category = "Transport",
                ImageUrl = "https://example.com/image.jpg",
                IsDaily = true
            };

            // Act
            var result = await _service.CreateMissionAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Mission created successfully");
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("New Mission");
            result.Data.BasePoints.Should().Be(50);
            result.Data.Category.Should().Be("Transport");
            result.Data.IsDaily.Should().BeTrue();
            result.Data.IsActive.Should().BeTrue();
            result.Data.Id.Should().BeGreaterThan(0);

            var savedMission = await _context.EcoMissions.FindAsync(result.Data.Id);
            savedMission.Should().NotBeNull();
            savedMission!.Title.Should().Be("New Mission");
        }

        [Fact]
        public async Task CreateMissionAsync_WithMinimalData_CreatesMissionWithDefaults()
        {
            // Arrange
            var dto = new CreateEcoMissionDto
            {
                Title = "Minimal Mission",
                Description = "Minimal Description",
                BasePoints = 10,
                Category = "Other",
                ImageUrl = null,
                IsDaily = false
            };

            // Act
            var result = await _service.CreateMissionAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Category.Should().Be("Other");
            result.Data.IsDaily.Should().BeFalse();
            result.Data.ImageUrl.Should().BeNull();
        }

        // ====================================================================
        // Part 4: UpdateMissionAsync
        // ====================================================================

        [Fact]
        public async Task UpdateMissionAsync_WhenMissionExists_UpdatesMission()
        {
            // Arrange
            var mission = await CreateTestMissionAsync("Original Title", "Original Desc", 30, "Recycling");

            var dto = new UpdateEcoMissionDto
            {
                Title = "Updated Title",
                Description = "Updated Desc",
                BasePoints = 60,
                Category = "Energy",
                ImageUrl = "https://example.com/new-image.jpg",
                IsDaily = true,
                IsActive = true
            };

            // Act
            var result = await _service.UpdateMissionAsync(mission.Id, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Mission updated successfully");
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Updated Title");
            result.Data.BasePoints.Should().Be(60);
            result.Data.Category.Should().Be("Energy");
            result.Data.IsDaily.Should().BeTrue();


            var savedMission = await _context.EcoMissions.FindAsync(mission.Id);
            savedMission!.Title.Should().Be("Updated Title");
            savedMission.BasePoints.Should().Be(60);
            savedMission.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMissionAsync_WhenMissionDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var dto = new UpdateEcoMissionDto
            {
                Title = "Updated Title",
                Description = "Updated Desc",
                BasePoints = 60,
                Category = "Energy"
            };

            // Act
            var result = await _service.UpdateMissionAsync(999, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task UpdateMissionAsync_CanDeactivateMission()
        {
            // Arrange
            var mission = await CreateTestMissionAsync("Active Mission", "Desc", 30, "Recycling");

            var dto = new UpdateEcoMissionDto
            {
                Title = "Active Mission",
                Description = "Desc",
                BasePoints = 30,
                Category = "Recycling",
                IsActive = false
            };

            // Act
            var result = await _service.UpdateMissionAsync(mission.Id, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.IsActive.Should().BeFalse();


            var savedMission = await _context.EcoMissions.FindAsync(mission.Id);
            savedMission!.IsActive.Should().BeFalse();
        }

        // ====================================================================
        // Part 5: DeleteMissionAsync
        // ====================================================================

        [Fact]
        public async Task DeleteMissionAsync_WhenMissionExists_PerformsSoftDelete()
        {
            // Arrange
            var mission = await CreateTestMissionAsync("To Be Deleted", "Desc", 30, "Recycling");

            var initialMission = await _context.EcoMissions.FindAsync(mission.Id);
            initialMission!.IsActive.Should().BeTrue();

            // Act
            var result = await _service.DeleteMissionAsync(mission.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Mission deleted successfully");
            result.Data.Should().BeTrue();


            var deletedMission = await _context.EcoMissions.FindAsync(mission.Id);
            deletedMission!.IsActive.Should().BeFalse();
            deletedMission.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteMissionAsync_WhenMissionDoesNotExist_ReturnsFailure()
        {
            // Act
            var result = await _service.DeleteMissionAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteMissionAsync_WhenMissionAlreadyInactive_ReturnsFailure()
        {
            // Arrange
            var mission = await CreateTestMissionAsync("Already Inactive", "Desc", 30, "Recycling", isActive: false);

            // Act
            var result = await _service.DeleteMissionAsync(mission.Id);

            // Assert
             result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Mission is already inactive");
        }

        // ====================================================================
        // Part 6: GetCategoriesAsync 
        // ====================================================================

        [Fact]
        public async Task GetCategoriesAsync_WhenCategoriesExist_ReturnsDistinctCategories()
        {
            // Arrange
            await CreateTestMissionAsync("Mission 1", "Desc", 30, "Recycling");
            await CreateTestMissionAsync("Mission 2", "Desc", 40, "Energy");
            await CreateTestMissionAsync("Mission 3", "Desc", 50, "Recycling");
            await CreateTestMissionAsync("Mission 4", "Desc", 60, "Transport");

            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Should().Contain(new[] { "Recycling", "Energy", "Transport" });
        }

        [Fact]
        public async Task GetCategoriesAsync_WhenNoCategoriesExist_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCategoriesAsync_OnlyReturnsCategoriesFromActiveMissions()
        {
            // Arrange
            await CreateTestMissionAsync("Active Recycling", "Desc", 30, "Recycling", isActive: true);
            await CreateTestMissionAsync("Inactive Energy", "Desc", 40, "Energy", isActive: false);
            await CreateTestMissionAsync("Active Transport", "Desc", 50, "Transport", isActive: true);

            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().Contain(new[] { "Recycling", "Transport" });
            result.Data.Should().NotContain("Energy");
        }
    }
}