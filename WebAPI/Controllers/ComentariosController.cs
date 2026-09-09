using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Context;
using WebAPI.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComentariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ComentariosController> _logger;

        public ComentariosController(AppDbContext context, ILogger<ComentariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("registo/{registoId}")]
        public async Task<IActionResult> GetByRegistoId(int registoId)
        {
            try
            {
                var comentarios = await _context.Comentarios
                    .Where(c => c.RegistoPrecoId == registoId)
                    .Include(c => c.Utilizador)
                    .Select(c => new
                    {
                        c.ComentarioId,
                        c.RegistoPrecoId,
                        c.UtilizadorId,
                        c.Conteudo,
                        c.DataCriacao,
                        Utilizador = new
                        {
                            Username = c.Utilizador != null ? c.Utilizador.Username : "Anônimo",
                            Email = c.Utilizador != null ? c.Utilizador.Email : null
                        }
                    })
                    .ToListAsync();

                return Ok(new { Success = true, Data = comentarios });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao buscar comentários para RegistoPrecoId {registoId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Success = false, Message = "Erro ao buscar comentários." });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] Comentario comentario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comentario.Conteudo))
                {
                    return BadRequest(new { Success = false, Message = "O conteúdo do comentário é obrigatório." });
                }

                var userIdClaim = User.FindFirst("utilizadorId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { Success = false, Message = "Usuário não identificado." });
                }
                comentario.UtilizadorId = userId;
                comentario.DataCriacao = DateTime.UtcNow;

                var registoExists = await _context.RegistosPrecos.AnyAsync(r => r.RegistoPrecoId == comentario.RegistoPrecoId);
                if (!registoExists)
                {
                    return BadRequest(new { Success = false, Message = "Registo de preço não encontrado." });
                }

                comentario.RegistoPreco = null; // Garantir que não tentamos salvar o objeto RegistoPreco
                comentario.Utilizador = null; // Garantir que não tentamos salvar o objeto Utilizador
                _context.Comentarios.Add(comentario);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetByRegistoId), new { registoId = comentario.RegistoPrecoId },
                    new { Success = true, Message = "Comentário adicionado com sucesso.", Data = comentario });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError($"[ERROR] Erro ao salvar comentário no banco (DbUpdateException): {dbEx.Message}\n{dbEx.StackTrace}\nInnerException: {dbEx.InnerException?.Message}");
                return StatusCode(500, new { Success = false, Message = "Erro ao adicionar comentário devido a um problema no banco de dados." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao criar comentário: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Success = false, Message = "Erro ao adicionar comentário." });
            }
        }
    }
}
