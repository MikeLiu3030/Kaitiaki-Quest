using FluentAssertions;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace KaitiakiQuest.API.Tests.Services;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly JwtService _service;

    private const string TestKey = "ThisIsASuperSecretKeyForTesting1234567890!";
    private const string TestIssuer = "https://test-issuer.com";
    private const string TestAudience = "https://test-audience.com";
    private const string TestExpiryMinutes = "60";

    public JwtServiceTests()
    {
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(c => c["Jwt:Key"]).Returns(TestKey);
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns(TestIssuer);
        _configMock.Setup(c => c["Jwt:Audience"]).Returns(TestAudience);
        _configMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns(TestExpiryMinutes);

        _service = new JwtService(_configMock.Object);
    }

    #region Helper Methods

    private ApplicationUser CreateTestUser(
        string id = "user-1",
        string? email = "test@example.com",
        string? userName = "TestUser",
        int totalXP = 500,
        int level = 10)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = userName,
            TotalXP = totalXP,
            Level = level
        };
    }

    private JwtSecurityToken DecodeToken(string tokenString)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(tokenString);
    }

    #endregion

    #region Token Generation - Basic

    [Fact]
    public void GenerateToken_ValidUser_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ValidUser_ShouldReturnValidJwtFormat()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);

        // Assert - JWT 格式为 header.payload.signature（三段用 . 分隔）
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_ValidUser_ShouldBeReadableAsJwt()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);

        // Assert - 不抛异常即为有效 JWT
        var act = () => DecodeToken(token);
        act.Should().NotThrow();
    }

    #endregion

    #region Token Claims

    [Fact]
    public void GenerateToken_ShouldContainUserIdClaim()
    {
        // Arrange
        var user = CreateTestUser(id: "user-abc-123");
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be("user-abc-123");
    }

    [Fact]
    public void GenerateToken_ShouldContainEmailClaim()
    {
        // Arrange
        var user = CreateTestUser(email: "hello@kaitiaki.com");
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be("hello@kaitiaki.com");
    }

    [Fact]
    public void GenerateToken_ShouldContainUserNameClaim()
    {
        // Arrange
        var user = CreateTestUser(userName: "KaitiakiHero");
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be("KaitiakiHero");
    }

    [Fact]
    public void GenerateToken_ShouldContainTotalXPClaim()
    {
        // Arrange
        var user = CreateTestUser(totalXP: 1234);
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var xpClaim = jwt.Claims.FirstOrDefault(c => c.Type == "TotalXP");
        xpClaim.Should().NotBeNull();
        xpClaim!.Value.Should().Be("1234");
    }

    [Fact]
    public void GenerateToken_ShouldContainLevelClaim()
    {
        // Arrange
        var user = CreateTestUser(level: 25);
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var levelClaim = jwt.Claims.FirstOrDefault(c => c.Type == "Level");
        levelClaim.Should().NotBeNull();
        levelClaim!.Value.Should().Be("25");
    }

    [Fact]
    public void GenerateToken_NullEmail_ShouldUseEmptyString()
    {
        // Arrange
        var user = CreateTestUser(email: null);
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void GenerateToken_NullUserName_ShouldUseEmptyString()
    {
        // Arrange
        var user = CreateTestUser(userName: null);
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be(string.Empty);
    }

    #endregion

    #region Token Roles

    [Fact]
    public void GenerateToken_SingleRole_ShouldContainRoleClaim()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "Admin" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(1);
        roleClaims[0].Value.Should().Be("Admin");
    }

    [Fact]
    public void GenerateToken_MultipleRoles_ShouldContainAllRoleClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "Admin", "Moderator", "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().HaveCount(3);
        roleClaims.Should().Contain("Admin");
        roleClaims.Should().Contain("Moderator");
        roleClaims.Should().Contain("User");
    }

    [Fact]
    public void GenerateToken_EmptyRoles_ShouldNotContainRoleClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string>();

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().BeEmpty();
    }

    #endregion

    #region Token Issuer / Audience

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuer()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        jwt.Issuer.Should().Be(TestIssuer);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectAudience()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        jwt.Audiences.Should().Contain(TestAudience);
    }

    #endregion

    #region Token Expiration

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiration()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };
        var beforeCreation = DateTime.UtcNow;

        // Act
        var token = _service.GenerateToken(user, roles);
        var afterCreation = DateTime.UtcNow;
        var jwt = DecodeToken(token);

        // Assert
        var expectedExpiry = beforeCreation.AddMinutes(60);
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_CustomExpiryMinutes_ShouldRespectConfig()
    {
        // Arrange
        _configMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("30");
        var service = new JwtService(_configMock.Object);
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_NullExpiryConfig_ShouldDefaultTo1440Minutes()
    {
        // Arrange
        _configMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns((string?)null);
        var service = new JwtService(_configMock.Object);
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert - 默认 1440 分钟 = 24 小时
        var expectedExpiry = DateTime.UtcNow.AddMinutes(1440);
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Token Signature

    [Fact]
    public void GenerateToken_ShouldUseHmacSha256Algorithm()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        jwt.Header.Alg.Should().Be("HS256");
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var user1 = CreateTestUser(id: "user-1", userName: "Alice");
        var user2 = CreateTestUser(id: "user-2", userName: "Bob");
        var roles = new List<string> { "User" };

        // Act
        var token1 = _service.GenerateToken(user1, roles);
        var token2 = _service.GenerateToken(user2, roles);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_SameUserDifferentRoles_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var roles1 = new List<string> { "User" };
        var roles2 = new List<string> { "Admin" };

        // Act
        var token1 = _service.GenerateToken(user, roles1);
        var token2 = _service.GenerateToken(user, roles2);

        // Assert
        token1.Should().NotBe(token2);
    }

    #endregion

    #region Token Validation (Integration-style)

    [Fact]
    public void GenerateToken_ShouldBeValidatableWithCorrectKey()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };
        var token = _service.GenerateToken(user, roles);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
        };

        var handler = new JwtSecurityTokenHandler();

        // Act
        var act = () => handler.ValidateToken(token, validationParameters, out _);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_WrongKey_ShouldFailValidation()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "User" };
        var token = _service.GenerateToken(user, roles);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("WrongKeyWrongKeyWrongKeyWrongKey12345!"))
        };

        var handler = new JwtSecurityTokenHandler();

        // Act
        var act = () => handler.ValidateToken(token, validationParameters, out _);

        // Assert
        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GenerateToken_ZeroXPAndLevel_ShouldContainZeroClaims()
    {
        // Arrange
        var user = CreateTestUser(totalXP: 0, level: 0);
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        jwt.Claims.First(c => c.Type == "TotalXP").Value.Should().Be("0");
        jwt.Claims.First(c => c.Type == "Level").Value.Should().Be("0");
    }

    [Fact]
    public void GenerateToken_LargeXPValue_ShouldHandleCorrectly()
    {
        // Arrange
        var user = CreateTestUser(totalXP: int.MaxValue, level: 999);
        var roles = new List<string> { "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        jwt.Claims.First(c => c.Type == "TotalXP").Value.Should().Be(int.MaxValue.ToString());
        jwt.Claims.First(c => c.Type == "Level").Value.Should().Be("999");
    }

    [Fact]
    public void GenerateToken_ShouldContainExactlyExpectedClaimCount()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _service.GenerateToken(user, roles);
        var jwt = DecodeToken(token);

        // Assert
        // 5 基础 claims + 2 role claims + JWT 内置 claims (exp, iss, aud, nbf, iat)
        var customClaims = jwt.Claims.Where(c =>
            c.Type == ClaimTypes.NameIdentifier ||
            c.Type == ClaimTypes.Email ||
            c.Type == ClaimTypes.Name ||
            c.Type == "TotalXP" ||
            c.Type == "Level" ||
            c.Type == ClaimTypes.Role).ToList();

        customClaims.Should().HaveCount(7); // 5 + 2 roles
    }

    #endregion
}