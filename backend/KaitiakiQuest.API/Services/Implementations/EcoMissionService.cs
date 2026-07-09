using Microsoft.EntityFrameworkCore;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services.Interfaces;

namespace KaitiakiQuest.API.Services.Implementations  
{
    public class EcoMissionService : IEcoMissionService
    {
        private readonly ApplicationDbContext _context;

        public EcoMissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<List<EcoMissionResponseDto>>> GetAllMissionsAsync(string? category, bool? isDaily)
        {
            var query = _context.EcoMissions
                .Where(m => m.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(m => m.Category == category);

            if (isDaily.HasValue)
                query = query.Where(m => m.IsDaily == isDaily.Value);

            var missions = await query
                .Select(m => new EcoMissionResponseDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    BasePoints = m.BasePoints,
                    Category = m.Category,
                    ImageUrl = m.ImageUrl,
                    IsDaily = m.IsDaily,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return ServiceResult<List<EcoMissionResponseDto>>.Success(missions);
        }

        public async Task<ServiceResult<EcoMissionResponseDto>> GetMissionByIdAsync(int id)
        {
            var mission = await _context.EcoMissions
                .Where(m => m.Id == id && m.IsActive)
                .Select(m => new EcoMissionResponseDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    BasePoints = m.BasePoints,
                    Category = m.Category,
                    ImageUrl = m.ImageUrl,
                    IsDaily = m.IsDaily,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (mission == null)
                return ServiceResult<EcoMissionResponseDto>.Failure("Mission not found");

            return ServiceResult<EcoMissionResponseDto>.Success(mission);
        }

        public async Task<ServiceResult<EcoMissionResponseDto>> CreateMissionAsync(CreateEcoMissionDto dto)
        {
            var mission = new EcoMission
            {
                Title = dto.Title,
                Description = dto.Description,
                BasePoints = dto.BasePoints,
                Category = dto.Category,
                ImageUrl = dto.ImageUrl,
                IsDaily = dto.IsDaily,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.EcoMissions.Add(mission);
            await _context.SaveChangesAsync();

            var response = new EcoMissionResponseDto
            {
                Id = mission.Id,
                Title = mission.Title,
                Description = mission.Description,
                BasePoints = mission.BasePoints,
                Category = mission.Category,
                ImageUrl = mission.ImageUrl,
                IsDaily = mission.IsDaily,
                IsActive = mission.IsActive,
                CreatedAt = mission.CreatedAt
            };

            return ServiceResult<EcoMissionResponseDto>.Success(response, "Mission created successfully");
        }

        public async Task<ServiceResult<EcoMissionResponseDto>> UpdateMissionAsync(int id, UpdateEcoMissionDto dto)
        {
            var mission = await _context.EcoMissions.FindAsync(id);
            if (mission == null)
                return ServiceResult<EcoMissionResponseDto>.Failure("Mission not found");

            mission.Title = dto.Title;
            mission.Description = dto.Description;
            mission.BasePoints = dto.BasePoints;
            mission.Category = dto.Category;
            mission.ImageUrl = dto.ImageUrl;
            mission.IsDaily = dto.IsDaily;
            mission.IsActive = dto.IsActive;
            mission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new EcoMissionResponseDto
            {
                Id = mission.Id,
                Title = mission.Title,
                Description = mission.Description,
                BasePoints = mission.BasePoints,
                Category = mission.Category,
                ImageUrl = mission.ImageUrl,
                IsDaily = mission.IsDaily,
                IsActive = mission.IsActive,
                CreatedAt = mission.CreatedAt
            };

            return ServiceResult<EcoMissionResponseDto>.Success(response, "Mission updated successfully");
        }

        public async Task<ServiceResult<bool>> DeleteMissionAsync(int id)
        {
            var mission = await _context.EcoMissions.FindAsync(id);
            if (mission == null)
                return ServiceResult<bool>.Failure("Mission not found");

            mission.IsActive = false;
            mission.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true, "Mission deleted successfully");
        }

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            var categories = await _context.EcoMissions
                .Where(m => m.IsActive)
                .Select(m => m.Category)
                .Distinct()
                .ToListAsync();

            return ServiceResult<List<string>>.Success(categories);
        }
    }
}