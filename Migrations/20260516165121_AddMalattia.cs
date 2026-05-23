using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioneFerie.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMalattia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodiceMalattia",
                table: "RichiestaFerie",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodiceMalattia",
                table: "RichiestaFerie");
        }
    }
}
