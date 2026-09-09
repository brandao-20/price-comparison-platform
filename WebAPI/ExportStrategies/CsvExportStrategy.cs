using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WebAPI.Entities;

namespace WebAPI.ExportStrategies
{
    public class CsvExportStrategy : IExportStrategy
    {
        public byte[] Export(List<Relatorio> data)
        {
            var sb = new StringBuilder();
            // cabeçalho
            sb.AppendLine("NomeProduto,NomeLoja,Preco,Data,NomeCategoria");

            foreach (var item in data)
            {
                // força ponto decimal e formato de data invariant
                var preco = item.Preco.ToString("F2", CultureInfo.InvariantCulture);
                var dataStr = item.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                sb.AppendLine(
                    string.Join(",",
                        CsvField.Escape(item.NomeProduto),
                        CsvField.Escape(item.NomeLoja),
                        preco,
                        dataStr,
                        CsvField.Escape(item.NomeCategoria)
                    )
                );
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
