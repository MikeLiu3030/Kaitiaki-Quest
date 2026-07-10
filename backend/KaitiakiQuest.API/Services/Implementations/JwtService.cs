using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KaitiakiQuest.API.Services.Implementations
{
    public class JwtService: IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("TotalXP", user.TotalXP.ToString()),
                new Claim("Level", user.Level.ToString()),
            };

            // Add role Claim for RBAC
            foreach (var role in roles) 
            {
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }

            // Create a signature key
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "IloveYouForeverUntillTheSunDisapper!")
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Package the Claims into tokens and set the expiration time
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
