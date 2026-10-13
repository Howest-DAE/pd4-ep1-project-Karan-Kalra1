using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PD4Dodgeball.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCrownsToMatchPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CrownsRemaining",
                table: "MatchPlayers",
                type: "int",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrownsRemaining",
                table: "MatchPlayers");
        }
    }
}
