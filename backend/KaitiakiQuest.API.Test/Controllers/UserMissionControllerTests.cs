using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using static KaitiakiQuest.API.DTOs.AuthDtos;

namespace KaitiakiQuest.API.Tests.Controllers
{
    public class UserMissionsControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly IServiceScope _scope;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserMissionsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            _dbContext.Database.EnsureCreated();
            InitializeTestUsers().GetAwaiter().GetResult();
        }

        #region Helper Methods

        private async Task InitializeTestUsers()
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            var adminEmail = "admin@kaitiaki.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(adminUser, "Admin123!");
            }
            if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                await _userManager.AddToRoleAsync(adminUser, "Admin");

            var userEmail = "user@kaitiaki.com";
            var regularUser = await _userManager.FindByEmailAsync(userEmail);
            if (regularUser == null)
            {
                regularUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(regularUser, "User123!");
            }
            if (!await _userManager.IsInRoleAsync(regularUser, "User"))
                await _userManager.AddToRoleAsync(regularUser, "User");
        }

        private async Task<string> GetUserTokenAsync()
        {
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "user@kaitiaki.com",
                password = "User123!"
            });
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            return result!.Data!.Token;
        }

        private async Task<int> CreateTestMissionAsync()
        {
            var mission = new EcoMission
            {
                Title = "Test Mission",
                Description = "Test Description",
                BasePoints = 30,
                Category = "Recycling",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.EcoMissions.Add(mission);
            await _dbContext.SaveChangesAsync();
            return mission.Id;
        }

        private async Task<int> CreateTestUserMissionAsync(string userId, int missionId, MissionStatus status = MissionStatus.Pending)
        {
            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = missionId,
                Status = status,
                AcceptedDate = DateTime.UtcNow
            };
            _dbContext.UserMissions.Add(userMission);
            await _dbContext.SaveChangesAsync();
            return userMission.Id;
        }

        #endregion

        #region GET: /api/usermissions/my-missions

        [Fact]
        public async Task GetMyMissions_WithValidToken_ReturnsOk()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/usermissions/my-missions");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserMissionResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyMissions_WithStatusFilter_ReturnsFilteredResults()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/usermissions/my-missions?status=Pending");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserMissionResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().AllSatisfy(m => m.Status.Should().Be("Pending"));
        }

        [Fact]
        public async Task GetMyMissions_WithInvalidStatus_ReturnsEmptyList()
        {
            // Service returns empty list for invalid status (not an error)
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/usermissions/my-missions?status=Invalid");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserMissionResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task GetMyMissions_WithoutToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/usermissions/my-missions");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GET: /api/usermissions/stats

        [Fact]
        public async Task GetMyStats_WithValidToken_ReturnsOk()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/usermissions/stats");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyStats_WithoutToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/usermissions/stats");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region POST: /api/usermissions/accept

        [Fact]
        public async Task AcceptMission_WithValidToken_ReturnsOk()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var missionId = await CreateTestMissionAsync();

            var dto = new AcceptMissionDto { EcoMissionId = missionId };

            var response = await _client.PostAsJsonAsync("/api/usermissions/accept", dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Mission accepted successfully");
            result.Data.Should().NotBeNull();
            result.Data!.EcoMissionId.Should().Be(missionId);
            result.Data.Status.Should().Be("Pending");
        }

        [Fact]
        public async Task AcceptMission_WhenAlreadyAccepted_ReturnsBadRequest()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var missionId = await CreateTestMissionAsync();

            var dto = new AcceptMissionDto { EcoMissionId = missionId };
            await _client.PostAsJsonAsync("/api/usermissions/accept", dto);

            var response = await _client.PostAsJsonAsync("/api/usermissions/accept", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("You already accepted this mission");
        }

        [Fact]
        public async Task AcceptMission_WhenMissionNotActive_ReturnsBadRequest()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var mission = new EcoMission
            {
                Title = "Inactive Mission",
                Description = "This mission is inactive",
                BasePoints = 10,
                Category = "Recycling",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.EcoMissions.Add(mission);
            await _dbContext.SaveChangesAsync();

            var dto = new AcceptMissionDto { EcoMissionId = mission.Id };

            var response = await _client.PostAsJsonAsync("/api/usermissions/accept", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Mission not available");
        }

        [Fact]
        public async Task AcceptMission_WithoutToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var dto = new AcceptMissionDto { EcoMissionId = 1 };

            var response = await _client.PostAsJsonAsync("/api/usermissions/accept", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region PUT: /api/usermissions/{id}/complete

        [Fact]
        public async Task CompleteMission_WithValidToken_ReturnsOk()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            var missionId = await CreateTestMissionAsync();
            var userMissionId = await CreateTestUserMissionAsync(userId, missionId, MissionStatus.Pending);

            var dto = new CompleteMissionDto { EvidenceImageUrl = "https://example.com/evidence.jpg" };

            var response = await _client.PutAsJsonAsync($"/api/usermissions/{userMissionId}/complete", dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Mission completed! 🎉");
            result.Data.Should().NotBeNull();
            result.Data!.Status.Should().Be("Completed");
            result.Data.EvidenceImageUrl.Should().Be("https://example.com/evidence.jpg");
        }

        [Fact]
        public async Task CompleteMission_WhenMissionNotFound_ReturnsBadRequest()
        {
            // Controller returns BadRequest when mission not found
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsJsonAsync("/api/usermissions/99999/complete", new CompleteMissionDto());

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
        }

        [Fact]
        public async Task CompleteMission_WithoutToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PutAsJsonAsync("/api/usermissions/1/complete", new CompleteMissionDto());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region DELETE: /api/usermissions/{id}

        [Fact]
        public async Task AbandonMission_WithValidToken_ReturnsOk()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            var missionId = await CreateTestMissionAsync();
            var userMissionId = await CreateTestUserMissionAsync(userId, missionId, MissionStatus.Pending);

            var response = await _client.DeleteAsync($"/api/usermissions/{userMissionId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Mission abandoned");

            _dbContext.ChangeTracker.Clear();
            var userMission = await _dbContext.UserMissions.FindAsync(userMissionId);
            userMission!.Status.Should().Be(MissionStatus.Failed);
        }

        [Fact]
        public async Task AbandonMission_WhenMissionNotFound_ReturnsBadRequest()
        {
            // Controller returns BadRequest when mission not found
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.DeleteAsync("/api/usermissions/99999");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Mission not found");
        }

        [Fact]
        public async Task AbandonMission_WhenAlreadyCompleted_ReturnsBadRequest()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            var missionId = await CreateTestMissionAsync();
            var userMissionId = await CreateTestUserMissionAsync(userId, missionId, MissionStatus.Completed);

            var response = await _client.DeleteAsync($"/api/usermissions/{userMissionId}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Cannot abandon completed mission");
        }

        [Fact]
        public async Task AbandonMission_WithoutToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.DeleteAsync("/api/usermissions/1");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GET: /api/usermissions/leaderboard (Public)

        [Fact]
        public async Task GetLeaderboard_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/usermissions/leaderboard");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<object>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        public void Dispose()
        {
            _scope?.Dispose();
            _dbContext?.Dispose();
            _client?.Dispose();
        }
    }
}