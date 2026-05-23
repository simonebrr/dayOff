using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioneFerie.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Successo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Cognome = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Ruolo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RichiestaFerie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInizio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataFine = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OreRichieste = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    DataRichiesta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MotivoRifiuto = table.Column<string>(type: "TEXT", nullable: true),
                    GoogleCalendarEventId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RichiestaFerie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RichiestaFerie_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaldoFerie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    OreTotali = table.Column<decimal>(type: "TEXT", nullable: false),
                    OreUsate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Anno = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldoFerie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaldoFerie_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RichiestaFerie_UtenteId",
                table: "RichiestaFerie",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SaldoFerie_UtenteId",
                table: "SaldoFerie",
                column: "UtenteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Email",
                table: "Utenti",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "RichiestaFerie");

            migrationBuilder.DropTable(
                name: "SaldoFerie");

            migrationBuilder.DropTable(
                name: "Utenti");
        }
    }
}
