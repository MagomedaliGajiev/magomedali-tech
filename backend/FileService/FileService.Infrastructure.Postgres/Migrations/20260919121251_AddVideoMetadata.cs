using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "video_duration",
                table: "media_assets",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "video_height",
                table: "media_assets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "video_width",
                table: "media_assets",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "video_duration",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "video_height",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "video_width",
                table: "media_assets");
        }
    }
}
