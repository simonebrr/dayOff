using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioneFerie.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAzienda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aziende",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    EmailMalattia = table.Column<string>(type: "TEXT", nullable: true),
                    CapoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aziende", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aziende_Utenti_CapoId",
                        column: x => x.CapoId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "AziendaId",
                table: "Utenti",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_AziendaId",
                table: "Utenti",
                column: "AziendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Aziende_CapoId",
                table: "Aziende",
                column: "CapoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Utenti_Aziende_AziendaId",
                table: "Utenti",
                column: "AziendaId",
                principalTable: "Aziende",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Utenti_Aziende_AziendaId",
                table: "Utenti");

            migrationBuilder.DropTable(
                name: "Aziende");

            migrationBuilder.DropIndex(
                name: "IX_Utenti_AziendaId",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "AziendaId",
                table: "Utenti");
        }
    }
}
