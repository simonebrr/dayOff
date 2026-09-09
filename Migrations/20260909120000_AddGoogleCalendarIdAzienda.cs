using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioneFerie.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleCalendarIdAzienda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleCalendarId",
                table: "Aziende",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleCalendarId",
                table: "Aziende");
        }
    }
}
