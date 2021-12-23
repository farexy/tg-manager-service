using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TG.Manager.Service.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_ports",
                columns: table => new
                {
                    port = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    svc_name = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_ports", x => x.port);
                });

            migrationBuilder.CreateTable(
                name: "test_battle_servers",
                columns: table => new
                {
                    battle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    ip = table.Column<string>(type: "text", nullable: true),
                    initialization_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_battle_servers", x => x.battle_id);
                });

            migrationBuilder.CreateTable(
                name: "battle_servers",
                columns: table => new
                {
                    battle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    node_ip = table.Column<string>(type: "text", nullable: true),
                    deployment_name = table.Column<string>(type: "text", nullable: false),
                    initialization_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_servers", x => x.battle_id);
                    table.ForeignKey(
                        name: "fk_battle_servers_node_ports_port",
                        column: x => x.port,
                        principalTable: "node_ports",
                        principalColumn: "port",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_battle_servers_port",
                table: "battle_servers",
                column: "port",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_servers");

            migrationBuilder.DropTable(
                name: "test_battle_servers");

            migrationBuilder.DropTable(
                name: "node_ports");
        }
    }
}
