using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using WebAPI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Entities;
using WebAPI.Helpers;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUtilizadorRepository _utilizadorRepository;
        private readonly IRoleService _roleService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            IUtilizadorRepository utilizadorRepository,
            IRoleService roleService,
            JwtSettings jwtSettings)
        {
            _utilizadorRepository = utilizadorRepository;
            _roleService = roleService;
            _jwtSettings = jwtSettings;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "O nome de utilizador é obrigatório.",
                    ErrorCode = "INVALID_USERNAME",
                    StatusCode = 400,
                    Data = null
                });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "A senha é obrigatória.",
                    ErrorCode = "INVALID_PASSWORD",
                    StatusCode = 400,
                    Data = null
                });

            Expression<Func<Utilizador, bool>> predicate = u => u.Username == request.Username;
            var users = await _utilizadorRepository.FindWithDetailsAsync(predicate);
            var user = users.FirstOrDefault();

            if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Credenciais inválidas.",
                    ErrorCode = "INVALID_CREDENTIALS",
                    StatusCode = 401,
                    Data = null
                });

            var token = GenerateJwtToken(user);
            var role = _roleService.NormalizeRole(user.TipoUtilizador?.Tipo ?? "USER");
            var response = new LoginResponse { Token = token, Role = role };
            Response.Headers.CacheControl = "no-store";
            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Message = "Login realizado com sucesso.",
                StatusCode = 200,
                Data = response
            });
        }

        private string GenerateJwtToken(Utilizador user)
        {
            var roleNormalized = _roleService.NormalizeRole(user.TipoUtilizador?.Tipo ?? "USER");
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("utilizadorId", user.UtilizadorId.ToString()),
                new Claim(ClaimTypes.Role, roleNormalized),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = _jwtSettings.SigningKey;
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
