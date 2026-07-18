using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KaitiakiQuest.API.Services.Implementations
{
    public class BadgeService : IBadgeService
    {
        private readonly ApplicationDbContext _context;
        public BadgeService(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<ServiceResult<List<BadgeResponseDto>>> GetAllBadgesAsync()
        {
            var badges = await _context.Badges
                .Where(b => b.IsActive)
                .Select(b => new BadgeResponseDto { 
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    IconUrl = b.IconUrl,
                    UnlockXP = b.UnlockXP,
                    IsActive = b.IsActive,
                })
                .ToListAsync();

            return ServiceResult<List<BadgeResponseDto>>.Success(badges, "Retrieve Badges successfully.");
        }

        public async Task<ServiceResult<List<UserBadgeResponseDto>>> GetUserBadgesAsync(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                return ServiceResult<List<UserBadgeResponseDto>>.Failure("User not found!");
            }

            var userBadges = await _context.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .Select(ub => new UserBadgeResponseDto
                {
                    Id = ub.Id,
                    UserId = userId,
                    BadgeId = ub.BadgeId,
                    AwardedDate = ub.AwardedDate,
                    Badge = new BadgeResponseDto
                    {
                        Id = ub.Badge!.Id,
                        Name = ub.Badge.Name,
                        Description = ub.Badge.Description,
                        IconUrl = ub.Badge.IconUrl,
                        UnlockXP = ub.Badge.UnlockXP,
                        IsActive = ub.Badge.IsActive
                    },
                })
                .OrderByDescending(ub => ub.AwardedDate)
                .ToListAsync();
            return ServiceResult<List<UserBadgeResponseDto>>.Success(userBadges);
        }
    }
}
