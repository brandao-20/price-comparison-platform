using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Entities;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TipoUtilizadoresController : ControllerBase
    {
        private readonly ITipoUtilizadorRepository _tipoUtilizadorRepository;

        public TipoUtilizadoresController(ITipoUtilizadorRepository tipoUtilizadorRepository)
        {
            _tipoUtilizadorRepository = tipoUtilizadorRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TipoUtilizador>>> GetAll()
        {
            return Ok(await _tipoUtilizadorRepository.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TipoUtilizador>> GetById(int id)
        {
            var tipo = await _tipoUtilizadorRepository.GetByIdAsync(id);
            if (tipo == null) return NotFound();
            return tipo;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TipoUtilizador>> Create(TipoUtilizador tipo)
        {
            await _tipoUtilizadorRepository.AddAsync(tipo);
            return CreatedAtAction(nameof(GetById), new { id = tipo.TipoUtilizadorId }, tipo);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, TipoUtilizador tipo)
        {
            if (id != tipo.TipoUtilizadorId) return BadRequest();

            try
            {
                await _tipoUtilizadorRepository.UpdateAsync(tipo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await _tipoUtilizadorRepository.GetByIdAsync(id);
            if (tipo == null) return NotFound();

            await _tipoUtilizadorRepository.DeleteAsync(tipo);
            return NoContent();
        }
    }
}
