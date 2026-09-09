using WebAPI.ExportStrategies;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebAPI.Repositories;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly ILojaRepository _lojaRepository;
        private readonly IProdutoRepository _produtoRepository;
        private readonly IRegistosPrecoRepository _registosPrecoRepository;

        public ExportController(
            ILojaRepository lojaRepository,
            IProdutoRepository produtoRepository,
            IRegistosPrecoRepository registosPrecoRepository)
        {
            _lojaRepository = lojaRepository;
            _produtoRepository = produtoRepository;
            _registosPrecoRepository = registosPrecoRepository;
        }

        // Exporta Relatório Geral de Lojas em CSV
        // Endpoint: GET /api/Export/lojas/csv
        [HttpGet("lojas/csv")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> ExportLojasCsv()
        {
            var lojas = await _lojaRepository.GetAllWithDetailsAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Loja;Endereco;Cidade;Pais");

            foreach (var loja in lojas)
            {
                var cidade = loja.Localizacao?.Cidade ?? "";
                var pais = loja.Localizacao?.Pais ?? "";
                sb.AppendLine(string.Join(";", new[] { loja.Nome, loja.Endereco, cidade, pais }.Select(value => CsvField.Escape(value, ';'))));
            }

            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(csvBytes, "text/csv", "relatorio_lojas.csv");
        }

        // Exporta Relatório Geral de Lojas em PDF
        // Endpoint: GET /api/Export/lojas/pdf
        [HttpGet("lojas/pdf")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> ExportLojasPdf()
        {
            var lojas = await _lojaRepository.GetAllWithDetailsAsync();

            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            document.Add(new Paragraph("Relatório de Lojas")
                .SetFontSize(16)
                .SetBold());
            document.Add(new Paragraph(" "));

            foreach (var loja in lojas)
            {
                document.Add(new Paragraph($"Loja: {loja.Nome}")
                    .SetFontSize(12)
                    .SetBold());
                document.Add(new Paragraph($"Endereço: {loja.Endereco}"));
                document.Add(new Paragraph($"Localização: {loja.Localizacao?.Cidade ?? "N/A"}, {loja.Localizacao?.Pais ?? "N/A"}"));
                document.Add(new Paragraph(" "));
            }

            document.Close();
            var pdfBytes = memoryStream.ToArray();
            return File(pdfBytes, "application/pdf", "relatorio_lojas.pdf");
        }

        // Exporta Relatório Geral de Produtos em PDF
        // Endpoint: GET /api/Export/produtos/pdf
        [HttpGet("produtos/pdf")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> ExportProdutosPdf()
        {
            var produtos = await _produtoRepository.GetAllWithDetailsAsync();

            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            document.Add(new Paragraph("Relatório de Produtos")
                .SetFontSize(16)
                .SetBold());
            document.Add(new Paragraph(" "));

            foreach (var p in produtos)
            {
                document.Add(new Paragraph($"Produto: {p.Nome}").SetBold());
                document.Add(new Paragraph($"Marca: {p.Marca}"));
                document.Add(new Paragraph($"Categoria: {p.Categoria?.Nome ?? "N/A"}"));
                document.Add(new Paragraph(" "));
            }

            document.Close();
            var pdfBytes = memoryStream.ToArray();
            return File(pdfBytes, "application/pdf", "relatorio_produtos.pdf");
        }

        // Exporta Relatório Geral de Produtos em CSV
        // Endpoint: GET /api/Export/produtos/csv
        [HttpGet("produtos/csv")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> ExportProdutosCsv()
        {
            var produtos = await _produtoRepository.GetAllWithDetailsAsync();
            var sb = new StringBuilder();
            sb.AppendLine("ProdutoId;Nome;Marca;Descricao;Categoria");

            foreach (var p in produtos)
            {
                sb.AppendLine(string.Join(";", new[] { p.ProdutoId.ToString(), p.Nome, p.Marca, p.Descricao, p.Categoria?.Nome ?? "N/A" }.Select(value => CsvField.Escape(value, ';'))));
            }

            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(csvBytes, "text/csv", "relatorio_produtos.csv");
        }

        // Exporta Relatório Específico de Produto em PDF
        // Endpoint: GET /api/Export/produtos/{produtoId}/pdf
        [HttpGet("produtos/{produtoId:int}/pdf")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> ExportProdutoPdf(int produtoId)
        {
            var produto = await _produtoRepository.GetByIdWithDetailsAsync(produtoId);
            if (produto == null)
            {
                return NotFound(new { Message = "Produto não encontrado." });
            }

            var registos = await _registosPrecoRepository.GetAllWithDetailsAsync();
            var lojasInfo = registos
                .Where(r => r.ProdutoId == produtoId)
                .GroupBy(r => r.LojaId)
                .Select(g => new
                {
                    LojaNome = g.Select(r => r.Loja != null ? r.Loja.Nome : "N/A").FirstOrDefault(),
                    LatestPrice = g.OrderByDescending(r => r.DataRegisto).FirstOrDefault()?.Preco ?? 0,
                    LatestDate = g.OrderByDescending(r => r.DataRegisto).FirstOrDefault()?.DataRegisto ?? DateTime.MinValue
                })
                .ToList();

            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            document.Add(new Paragraph($"Relatório do Produto: {produto.Nome}")
                .SetFontSize(16)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"Categoria: {produto.Categoria?.Nome ?? "N/A"}")
                .SetFontSize(12));
            document.Add(new Paragraph(" "));

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 40, 30, 30 }));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            table.AddHeaderCell("Loja");
            table.AddHeaderCell("Último Preço");
            table.AddHeaderCell("Data do Registro");

            foreach (var loja in lojasInfo)
            {
                table.AddCell(loja.LojaNome);
                table.AddCell(loja.LatestPrice.ToString("C"));
                table.AddCell(loja.LatestDate.ToString("g"));
            }

            document.Add(table);
            document.Close();

            var pdfBytes = memoryStream.ToArray();
            return File(pdfBytes, "application/pdf", $"relatorio_produto_{produtoId}.pdf");
        }

        // Exporta Relatório Específico de Produto em CSV
        // Endpoint: GET /api/Export/produtos/{produtoId}/csv
        [HttpGet("produtos/{produtoId:int}/csv")]
        [Authorize(Roles = "Admin,UserManager")]
        public async Task<IActionResult> ExportProdutoCsv(int produtoId)
        {
            var produto = await _produtoRepository.GetByIdWithDetailsAsync(produtoId);
            if (produto == null)
            {
                return NotFound(new { Message = "Produto não encontrado." });
            }

            var registos = await _registosPrecoRepository.GetAllWithDetailsAsync();
            var lojasInfo = registos
                .Where(r => r.ProdutoId == produtoId)
                .GroupBy(r => r.LojaId)
                .Select(g => new
                {
                    LojaNome = g.Select(r => r.Loja != null ? r.Loja.Nome : "N/A").FirstOrDefault(),
                    LatestPrice = g.OrderByDescending(r => r.DataRegisto).FirstOrDefault()?.Preco ?? 0,
                    LatestDate = g.OrderByDescending(r => r.DataRegisto).FirstOrDefault()?.DataRegisto ?? DateTime.MinValue
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Loja;Último Preço;Data do Registro");
            foreach (var loja in lojasInfo)
            {
                sb.AppendLine(string.Join(";", new[] { loja.LojaNome, loja.LatestPrice.ToString("C"), loja.LatestDate.ToString("g") }.Select(value => CsvField.Escape(value, ';'))));
            }

            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(csvBytes, "text/csv", $"relatorio_produto_{produtoId}.csv");
        }
    }
}
