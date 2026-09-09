using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Entities;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalizacoesController : ControllerBase
    {
        private readonly ILocalizacaoRepository _localizacaoRepository;

        public LocalizacoesController(ILocalizacaoRepository localizacaoRepository)
        {
            _localizacaoRepository = localizacaoRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Localizacao>>> GetAll()
        {
            return Ok(await _localizacaoRepository.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Localizacao>> GetById(int id)
        {
            var localizacao = await _localizacaoRepository.GetByIdAsync(id);
            if (localizacao == null) return NotFound();
            return Ok(localizacao); // Encapsulado como ActionResult
        }

        [HttpPost]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<ActionResult<WebAPI.Entities.Localizacao>> Create([FromBody] WebAPI.Entities.Localizacao localizacao)
        {
            await _localizacaoRepository.AddAsync(localizacao);
            return CreatedAtAction(nameof(GetById), new { id = localizacao.LocalizacaoId }, localizacao);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> Update(int id, [FromBody] WebAPI.Entities.Localizacao localizacao)
        {
            if (id != localizacao.LocalizacaoId) return BadRequest();

            try
            {
                await _localizacaoRepository.UpdateAsync(localizacao);
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
            var localizacao = await _localizacaoRepository.GetByIdAsync(id);
            if (localizacao == null) return NotFound();

            await _localizacaoRepository.DeleteAsync(localizacao);
            return NoContent();
        }
    }
}
