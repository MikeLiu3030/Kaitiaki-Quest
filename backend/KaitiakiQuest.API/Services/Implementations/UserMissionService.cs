using Microsoft.EntityFrameworkCore;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace KaitiakiQuest.API.Services.Implementations
{
    public class UserMissionService : IUserMissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGamificationService _gamificationService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserMissionService> _logger;

        public UserMissionService(
            ApplicationDbContext context, 
            IGamificationService gamificationService, 
            IMemoryCache cache, 
            ILogger<UserMissionService> logger)
        {
            _context = context;
            _gamificationService = gamificationService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ServiceResult<List<UserMissionResponseDto>>> GetMyMissionsAsync(string userId, string? status)
        {
            MissionStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse<MissionStatus>(status, true, out var parsedStatus))
                {
                    return ServiceResult<List<UserMissionResponseDto>>.Failure("Invalid mission status parameter.");
                }
                statusEnum = parsedStatus;
            }

            var query = _context.UserMissions
                .Include(um => um.EcoMission)
                .Where(um => um.UserId == userId);

            if (statusEnum.HasValue) 
            { 
                query = query.Where(um => um.Status == statusEnum.Value);
            }


            var missions = await query
                .OrderByDescending(um => um.AcceptedDate)
                .Select(um => new UserMissionResponseDto
                {
                    Id = um.Id,
                    EcoMissionId = um.EcoMissionId,
                    MissionTitle = um.EcoMission != null ? um.EcoMission.Title : "Unknown",
                    MissionDescription = um.EcoMission != null ? um.EcoMission.Description : "",
                    EarnedXP = um.EarnedXP,
                    Status = um.Status.ToString(),
                    AcceptedDate = um.AcceptedDate,
                    CompletedDate = um.CompletedDate,
                    EvidenceImageUrl = um.EvidenceImageUrl
                })
                .ToListAsync();

            return ServiceResult<List<UserMissionResponseDto>>.Success(missions);
        }

        public async Task<ServiceResult<object>> GetMyStatsAsync(string userId)
        {
            var totalMissions = await _context.UserMissions
                .Where(um => um.UserId == userId && um.Status == MissionStatus.Completed)
                .CountAsync();

            var totalXP = await _context.UserMissions
                .Where(um => um.UserId == userId && um.Status == MissionStatus.Completed)
                .SumAsync(um => um.EarnedXP);

            var user = await _context.Users.FindAsync(userId);
            var currentStreak = user?.CurrentStreak ?? 0;

            var weeklyMissions = await _context.UserMissions
                .Where(um => um.UserId == userId
                    && um.Status == MissionStatus.Completed
                    && um.CompletedDate >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            var stats = new
            {
                TotalMissions = totalMissions,
                TotalXP = totalXP,
                CurrentStreak = currentStreak,
                WeeklyMissions = weeklyMissions,
                Level = user?.Level ?? 1
            };

            return ServiceResult<object>.Success(stats);
        }

        public async Task<ServiceResult<UserMissionResponseDto>> AcceptMissionAsync(string userId, AcceptMissionDto dto)
        {
            var mission = await _context.EcoMissions
                .FirstOrDefaultAsync(m => m.Id == dto.EcoMissionId && m.IsActive);
            if (mission == null)
                return ServiceResult<UserMissionResponseDto>.Failure("Mission not available");

            var existing = await _context.UserMissions
                .FirstOrDefaultAsync(um => um.UserId == userId
                    && um.EcoMissionId == dto.EcoMissionId
                    && um.Status == MissionStatus.Pending);
            if (existing != null)
                return ServiceResult<UserMissionResponseDto>.Failure("You already accepted this mission");

            var userMission = new UserMission
            {
                UserId = userId,
                EcoMissionId = dto.EcoMissionId,
                Status = MissionStatus.Pending,
                AcceptedDate = DateTime.UtcNow
            };

            _context.UserMissions.Add(userMission);
            await _context.SaveChangesAsync();

            var response = new UserMissionResponseDto
            {
                Id = userMission.Id,
                EcoMissionId = userMission.EcoMissionId,
                MissionTitle = mission.Title,
                MissionDescription = mission.Description,
                Status = userMission.Status.ToString(),
                AcceptedDate = userMission.AcceptedDate
            };

            return ServiceResult<UserMissionResponseDto>.Success(response, "Mission accepted successfully");
        }

        public async Task<ServiceResult<UserMissionResponseDto>> CompleteMissionAsync(
            string userId, 
            int missionId, 
            CompleteMissionDto? dto)
        {
            var userMission = await _context.UserMissions
                .Include(um => um.EcoMission)
                .FirstOrDefaultAsync(um => um.Id == missionId && um.UserId == userId);

            if (userMission == null)
                return ServiceResult<UserMissionResponseDto>.Failure("Mission not found");

            if (userMission.Status != MissionStatus.Pending)
                return ServiceResult<UserMissionResponseDto>.Failure("Mission already completed or failed");
            //  Update task status
            userMission.Status = MissionStatus.Completed;
            userMission.CompletedDate = DateTime.UtcNow;
            if (dto != null && !string.IsNullOrEmpty(dto.EvidenceImageUrl))
                userMission.EvidenceImageUrl = dto.EvidenceImageUrl;

            // Call game engine to calculate points.
            userMission.EarnedXP = await _gamificationService.ProcessMissionCompletion(userId, userMission);

            // Update user information
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Accumulate the total score
                user.TotalXP += userMission.EarnedXP;
                // Update level
                user.Level = user.TotalXP / 100 + 1;
                // Check Streak
                await _gamificationService.UpdateStreak(userId);

                // Check and award new badges.
                await _gamificationService.CheckAndAwardBadges(userId);
            }

            await _context.SaveChangesAsync();

            // Clear the cached ranking list
            _cache.Remove("Leaderboard");
            _logger.LogInformation("Leaderboard cache cleared after mission completion");

            // Create response
            var response = new UserMissionResponseDto
            {
                Id = userMission.Id,
                EcoMissionId = userMission.EcoMissionId,
                MissionTitle = userMission.EcoMission?.Title ?? "Unknown",
                MissionDescription = userMission.EcoMission?.Description ?? "",
                EarnedXP = userMission.EarnedXP,
                Status = userMission.Status.ToString(),
                AcceptedDate = userMission.AcceptedDate,
                CompletedDate = userMission.CompletedDate,
                EvidenceImageUrl = userMission.EvidenceImageUrl
            };

            return ServiceResult<UserMissionResponseDto>.Success(response, "Mission completed! 🎉");
        }

        public async Task<ServiceResult<bool>> AbandonMissionAsync(string userId, int missionId)
        {
            var userMission = await _context.UserMissions
                .FirstOrDefaultAsync(um => um.Id == missionId && um.UserId == userId);

            if (userMission == null)
                return ServiceResult<bool>.Failure("Mission not found");

            if (userMission.Status != MissionStatus.Pending)
                return ServiceResult<bool>.Failure("Cannot abandon completed mission");

            userMission.Status = MissionStatus.Failed;
            userMission.FailedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true, "Mission abandoned");
        }

        public async Task<ServiceResult<List<object>>> GetLeaderboardAsync()
        {
            // Retrieve the leaderboard from cache.
            const string cacheKey = "Leaderboard";

            if (_cache.TryGetValue(cacheKey, out List<object>? cachedLeaderboard) && cachedLeaderboard != null)
            {
                _logger.LogInformation("Leaderboard returned from cache");
                return ServiceResult<List<object>>.Success(cachedLeaderboard);
            }

            // Cache miss. Query from the database
            _logger.LogInformation("Loeaderboard cache miss, querying database");
            
            var leaderboard = await _context.Users
                .Where(u => u.TotalXP > 0)
                .OrderByDescending(u => u.TotalXP)
                .Take(10)
                .Select(u => new
                {
                    u.UserName,
                    u.TotalXP,
                    u.Level,
                    u.CurrentStreak
                })
                .ToListAsync();
            var result = leaderboard.Cast<object>().ToList();

            // Save to cache, valid for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2))
                .SetPriority(CacheItemPriority.High);
            _cache.Set(cacheKey, result, cacheOptions);

            return ServiceResult<List<object>>.Success(result);
        }

    }
}