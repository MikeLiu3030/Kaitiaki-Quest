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
using static KaitiakiQuest.API.DTOs.AuthDtos;

namespace KaitiakiQuest.API.Tests.Controllers
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly IServiceScope _scope;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthControllerTests(CustomWebApplicationFactory factory)
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
            var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
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
            var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            return result!.Data!.Token;
        }

        #endregion

        #region POST: /api/auth/register

        [Fact]
        public async Task Register_WithValidData_ReturnsOk()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Email = "newuser@test.com",
                Password = "Test123!",
                Username = "newuser"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Registration successful!");
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().NotBeNullOrEmpty();
            result.Data.Email.Should().Be("newuser@test.com");
            result.Data.UserName.Should().Be("newuser");
            result.Data.Roles.Should().Contain("User");
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Email = "user@kaitiaki.com", 
                Password = "Test123!",
                Username = "existinguser"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Email already registered");
        }

        [Fact]
        public async Task Register_WithShortPassword_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Email = "shortpwd@test.com",
                Password = "123",
                Username = "shortpwd"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problem = await response.Content
                .ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();

            problem.Should().NotBeNull();
            problem!.Errors.Should().NotBeEmpty();
            problem.Errors.Should().ContainKey("Password");
        }

        [Fact]
        public async Task Register_WithInvalidEmailFormat_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Email = "invalid-email",
                Password = "Test123!",
                Username = "invaliduser"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);


            var problem = await response.Content
                .ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();

            problem.Should().NotBeNull();
            problem!.Errors.Should().NotBeEmpty();
            problem.Errors.Should().ContainKey("Email");
        }

        #endregion

        #region POST: /api/auth/login

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOk()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "user@kaitiaki.com",
                Password = "User123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Login successful!");
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().NotBeNullOrEmpty();
            result.Data.Email.Should().Be("user@kaitiaki.com");
            result.Data.UserName.Should().Be("user@kaitiaki.com");
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "user@kaitiaki.com",
                Password = "WrongPassword!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid email or password");
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "Test123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid email or password");
        }

        #endregion

        #region GET: /api/auth/me

        [Fact]
        public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
        {
            // Arrange
            var token = await GetUserTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCurrentUser_WithAdminToken_ReturnsAdminUserInfo()
        {
            // Arrange
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/auth/me");

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