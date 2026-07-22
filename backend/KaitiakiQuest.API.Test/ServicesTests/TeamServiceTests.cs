using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.SignalR;
using Xunit;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services.Implementations;
using KaitiakiQuest.API.Hubs;

namespace KaitiakiQuest.API.Tests.Services;

public class TeamServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<TeamService>> _loggerMock;
    private readonly Mock<IHubContext<TeamHub>> _hubContextMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly TeamService _service;

    public TeamServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<TeamService>>();

        // Mock SignalR
        _groupManagerMock = new Mock<IGroupManager>();
        _clientProxyMock = new Mock<IClientProxy>();
        _clientsMock = new Mock<IHubClients>();

        _hubContextMock = new Mock<IHubContext<TeamHub>>();
        _hubContextMock.Setup(h => h.Groups).Returns(_groupManagerMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.GroupExcept(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_clientProxyMock.Object);

        _service = new TeamService(
            _context,
            _cache,
            _loggerMock.Object,
            _hubContextMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _cache.Dispose();
    }

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUserAsync(string userId, int? teamId = null)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"test-{userId}",
            Email = $"{userId}@test.com",
            TeamId = teamId,
            TotalXP = 100,
            Level = 5
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Team> CreateTestTeamAsync(string name, string? createdByUserId = null)
    {
        var team = new Team
        {
            Name = name,
            Description = "Test team",
            InviteCode = GenerateTestInviteCode(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            TotalTeamXP = 0
        };
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
        return team;
    }

    private string GenerateTestInviteCode()
    {
        return Guid.NewGuid().ToString("N")[..8].ToUpper();
    }

    #endregion

    #region CreateTeamAsync Tests

    [Fact]
    public async Task CreateTeamAsync_ValidInput_ShouldCreateTeam()
    {
        // Arrange
        var userId = "user-1";
        await CreateTestUserAsync(userId);
        var dto = new CreateTeamDto { Name = "Test Team", Description = "A test team" };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Test Team");
        result.Data.Description.Should().Be("A test team");
        result.Data.InviteCode.Should().NotBeNullOrEmpty();
        result.Data.TotalTeamXP.Should().Be(0);
        result.Data.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateTeamAsync_WithConnectionId_ShouldAddToSignalRGroup()
    {
        // Arrange
        var userId = "user-2";
        await CreateTestUserAsync(userId);
        var dto = new CreateTeamDto
        {
            Name = "SignalR Team",
            ConnectionId = "conn-123"
        };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _groupManagerMock.Verify(
            x => x.AddToGroupAsync(
                "conn-123",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTeamAsync_WithoutConnectionId_ShouldNotCallSignalR()
    {
        // Arrange
        var userId = "user-3";
        await CreateTestUserAsync(userId);
        var dto = new CreateTeamDto { Name = "No SignalR Team", ConnectionId = null };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _groupManagerMock.Verify(
            x => x.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTeamAsync_UserAlreadyInTeam_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-4";
        var team = await CreateTestTeamAsync("Existing Team", userId);
        await CreateTestUserAsync(userId, teamId: team.Id);

        var dto = new CreateTeamDto { Name = "Another Team" };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("already in a team");
    }

    [Fact]
    public async Task CreateTeamAsync_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateTeamDto { Name = "Ghost Team" };

        // Act
        var result = await _service.CreateTeamAsync("non-existent-user", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task CreateTeamAsync_EmptyName_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-5";
        await CreateTestUserAsync(userId);
        var dto = new CreateTeamDto { Name = "" };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Team name is required");
    }

    [Fact]
    public async Task CreateTeamAsync_NameTooLong_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-6";
        await CreateTestUserAsync(userId);
        var dto = new CreateTeamDto { Name = new string('A', 51) };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("too long");
    }

    [Fact]
    public async Task CreateTeamAsync_DuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-7";
        await CreateTestUserAsync(userId);
        await CreateTestTeamAsync("Duplicate Team");

        var dto = new CreateTeamDto { Name = "Duplicate Team" };

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateTeamAsync_ShouldClearLeaderboardCache()
    {
        // Arrange
        var userId = "user-8";
        await CreateTestUserAsync(userId);
        var dto = new CreateTeamDto { Name = "Cache Team" };

        // 先写入缓存
        _cache.Set("TeamLeaderboard", new List<TeamLeaderboardDto>());

        // Act
        await _service.CreateTeamAsync(userId, dto);

        // Assert
        _cache.TryGetValue("TeamLeaderboard", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreateTeamAsync_NullDto_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-9";
        await CreateTestUserAsync(userId);

        // Act
        var result = await _service.CreateTeamAsync(userId, null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTeamAsync_EmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateTeamDto { Name = "Test" };

        // Act
        var result = await _service.CreateTeamAsync("", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetMyTeamAsync Tests

    [Fact]
    public async Task GetMyTeamAsync_UserInTeam_ShouldReturnTeamDetail()
    {
        // Arrange
        var userId = "user-10";
        var team = await CreateTestTeamAsync("My Team", userId);
        await CreateTestUserAsync(userId, teamId: team.Id);

        // Act
        var result = await _service.GetMyTeamAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("My Team");
        result.Data.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyTeamAsync_UserNotInTeam_ShouldReturnNull()
    {
        // Arrange
        var userId = "user-11";
        await CreateTestUserAsync(userId);

        // Act
        var result = await _service.GetMyTeamAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("You are not in a team");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetMyTeamAsync_UserNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetMyTeamAsync("non-existent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task GetMyTeamAsync_EmptyUserId_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetMyTeamAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_ValidId_ShouldReturnTeam()
    {
        // Arrange
        var team = await CreateTestTeamAsync("Detail Team", "leader-1");
        await CreateTestUserAsync("leader-1", teamId: team.Id);

        // Act
        var result = await _service.GetTeamByIdAsync(team.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(team.Id);
        result.Data.Name.Should().Be("Detail Team");
        result.Data.InviteCode.Should().Be(team.InviteCode);
    }

    [Fact]
    public async Task GetTeamByIdAsync_InvalidId_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetTeamByIdAsync(0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Invalid Team ID");
    }

    [Fact]
    public async Task GetTeamByIdAsync_NonExistentTeam_ShouldReturnFailure()
    {
        // Act
        var result = await _service.GetTeamByIdAsync(9999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Team not found");
    }

    #endregion

    #region JoinTeamAsync Tests

    [Fact]
    public async Task JoinTeamAsync_ValidInviteCode_ShouldJoinTeam()
    {
        // Arrange
        var leaderId = "leader-2";
        var team = await CreateTestTeamAsync("Join Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        var userId = "user-12";
        await CreateTestUserAsync(userId);

        var dto = new JoinTeamDto
        {
            InviteCode = team.InviteCode,
            ConnectionId = "conn-456"
        };

        // Act
        var result = await _service.JoinTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var userInDb = await _context.Users.FindAsync(userId);
        userInDb!.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task JoinTeamAsync_WithConnectionId_ShouldAddToGroup()
    {
        // Arrange
        var leaderId = "leader-3";
        var team = await CreateTestTeamAsync("Group Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        var userId = "user-13";
        await CreateTestUserAsync(userId);

        var dto = new JoinTeamDto
        {
            InviteCode = team.InviteCode,
            ConnectionId = "conn-789"
        };

        // Act
        await _service.JoinTeamAsync(userId, dto);

        // Assert
        _groupManagerMock.Verify(
            x => x.AddToGroupAsync("conn-789", team.InviteCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinTeamAsync_UserAlreadyInTeam_ShouldReturnFailure()
    {
        // Arrange
        var team1 = await CreateTestTeamAsync("Team A", "leader-4");
        var team2 = await CreateTestTeamAsync("Team B", "leader-5");
        await CreateTestUserAsync("leader-4", teamId: team1.Id);
        await CreateTestUserAsync("leader-5", teamId: team2.Id);

        var userId = "user-14";
        await CreateTestUserAsync(userId, teamId: team1.Id);

        var dto = new JoinTeamDto { InviteCode = team2.InviteCode, ConnectionId = "conn" };

        // Act
        var result = await _service.JoinTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("already in a team");
    }

    [Fact]
    public async Task JoinTeamAsync_InvalidInviteCode_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-15";
        await CreateTestUserAsync(userId);

        var dto = new JoinTeamDto { InviteCode = "INVALID1", ConnectionId = "conn" };

        // Act
        var result = await _service.JoinTeamAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Invalid invite code");
    }

    [Fact]
    public async Task JoinTeamAsync_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var dto = new JoinTeamDto { InviteCode = "ABC123", ConnectionId = "conn" };

        // Act
        var result = await _service.JoinTeamAsync("ghost-user", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task JoinTeamAsync_ShouldBroadcastUserJoined()
    {
        // Arrange
        var leaderId = "leader-6";
        var team = await CreateTestTeamAsync("Broadcast Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        var userId = "user-16";
        await CreateTestUserAsync(userId);

        var dto = new JoinTeamDto
        {
            InviteCode = team.InviteCode,
            ConnectionId = "conn-broadcast"
        };

        // Act
        await _service.JoinTeamAsync(userId, dto);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserJoined",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region LeaveTeamAsync Tests

    [Fact]
    public async Task LeaveTeamAsync_RegularMember_ShouldLeaveTeam()
    {
        // Arrange
        var leaderId = "leader-7";
        var team = await CreateTestTeamAsync("Leave Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        var userId = "user-17";
        await CreateTestUserAsync(userId, teamId: team.Id);

        // Act
        var result = await _service.LeaveTeamAsync(userId, "conn-leave");

        // Assert
        result.IsSuccess.Should().BeTrue();

        var userInDb = await _context.Users.FindAsync(userId);
        userInDb!.TeamId.Should().BeNull();
    }

    [Fact]
    public async Task LeaveTeamAsync_LeaderWithMembers_ShouldTransferLeadership()
    {
        // Arrange
        var leaderId = "leader-8";
        var member2Id = "member-2";
        var team = await CreateTestTeamAsync("Transfer Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);
        await CreateTestUserAsync(member2Id, teamId: team.Id);

        // Act
        var result = await _service.LeaveTeamAsync(leaderId, "conn-leader");

        // Assert
        result.IsSuccess.Should().BeTrue();

        var teamInDb = await _context.Teams.FindAsync(team.Id);
        teamInDb!.CreatedByUserId.Should().Be(member2Id);

        var leaderInDb = await _context.Users.FindAsync(leaderId);
        leaderInDb!.TeamId.Should().BeNull();
    }

    [Fact]
    public async Task LeaveTeamAsync_LeaderAlone_ShouldDeleteTeam()
    {
        // Arrange
        var leaderId = "leader-9";
        var team = await CreateTestTeamAsync("Solo Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        // Act
        var result = await _service.LeaveTeamAsync(leaderId, "conn-solo");

        // Assert
        result.IsSuccess.Should().BeTrue();

        var teamInDb = await _context.Teams.FindAsync(team.Id);
        teamInDb.Should().BeNull();

        var leaderInDb = await _context.Users.FindAsync(leaderId);
        leaderInDb!.TeamId.Should().BeNull();
    }

    [Fact]
    public async Task LeaveTeamAsync_UserNotInTeam_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-18";
        await CreateTestUserAsync(userId);

        // Act
        var result = await _service.LeaveTeamAsync(userId, "conn");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not in any team");
    }

    [Fact]
    public async Task LeaveTeamAsync_UserNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _service.LeaveTeamAsync("ghost", "conn");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task LeaveTeamAsync_ShouldRemoveFromSignalRGroup()
    {
        // Arrange
        var leaderId = "leader-10";
        var team = await CreateTestTeamAsync("SignalR Leave", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        var userId = "user-19";
        await CreateTestUserAsync(userId, teamId: team.Id);

        // Act
        await _service.LeaveTeamAsync(userId, "conn-remove");

        // Assert
        _groupManagerMock.Verify(
            x => x.RemoveFromGroupAsync("conn-remove", team.InviteCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveTeamAsync_ShouldBroadcastUserLeft()
    {
        // Arrange
        var leaderId = "leader-11";
        var team = await CreateTestTeamAsync("Broadcast Leave", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        var userId = "user-20";
        await CreateTestUserAsync(userId, teamId: team.Id);

        // Act
        await _service.LeaveTeamAsync(userId, "conn-broadcast-leave");

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "UserLeft",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateTeamXPAsync Tests

    [Fact]
    public async Task UpdateTeamXPAsync_UserInTeam_ShouldAddXP()
    {
        // Arrange
        var leaderId = "leader-12";
        var team = await CreateTestTeamAsync("XP Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        // Act
        var result = await _service.UpdateTeamXPAsync(leaderId, 50);

        // Assert
        result.Should().Be(50);

        var teamInDb = await _context.Teams.FindAsync(team.Id);
        teamInDb!.TotalTeamXP.Should().Be(50);
        teamInDb.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTeamXPAsync_MultipleUpdates_ShouldAccumulate()
    {
        // Arrange
        var leaderId = "leader-13";
        var team = await CreateTestTeamAsync("Accumulate XP", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);

        // Act
        await _service.UpdateTeamXPAsync(leaderId, 30);
        await _service.UpdateTeamXPAsync(leaderId, 20);
        var result = await _service.UpdateTeamXPAsync(leaderId, 10);

        // Assert
        result.Should().Be(60);
    }

    [Fact]
    public async Task UpdateTeamXPAsync_UserNotInTeam_ShouldReturnZero()
    {
        // Arrange
        var userId = "user-21";
        await CreateTestUserAsync(userId);

        // Act
        var result = await _service.UpdateTeamXPAsync(userId, 100);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task UpdateTeamXPAsync_ShouldClearCache()
    {
        // Arrange
        var leaderId = "leader-14";
        var team = await CreateTestTeamAsync("Cache XP Team", leaderId);
        await CreateTestUserAsync(leaderId, teamId: team.Id);
        _cache.Set("TeamLeaderboard", new List<TeamLeaderboardDto>());

        // Act
        await _service.UpdateTeamXPAsync(leaderId, 25);

        // Assert
        _cache.TryGetValue("TeamLeaderboard", out _).Should().BeFalse();
    }

    #endregion

    #region GetTeamLeaderboardAsync Tests

    [Fact]
    public async Task GetTeamLeaderboardAsync_ShouldReturnRankedTeams()
    {
        // Arrange
        var team1 = await CreateTestTeamAsync("Team Alpha");
        team1.TotalTeamXP = 300;
        var team2 = await CreateTestTeamAsync("Team Beta");
        team2.TotalTeamXP = 100;
        var team3 = await CreateTestTeamAsync("Team Gamma");
        team3.TotalTeamXP = 200;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTeamLeaderboardAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data![0].TeamName.Should().Be("Team Alpha");
        result.Data[0].Rank.Should().Be(1);
        result.Data[1].TeamName.Should().Be("Team Gamma");
        result.Data[1].Rank.Should().Be(2);
        result.Data[2].TeamName.Should().Be("Team Beta");
        result.Data[2].Rank.Should().Be(3);
    }

    [Fact]
    public async Task GetTeamLeaderboardAsync_ShouldUseCache()
    {
        // Arrange
        var cachedData = new List<TeamLeaderboardDto>
        {
            new TeamLeaderboardDto { Rank = 1, TeamName = "Cached Team", TotalTeamXP = 999 }
        };
        _cache.Set("TeamLeaderboard", cachedData);

        // Act
        var result = await _service.GetTeamLeaderboardAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].TeamName.Should().Be("Cached Team");
        result.Data[0].TotalTeamXP.Should().Be(999);
    }

    [Fact]
    public async Task GetTeamLeaderboardAsync_ShouldCacheResult()
    {
        // Arrange
        var team = await CreateTestTeamAsync("Cache Test Team");
        team.TotalTeamXP = 500;
        await _context.SaveChangesAsync();

        // Act
        await _service.GetTeamLeaderboardAsync();

        // Assert
        _cache.TryGetValue("TeamLeaderboard", out List<TeamLeaderboardDto>? cached).Should().BeTrue();
        cached.Should().NotBeNull();
        cached!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTeamLeaderboardAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetTeamLeaderboardAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    #endregion
}