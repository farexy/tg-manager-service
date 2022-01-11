﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TG.Manager.Service.Db;

namespace TG.Manager.Service.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.12")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("TG.Manager.Service.Entities.BattleServer", b =>
                {
                    b.Property<Guid>("BattleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("battle_id");

                    b.Property<string>("DeploymentName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("deployment_name");

                    b.Property<DateTime>("InitializationTime")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("initialization_time");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_update");

                    b.Property<string>("NodeIp")
                        .HasColumnType("text")
                        .HasColumnName("node_ip");

                    b.Property<int>("Port")
                        .HasColumnType("integer")
                        .HasColumnName("port");

                    b.Property<int>("State")
                        .HasColumnType("integer")
                        .HasColumnName("state");

                    b.HasKey("BattleId")
                        .HasName("pk_battle_servers");

                    b.HasIndex("Port")
                        .IsUnique()
                        .HasDatabaseName("ix_battle_servers_port");

                    b.ToTable("battle_servers");
                });

            modelBuilder.Entity("TG.Manager.Service.Entities.NodePort", b =>
                {
                    b.Property<int>("Port")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("port")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("LastUpdate")
                        .IsConcurrencyToken()
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_update");

                    b.Property<int>("State")
                        .HasColumnType("integer")
                        .HasColumnName("state");

                    b.Property<string>("SvcName")
                        .HasColumnType("text")
                        .HasColumnName("svc_name");

                    b.HasKey("Port")
                        .HasName("pk_node_ports");

                    b.ToTable("node_ports");
                });

            modelBuilder.Entity("TG.Manager.Service.Entities.TestBattleServer", b =>
                {
                    b.Property<Guid>("BattleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("battle_id");

                    b.Property<DateTime>("InitializationTime")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("initialization_time");

                    b.Property<string>("Ip")
                        .HasColumnType("text")
                        .HasColumnName("ip");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_update");

                    b.Property<int>("Port")
                        .HasColumnType("integer")
                        .HasColumnName("port");

                    b.Property<int>("State")
                        .HasColumnType("integer")
                        .HasColumnName("state");

                    b.HasKey("BattleId")
                        .HasName("pk_test_battle_servers");

                    b.ToTable("test_battle_servers");
                });

            modelBuilder.Entity("TG.Manager.Service.Entities.BattleServer", b =>
                {
                    b.HasOne("TG.Manager.Service.Entities.NodePort", "NodePort")
                        .WithOne()
                        .HasForeignKey("TG.Manager.Service.Entities.BattleServer", "Port")
                        .HasConstraintName("fk_battle_servers_node_ports_port")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("NodePort");
                });
#pragma warning restore 612, 618
        }
    }
}
