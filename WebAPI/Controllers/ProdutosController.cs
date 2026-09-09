using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using WebAPI.Entities;
using WebAPI.Repositories;
using WebAPI.Extensions;
using WebAPI.Context;
using WebAPI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IRegistosPrecoRepository _registosPrecoRepository;
        private readonly ILogger<ProdutosController> _logger;
        private readonly AppDbContext _context; // Adicionado para acessar Favoritos

        public ProdutosController(
            IProdutoRepository produtoRepository,
            ICategoriaRepository categoriaRepository,
            IRegistosPrecoRepository registosPrecoRepository,
            ILogger<ProdutosController> logger,
            AppDbContext context) // Injetar o contexto
        {
            _produtoRepository = produtoRepository;
            _categoriaRepository = categoriaRepository;
            _registosPrecoRepository = registosPrecoRepository;
            _logger = logger;
            _context = context;
        }

        // GET: api/Produtos?page=1&pageSize=5
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<Produto>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 5;


                var totalItems = await _produtoRepository.CountAsync();
                var skip = (page - 1) * pageSize;
                var produtos = await _produtoRepository.GetPagedWithDetailsAsync(skip, pageSize);

                Response.Headers["X-Total-Count"] = totalItems.ToString();
                return Ok(new ApiResponse<IEnumerable<Produto>>
                {
                    Success = true,
                    Message = "Produtos obtidos com sucesso.",
                    StatusCode = 200,
                    Data = produtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao obter produtos - Página: {Page}, Tamanho da página: {PageSize}", page, pageSize);
                return StatusCode(500, new ApiResponse<IEnumerable<Produto>>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Produto>>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<Produto>
                    {
                        Success = false,
                        Message = "ID do produto deve ser maior que zero.",
                        ErrorCode = "INVALID_ID",
                        StatusCode = 400,
                        Data = null
                    });
                }


                var produto = await _produtoRepository.GetByIdWithDetailsAsync(id);
                if (produto == null)
                {
                    return NotFound(new ApiResponse<Produto>
                    {
                        Success = false,
                        Message = "Produto não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<Produto>
                {
                    Success = true,
                    Message = "Produto obtido com sucesso.",
                    StatusCode = 200,
                    Data = produto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao obter produto com ID: {Id}", id);
                return StatusCode(500, new ApiResponse<Produto>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("{id}/credibilidade")]
        public async Task<ActionResult<ApiResponse<object>>> GetAdjustedCredibility(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do produto deve ser maior que zero.",
                        ErrorCode = "INVALID_ID",
                        StatusCode = 400,
                        Data = null
                    });
                }


                var produto = await _produtoRepository.GetByIdAsync(id);
                if (produto == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Produto não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                var registos = await _registosPrecoRepository.GetByProdutoIdAsync(id);
                if (registos == null || !registos.Any())
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Nenhum registo de preço encontrado.",
                        StatusCode = 200,
                        Data = new { Credibilidade = 0.0 }
                    });
                }

                double totalCredibilidade = 0;
                int count = 0;

                foreach (var registo in registos)
                {
                    var meses = (DateTime.UtcNow - registo.DataRegisto).TotalDays / 30;
                    var adjusted = (double)registo.Credibilidade - (0.1 * meses);
                    if (adjusted < 0) adjusted = 0;

                    totalCredibilidade += adjusted;
                    count++;
                }

                var credibilidadeMedia = count > 0 ? totalCredibilidade / count : 0;
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Credibilidade ajustada calculada com sucesso.",
                    StatusCode = 200,
                    Data = new { Credibilidade = credibilidadeMedia }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao calcular credibilidade ajustada para o produto com ID: {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "UserManager,Admin")]
        public async Task<ActionResult<ApiResponse<Produto>>> Create(Produto produto)
        {
            try
            {

                if (string.IsNullOrEmpty(produto.Nome) || string.IsNullOrWhiteSpace(produto.Marca))
                {
                    return BadRequest(new ApiResponse<Produto>
                    {
                        Success = false,
                        Message = "Nome e Marca são obrigatórios.",
                        ErrorCode = "INVALID_DATA",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool categoryExists = await _categoriaRepository.ExistsAsync(produto.CategoriaId);
                if (!categoryExists)
                {
                    return BadRequest(new ApiResponse<Produto>
                    {
                        Success = false,
                        Message = $"A categoria {produto.CategoriaId} não existe.",
                        ErrorCode = "INVALID_CATEGORY",
                        StatusCode = 400,
                        Data = null
                    });
                }

                // Product writes must not persist client-supplied account or category graphs.
                produto.Categoria = null;
                produto.Favoritos = new();
                await _produtoRepository.AddAsync(produto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = produto.ProdutoId },
                    new ApiResponse<Produto>
                    {
                        Success = true,
                        Message = "Produto criado com sucesso.",
                        StatusCode = 201,
                        Data = produto
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao criar produto: Nome={Nome}, Marca={Marca}, CategoriaId={CategoriaId}", produto.Nome, produto.Marca, produto.CategoriaId);
                return StatusCode(500, new ApiResponse<Produto>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "UserManager,Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, Produto produto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do produto deve ser maior que zero.",
                        ErrorCode = "INVALID_ID",
                        StatusCode = 400,
                        Data = null
                    });
                }


                if (id != produto.ProdutoId)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do produto não coincide.",
                        ErrorCode = "INVALID_ID",
                        StatusCode = 400,
                        Data = null
                    });
                }

                bool categoryExists = await _categoriaRepository.ExistsAsync(produto.CategoriaId);
                if (!categoryExists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"A categoria {produto.CategoriaId} não existe.",
                        ErrorCode = "INVALID_CATEGORY",
                        StatusCode = 400,
                        Data = null
                    });
                }

                produto.Categoria = null;
                produto.Favoritos = new();
                await _produtoRepository.UpdateAsync(produto);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Produto atualizado com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Produto não encontrado.",
                    ErrorCode = "NOT_FOUND",
                    StatusCode = 404,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao atualizar produto com ID: {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "UserManager,Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID do produto deve ser maior que zero.",
                        ErrorCode = "INVALID_ID",
                        StatusCode = 400,
                        Data = null
                    });
                }


                var produto = await _produtoRepository.GetByIdAsync(id);
                if (produto == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Produto não encontrado.",
                        ErrorCode = "NOT_FOUND",
                        StatusCode = 404,
                        Data = null
                    });
                }

                await _produtoRepository.DeleteAsync(produto);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Produto removido com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao deletar produto com ID: {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<Produto>>>> Search(
            [FromQuery] string? nome,
            [FromQuery] int? categoriaId,
            [FromQuery] string? store,
            [FromQuery] DateTime? dateFrom)
        {
            try
            {

                Expression<Func<Produto, bool>> predicate = p => true;

                if (!string.IsNullOrWhiteSpace(nome))
                {
                    string nomeLower = nome.ToLower();
                    predicate = p => p.Nome.ToLower().Contains(nomeLower);
                }

                if (categoriaId.HasValue)
                {
                    Expression<Func<Produto, bool>> categoriaPredicate = p => p.CategoriaId == categoriaId.Value;
                    predicate = predicate.And(categoriaPredicate);
                }

                if (!string.IsNullOrWhiteSpace(store))
                {
                    Expression<Func<Produto, bool>> storePredicate = p =>
                        p.RegistosPrecos.Any(rp => rp.Loja != null && rp.Loja.Nome.ToLower().Contains(store.ToLower()));
                    predicate = predicate.And(storePredicate);
                }

                if (dateFrom.HasValue)
                {
                    Expression<Func<Produto, bool>> datePredicate = p =>
                        p.RegistosPrecos.Any(rp => rp.DataRegisto >= dateFrom.Value);
                    predicate = predicate.And(datePredicate);
                }

                var produtos = await _produtoRepository.FindWithDetailsAsync(predicate);
                return Ok(new ApiResponse<IEnumerable<Produto>>
                {
                    Success = true,
                    Message = "Pesquisa realizada com sucesso.",
                    StatusCode = 200,
                    Data = produtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao pesquisar produtos - Nome: {Nome}, CategoriaId: {CategoriaId}, Store: {Store}, DateFrom: {DateFrom}", nome, categoriaId, store, dateFrom);
                return StatusCode(500, new ApiResponse<IEnumerable<Produto>>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("favorites/{userId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<IEnumerable<Produto>>>> GetFavorites(int userId)
        {
            if (User.FindFirst("utilizadorId")?.Value != userId.ToString()) return Forbid();
            try
            {

                // Buscar os IDs dos produtos favoritos do usuário
                var favoriteProductIds = await _context.Favoritos
                    .Where(f => f.UtilizadorId == userId) // Corrigido de UserId para UtilizadorId
                    .Select(f => f.ProdutoId)
                    .ToListAsync();

                if (!favoriteProductIds.Any())
                {
                    return Ok(new ApiResponse<IEnumerable<Produto>>
                    {
                        Success = true,
                        Message = "Nenhum produto favorito encontrado.",
                        StatusCode = 200,
                        Data = new List<Produto>()
                    });
                }

                // Buscar os produtos correspondentes aos IDs favoritos
                var produtos = await _produtoRepository.FindWithDetailsAsync(p => favoriteProductIds.Contains(p.ProdutoId));

                return Ok(new ApiResponse<IEnumerable<Produto>>
                {
                    Success = true,
                    Message = "Produtos favoritos obtidos com sucesso.",
                    StatusCode = 200,
                    Data = produtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Erro ao buscar produtos favoritos para o usuário {UserId}", userId);
                return StatusCode(500, new ApiResponse<IEnumerable<Produto>>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Data = null
                });
            }
        }
    }
}
