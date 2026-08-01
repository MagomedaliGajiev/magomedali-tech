using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationContentService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonVideoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "video_id",
                table: "lessons",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "video_id",
                table: "lessons");
        }
    }
}