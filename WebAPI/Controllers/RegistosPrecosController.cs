using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Entities;
using WebAPI.Hubs;
using WebAPI.Repositories;
using WebAPI.Helpers;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistosPrecosController : ControllerBase
    {
        private readonly IRegistosPrecoRepository _registosPrecoRepository;
        private readonly IProdutoRepository _produtoRepository;
        private readonly ILojaRepository _lojaRepository;
        private readonly IUtilizadorRepository _utilizadorRepository;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<RegistosPrecosController> _logger;

        public RegistosPrecosController(
            IRegistosPrecoRepository registosPrecoRepository,
            IProdutoRepository produtoRepository,
            ILojaRepository lojaRepository,
            IUtilizadorRepository utilizadorRepository,
            IHubContext<ChatHub> hubContext,
            ILogger<RegistosPrecosController> logger)
        {
            _registosPrecoRepository = registosPrecoRepository;
            _produtoRepository = produtoRepository;
            _lojaRepository = lojaRepository;
            _utilizadorRepository = utilizadorRepository;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RegistoPrecoModel>>>> GetAll()
        {
            try
            {
                var registos = await _registosPrecoRepository.GetAllWithDetailsAsync();
                var modelos = registos.Select(r => new RegistoPrecoModel
                {
                    RegistoId = r.RegistoPrecoId,
                    ProdutoId = r.ProdutoId,
                    ProdutoNome = r.Produto?.Nome ?? "",
                    LojaId = r.LojaId,
                    LojaNome = r.Loja?.Nome ?? "",
                    Preco = r.Preco,
                    DataRegisto = r.DataRegisto
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<RegistoPrecoModel>>
                {
                    Success = true,
                    Message = "Registros de preços obtidos com sucesso.",
                    StatusCode = 200,
                    Data = modelos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao buscar todos os registros de preços: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<IEnumerable<RegistoPrecoModel>>
                {
                    Success = false,
                    Message = "Erro ao buscar os registros de preços.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("produto/{produtoId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RegistosPreco>>>> GetByProdutoId(int produtoId)
        {
            if (produtoId <= 0)
            {
                return BadRequest(new ApiResponse<IEnumerable<RegistosPreco>>
                {
                    Success = false,
                    Message = "ID do produto deve ser maior que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                var registos = await _registosPrecoRepository.GetByProdutoIdAsync(produtoId);
                if (registos == null || !registos.Any())
                {
                    return NotFound(new ApiResponse<IEnumerable<RegistosPreco>>
                    {
                        Success = false,
                        Message = "Nenhum registro de preço encontrado para este produto.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<IEnumerable<RegistosPreco>>
                {
                    Success = true,
                    Message = "Registros de preços obtidos com sucesso.",
                    StatusCode = 200,
                    Data = registos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao buscar registros de preços para o produto {produtoId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<IEnumerable<RegistosPreco>>
                {
                    Success = false,
                    Message = "Erro ao buscar os registros de preços.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RegistosPreco>>> GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<RegistosPreco>
                {
                    Success = false,
                    Message = "ID do registro deve ser maior que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                var registo = await _registosPrecoRepository.GetByIdWithDetailsAsync(id);
                if (registo == null)
                {
                    return NotFound(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = "Registro de preço não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<RegistosPreco>
                {
                    Success = true,
                    Message = "Registro de preço obtido com sucesso.",
                    StatusCode = 200,
                    Data = registo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao buscar registro de preço com ID {id}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<RegistosPreco>
                {
                    Success = false,
                    Message = "Erro ao buscar o registro de preço.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("{id}/credibility")]
        public async Task<ActionResult<ApiResponse<object>>> GetAdjustedCredibility(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID do registro deve ser maior que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                var registo = await _registosPrecoRepository.GetByIdWithDetailsAsync(id);
                if (registo == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Registro de preço não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                var meses = (DateTime.UtcNow - registo.DataRegisto).TotalDays / 30;
                var adjusted = registo.Credibilidade - (decimal)(0.1 * meses);
                if (adjusted < 0) adjusted = 0;

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Credibilidade ajustada calculada com sucesso.",
                    StatusCode = 200,
                    Data = new { Credibilidade = adjusted }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao calcular credibilidade para o registro {id}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao calcular a credibilidade.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("latest/{produtoId:int}/{lojaId:int}")]
        public async Task<ActionResult<ApiResponse<RegistosPreco>>> GetLatestPrice(int produtoId, int lojaId)
        {
            if (produtoId <= 0 || lojaId <= 0)
            {
                return BadRequest(new ApiResponse<RegistosPreco>
                {
                    Success = false,
                    Message = "IDs devem ser maiores que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                var latest = await _registosPrecoRepository.GetLatestPriceAsync(produtoId, lojaId);
                if (latest == null)
                {
                    return NotFound(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = "Nenhum preço registrado para este produto nesta loja.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<RegistosPreco>
                {
                    Success = true,
                    Message = "Último preço obtido com sucesso.",
                    StatusCode = 200,
                    Data = latest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao buscar último preço para ProdutoId {produtoId}, LojaId {lojaId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<RegistosPreco>
                {
                    Success = false,
                    Message = "Erro ao buscar o último preço.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RegistosPreco>>> Create(RegistosPreco registo)
        {
            if (registo.ProdutoId <= 0 || registo.LojaId <= 0)
            {
                return BadRequest(new ApiResponse<RegistosPreco>
                {
                    Success = false,
                    Message = "IDs devem ser maiores que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                if (registo.Preco <= 0)
                {
                    return BadRequest(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = "O preço deve ser maior que zero.",
                        ErrorCode = "INVALID_DATA",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool prodExists = await _produtoRepository.ExistsAsync(registo.ProdutoId);
                if (!prodExists)
                {
                    return BadRequest(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = $"Produto {registo.ProdutoId} não existe.",
                        ErrorCode = "INVALID_PRODUCT",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool storeExists = await _lojaRepository.ExistsAsync(registo.LojaId);
                if (!storeExists)
                {
                    return BadRequest(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = $"Loja {registo.LojaId} não existe.",
                        ErrorCode = "INVALID_STORE",
                        StatusCode = 400,
                        Data = null
                    });
                }

                if (registo.DataRegisto > DateTime.UtcNow)
                {
                    return BadRequest(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = "Data de registro não pode ser futura.",
                        ErrorCode = "INVALID_DATE",
                        StatusCode = 400,
                        Data = null
                    });
                }

                if (registo.DataRegisto == default)
                    registo.DataRegisto = DateTime.UtcNow;

                registo.Produto = null;
                registo.Loja = null;
                registo.TipoAcao = null;
                registo.Utilizador = null;
                registo.Comentarios = new();

                registo.Credibilidade = 1;
                var userIdClaim = User.FindFirst("utilizadorId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<RegistosPreco>
                    {
                        Success = false,
                        Message = "Usuário não identificado.",
                        ErrorCode = "UNAUTHORIZED",
                        StatusCode = 401,
                        Data = null
                    });
                }
                registo.UtilizadorId = userId;

                await _registosPrecoRepository.AddAsync(registo);

                // Incrementar pontos do utilizador
                var utilizador = await _utilizadorRepository.GetByIdAsync(userId);
                if (utilizador != null)
                {
                    utilizador.Pontos += 5; // +5 pontos por registo
                    await _utilizadorRepository.UpdateAsync(utilizador);
                }

                // Notificar utilizadores que favoritaram o produto
                await _hubContext.Clients.All.SendAsync("PriceChanged", registo.ProdutoId, registo.Preco);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = registo.RegistoPrecoId },
                    new ApiResponse<RegistosPreco>
                    {
                        Success = true,
                        Message = "Registro de preço criado com sucesso.",
                        StatusCode = 201,
                        Data = registo
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao criar registro de preço: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<RegistosPreco>
                {
                    Success = false,
                    Message = "Erro ao criar o registro de preço.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, RegistosPreco registo)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID do registro deve ser maior que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                if (id != registo.RegistoPrecoId)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do registro não coincide.",
                        ErrorCode = "INVALID_ID",
                        StatusCode = 400,
                        Data = null
                    });
                }

                if (registo.DataRegisto > DateTime.UtcNow)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Data de registro não pode ser futura.",
                        ErrorCode = "INVALID_DATE",
                        StatusCode = 400,
                        Data = null
                    });
                }

                if (registo.Preco <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "O preço deve ser maior que zero.",
                        ErrorCode = "INVALID_DATA",
                        StatusCode = 400,
                        Data = null
                    });
                }

                if (registo.ProdutoId <= 0 || registo.LojaId <= 0 || registo.TipoAcaoId <= 0 || registo.UtilizadorId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "IDs devem ser maiores que zero.",
                        ErrorCode = "INVALID_DATA",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool prodExists = await _produtoRepository.ExistsAsync(registo.ProdutoId);
                if (!prodExists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Produto {registo.ProdutoId} não existe.",
                        ErrorCode = "INVALID_PRODUCT",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool storeExists = await _lojaRepository.ExistsAsync(registo.LojaId);
                if (!storeExists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Loja {registo.LojaId} não existe.",
                        ErrorCode = "INVALID_STORE",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool userExists = await _utilizadorRepository.ExistsAsync(registo.UtilizadorId);
                if (!userExists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Utilizador {registo.UtilizadorId} não existe.",
                        ErrorCode = "INVALID_USER",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var existingRegisto = await _registosPrecoRepository.GetByIdAsync(id);
                if (existingRegisto == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Registro de preço não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                if (User.FindFirst("utilizadorId")?.Value != existingRegisto.UtilizadorId.ToString()
                    && !User.IsInRole("Admin") && !User.IsInRole("UserManager")) return Forbid();
                // Update the tracked record; ownership and credibility cannot be supplied by the client.
                existingRegisto.Preco = registo.Preco;
                existingRegisto.DataRegisto = registo.DataRegisto;
                existingRegisto.ProdutoId = registo.ProdutoId;
                existingRegisto.LojaId = registo.LojaId;
                existingRegisto.TipoAcaoId = registo.TipoAcaoId;

                await _registosPrecoRepository.UpdateAsync(existingRegisto);

                await _hubContext.Clients.All.SendAsync("PriceChanged", registo.ProdutoId, registo.Preco);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Registro de preço atualizado com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao atualizar registro de preço com ID {id}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao atualizar o registro de preço.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID do registro deve ser maior que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                var registo = await _registosPrecoRepository.GetByIdAsync(id);
                if (registo == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Registro de preço não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                if (User.FindFirst("utilizadorId")?.Value != registo.UtilizadorId.ToString()
                    && !User.IsInRole("Admin") && !User.IsInRole("UserManager")) return Forbid();
                await _registosPrecoRepository.DeleteAsync(registo);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Registro de preço removido com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao deletar registro de preço com ID {id}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao deletar o registro de preço.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPost("confirm/{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ConfirmPrice(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID do registro deve ser maior que zero.",
                    ErrorCode = "INVALID_ID",
                    StatusCode = 400,
                    Data = null
                });
            }

            try
            {
                var registo = await _registosPrecoRepository.GetByIdAsync(id);
                if (registo == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Registro de preço não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                var userIdClaim = User.FindFirst("utilizadorId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Usuário não identificado.",
                        ErrorCode = "UNAUTHORIZED",
                        StatusCode = 401,
                        Data = null
                    });
                }
                registo.Credibilidade = Math.Min(registo.Credibilidade + 1, 10); // Ajustado para +1, limite 10
                registo.DataRegisto = DateTime.UtcNow;
                await _registosPrecoRepository.UpdateAsync(registo);

                // Incrementar pontos do utilizador
                var utilizador = await _utilizadorRepository.GetByIdAsync(userId);
                if (utilizador != null)
                {
                    utilizador.Pontos += 2; // +2 pontos por confirmação
                    await _utilizadorRepository.UpdateAsync(utilizador);
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Preço confirmado com sucesso.",
                    StatusCode = 200,
                    Data = new { Credibilidade = registo.Credibilidade }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Erro ao confirmar preço para o registro {id}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erro ao confirmar o preço.",
                    ErrorCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }
    }

    public class RegistoPrecoModel
    {
        public int RegistoId { get; set; }
        public int ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = "";
        public int LojaId { get; set; }
        public string LojaNome { get; set; } = "";
        public decimal Preco { get; set; }
        public DateTime DataRegisto { get; set; }
    }
}
