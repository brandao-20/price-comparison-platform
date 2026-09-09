using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier snapshots contained these objects, but their migrations never created them.
            // Reconcile manually modified databases before applying this migration.
            migrationBuilder.AddColumn<int>("ParentId", "Categorias", type: "integer", nullable: true);
            migrationBuilder.AddColumn<string>("PlaceId", "Lojas", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>("Supermercado", "Lojas", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>("Pontos", "Utilizadores", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.Sql("ALTER TABLE \"Mensagens\" RENAME CONSTRAINT \"PK_Mensagens\" TO \"Mensagens_pkey\";");
            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    ComentarioId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Conteudo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegistoPrecoId = table.Column<int>(type: "integer", nullable: false),
                    UtilizadorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Comentarios_pkey", x => x.ComentarioId);
                    table.ForeignKey("FK_Comentarios_RegistosPrecos", x => x.RegistoPrecoId, "RegistosPrecos", "RegistoPrecoId", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_Comentarios_Utilizadores", x => x.UtilizadorId, "Utilizadores", "UtilizadorId", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    FavoritoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    UtilizadorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Favoritos_pkey", x => x.FavoritoId);
                    table.ForeignKey("FK_Favoritos_Produtos", x => x.ProdutoId, "Produtos", "ProdutoId", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_Favoritos_Utilizadores", x => x.UtilizadorId, "Utilizadores", "UtilizadorId", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "Relatorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeProduto = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeLoja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeCategoria = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProdutoId = table.Column<int>(type: "integer", nullable: false),
                    LojaId = table.Column<int>(type: "integer", nullable: false),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Relatorios_pkey", x => x.Id);
                    table.ForeignKey("FK_Relatorios_Produtos", x => x.ProdutoId, "Produtos", "ProdutoId");
                    table.ForeignKey("FK_Relatorios_Lojas", x => x.LojaId, "Lojas", "LojaId");
                    table.ForeignKey("FK_Relatorios_Categorias", x => x.CategoriaId, "Categorias", "CategoriaId");
                });
            migrationBuilder.CreateIndex("IX_Categorias_ParentId", "Categorias", "ParentId");
            migrationBuilder.AddForeignKey("FK_Categorias_Parent", "Categorias", "ParentId", "Categorias", principalColumn: "CategoriaId");
            migrationBuilder.CreateIndex("IX_Comentarios_RegistoPrecoId", "Comentarios", "RegistoPrecoId");
            migrationBuilder.CreateIndex("IX_Comentarios_UtilizadorId", "Comentarios", "UtilizadorId");
            migrationBuilder.CreateIndex("IX_Favoritos_ProdutoId", "Favoritos", "ProdutoId");
            migrationBuilder.CreateIndex("IX_Favoritos_UtilizadorId", "Favoritos", "UtilizadorId");
            migrationBuilder.CreateIndex("IX_Relatorios_ProdutoId", "Relatorios", "ProdutoId");
            migrationBuilder.CreateIndex("IX_Relatorios_LojaId", "Relatorios", "LojaId");
            migrationBuilder.CreateIndex("IX_Relatorios_CategoriaId", "Relatorios", "CategoriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("Comentarios");
            migrationBuilder.DropTable("Favoritos");
            migrationBuilder.DropTable("Relatorios");
            migrationBuilder.DropForeignKey("FK_Categorias_Parent", "Categorias");
            migrationBuilder.DropIndex("IX_Categorias_ParentId", "Categorias");
            migrationBuilder.DropColumn("ParentId", "Categorias");
            migrationBuilder.DropColumn("PlaceId", "Lojas");
            migrationBuilder.DropColumn("Supermercado", "Lojas");
            migrationBuilder.DropColumn("Pontos", "Utilizadores");
            migrationBuilder.Sql("ALTER TABLE \"Mensagens\" RENAME CONSTRAINT \"Mensagens_pkey\" TO \"PK_Mensagens\";");
        }
    }
}
