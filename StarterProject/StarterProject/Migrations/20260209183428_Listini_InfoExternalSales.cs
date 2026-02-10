using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterProject.Migrations
{
    /// <inheritdoc />
    public partial class Listini_InfoExternalSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    PartitaIVA = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RagioneSociale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Indirizzo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CAP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comune = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.PartitaIVA);
                });

            migrationBuilder.CreateTable(
                name: "InfoCommercialiEsterni",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartitaIVA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RagioneSociale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoCommercialiEsterni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfoCommercialiEsterni_AspNetUsers_Id",
                        column: x => x.Id,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Listini",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listini", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListiniDettaglio",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KWp = table.Column<double>(type: "float", nullable: false),
                    Prezzo = table.Column<double>(type: "float", nullable: false),
                    Provvigione = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListiniDettaglio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListiniDettaglio_Listini_Id",
                        column: x => x.Id,
                        principalTable: "Listini",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clienti");

            migrationBuilder.DropTable(
                name: "InfoCommercialiEsterni");

            migrationBuilder.DropTable(
                name: "ListiniDettaglio");

            migrationBuilder.DropTable(
                name: "Listini");
        }
    }
}
