using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using static KaitiakiQuest.API.DTOs.AuthDtos;

namespace KaitiakiQuest.API.Tests.Controllers
{
    public class TeamsControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly IServiceScope _scope;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TeamsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            _dbContext.Database.EnsureCreated();

            // ✅ 每个测试方法开始前清空 team 业务数据，避免测试间数据污染
            //    必须在 InitializeTestUsers 之前执行
            CleanDatabase();
            InitializeTestUsers().GetAwaiter().GetResult();
        }

        #region Helper Methods

        /// <summary>
        /// 清空 team 业务数据并解除所有用户的 team 归属，
        /// 保证每个测试方法从干净状态开始。
        /// 注意：不删除 Users / Roles，Identity 数据保留，否则登录会失败。
        /// </summary>
        private void CleanDatabase()
        {
            // 1) 先解除用户与 team 的外键关联
            foreach (var user in _dbContext.Users)
                user.TeamId = null;
            _dbContext.SaveChanges();

            // 2) 再删除所有 team
            _dbContext.Teams.RemoveRange(_dbContext.Teams.ToList());
            _dbContext.SaveChanges();
        }

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

        private async Task<int> CreateTestTeamAsync(string userId, string name = "Test Team")
        {
            var team = new Team
            {
                Name = name,
                // ✅ 唯一邀请码，避免多个 team 共用一个 code 导致 join 加错队
                InviteCode = $"CODE{Guid.NewGuid():N}".Substring(0, 8).ToUpper(),
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                TotalTeamXP = 0
            };
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Add user to team
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.TeamId = team.Id;
                await _dbContext.SaveChangesAsync();
            }

            return team.Id;
        }

        private async Task<int> GetFirstTeamIdAsync()
        {
            var team = await _dbContext.Teams.FirstOrDefaultAsync();
            return team.Id;
        }

        #endregion

        #region GET: /api/teams/my-team

        [Fact]
        public async Task GetMyTeam_WithValidToken_ReturnsOk()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            await CreateTestTeamAsync(userId);

            // Act
            var response = await _client.GetAsync("/api/teams/my-team");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Test Team");
        }

        [Fact]
        public async Task GetMyTeam_WhenNoTeam_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/teams/my-team");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("You are not in a team");
        }

        [Fact]
        public async Task GetMyTeam_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/teams/my-team");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GET: /api/teams/{teamId}

        [Fact]
        public async Task GetTeamById_WhenExists_ReturnsOk()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            var teamId = await CreateTestTeamAsync(userId);

            // Act
            var response = await _client.GetAsync($"/api/teams/{teamId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(teamId);
            result.Data.Name.Should().Be("Test Team");
        }

        [Fact]
        public async Task GetTeamById_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/teams/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Team not found");
        }

        [Fact]
        public async Task GetTeamById_WithInvalidId_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/teams/0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid");
        }

        [Fact]
        public async Task GetTeamById_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/teams/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region POST: /api/teams

        [Fact]
        public async Task CreateTeam_WithValidData_ReturnsCreated()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new CreateTeamDto
            {
                Name = "New Test Team",
                Description = "Team Description"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("New Test Team");
            result.Data.Description.Should().Be("Team Description");
            result.Data.InviteCode.Should().NotBeNullOrEmpty();
            result.Data.MemberCount.Should().Be(1);
        }

        [Fact]
        public async Task CreateTeam_WithDuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // ✅ 由 admin 创建第一个同名 team，保证 user 不在任何 team 中，
            //    这样 user 的第二次创建才会真正走到“重名”校验分支
            var adminUserId = (await _userManager.FindByEmailAsync("admin@kaitiaki.com"))!.Id;
            await CreateTestTeamAsync(adminUserId, "Duplicate Team");

            var dto = new CreateTeamDto
            {
                Name = "Duplicate Team",
                Description = "Should fail"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("A team with this name already exists");
        }

        [Fact]
        public async Task CreateTeam_WhenAlreadyInTeam_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            await CreateTestTeamAsync(userId);

            var dto = new CreateTeamDto
            {
                Name = "Another Team",
                Description = "Should fail"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("You are already in a team.");
        }

        [Fact]
        public async Task CreateTeam_WithEmptyName_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new CreateTeamDto
            {
                Name = "",
                Description = "Should fail"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreateTeam_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            var dto = new CreateTeamDto
            {
                Name = "Unauthorized Team"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region POST: /api/teams/join

        [Fact]
        public async Task JoinTeam_WithValidInviteCode_ReturnsOk()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;

            // Create a team as another user (admin)
            var adminUserId = (await _userManager.FindByEmailAsync("admin@kaitiaki.com"))!.Id;
            var teamId = await CreateTestTeamAsync(adminUserId, "Joinable Team");

            // Get the invite code
            var team = await _dbContext.Teams.FindAsync(teamId);
            var inviteCode = team!.InviteCode;

            // Remove user from any existing team
            var user = await _dbContext.Users.FindAsync(userId);
            user!.TeamId = null;
            await _dbContext.SaveChangesAsync();

            var dto = new JoinTeamDto
            {
                InviteCode = inviteCode
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/join", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Joinable Team");
        }

        [Fact]
        public async Task JoinTeam_WithInvalidInviteCode_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new JoinTeamDto
            {
                InviteCode = "INVALID123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/join", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid invite code. Team not found.");
        }

        [Fact]
        public async Task JoinTeam_WhenAlreadyInTeam_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            await CreateTestTeamAsync(userId);

            var dto = new JoinTeamDto
            {
                InviteCode = "SOME123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/join", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("You are already in a team. Please leave your current team first.");
        }

        [Fact]
        public async Task JoinTeam_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            var dto = new JoinTeamDto
            {
                InviteCode = "SOME123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/join", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region POST: /api/teams/leave

        [Fact]
        public async Task LeaveTeam_WhenInTeam_ReturnsOk()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            await CreateTestTeamAsync(userId);

            var dto = new LeaveTeamRequestDto();

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/leave", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("You have left the team.");
        }

        [Fact]
        public async Task LeaveTeam_WhenNotInTeam_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Ensure user not in team
            var userId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;
            var user = await _dbContext.Users.FindAsync(userId);
            user!.TeamId = null;
            await _dbContext.SaveChangesAsync();

            var dto = new LeaveTeamRequestDto();

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/leave", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("You are not in any team");
        }

        [Fact]
        public async Task LeaveTeam_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            var dto = new LeaveTeamRequestDto();

            // Act
            var response = await _client.PostAsJsonAsync("/api/teams/leave", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GET: /api/teams/leaderboard (Public)

        [Fact]
        public async Task GetTeamLeaderboard_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/teams/leaderboard");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeamLeaderboardDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTeamLeaderboard_ReturnsRankedData()
        {
            // Arrange - Create two teams
            var adminUserId = (await _userManager.FindByEmailAsync("admin@kaitiaki.com"))!.Id;
            var userUserId = (await _userManager.FindByEmailAsync("user@kaitiaki.com"))!.Id;

            // Remove user from any team first
            var user = await _dbContext.Users.FindAsync(userUserId);
            user!.TeamId = null;
            await _dbContext.SaveChangesAsync();

            await CreateTestTeamAsync(adminUserId, "Alpha Team");
            await CreateTestTeamAsync(userUserId, "Beta Team");

            // Act
            var response = await _client.GetAsync("/api/teams/leaderboard");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeamLeaderboardDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().BeGreaterThanOrEqualTo(2);
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