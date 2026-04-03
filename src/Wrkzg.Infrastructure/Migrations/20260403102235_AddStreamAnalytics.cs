using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStreamAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StreamSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TwitchStreamId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    PeakViewers = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageViewers = table.Column<double>(type: "REAL", nullable: true),
                    UniqueChatters = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalMessages = table.Column<int>(type: "INTEGER", nullable: true),
                    NewFollowers = table.Column<int>(type: "INTEGER", nullable: true),
                    NewSubscribers = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategorySegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StreamSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TwitchCategoryId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    AverageViewers = table.Column<double>(type: "REAL", nullable: true),
                    PeakViewers = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorySegments_StreamSessions_StreamSessionId",
                        column: x => x.StreamSessionId,
                        principalTable: "StreamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewerSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StreamSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewerSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewerSnapshots_StreamSessions_StreamSessionId",
                        column: x => x.StreamSessionId,
                        principalTable: "StreamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategorySegments_StreamSessionId",
                table: "CategorySegments",
                column: "StreamSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewerSnapshots_StreamSessionId",
                table: "ViewerSnapshots",
                column: "StreamSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategorySegments");

            migrationBuilder.DropTable(
                name: "ViewerSnapshots");

            migrationBuilder.DropTable(
                name: "StreamSessions");
        }
    }
}
