using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KaitiakiQuest.API.Hubs
{
    [Authorize]
    public class TeamHub: Hub
    {
        private readonly ILogger<TeamHub> _logger;

        public TeamHub(ILogger<TeamHub> logger)
        {
            _logger = logger;
        }

        ///<summary>
        ///The user joins the team room (called by the frontend)
        /// </summary>
        public async Task JoinTeamRoom(string teamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team-{teamId}");
            _logger.LogInformation($"User {Context.UserIdentifier} joined team {teamId}");
        }

        ///<summary>
        ///The user leaves the team room
        /// </summary>
        public async Task LeaveTeamRoom(string teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team-{teamId}");
            _logger.LogInformation($"User {Context.UserIdentifier} left team {teamId}");
        }

        ///<summary>
        ///Notify team members: Someone has completed the task
        /// </summary>
        public async Task NotifyMissionCompleted(
            string teamId,
            string userName,
            string missionTitle,
            int earnedXP)
        {
            await Clients.Group($"team-{teamId}").SendAsync(
                "MissionCompleted",
                new 
                {
                    UserNmae = userName,
                    MissionTitle = missionTitle,
                    EarnedXP = earnedXP,
                    CompletedAt = DateTime.UtcNow
                });

            _logger.LogInformation($"Notified team {teamId}: {userName} completed {missionTitle}");
        }

        /// <summary>
        /// Notify team members: The overall XP update for the team
        /// </summary>
        public async Task NotifyTeamXPUpdated(string teamId, int newTotalXP)
        {
            await Clients.Group($"team-{teamId}").SendAsync(
                "TeamXPUpdated",
                new
                {
                    TotalTeamXP = newTotalXP,
                    UpdatedAt = DateTime.UtcNow
                });
        }
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"User {Context.UserIdentifier} connected");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"User {Context.UserIdentifier} disconnected");
            await base.OnDisconnectedAsync(exception);
        }

    }
}
