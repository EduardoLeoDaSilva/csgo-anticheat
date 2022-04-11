﻿// <auto-generated />
using System;
using CsgoAntiCheatDudu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CsgoAntiCheatDudu.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    partial class ApplicationContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-preview.2.22153.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("CsgoAntiCheatDudu.Player", b =>
                {
                    b.Property<string>("SteamId")
                        .HasColumnType("varchar(95)");

                    b.Property<DateTime>("Expiration")
                        .HasColumnType("datetime");

                    b.Property<bool>("IsAntiCheatOpen")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastPhotoTaken")
                        .HasColumnType("datetime");

                    b.Property<string>("Map")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("SteamId");

                    b.ToTable("players", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}