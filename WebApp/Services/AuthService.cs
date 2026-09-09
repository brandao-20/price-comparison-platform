using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace WebApp.Services
{
    public class AuthService
    {
        public string UserName { get; set; } = "";
        public string Role { get; set; } = "";
        public string Token { get; set; } = "";

        // Armazena o ID do utilizador com login efetuado
        public int UserId { get; set; } = 0;

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public bool IsAdmin => Role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        public bool IsManager => Role.Equals("USER_MANAGER", StringComparison.OrdinalIgnoreCase) || Role.Equals("USERMANAGER", StringComparison.OrdinalIgnoreCase);
        public bool IsUser => Role.Equals("USER", StringComparison.OrdinalIgnoreCase);

        public void Clear()
        {
            UserName = "";
            Role = "";
            Token = "";
            UserId = 0;
        }

        // This is a client-side expiry check only. The API validates signatures and authorization.
        public bool ValidateToken()
        {
            try
            {
                if (!string.IsNullOrEmpty(Token))
                {
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(Token);
                    var value = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                    if (long.TryParse(value, out var exp) && DateTimeOffset.FromUnixTimeSeconds(exp) > DateTimeOffset.UtcNow)
                        return true;
                }
            }
            catch (Exception)
            {
                // Invalid tokens must not leave stale UI identity behind.
            }
            Clear();
            return false;
        }
    }
}
