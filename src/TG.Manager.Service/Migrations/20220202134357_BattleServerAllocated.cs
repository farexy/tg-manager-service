using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TG.Manager.Service.Migrations
{
    public partial class BattleServerAllocated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allocated",
                table: "battle_servers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allocated",
                table: "battle_servers");
        }
    }
}
