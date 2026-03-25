using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HotelBookingApp.Helpers
{
    /// <summary>Generates and validates JWT tokens using configuration settings.</summary>
    public class JwtTokenHelper
    {
        private readonly IConfiguration _config;

        public JwtTokenHelper(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Generates a signed JWT for the given user identity.</summary>
        public string GenerateToken(int userId, string userName, string role)
        {
            var key     = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
            var issuer   = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expiryDays = int.TryParse(_config["Jwt:ExpiryInDays"], out var d) ? d : 1;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.Name,           userName),
                new(ClaimTypes.Role,           role.ToLower()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var securityKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials  = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
