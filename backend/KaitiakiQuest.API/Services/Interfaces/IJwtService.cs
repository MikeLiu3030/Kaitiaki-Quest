using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user, IList<string> roles);
    }
}
