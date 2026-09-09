using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Entities;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MensagensController : ControllerBase
    {
        private readonly IMensagemRepository _mensagemRepository;

        public MensagensController(IMensagemRepository mensagemRepository)
        {
            _mensagemRepository = mensagemRepository;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Mensagem>>> GetMessagesByUser(int userId)
        {
            if (!int.TryParse(User.FindFirst("utilizadorId")?.Value, out var currentUserId)) return Unauthorized();
            if (currentUserId != userId && !User.IsInRole("Admin")) return Forbid();
            var mensagens = await _mensagemRepository.GetByUserIdAsync(userId);
            return Ok(mensagens);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Mensagem>> GetMessage(int id)
        {
            var mensagem = await _mensagemRepository.GetByIdAsync(id);
            if (!int.TryParse(User.FindFirst("utilizadorId")?.Value, out var currentUserId)) return Unauthorized();
            if (mensagem.RemetenteId != currentUserId && mensagem.DestinatarioId != currentUserId && !User.IsInRole("Admin")) return Forbid();
            return Ok(mensagem);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Mensagem>>> GetAllMessages()
        {
            var mensagens = await _mensagemRepository.GetAllWithDetailsAsync();
            return Ok(mensagens);
        }

        [HttpPost]
        public async Task<ActionResult<Mensagem>> CreateMessage(Mensagem mensagem)
        {
            if (!int.TryParse(User.FindFirst("utilizadorId")?.Value, out var currentUserId) || currentUserId <= 0) return Unauthorized();
            if (mensagem.DestinatarioId <= 0 || string.IsNullOrWhiteSpace(mensagem.Conteudo) || mensagem.Conteudo.Length > 1000)
                return BadRequest(new { Message = "A recipient and a message of up to 1000 characters are required." });
            mensagem = new Mensagem
            {
                RemetenteId = currentUserId,
                DestinatarioId = mensagem.DestinatarioId,
                Conteudo = mensagem.Conteudo,
                DataEnvio = DateTime.UtcNow
            };
            await _mensagemRepository.AddAsync(mensagem);
            return CreatedAtAction(nameof(GetMessage), new { id = mensagem.MensagemId }, mensagem);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var mensagem = await _mensagemRepository.GetByIdAsync(id);
            await _mensagemRepository.DeleteAsync(mensagem);
            return NoContent();
        }
    }
}
