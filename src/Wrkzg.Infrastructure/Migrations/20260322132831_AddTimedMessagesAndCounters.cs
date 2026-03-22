using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimedMessagesAndCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Counters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResponseTemplate = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: "{name}: {value}"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimedMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Messages = table.Column<string>(type: "TEXT", nullable: false),
                    NextMessageIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10),
                    MinChatLines = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    RunWhenOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    RunWhenOffline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastFiredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimedMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Counters_Trigger",
                table: "Counters",
                column: "Trigger",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Counters");

            migrationBuilder.DropTable(
                name: "TimedMessages");
        }
    }
}
