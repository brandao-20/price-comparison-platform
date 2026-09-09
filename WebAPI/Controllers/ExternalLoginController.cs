using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebAPI.Configuration;
using WebAPI.Entities;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalLoginController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly Uri _frontendUrl;
        private readonly GoogleLoginCodeStore _codes;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IUtilizadorRepository _utilizadorRepository;
        private readonly ITipoUtilizadorRepository _tipoUtilizadorRepository;
        private readonly IRoleService _roleService;

        public ExternalLoginController(
            JwtSettings jwtSettings,
            Uri frontendUrl,
            GoogleLoginCodeStore codes,
            IAuthenticationSchemeProvider schemes,
            IUtilizadorRepository utilizadorRepository,
            ITipoUtilizadorRepository tipoUtilizadorRepository,
            IRoleService roleService)
        {
            _jwtSettings = jwtSettings;
            _frontendUrl = frontendUrl;
            _codes = codes;
            _schemes = schemes;
            _utilizadorRepository = utilizadorRepository;
            _tipoUtilizadorRepository = tipoUtilizadorRepository;
            _roleService = roleService;
        }

        [HttpGet("Google")]
        public async Task<IActionResult> GoogleLogin([FromQuery] string challenge)
        {
            if (await _schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) == null)
                return Redirect(new Uri(_frontendUrl, "login?error=GoogleNotConfigured").AbsoluteUri);
            if (!GoogleLoginCodeStore.IsValidChallenge(challenge))
                return BadRequest(new { Message = "A valid login challenge is required." });
            var authProperties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };

            authProperties.Items["code_challenge"] = challenge;
            return Challenge(authProperties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("GoogleCallback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync("ExternalCookies");
            await HttpContext.SignOutAsync("ExternalCookies");
            Response.Headers.CacheControl = "no-store";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            if (!result.Succeeded || result.Principal == null)
            {
                return Redirect(new Uri(_frontendUrl, "login?error=GoogleLoginFailed").AbsoluteUri);
            }

            string? challenge = null;
            result.Properties?.Items.TryGetValue("code_challenge", out challenge);
            if (!GoogleLoginCodeStore.IsValidChallenge(challenge))
                return Redirect(new Uri(_frontendUrl, "login?error=GoogleLoginFailed").AbsoluteUri);

            var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(googleId))
            {
                return Redirect(new Uri(_frontendUrl, "login?error=GoogleIdMissing").AbsoluteUri);
            }

            var givenName = result.Principal.FindFirst("given_name")?.Value;
            var familyName = result.Principal.FindFirst("family_name")?.Value;
            var fullName = (givenName + " " + familyName).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = result.Principal.Identity?.Name ?? "GoogleUser";
            }
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "noemail@example.com";

            var user = (await _utilizadorRepository.FindAsync(u => u.GoogleId == googleId)).FirstOrDefault();
            if (user == null)
            {
                // External registration always creates a regular user.
                var tipo = (await _tipoUtilizadorRepository.FindAsync(t => t.Tipo == "USER")).FirstOrDefault();
                if (tipo == null)
                {
                    tipo = new TipoUtilizador { Tipo = "USER" };
                    await _tipoUtilizadorRepository.AddAsync(tipo);
                }

                user = new Utilizador
                {
                    Username = fullName,
                    Email = email,
                    GoogleId = googleId,
                    TipoUtilizadorId = tipo.TipoUtilizadorId,
                    Password = "",
                    DataCriacao = DateTime.UtcNow
                };
                await _utilizadorRepository.AddAsync(user);
            }
            else
            {
                user.Username = fullName;
                user.Email = email;
                await _utilizadorRepository.UpdateAsync(user);
            }

            // Load the existing account role before issuing its token.
            if (user.TipoUtilizador == null)
            {
                var tipo = user.TipoUtilizadorId.HasValue
                    ? await _tipoUtilizadorRepository.GetByIdAsync(user.TipoUtilizadorId.Value)
                    : null;
                user.TipoUtilizador = tipo ?? new TipoUtilizador { Tipo = "USER" };
            }

            var token = GenerateJwtForGoogleUser(user);
            var code = _codes.Issue(token, challenge!);
            return Redirect(new Uri(_frontendUrl, $"login?code={Uri.EscapeDataString(code)}").AbsoluteUri);
        }

        [HttpPost("exchange")]
        public IActionResult Exchange([FromBody] GoogleCodeRequest request)
        {
            Response.Headers.CacheControl = "no-store";
            var token = _codes.Redeem(request.Code, request.Verifier);
            if (token == null)
                return Unauthorized(new { Message = "The login code is invalid or expired. Start Google login again." });
            return Ok(new { Token = token });
        }

        public sealed class GoogleCodeRequest
        {
            public string Code { get; set; } = "";
            public string Verifier { get; set; } = "";
        }

        private string GenerateJwtForGoogleUser(Utilizador user)
        {
            var signingKey = _jwtSettings.SigningKey;
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var role = _roleService.NormalizeRole(user.TipoUtilizador?.Tipo ?? "USER");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim("utilizadorId", user.UtilizadorId.ToString())
            };


            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
