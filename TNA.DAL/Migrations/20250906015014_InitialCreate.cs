using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TNA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClanMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PlayerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClanId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProfileImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClanMemberSocialMedias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    SocialMediaId = table.Column<string>(type: "char(2)", nullable: false),
                    SocialMediaUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanMemberSocialMedias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClanId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClanName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClanTag = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClanLevel = table.Column<int>(type: "int", nullable: false),
                    ClanMemberCount = table.Column<int>(type: "int", nullable: false),
                    DateOfUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MapName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DBNOs = table.Column<int>(type: "int", nullable: false),
                    Assists = table.Column<int>(type: "int", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    HeadshotsKills = table.Column<int>(type: "int", nullable: false),
                    DamageDealt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revive = table.Column<int>(type: "int", nullable: false),
                    TeamKill = table.Column<int>(type: "int", nullable: false),
                    TimeSurvived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WinPlace = table.Column<int>(type: "int", nullable: false),
                    MatchCreatedAt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMatches", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanMembers");

            migrationBuilder.DropTable(
                name: "ClanMemberSocialMedias");

            migrationBuilder.DropTable(
                name: "Clans");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "PlayerMatches");
        }
    }
}
