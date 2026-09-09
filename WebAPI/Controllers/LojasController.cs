using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Entities;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LojasController : ControllerBase
    {
        private readonly ILojaRepository _lojaRepository;

        public LojasController(ILojaRepository lojaRepository)
        {
            _lojaRepository = lojaRepository;
        }

        // GET: api/Lojas?page=1&pageSize=5
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Loja>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;

            var totalItems = await _lojaRepository.CountAsync();
            var skip = (page - 1) * pageSize;
            var lojas = await _lojaRepository.GetPagedWithDetailsAsync(skip, pageSize);

            Response.Headers["X-Total-Count"] = totalItems.ToString();
            return Ok(lojas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Loja>> GetById(int id)
        {
            var loja = await _lojaRepository.GetByIdWithDetailsAsync(id);
            if (loja == null) return NotFound();
            return loja;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<ActionResult<Loja>> Create(Loja loja)
        {
            // Verifica se a loja já existe com base em PlaceId, nome/endereço ou coordenadas
            var existingLojas = await _lojaRepository.GetAllWithDetailsAsync();
            var existingLoja = existingLojas.FirstOrDefault(s =>
                // Verifica pelo PlaceId (se disponível)
                (!string.IsNullOrEmpty(s.PlaceId) && !string.IsNullOrEmpty(loja.PlaceId) && s.PlaceId.Equals(loja.PlaceId, StringComparison.OrdinalIgnoreCase)) ||
                // Verifica por nome e endereço
                (s.Nome.Equals(loja.Nome, StringComparison.OrdinalIgnoreCase) &&
                 s.Endereco.Equals(loja.Endereco, StringComparison.OrdinalIgnoreCase)) ||
                // Verifica por coordenadas (latitude e longitude)
                (s.Localizacao != null && loja.Localizacao != null &&
                 Math.Abs((s.Localizacao.Latitude ?? 0m) - (loja.Localizacao.Latitude ?? 0m)) < 0.0001m &&
                 Math.Abs((s.Localizacao.Longitude ?? 0m) - (loja.Localizacao.Longitude ?? 0m)) < 0.0001m)
            );

            if (existingLoja != null)
            {
                return Conflict(new { message = "Esta loja já existe no sistema.", existingLoja.LojaId });
            }

            if (loja.Localizacao != null)
            {
                if (string.IsNullOrWhiteSpace(loja.Localizacao.GoogleMapsUrl) &&
                    loja.Localizacao.Latitude.HasValue &&
                    loja.Localizacao.Longitude.HasValue)
                {
                    loja.Localizacao.GoogleMapsUrl =
                        $"https://maps.google.com/?q={loja.Localizacao.Latitude},{loja.Localizacao.Longitude}";
                }
            }

            await _lojaRepository.AddAsync(loja);
            return CreatedAtAction(nameof(GetById), new { id = loja.LojaId }, loja);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> Update(int id, Loja loja)
        {
            if (id != loja.LojaId) return BadRequest();

            if (loja.Localizacao != null)
            {
                if (string.IsNullOrWhiteSpace(loja.Localizacao.GoogleMapsUrl) &&
                    loja.Localizacao.Latitude.HasValue &&
                    loja.Localizacao.Longitude.HasValue)
                {
                    loja.Localizacao.GoogleMapsUrl =
                        $"https://maps.google.com/?q={loja.Localizacao.Latitude},{loja.Localizacao.Longitude}";
                }
            }

            try
            {
                await _lojaRepository.UpdateAsync(loja);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var loja = await _lojaRepository.GetByIdAsync(id);
            if (loja == null) return NotFound();

            await _lojaRepository.DeleteAsync(loja);
            return NoContent();
        }
    }
}
