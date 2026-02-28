using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterProject.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentifierTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableThatChanged",
                schema: "History",
                table: "AuditLog");

            migrationBuilder.CreateTable(
                name: "Identifiers",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdentifierKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentifierId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identifiers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Identifiers",
                schema: "identity");

            migrationBuilder.AddColumn<string>(
                name: "TableThatChanged",
                schema: "History",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
