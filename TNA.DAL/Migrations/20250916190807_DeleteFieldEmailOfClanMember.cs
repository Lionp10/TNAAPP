using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TNA.DAL.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFieldEmailOfClanMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ClanMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ClanMembers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
