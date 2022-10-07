﻿// <auto-generated />
using System;
using MachineDataApi.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MachineDataApi.Migrations
{
    [DbContext(typeof(MachineDataDbContext))]
    partial class MachineDataDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("MachineDataApi.Models.Machine", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("Machines");
                });

            modelBuilder.Entity("MachineDataApi.Models.MachineData", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("MachineId")
                        .HasColumnType("uuid");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("MachineId");

                    b.ToTable("MachineData");
                });

            modelBuilder.Entity("MachineDataApi.Models.MachineData", b =>
                {
                    b.HasOne("MachineDataApi.Models.Machine", null)
                        .WithMany("Data")
                        .HasForeignKey("MachineId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MachineDataApi.Models.Machine", b =>
                {
                    b.Navigation("Data");
                });
#pragma warning restore 612, 618
        }
    }
}
