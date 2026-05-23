using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioneFerie.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLogModificaSaldo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogModificheSaldo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrePrecedenti = table.Column<decimal>(type: "TEXT", nullable: false),
                    OreNuove = table.Column<decimal>(type: "TEXT", nullable: false),
                    DataModifica = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModificatoDa = table.Column<string>(type: "TEXT", nullable: false),
                    Anno = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogModificheSaldo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogModificheSaldo_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogModificheSaldo_UtenteId",
                table: "LogModificheSaldo",
                column: "UtenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogModificheSaldo");
        }
    }
}
