using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRaffleDrawVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PendingWinnerId",
                table: "Raffles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RaffleDraws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaffleId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DrawNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAccepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RedrawReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DrawnAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaffleDraws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaffleDraws_Raffles_RaffleId",
                        column: x => x.RaffleId,
                        principalTable: "Raffles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaffleDraws_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Raffles_PendingWinnerId",
                table: "Raffles",
                column: "PendingWinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RaffleDraws_RaffleId",
                table: "RaffleDraws",
                column: "RaffleId");

            migrationBuilder.CreateIndex(
                name: "IX_RaffleDraws_UserId",
                table: "RaffleDraws",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Raffles_Users_PendingWinnerId",
                table: "Raffles",
                column: "PendingWinnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Raffles_Users_PendingWinnerId",
                table: "Raffles");

            migrationBuilder.DropTable(
                name: "RaffleDraws");

            migrationBuilder.DropIndex(
                name: "IX_Raffles_PendingWinnerId",
                table: "Raffles");

            migrationBuilder.DropColumn(
                name: "PendingWinnerId",
                table: "Raffles");
        }
    }
}
