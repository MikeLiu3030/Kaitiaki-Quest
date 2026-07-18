using KaitiakiQuest.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace KaitiakiQuest.API.Hubs
{
    [Authorize]
    public class TeamHub: Hub
    {
        private readonly ILogger<TeamHub> _logger;
        private readonly ApplicationDbContext _dbContext;

        public TeamHub(ILogger<TeamHub> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        ///<summary>
        /// Assign room automatically by user Token
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("[SignalR] connection failed: cannot find user credential!");
                await base.OnConnectedAsync();
                return;
            }

            // Automatically assign the specified room when user has joined.
            await AssignUserToTeamRoomAsync(userId!);
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"User {Context.UserIdentifier} disconnected");
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Core private method: handle database queries and the logic to join a team
        /// </summary>
        /// <returns></returns>
        private async Task AssignUserToTeamRoomAsync(string userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"[SignalR Debug] warning：the user {userId} is not found in the database！");
                return;
            }
            if (user.Team == null)
            {
                _logger.LogWarning($"[SignalR Debug] warning：The user {user.UserName} does not join a team！");
                return;
            }

            string roomName = user.Team.InviteCode.Trim();
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            _logger.LogInformation($"[SignalR Debug] Successfully join a team. User: {user.UserName}, Team: '{roomName}', ConnectionId: {Context.ConnectionId}");
        }

    }
}
