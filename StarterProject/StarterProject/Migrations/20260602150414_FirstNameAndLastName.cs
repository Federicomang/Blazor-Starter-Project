using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterProject.Migrations
{
    /// <inheritdoc />
    public partial class FirstNameAndLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "identity",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "identity",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [identity].[AspNetUsers]
                SET
                    FirstName = REPLACE(LEFT(UserName, CHARINDEX('.', UserName) - 1), '_', ' '),
                    LastName = REPLACE(SUBSTRING(UserName, CHARINDEX('.', UserName) + 1, LEN(UserName)), '_', ' ')
                WHERE UserName IS NOT NULL
                AND CHARINDEX('.', UserName) > 0;
             """);

            migrationBuilder.Sql("""
                UPDATE [identity].[AspNetUsers]
                SET
                    UserName = Email,
                    NormalizedUserName = NormalizedEmail
                WHERE Email IS NOT NULL;
             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "identity",
                table: "AspNetUsers");
        }
    }
}
