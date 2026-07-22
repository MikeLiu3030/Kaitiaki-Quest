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
    public class BadgesControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly IServiceScope _scope;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public BadgesControllerTests(CustomWebApplicationFactory factory)
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

        #endregion

        #region GET: /api/badges

        [Fact]
        public async Task GetAllBadges_WithValidToken_ReturnsOk()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/badges");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<BadgeResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllBadges_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/badges");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GET: /api/badges/my-badges

        [Fact]
        public async Task GetMyBadges_WithValidToken_ReturnsOk()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/badges/my-badges");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserBadgeResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyBadges_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/badges/my-badges");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        public void Dispose()
        {
            _scope?.Dispose();
            _client?.Dispose();
        }
    }
}