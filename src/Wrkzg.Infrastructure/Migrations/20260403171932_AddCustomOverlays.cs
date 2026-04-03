using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomOverlays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomOverlays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Html = table.Column<string>(type: "TEXT", nullable: false),
                    Css = table.Column<string>(type: "TEXT", nullable: false),
                    JavaScript = table.Column<string>(type: "TEXT", nullable: false),
                    FieldDefinitions = table.Column<string>(type: "TEXT", nullable: false),
                    FieldValues = table.Column<string>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomOverlays", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomOverlays");
        }
    }
}
