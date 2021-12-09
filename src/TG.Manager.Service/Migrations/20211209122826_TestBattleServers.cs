using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TG.Manager.Service.Migrations
{
    public partial class TestBattleServers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_battle_servers",
                columns: table => new
                {
                    battle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    load_balancer_port = table.Column<int>(type: "integer", nullable: false),
                    load_balancer_ip = table.Column<string>(type: "text", nullable: true),
                    initialization_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_battle_servers", x => x.battle_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_battle_servers");
        }
    }
}
