using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioneFerie.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleRefreshTokenAzienda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleRefreshToken",
                table: "Aziende",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleRefreshToken",
                table: "Aziende");
        }
    }
}
