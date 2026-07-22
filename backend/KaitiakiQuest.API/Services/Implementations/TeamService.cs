using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Hubs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace KaitiakiQuest.API.Services.Implementations
{
    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TeamService> _logger;
        private readonly IHubContext<TeamHub> _hubContext;

        public TeamService(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            ILogger<TeamService> logger,
            IHubContext<TeamHub> hubContext)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _hubContext = hubContext;
        }

        ///<summary>
        ///Generate random invitation codes(8-digit alphanumeric numbers for easy sharing)
        /// </summary>
        private string GenerateInviteCode()
        {
            const string allowedchars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            return RandomNumberGenerator.GetString(allowedchars, 8);
        }
        //===============================
        // Get the user's team
        //===============================
        public async Task<ServiceResult<TeamDetailDto>> GetMyTeamAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<TeamDetailDto>.Failure("User ID cannot be null or empty.");
            }

            var result = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    HasTeam = u.Team != null,
                    Team = u.Team == null ? null : new TeamDetailDto
                    {
                        Id = u.Team.Id,
                        Name = u.Team.Name,
                        Description = u.Team.Description,
                        InviteCode = u.Team.InviteCode,
                        TotalTeamXP = u.Team.TotalTeamXP,
                        MemberCount = u.Team.Members.Count,
                        TeamLeaderName = u.Team.CreatedByUser != null ? u.Team.CreatedByUser.UserName : "Unknown",
                        CreatedAt = u.Team.CreatedAt,
                        Members = u.Team.Members.Select(m => new TeamMemberDto
                        {
                            UserId = m.Id,
                            UserName = m.UserName ?? "Unknown",
                            Email = m.Email ?? "Unknown",
                            TotalXP = m.TotalXP,
                            Level = m.Level,
                            IsTeamLeader = u.Team.CreatedByUserId == m.Id
                        }).ToList()
                    }
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return ServiceResult<TeamDetailDto>.Failure("User not found");
            }

            return ServiceResult<TeamDetailDto>.Success(result.Team!);
        }

        //========================================
        // Get the detail of specified Team
        //========================================
        public async Task<ServiceResult<TeamDetailDto>> GetTeamByIdAsync(int teamId)
        {
            if (teamId <= 0)
            {
                return ServiceResult<TeamDetailDto>.Failure("Invalid Team ID.");
            }

            var teamDto = await _context.Teams
                .Where(t => t.Id == teamId)
                .Select(t => new TeamDetailDto 
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    InviteCode = t.InviteCode,
                    TotalTeamXP = t.TotalTeamXP,
                    MemberCount = t.Members.Count,
                    TeamLeaderName = t.CreatedByUser != null ? t.CreatedByUser.UserName : "Unknown",
                    CreatedAt = t.CreatedAt,
                    Members = t.Members.Select(m => new TeamMemberDto
                    {
                        UserId = m.Id,
                        UserName = m.UserName ?? "Unknown",
                        Email = m.Email ?? "Unknown",
                        TotalXP = m.TotalXP,
                        Level = m.Level,
                        IsTeamLeader = t.CreatedByUserId == m.Id
                    })
                    .ToList()
                })
                .FirstOrDefaultAsync();

            if (teamDto == null)
            {
                return ServiceResult<TeamDetailDto>.Failure("Team not found");
            }

            return ServiceResult<TeamDetailDto>.Success(teamDto);
        }

        //===============================
        // Create a team
        //===============================

        public async Task<ServiceResult<TeamDetailDto>> CreateTeamAsync(
            string userId, 
            CreateTeamDto dto)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<TeamDetailDto>.Failure("User ID cannot be null or empty.");
            }

            if (dto == null) 
            {
                return ServiceResult<TeamDetailDto>.Failure("Request data cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult<TeamDetailDto>.Failure("Team name is required.");
            }

            if (dto.Name.Length > 50)
            {
                return ServiceResult<TeamDetailDto>.Failure("Team name is too long.");
            }

            // check if user exists and their team status
            var userExistsAndHasTeam = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.TeamId})
                .FirstOrDefaultAsync ();
            if (userExistsAndHasTeam == null)
            {
                return ServiceResult<TeamDetailDto>.Failure("User not found");
            }

            if (userExistsAndHasTeam.TeamId != null)
            {
                return ServiceResult<TeamDetailDto>.Failure("You are already in a team.");
            }

            // check if the Team name has been occupied
            var nameExists = await _context.Teams.AnyAsync(t => t.Name == dto.Name);
            if (nameExists)
            {
                return ServiceResult<TeamDetailDto>.Failure("A team with this name already exists");
            }

            // Generate a unique invitation code
            string inviteCode ;
            do
            {
                inviteCode = GenerateInviteCode();
            } while (await _context.Teams.AnyAsync(t => t.InviteCode == inviteCode)); // avoid duplication

            // Create a team
            var team = new Team
            {
                Name = dto.Name,
                Description = dto.Description,
                InviteCode = inviteCode,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                TotalTeamXP = 0
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Add the creator to the team 
            var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userToUpdate != null)
            {
                userToUpdate.TeamId = team.Id;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(dto.ConnectionId))
            {               
                await _hubContext.Groups.AddToGroupAsync(dto.ConnectionId, inviteCode.Trim());
            }

            // print log information
            _logger.LogInformation($"User {userId} created team {team.Name} with invite code {inviteCode}");

            //Clear the cached team leaderboard
            _cache.Remove("TeamLeaderboard");

            return await GetTeamByIdAsync(team.Id);

        }

        //===============================
        // Join a team
        //===============================
        public async Task<ServiceResult<TeamDetailDto>> JoinTeamAsync(string userId, JoinTeamDto dto)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<TeamDetailDto>.Failure("User ID cannot be null or empty.");
            }

            if (dto == null)
            {
                return ServiceResult<TeamDetailDto>.Failure("Request data cannot be null.");
            }

            // check if the user exists.
            var userInfo = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new {u.UserName, u.TeamId })
                .FirstOrDefaultAsync();

            if (userInfo == null)
            {
                return ServiceResult<TeamDetailDto>.Failure("User not found");
            }

            // Check if user has joined a team.
            if (userInfo.TeamId != null)
                return ServiceResult<TeamDetailDto>.Failure("You are already in a team. Please leave your current team first.");

            var inviteCodeUpper = dto.InviteCode.ToUpper().Trim();
            // Check if the team existed
            var targetTeamId = await _context.Teams
                .Where(t => t.InviteCode == inviteCodeUpper)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(); // if no team, the default value is 0.

            // check if the teamId == 0         
            if (targetTeamId == 0)
                return ServiceResult<TeamDetailDto>.Failure("Invalid invite code. Team not found.");

            // update database (add TeamId to the user)
            var userToJoin = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userToJoin != null)
            {
                userToJoin.TeamId = targetTeamId;
                await _context.SaveChangesAsync();
            }

            //Synchronize and update the status of SignalR (ADD the user's websocket connection id to the group)
            string roomName = inviteCodeUpper;
            if (!string.IsNullOrEmpty(dto.ConnectionId))
            {                
                // SignalR join a team
                await _hubContext.Groups.AddToGroupAsync(dto.ConnectionId, roomName);               
            }

            //Bradcast message: you join a team
            var clientsToNotify = !string.IsNullOrEmpty(dto.ConnectionId)
                ? _hubContext.Clients.GroupExcept(roomName, new[] { dto.ConnectionId }) // Send it to everyone in the room except me 
                : _hubContext.Clients.Group(roomName); // If the network is disconnected and the connectionId is not sent, then send it to everyone

            await clientsToNotify.SendAsync("UserJoined", new
            {
                UserName = userInfo.UserName,
                Message = "Join our team",
                JoinedAt = DateTime.UtcNow,
            });

            _logger.LogInformation($"User {userInfo.UserName} joined team via invite code {dto.InviteCode}");
            
            //Clear the cached team leaderboard
            _cache.Remove("TeamLeaderboard");

            return await GetMyTeamAsync(userId);
        }

        //===============================
        // Leave a team
        //===============================
        public async Task<ServiceResult<bool>> LeaveTeamAsync(string userId, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult<bool>.Failure("User ID cannot be null or empty.");
            }



            // Get user's team information
            var userInfo = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {   u.UserName,
                    u.TeamId,
                    IsTeamLeader = u.Team != null && u.Team.CreatedByUserId == userId,
                    InviteCode = u.Team != null ? u.Team.InviteCode : null, // get invite code.
                })
                .FirstOrDefaultAsync();

            if (userInfo == null)
                return ServiceResult<bool>.Failure("User not found");

            if (userInfo.TeamId == null)
                return ServiceResult<bool>.Failure("You are not in any team");
            // Get the current Team ID
            var currentTeamId = userInfo.TeamId.Value;

            if (userInfo.InviteCode == null)
                return ServiceResult<bool>.Failure("You are not in any team");
            // Get the current Team invite code.
            string inviteCode = userInfo.InviteCode;          

            
            // If it is the team leader,
            // it is necessary to check whether there are any other members
            // If it is not a team leader only set the the user's TeamId = null.
            if (userInfo.IsTeamLeader)
            {
                // Check if there is any other members and find the first person.
                var otherMemberId = await _context.Users
                    .Where(u => u.TeamId == userInfo.TeamId && u.Id != userId)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                
                if (otherMemberId != null)
                {
                    // If there are other members, transfer the authority of the team leader
                    var teamToTransfer = await _context.Teams.FirstOrDefaultAsync(t => t.Id == currentTeamId);
                    if (teamToTransfer != null)
                    {
                        teamToTransfer.CreatedByUserId = otherMemberId;
                        await _context.SaveChangesAsync();
                    }
                } 
                else
                {
                    // Team has only teamleader, remove team
                    var teamToDelete = await _context.Teams.FirstOrDefaultAsync(t => t.Id == currentTeamId);
                    if (teamToDelete != null)
                    {
                        _context.Teams.Remove(teamToDelete);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // Whether you are the team leader or not,
            // remove yourself from the team (TeamId = null)
            var userToLeave = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userToLeave != null)
            {
                userToLeave.TeamId = null;
                await _context.SaveChangesAsync();
            }


            string roomName = userInfo.InviteCode.Trim();

            // Bradcast: user left team. 
            var clientsToNotify = !string.IsNullOrWhiteSpace(connectionId)
                ? _hubContext.Clients.GroupExcept(roomName, new[] { connectionId }) // Send it to everyone in the room except me 
                : _hubContext.Clients.Group(roomName);  // If the network is disconnected and the connectionId is not sent, then send it to everyone

            await clientsToNotify.SendAsync("UserLeft", new
            {
                UserName = userInfo.UserName,
                Message = "left our team.",
                LeftAt = DateTime.UtcNow,
            });
            // remove connection id from your signalR.
            if (!string.IsNullOrWhiteSpace(connectionId))
            {
                await _hubContext.Groups.RemoveFromGroupAsync(connectionId, inviteCode.Trim());
            }

            //Clear the cached team leaderboard
            _cache.Remove("TeamLeaderboard");

            return ServiceResult<bool>.Success(true, "You have left the team.");
        }

        //========================================
        //Update TotalTeamXP
        //========================================
        public async Task<int> UpdateTeamXPAsync(string userId, int earnedXP)
        {
            // Update TotalTeamXP property dirctly.
            var teamId = await _context.Users
                .Where(u => u.Id == userId && u.TeamId != null)
                .Select(u => u.TeamId)
                .FirstOrDefaultAsync();

            if (teamId == null)
                return 0;
            var team = await _context.Teams.FindAsync(teamId.Value);

            if (team == null) return 0;

            // accumulate XP
            team.TotalTeamXP += earnedXP;
            team.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();



            _cache.Remove("TeamLeaderboard");

            return team.TotalTeamXP;
        }

        //============================
        //Get TeamLeaderboard
        //============================
        public async Task<ServiceResult<List<TeamLeaderboardDto>>> GetTeamLeaderboardAsync()
        {
            const string cacheKey = "TeamLeaderboard";

            if (_cache.TryGetValue(cacheKey, out List<TeamLeaderboardDto>? cached) && cached != null)
            {
                _logger.LogInformation("Team leaderboard returned from cache");
                return ServiceResult<List<TeamLeaderboardDto>>.Success(cached);
            }

            _logger.LogInformation("Team leaderboard cache miss, querying database");

            var teams = await _context.Teams
                .Where(t => t.TotalTeamXP > 0 || t.Members.Any())
                .OrderByDescending(t => t.TotalTeamXP)
                .Select(t => new TeamLeaderboardDto
                {
                    TeamId = t.Id,
                    TeamName = t.Name,
                    TotalTeamXP = t.TotalTeamXP,
                    MemberCount = t.Members.Count,
                    TeamLeaderName = t.CreatedByUser != null ? t.CreatedByUser.UserName : "Unknown"
                })
                .ToListAsync();

            // add ranking
            for (int i = 0; i < teams.Count; i++)
            {
                teams[i].Rank = i + 1;
            }

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(cacheKey, teams, options);

            return ServiceResult<List<TeamLeaderboardDto>>.Success(teams);
        }
    }

}
