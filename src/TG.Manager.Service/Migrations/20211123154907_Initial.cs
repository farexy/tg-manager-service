﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TG.Manager.Service.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battle_servers",
                columns: table => new
                {
                    port = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    state = table.Column<int>(type: "integer", nullable: false),
                    battle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deployment_name = table.Column<string>(type: "text", nullable: false),
                    svc_name = table.Column<string>(type: "text", nullable: false),
                    initialization_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_servers", x => x.port);
                });

            migrationBuilder.CreateIndex(
                name: "ix_battle_servers_battle_id",
                table: "battle_servers",
                column: "battle_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_servers");
        }
    }
}
