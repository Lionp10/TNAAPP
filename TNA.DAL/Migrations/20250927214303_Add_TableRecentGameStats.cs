using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TNA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Add_TableRecentGameStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecentGamesStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DateOfUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MapName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GameMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsCustomMatch = table.Column<bool>(type: "bit", nullable: false),
                    DBNOs = table.Column<int>(type: "int", nullable: false),
                    Assists = table.Column<int>(type: "int", nullable: false),
                    Boots = table.Column<int>(type: "int", nullable: false),
                    DamageDealt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeadshotsKills = table.Column<int>(type: "int", nullable: false),
                    Heals = table.Column<int>(type: "int", nullable: false),
                    KillPlace = table.Column<int>(type: "int", nullable: false),
                    KillStreaks = table.Column<int>(type: "int", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    LongestKill = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revives = table.Column<int>(type: "int", nullable: false),
                    RideDistance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SwimDistance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WalkDistance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RoadKills = table.Column<int>(type: "int", nullable: false),
                    TeamKills = table.Column<int>(type: "int", nullable: false),
                    TimeSurvived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VehicleDestroys = table.Column<int>(type: "int", nullable: false),
                    WeaponsAcquired = table.Column<int>(type: "int", nullable: false),
                    WinPlace = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentGamesStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecentGamesStats_PlayerId",
                table: "RecentGamesStats",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecentGamesStats");
        }
    }
}
