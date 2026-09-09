using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Entities;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TipoAcaosController : ControllerBase
    {
        private readonly ITipoAcaoRepository _tipoAcaoRepository;

        public TipoAcaosController(ITipoAcaoRepository tipoAcaoRepository)
        {
            _tipoAcaoRepository = tipoAcaoRepository;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TipoAcao>>>> GetAll()
        {
            try
            {
                var tipoAcaos = await _tipoAcaoRepository.GetAllAsync();
                return Ok(new ApiResponse<IEnumerable<TipoAcao>>
                {
                    Success = true,
                    Message = "Tipos de ação carregados com sucesso.",
                    StatusCode = 200,
                    Data = tipoAcaos
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<TipoAcaosController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<IEnumerable<TipoAcao>>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TipoAcao>>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<TipoAcao>
                    {
                        Success = false,
                        Message = "ID do tipo de ação inválido.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var tipoAcao = await _tipoAcaoRepository.GetByIdAsync(id);
                if (tipoAcao == null)
                {
                    return NotFound(new ApiResponse<TipoAcao>
                    {
                        Success = false,
                        Message = "Tipo de ação não encontrado.",
                        StatusCode = 404,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<TipoAcao>
                {
                    Success = true,
                    Message = "Tipo de ação encontrado com sucesso.",
                    StatusCode = 200,
                    Data = tipoAcao
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<TipoAcaosController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<TipoAcao>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<TipoAcao>>> Create(TipoAcao tipoAcao)
        {
            try
            {
                if (tipoAcao == null || string.IsNullOrEmpty(tipoAcao.Tipo))
                {
                    return BadRequest(new ApiResponse<TipoAcao>
                    {
                        Success = false,
                        Message = "Dados do tipo de ação inválidos.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                await _tipoAcaoRepository.AddAsync(tipoAcao);
                return CreatedAtAction(nameof(GetById), new { id = tipoAcao.TipoAcaoId }, new ApiResponse<TipoAcao>
                {
                    Success = true,
                    Message = "Tipo de ação criado com sucesso.",
                    StatusCode = 201,
                    Data = tipoAcao
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<TipoAcaosController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<TipoAcao>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, TipoAcao tipoAcao)
        {
            try
            {
                if (id <= 0 || id != tipoAcao.TipoAcaoId)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do tipo de ação inválido ou não corresponde ao objeto fornecido.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var existingTipoAcao = await _tipoAcaoRepository.GetByIdAsync(id);
                if (existingTipoAcao == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tipo de ação não encontrado.",
                        StatusCode = 404,
                        Data = null
                    });
                }

                await _tipoAcaoRepository.UpdateAsync(tipoAcao);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Tipo de ação atualizado com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Tipo de ação não encontrado.",
                    StatusCode = 404,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<TipoAcaosController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do tipo de ação inválido.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var tipoAcao = await _tipoAcaoRepository.GetByIdAsync(id);
                if (tipoAcao == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tipo de ação não encontrado.",
                        StatusCode = 404,
                        Data = null
                    });
                }

                await _tipoAcaoRepository.DeleteAsync(tipoAcao);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Tipo de ação excluído com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<TipoAcaosController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }
    }
}
