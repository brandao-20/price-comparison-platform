using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Entities;
using WebAPI.Repositories;
using WebAPI.DTOs;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RelatoriosController : ControllerBase
    {
        private readonly ILojaRepository _lojaRepository;
        private readonly IRegistosPrecoRepository _registosPrecoRepository;
        private readonly IProdutoRepository _produtoRepository;
        private readonly AppDbContext _context;

        public RelatoriosController(
            ILojaRepository lojaRepository,
            IRegistosPrecoRepository registosPrecoRepository,
            IProdutoRepository produtoRepository,
            AppDbContext context)
        {
            _lojaRepository = lojaRepository;
            _registosPrecoRepository = registosPrecoRepository;
            _produtoRepository = produtoRepository;
            _context = context;
        }

        [HttpGet("lojas")]
        public async Task<ActionResult<IEnumerable<LojaReportDto>>> GetLojasReport()
        {
            try
            {
                var lojas = await _lojaRepository.GetAllWithDetailsAsync();

                var registos = await _registosPrecoRepository.GetAllWithDetailsAsync();

                var report = new List<LojaReportDto>();

                foreach (var loja in lojas)
                {

                    var produtosInfo = registos
                        .Where(r => r.LojaId == loja.LojaId && r.Produto != null)
                        .GroupBy(r => r.ProdutoId)
                        .Select(g => new ProdutoPriceDto
                        {
                            ProdutoId = g.Key,
                            ProdutoNome = g.First().Produto?.Nome ?? "N/A",
                            LatestPrice = g.OrderByDescending(r => r.DataRegisto).First().Preco,
                            LatestDate = g.OrderByDescending(r => r.DataRegisto).First().DataRegisto
                        })
                        .ToList();

                    var categoriaCounts = registos
                        .Where(r => r.LojaId == loja.LojaId && r.Produto != null && r.Produto.Categoria != null)
                        .GroupBy(r => r.Produto.CategoriaId)
                        .Select(g => new CategoriaCountDto
                        {
                            CategoriaId = g.Key,
                            CategoriaNome = g.First().Produto.Categoria?.Nome ?? "N/A",
                            Count = g.Select(r => r.ProdutoId).Distinct().Count()
                        })
                        .ToList();

                    report.Add(new LojaReportDto
                    {
                        LojaId = loja.LojaId,
                        Nome = loja.Nome,
                        Endereco = loja.Endereco,
                        Localizacao = new Localizacao
                        {
                            Cidade = loja.Localizacao?.Cidade ?? "N/A",
                            Pais = loja.Localizacao?.Pais ?? "Portugal",
                            Latitude = (double)(loja.Localizacao?.Latitude ?? 0),
                            Longitude = (double)(loja.Localizacao?.Longitude ?? 0)
                        },
                        CategoriaCounts = categoriaCounts,
                        Produtos = produtosInfo
                    });
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erro ao gerar relatório de lojas: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Message = "Erro ao gerar relatório de lojas." });
            }
        }

        [HttpGet("lojas/{lojaId:int}")]
        public async Task<ActionResult<LojaReportDto>> GetLojaReport(int lojaId)
        {
            try
            {
                var loja = await _lojaRepository.GetByIdWithDetailsAsync(lojaId);
                if (loja == null)
                {
                    return NotFound($"Loja com ID {lojaId} não encontrada.");
                }

                var registos = await _registosPrecoRepository.GetAllWithDetailsAsync();

                var produtosInfo = registos
                    .Where(r => r.LojaId == lojaId && r.Produto != null)
                    .GroupBy(r => r.ProdutoId)
                    .Select(g => new ProdutoPriceDto
                    {
                        ProdutoId = g.Key,
                        ProdutoNome = g.First().Produto?.Nome ?? "N/A",
                        LatestPrice = g.OrderByDescending(r => r.DataRegisto).First().Preco,
                        LatestDate = g.OrderByDescending(r => r.DataRegisto).First().DataRegisto
                    })
                    .ToList();

                var categoriaCounts = registos
                    .Where(r => r.LojaId == lojaId && r.Produto != null && r.Produto.Categoria != null)
                    .GroupBy(r => r.Produto.CategoriaId)
                    .Select(g => new CategoriaCountDto
                    {
                        CategoriaId = g.Key,
                        CategoriaNome = g.First().Produto.Categoria?.Nome ?? "N/A",
                        Count = g.Select(r => r.ProdutoId).Distinct().Count()
                    })
                    .ToList();

                var dto = new LojaReportDto
                {
                    LojaId = loja.LojaId,
                    Nome = loja.Nome,
                    Endereco = loja.Endereco,
                    Localizacao = new Localizacao
                    {
                        Cidade = loja.Localizacao?.Cidade ?? "N/A",
                        Pais = loja.Localizacao?.Pais ?? "Portugal",
                        Latitude = (double)(loja.Localizacao?.Latitude ?? 0),
                        Longitude = (double)(loja.Localizacao?.Longitude ?? 0)
                    },
                    CategoriaCounts = categoriaCounts,
                    Produtos = produtosInfo
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erro ao gerar relatório para LojaId {lojaId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Message = "Erro ao gerar relatório da loja." });
            }
        }

        [HttpGet("produtos")]
        public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetAllProdutos()
        {
            try
            {
                var produtos = await _produtoRepository.GetAllAsync();
                var produtoDtos = produtos.Select(p => new ProdutoDto
                {
                    ProdutoId = p.ProdutoId,
                    Nome = p.Nome
                }).ToList();
                return Ok(produtoDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erro ao listar produtos: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Message = "Erro ao listar produtos." });
            }
        }

        [HttpGet("produtos/{produtoId:int}")]
        public async Task<ActionResult<ProdutoReportDto>> GetProdutoReport(int produtoId)
        {
            try
            {
                var produto = await _produtoRepository.GetByIdWithDetailsAsync(produtoId);
                if (produto == null)
                {
                    return NotFound($"Produto com ID {produtoId} não encontrado.");
                }

                var registos = await _registosPrecoRepository.GetAllWithDetailsAsync();

                var lojasInfo = registos
                    .Where(r => r.ProdutoId == produtoId)
                    .GroupBy(r => r.LojaId)
                    .Select(g => new LojaPriceDto
                    {
                        LojaId = g.Key,
                        LojaNome = g.Select(r => r.Loja != null ? r.Loja.Nome : "N/A").FirstOrDefault(),
                        LatestPrice = g.OrderByDescending(r => r.DataRegisto).FirstOrDefault()?.Preco ?? 0,
                        LatestDate = g.OrderByDescending(r => r.DataRegisto).FirstOrDefault()?.DataRegisto ?? DateTime.MinValue
                    })
                    .ToList();

                var dto = new ProdutoReportDto
                {
                    ProdutoId = produto.ProdutoId,
                    Nome = produto.Nome,
                    Categoria = produto.Categoria?.Nome ?? "N/A",
                    Lojas = lojasInfo
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erro ao gerar relatório para ProdutoId {produtoId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Message = "Erro ao gerar relatório do produto." });
            }
        }

        [HttpGet("produtos/{produtoId}/pricehistory")]
        public async Task<IActionResult> GetPriceHistory(int produtoId, [FromQuery] bool groupByStore = false)
        {
            try
            {
                var produtoExists = await _context.Produtos.AnyAsync(p => p.ProdutoId == produtoId);
                if (!produtoExists)
                {
                    return NotFound(new ApiResponse<List<PriceHistoryDto>>
                    {
                        Success = false,
                        Message = $"Produto com ID {produtoId} não encontrado.",
                        StatusCode = 404
                    });
                }

                var registos = await _context.RegistosPrecos
                    .Where(r => r.ProdutoId == produtoId)
                    .Include(r => r.Loja)
                    .Select(r => new
                    {
                        DataRegisto = r.DataRegisto,
                        Preco = r.Preco,
                        LojaNome = r.Loja != null ? r.Loja.Nome : "N/A"
                    })
                    .ToListAsync();


                if (!registos.Any())
                {
                    return Ok(new ApiResponse<List<PriceHistoryDto>>
                    {
                        Success = true,
                        Data = new List<PriceHistoryDto>(),
                        StatusCode = 200
                    });
                }

                List<PriceHistoryDto> priceHistory;

                if (groupByStore)
                {
                    priceHistory = registos
                        .GroupBy(r => r.LojaNome)
                        .Select(g => new PriceHistoryDto
                        {
                            LojaNome = g.Key,
                            Prices = g
                                .GroupBy(r => r.DataRegisto.Date)
                                .Select(grp => new PricePoint
                                {
                                    Date = grp.Key,
                                    Price = grp.Average(r => r.Preco)
                                })
                                .OrderBy(p => p.Date)
                                .ToList()
                        })
                        .ToList();
                }
                else
                {
                    priceHistory = registos
                        .GroupBy(r => r.DataRegisto.Date)
                        .Select(g => new PriceHistoryDto
                        {
                            LojaNome = "Média",
                            Prices = new List<PricePoint>
                            {
                                new PricePoint
                                {
                                    Date = g.Key,
                                    Price = g.Average(r => r.Preco)
                                }
                            }
                        })
                        .OrderBy(h => h.Prices?.FirstOrDefault()?.Date ?? DateTime.MinValue)
                        .ToList();
                }

                return Ok(new ApiResponse<List<PriceHistoryDto>>
                {
                    Success = true,
                    Data = priceHistory,
                    StatusCode = 200
                });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"[ERROR] Erro no banco ao buscar histórico de preços para ProdutoId {produtoId}: {dbEx.Message}\n{dbEx.InnerException?.Message}\n{dbEx.StackTrace}");
                return StatusCode(500, new ApiResponse<List<PriceHistoryDto>>
                {
                    Success = false,
                    Message = "Erro ao buscar o histórico de preços devido a um problema no banco de dados.",
                    StatusCode = 500
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erro ao buscar histórico de preços para ProdutoId {produtoId}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ApiResponse<List<PriceHistoryDto>>
                {
                    Success = false,
                    Message = "Erro ao buscar o histórico de preços.",
                    StatusCode = 500
                });
            }
        }
    }

    // DTOs
    public class LojaReportDto
    {
        public int LojaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public Localizacao Localizacao { get; set; } = new Localizacao();
        public List<CategoriaCountDto> CategoriaCounts { get; set; } = new();
        public List<ProdutoPriceDto> Produtos { get; set; } = new();
    }

    public class Localizacao
    {
        public int LocalizacaoId { get; set; }
        public string? Cidade { get; set; }
        public string? Pais { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CategoriaCountDto
    {
        public int CategoriaId { get; set; }
        public string? CategoriaNome { get; set; }
        public int Count { get; set; }
    }

    public class ProdutoPriceDto
    {
        public int ProdutoId { get; set; }
        public string? ProdutoNome { get; set; }
        public decimal LatestPrice { get; set; }
        public DateTime LatestDate { get; set; }
    }

    public class ProdutoDto
    {
        public int ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class ProdutoReportDto
    {
        public int ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public List<LojaPriceDto> Lojas { get; set; } = new();
    }

    public class LojaPriceDto
    {
        public int LojaId { get; set; }
        public string? LojaNome { get; set; }
        public decimal LatestPrice { get; set; }
        public DateTime LatestDate { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public int StatusCode { get; set; }
        public T? Data { get; set; }
    }

    public class PriceHistoryDto
    {
        public string? LojaNome { get; set; }
        public List<PricePoint>? Prices { get; set; }
    }

    public class PricePoint
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }
}
