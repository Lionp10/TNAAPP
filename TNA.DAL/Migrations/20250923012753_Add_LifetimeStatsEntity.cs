using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TNA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Add_LifetimeStatsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerLifetimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DateOfUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LifetimeJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerLifetimes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerLifetimes_PlayerId",
                table: "PlayerLifetimes",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerLifetimes");
        }
    }
}
