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
                name: "load_balancers",
                columns: table => new
                {
                    port = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    svc_name = table.Column<string>(type: "text", nullable: false),
                    public_ip = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_load_balancers", x => x.port);
                });

            migrationBuilder.CreateTable(
                name: "battle_servers",
                columns: table => new
                {
                    battle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    load_balancer_port = table.Column<int>(type: "integer", nullable: false),
                    deployment_name = table.Column<string>(type: "text", nullable: false),
                    initialization_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_servers", x => x.battle_id);
                    table.ForeignKey(
                        name: "fk_battle_servers_load_balancers_load_balancer_port",
                        column: x => x.load_balancer_port,
                        principalTable: "load_balancers",
                        principalColumn: "port",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_battle_servers_load_balancer_port",
                table: "battle_servers",
                column: "load_balancer_port",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_servers");

            migrationBuilder.DropTable(
                name: "load_balancers");
        }
    }
}
