using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRaffleExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Raffles",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Raffles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndReason",
                table: "Raffles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EntriesCloseAt",
                table: "Raffles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Keyword",
                table: "Raffles",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxEntries",
                table: "Raffles",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Raffles");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Raffles");

            migrationBuilder.DropColumn(
                name: "EndReason",
                table: "Raffles");

            migrationBuilder.DropColumn(
                name: "EntriesCloseAt",
                table: "Raffles");

            migrationBuilder.DropColumn(
                name: "Keyword",
                table: "Raffles");

            migrationBuilder.DropColumn(
                name: "MaxEntries",
                table: "Raffles");
        }
    }
}
