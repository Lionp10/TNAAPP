using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TNA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchAndPlayerMatchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchId",
                table: "PlayerMatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlayerId",
                table: "PlayerMatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "PlayerMatches");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "PlayerMatches");
        }
    }
}
