using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.DTOs;
using WebAPI.Entities;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrecosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PrecosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("produtos")]
        public async Task<ActionResult<List<Produto>>> GetProdutos()
        {
            var produtos = await _context.Produtos
                .Select(p => new Produto
                {
                    ProdutoId = p.ProdutoId,
                    Nome = p.Nome,
                    Marca = p.Marca
                })
                .OrderBy(p => p.Nome)
                .ToListAsync();

            return Ok(produtos);
        }

        [HttpGet("comparar/{produtoId}")]
        public async Task<ActionResult<PrecoComparacaoDTO>> CompararPrecos(int produtoId, [FromQuery] int? lojaId1 = null, [FromQuery] int? lojaId2 = null)
        {

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.ProdutoId == produtoId);

            if (produto == null)
            {
                return NotFound("Produto não encontrado.");
            }

            var lojasDisponiveisQuery = _context.RegistosPrecos
                .Where(r => r.ProdutoId == produtoId)
                .Include(r => r.Loja) // Carregar Loja antes de qualquer projeção
                .Select(r => r.Loja)
                .Distinct();

            var lojasDisponiveis = await lojasDisponiveisQuery
                .Select(l => new LojaDTO
                {
                    LojaId = l.LojaId,
                    Nome = l.Nome
                })
                .ToListAsync();


            var query = _context.RegistosPrecos
                .Where(r => r.ProdutoId == produtoId);

            if (lojaId1.HasValue && lojaId2.HasValue)
            {
                query = query.Where(r => r.LojaId == lojaId1.Value || r.LojaId == lojaId2.Value);
            }

            query = query.Include(r => r.Loja); // Carregar Loja depois de todos os filtros

            var registos = await query.ToListAsync();

            var precosAtuais = registos
                .GroupBy(r => r.LojaId)
                .Select(g => g.OrderByDescending(r => r.DataRegisto).FirstOrDefault())
                .Select(r => new PrecoAtualDTO
                {
                    NomeLoja = r.Loja.Nome,
                    Preco = r.Preco,
                    DataRegisto = r.DataRegisto
                })
                .ToList();


            var historicoPrecos = registos
                .Select(r => new HistoricoPrecoDTO
                {
                    NomeLoja = r.Loja.Nome,
                    Preco = r.Preco,
                    DataRegisto = r.DataRegisto
                })
                .OrderBy(r => r.DataRegisto)
                .ToList();


            var resultado = new PrecoComparacaoDTO
            {
                NomeProduto = produto.Nome,
                LojasDisponiveis = lojasDisponiveis,
                PrecosAtuais = precosAtuais,
                HistoricoPrecos = historicoPrecos
            };

            return Ok(resultado);
        }
    }
}
