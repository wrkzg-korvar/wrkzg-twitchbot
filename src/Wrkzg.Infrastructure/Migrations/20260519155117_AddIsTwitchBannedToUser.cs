using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTwitchBannedToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwitchBanned",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwitchBanned",
                table: "Users");
        }
    }
}
