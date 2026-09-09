using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using WebAPI.Entities;
using WebAPI.Helpers;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilizadoresController : ControllerBase
    {
        private readonly IUtilizadorRepository _utilizadorRepository;
        private readonly ITipoUtilizadorRepository _tipoUtilizadorRepository;

        public UtilizadoresController(
            IUtilizadorRepository utilizadorRepository,
            ITipoUtilizadorRepository tipoUtilizadorRepository)
        {
            _utilizadorRepository = utilizadorRepository;
            _tipoUtilizadorRepository = tipoUtilizadorRepository;
        }

        [HttpGet("check")]
        public async Task<ActionResult<ApiResponse<CheckAvailabilityResponse>>> CheckAvailability([FromQuery] string username, [FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(email))
                return BadRequest(new ApiResponse<CheckAvailabilityResponse>
                {
                    Success = false,
                    Message = "Pelo menos um parâmetro (username ou email) deve ser fornecido.",
                    ErrorCode = "INVALID_PARAMETERS",
                    StatusCode = 400,
                    Data = null
                });

            bool usernameExists = false;
            bool emailExists = false;

            if (!string.IsNullOrWhiteSpace(username))
            {
                var existingUsername = await _utilizadorRepository.FindAsync(u => u.Username.ToLower() == username.ToLower());
                usernameExists = existingUsername.Any();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingEmail = await _utilizadorRepository.FindAsync(u => u.Email.ToLower() == email.ToLower());
                emailExists = existingEmail.Any();
            }

            var response = new CheckAvailabilityResponse
            {
                UsernameExists = usernameExists,
                EmailExists = emailExists
            };

            return Ok(new ApiResponse<CheckAvailabilityResponse>
            {
                Success = true,
                Message = "Verificação concluída.",
                StatusCode = 200,
                Data = response
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<Utilizador>>> Register(Utilizador utilizador)
        {
            if (!ValidAccount(utilizador.Username, utilizador.Email) || string.IsNullOrWhiteSpace(utilizador.Password))
                return BadRequest(new { Message = "A valid username, email, and password are required." });
            // Verificar duplicatas
            var usernameExists = await _utilizadorRepository.FindAsync(u => u.Username.ToLower() == utilizador.Username.ToLower());
            if (usernameExists.Any())
                return BadRequest(new ApiResponse<Utilizador>
                {
                    Success = false,
                    Message = "O nome de utilizador já está em uso.",
                    ErrorCode = "DUPLICATE_USERNAME",
                    StatusCode = 400,
                    Data = null
                });

            var emailExists = await _utilizadorRepository.FindAsync(u => u.Email.ToLower() == utilizador.Email.ToLower());
            if (emailExists.Any())
                return BadRequest(new ApiResponse<Utilizador>
                {
                    Success = false,
                    Message = "O email já está em uso.",
                    ErrorCode = "DUPLICATE_EMAIL",
                    StatusCode = 400,
                    Data = null
                });

            // Public registration always creates a regular account, including on an empty database.
            var userTipo = (await _tipoUtilizadorRepository.FindAsync(t => t.Tipo == "USER")).FirstOrDefault();
            if (userTipo == null)
            {
                userTipo = new TipoUtilizador { Tipo = "USER" };
                await _tipoUtilizadorRepository.AddAsync(userTipo);
            }
            utilizador = new Utilizador
            {
                Username = utilizador.Username,
                Email = utilizador.Email,
                Password = PasswordHelper.HashPassword(utilizador.Password),
                TipoUtilizadorId = userTipo.TipoUtilizadorId,
                DataCriacao = DateTime.UtcNow
            };
            await _utilizadorRepository.AddAsync(utilizador);

            return CreatedAtAction(nameof(GetById), new { id = utilizador.UtilizadorId }, new ApiResponse<Utilizador>
            {
                Success = true,
                Message = "Utilizador criado com sucesso.",
                StatusCode = 201,
                Data = utilizador
            });
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<Utilizador>> GetById(int id)
        {
            if (User.FindFirst("utilizadorId")?.Value != id.ToString() && !User.IsInRole("Admin") && !User.IsInRole("UserManager"))
                return Forbid();
            var user = await _utilizadorRepository.GetByIdWithDetailsAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Utilizador>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            pageSize = Math.Min(pageSize, 100);

            var totalItems = await _utilizadorRepository.CountAsync();
            var skip = (page - 1) * pageSize;
            var users = await _utilizadorRepository.GetPagedWithDetailsAsync(skip, pageSize);

            Response.Headers["X-Total-Count"] = totalItems.ToString();
            return Ok(users);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> Update(int id, Utilizador utilizador)
        {
            if (id != utilizador.UtilizadorId) return BadRequest();

            var existingUser = await _utilizadorRepository.GetByIdAsync(id);
            if (existingUser == null) return NotFound();
            var existingRole = existingUser.TipoUtilizadorId.HasValue
                ? await _tipoUtilizadorRepository.GetByIdAsync(existingUser.TipoUtilizadorId.Value) : null;
            if (!User.IsInRole("Admin") && !string.Equals(existingRole?.Tipo, "USER", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            if (!ValidAccount(utilizador.Username, utilizador.Email))
                return BadRequest(new { Message = "A valid username and email are required." });
            existingUser.Username = utilizador.Username;
            existingUser.Email = utilizador.Email;
            existingUser.Telefone = utilizador.Telefone;
            existingUser.Cargo = utilizador.Cargo;
            if (!string.IsNullOrWhiteSpace(utilizador.Password))
                existingUser.Password = PasswordHelper.HashPassword(utilizador.Password);

            try
            {
                await _utilizadorRepository.UpdateAsync(existingUser);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst("utilizadorId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int currentUserId) && id == currentUserId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Não é possível remover a si próprio por este endpoint.",
                    ErrorCode = "INVALID_ACTION",
                    StatusCode = 400,
                    Data = null
                });
            }

            var user = await _utilizadorRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Utilizador não encontrado.",
                ErrorCode = "NOT_FOUND",
                StatusCode = 404,
                Data = null
            });

            var targetRole = user.TipoUtilizadorId.HasValue
                ? await _tipoUtilizadorRepository.GetByIdAsync(user.TipoUtilizadorId.Value) : null;
            if (!User.IsInRole("Admin") && !string.Equals(targetRole?.Tipo, "USER", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            await _utilizadorRepository.DeleteAsync(user);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Utilizador removido com sucesso.",
                StatusCode = 200,
                Data = null
            });
        }

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userIdClaim = User.FindFirst("utilizadorId");
            if (userIdClaim == null) return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Usuário não identificado.",
                ErrorCode = "UNAUTHORIZED",
                StatusCode = 401,
                Data = null
            });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID do usuário inválido.",
                    ErrorCode = "INVALID_USER_ID",
                    StatusCode = 400,
                    Data = null
                });

            var user = await _utilizadorRepository.GetByIdAsync(userId);
            if (user == null) return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Utilizador não encontrado.",
                ErrorCode = "NOT_FOUND",
                StatusCode = 404,
                Data = null
            });

            await _utilizadorRepository.DeleteAsync(user);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Conta removida com sucesso.",
                StatusCode = 200,
                Data = null
            });
        }

        [HttpPost("admincreate")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<ActionResult<ApiResponse<Utilizador>>> AdminCreate(UserDto dto)
        {
            if (!ValidAccount(dto.Username, dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || RoleToDatabase(dto.Tipo) == null)
                return BadRequest(new { Message = "Valid account details and a supported role are required." });
            if (!User.IsInRole("Admin") && RoleToDatabase(dto.Tipo) != "USER")
                return Forbid();
            // Verificar duplicatas
            var usernameExists = await _utilizadorRepository.FindAsync(u => u.Username.ToLower() == dto.Username.ToLower());
            if (usernameExists.Any())
                return BadRequest(new ApiResponse<Utilizador>
                {
                    Success = false,
                    Message = "O nome de utilizador já está em uso.",
                    ErrorCode = "DUPLICATE_USERNAME",
                    StatusCode = 400,
                    Data = null
                });

            var emailExists = await _utilizadorRepository.FindAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (emailExists.Any())
                return BadRequest(new ApiResponse<Utilizador>
                {
                    Success = false,
                    Message = "O email já está em uso.",
                    ErrorCode = "DUPLICATE_EMAIL",
                    StatusCode = 400,
                    Data = null
                });

            var roleDb = RoleToDatabase(dto.Tipo)!;
            var tipoEntity = (await _tipoUtilizadorRepository.FindAsync(t => t.Tipo == roleDb)).FirstOrDefault();
            if (tipoEntity == null)
            {
                tipoEntity = new TipoUtilizador { Tipo = roleDb };
                await _tipoUtilizadorRepository.AddAsync(tipoEntity);
            }

            var user = new Utilizador
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = PasswordHelper.HashPassword(dto.Password),
                TipoUtilizadorId = tipoEntity.TipoUtilizadorId,
                DataCriacao = DateTime.UtcNow
            };

            await _utilizadorRepository.AddAsync(user);
            return Ok(new ApiResponse<Utilizador>
            {
                Success = true,
                Message = "Utilizador criado com sucesso.",
                StatusCode = 200,
                Data = user
            });
        }

        [HttpGet("adminget/{id:int}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<ActionResult<ApiResponse<UserDto>>> AdminGetUser(int id)
        {
            var user = await _utilizadorRepository.GetByIdWithDetailsAsync(id);
            if (user == null) return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Utilizador não encontrado.",
                ErrorCode = "NOT_FOUND",
                StatusCode = 404,
                Data = null
            });

            var roleDb = user.TipoUtilizador?.Tipo ?? "USER";
            var dto = new UserDto
            {
                UtilizadorId = user.UtilizadorId,
                Username = user.Username,
                Email = user.Email,
                Tipo = NormalizeRoleFromDb(roleDb)
            };
            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Utilizador obtido com sucesso.",
                StatusCode = 200,
                Data = dto
            });
        }

        [HttpPut("adminedit/{id:int}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> AdminEditUser(int id, UserDto dto)
        {
            var user = await _utilizadorRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Utilizador não encontrado.",
                ErrorCode = "NOT_FOUND",
                StatusCode = 404,
                Data = null
            });

            if (!ValidAccount(dto.Username, dto.Email) || RoleToDatabase(dto.Tipo) == null)
                return BadRequest(new { Message = "Valid account details and a supported role are required." });
            var currentRole = user.TipoUtilizadorId.HasValue
                ? await _tipoUtilizadorRepository.GetByIdAsync(user.TipoUtilizadorId.Value) : null;
            if (!User.IsInRole("Admin") && (RoleToDatabase(dto.Tipo) != "USER"
                || !string.Equals(currentRole?.Tipo, "USER", StringComparison.OrdinalIgnoreCase)))
                return Forbid();

            // Verificar duplicatas (exceto para o próprio utilizador)
            var usernameExists = await _utilizadorRepository.FindAsync(u => u.Username.ToLower() == dto.Username.ToLower() && u.UtilizadorId != id);
            if (usernameExists.Any())
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "O nome de utilizador já está em uso.",
                    ErrorCode = "DUPLICATE_USERNAME",
                    StatusCode = 400,
                    Data = null
                });

            var emailExists = await _utilizadorRepository.FindAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.UtilizadorId != id);
            if (emailExists.Any())
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "O email já está em uso.",
                    ErrorCode = "DUPLICATE_EMAIL",
                    StatusCode = 400,
                    Data = null
                });

            user.Username = dto.Username;
            user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                user.Password = PasswordHelper.HashPassword(dto.NewPassword);
            }

            var roleDb = RoleToDatabase(dto.Tipo)!;
            var tipoEntity = (await _tipoUtilizadorRepository.FindAsync(t => t.Tipo == roleDb)).FirstOrDefault();
            if (tipoEntity == null)
            {
                tipoEntity = new TipoUtilizador { Tipo = roleDb };
                await _tipoUtilizadorRepository.AddAsync(tipoEntity);
            }
            user.TipoUtilizadorId = tipoEntity.TipoUtilizadorId;

            await _utilizadorRepository.UpdateAsync(user);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Utilizador atualizado com sucesso.",
                StatusCode = 200,
                Data = null
            });
        }

        [HttpGet("ranking")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetRanking()
        {
            var ranking = await _utilizadorRepository.GetAllAsync();
            var rankingData = ranking
                .Select(u => new { u.UtilizadorId, u.Username, u.Pontos })
                .OrderByDescending(u => u.Pontos)
                .ToList();
            return Ok(new ApiResponse<IEnumerable<object>>
            {
                Success = true,
                Message = "Ranking obtido com sucesso.",
                StatusCode = 200,
                Data = rankingData
            });
        }

        private static bool ValidAccount(string? username, string? email) =>
            !string.IsNullOrWhiteSpace(username) && username.Length <= 50 && email is { Length: <= 100 }
            && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email);

        private static string? RoleToDatabase(string? role) => role?.ToUpperInvariant() switch
        {
            "ADMIN" => "ADMIN",
            "USERMANAGER" or "USER_MANAGER" => "USER_MANAGER",
            "USER" => "USER",
            _ => null
        };

        private string NormalizeRoleFromDb(string roleDb)
        {
            switch (roleDb.ToUpper())
            {
                case "ADMIN": return "Admin";
                case "USER_MANAGER":
                case "USERMANAGER": return "UserManager";
                default: return "User";
            }
        }
    }

    public class UserDto
    {
        public int UtilizadorId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Tipo { get; set; } = "User";
        public string? NewPassword { get; set; }
    }

    public class CheckAvailabilityResponse
    {
        public bool UsernameExists { get; set; }
        public bool EmailExists { get; set; }
    }
}
