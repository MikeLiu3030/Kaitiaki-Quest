using KaitiakiQuest.API.DTOs;

namespace KaitiakiQuest.API.Services.Interfaces  
{
    public interface IEcoMissionService
    {
        Task<ServiceResult<List<EcoMissionResponseDto>>> GetAllMissionsAsync(string? category, bool? isDaily);
        Task<ServiceResult<EcoMissionResponseDto>> GetMissionByIdAsync(int id);
        Task<ServiceResult<EcoMissionResponseDto>> CreateMissionAsync(CreateEcoMissionDto dto);
        Task<ServiceResult<EcoMissionResponseDto>> UpdateMissionAsync(int id, UpdateEcoMissionDto dto);
        Task<ServiceResult<bool>> DeleteMissionAsync(int id);
        Task<ServiceResult<List<string>>> GetCategoriesAsync();
    }
}