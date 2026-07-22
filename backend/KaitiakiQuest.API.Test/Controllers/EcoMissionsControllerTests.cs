using FluentAssertions;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KaitiakiQuest.API.Tests.Controllers
{
    public class EcoMissionsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IServiceScope _scope;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EcoMissionsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            _scope = _factory.Services.CreateScope();
            var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            dbContext.Database.EnsureCreated();
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
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }

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
            {
                await _userManager.AddToRoleAsync(regularUser, "User");
            }
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "admin@kaitiaki.com",
                password = "Admin123!"
            });
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthDtos.AuthResponseDto>>();
            return result!.Data!.Token;
        }

        private async Task<string> GetUserTokenAsync()
        {
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "user@kaitiaki.com",
                password = "User123!"
            });
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthDtos.AuthResponseDto>>();
            return result!.Data!.Token;
        }


        private async Task<int> CreateTestMissionAsync()
        {
            var token = await GetAdminTokenAsync();

            var dto = new CreateEcoMissionDto
            {
                Title = "Test Mission For CRUD",
                Description = "Created for testing",
                BasePoints = 30,
                Category = "Recycling",
                IsDaily = false,
                IsActive = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/ecomissions")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EcoMissionResponseDto>>();
            return result!.Data!.Id;
        }

        #endregion

        #region GET: /api/ecomissions (Public)

        [Fact]
        public async Task GetMissions_WhenNoAuth_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/ecomissions");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EcoMissionResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMissions_WithCategoryFilter_ReturnsFilteredResults()
        {
            var response = await _client.GetAsync("/api/ecomissions?category=Recycling");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EcoMissionResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().AllSatisfy(m => m.Category.Should().Be("Recycling"));
        }

        [Fact]
        public async Task GetMissions_WithIsDailyFilter_ReturnsFilteredResults()
        {
            var response = await _client.GetAsync("/api/ecomissions?isDaily=true");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EcoMissionResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().AllSatisfy(m => m.IsDaily.Should().BeTrue());
        }

        #endregion

        #region GET: /api/ecomissions/{id} (Public)

        [Fact]
        public async Task GetMissionById_WhenExists_ReturnsOk()
        {
            var missionId = await CreateTestMissionAsync();

            var response = await _client.GetAsync($"/api/ecomissions/{missionId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EcoMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Id.Should().Be(missionId);
            result.Data.Title.Should().Be("Test Mission For CRUD");
            result.Data.BasePoints.Should().Be(30);
            result.Data.Category.Should().Be("Recycling");
        }

        [Fact]
        public async Task GetMissionById_WhenNotExists_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/ecomissions/99999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GET: /api/ecomissions/categories (Public)

        [Fact]
        public async Task GetCategories_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/ecomissions/categories");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().Contain("Recycling");
        }

        #endregion

        #region POST: /api/ecomissions (Admin only)

        [Fact]
        public async Task CreateMission_AsAdmin_ReturnsCreated()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new CreateEcoMissionDto
            {
                Title = "New Admin Mission",
                Description = "Created by admin",
                BasePoints = 50,
                Category = "Energy",
                IsDaily = false,
                IsActive = true
            };

            var response = await _client.PostAsJsonAsync("/api/ecomissions", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EcoMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Title.Should().Be("New Admin Mission");
            result.Data.BasePoints.Should().Be(50);
            result.Data.Category.Should().Be("Energy");
            result.Data.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateMission_AsUser_ReturnsForbidden()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new CreateEcoMissionDto
            {
                Title = "User Tries to Create",
                Description = "Should be forbidden",
                BasePoints = 10,
                Category = "Recycling"
            };

            var response = await _client.PostAsJsonAsync("/api/ecomissions", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateMission_WithoutAuth_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var dto = new CreateEcoMissionDto
            {
                Title = "No Auth Mission",
                Description = "Should be unauthorized",
                BasePoints = 10,
                Category = "Recycling"
            };

            var response = await _client.PostAsJsonAsync("/api/ecomissions", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region PUT: /api/ecomissions/{id} (Admin only)

        [Fact]
        public async Task UpdateMission_AsAdmin_ReturnsOk()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var missionId = await CreateTestMissionAsync();

            var dto = new UpdateEcoMissionDto
            {
                Title = "Updated Mission Title",
                Description = "Updated Description",
                BasePoints = 75,
                Category = "Transport",
                IsDaily = true,
                IsActive = true
            };

            var response = await _client.PutAsJsonAsync($"/api/ecomissions/{missionId}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EcoMissionResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Title.Should().Be("Updated Mission Title");
            result.Data.BasePoints.Should().Be(75);
            result.Data.Category.Should().Be("Transport");
            result.Data.IsDaily.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateMission_AsUser_ReturnsForbidden()
        {
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var missionId = await CreateTestMissionAsync();

            var dto = new UpdateEcoMissionDto
            {
                Title = "User Update Attempt",
                Description = "Should be forbidden",
                BasePoints = 10,
                Category = "Recycling"
            };

            var response = await _client.PutAsJsonAsync($"/api/ecomissions/{missionId}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateMission_WithoutAuth_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var missionId = await CreateTestMissionAsync();

            var dto = new UpdateEcoMissionDto
            {
                Title = "No Auth Update",
                Description = "Should be unauthorized",
                BasePoints = 10,
                Category = "Recycling"
            };

            var response = await _client.PutAsJsonAsync($"/api/ecomissions/{missionId}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateMission_WhenNotExists_ReturnsNotFound()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new UpdateEcoMissionDto
            {
                Title = "Non-existent Mission",
                Description = "Should be not found",
                BasePoints = 10,
                Category = "Recycling"
            };

            var response = await _client.PutAsJsonAsync("/api/ecomissions/99999", dto);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region DELETE: /api/ecomissions/{id} (Admin only)

        [Fact]
        public async Task DeleteMission_AsAdmin_ReturnsOk()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var missionId = await CreateTestMissionAsync();

            var response = await _client.DeleteAsync($"/api/ecomissions/{missionId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().BeTrue();

           
            var getResponse = await _client.GetAsync($"/api/ecomissions/{missionId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteMission_AsUser_ReturnsForbidden()
        {
           
            var token = await GetUserTokenAsync();
            
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            
            var missionId = await CreateTestMissionAsync();

            
            var response = await _client.DeleteAsync($"/api/ecomissions/{missionId}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteMission_WithoutAuth_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var missionId = await CreateTestMissionAsync();

            var response = await _client.DeleteAsync($"/api/ecomissions/{missionId}");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteMission_WhenNotExists_ReturnsNotFound()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.DeleteAsync("/api/ecomissions/99999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        public void Dispose()
        {
            _scope?.Dispose();
            _client?.Dispose();
        }
    }
}