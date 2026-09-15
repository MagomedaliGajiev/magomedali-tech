using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "storage_prefix",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "storage_location",
                table: "media_assets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "storage_key",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<bool>(
                name: "direct_upload",
                table: "media_assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing assets use their original key for upload and playback.
            migrationBuilder.Sql("UPDATE media_assets SET direct_upload = TRUE;");

            migrationBuilder.AddColumn<string>(
                name: "preview_storage_key",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_storage_location",
                table: "media_assets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_storage_prefix",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_storage_key",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_storage_location",
                table: "media_assets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_storage_prefix",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "version",
                table: "media_assets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "video_processes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    progress = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_processes", x => x.id);
                    table.ForeignKey(
                        name: "FK_video_processes_media_assets_video_asset_id",
                        column: x => x.video_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processing_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    result_data = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_critical_failure = table.Column<bool>(type: "boolean", nullable: false),
                    video_process_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_steps", x => x.id);
                    table.ForeignKey(
                        name: "FK_processing_steps_video_processes_video_process_id",
                        column: x => x.video_process_id,
                        principalTable: "video_processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_processing_steps_status_next_retry_at",
                table: "processing_steps",
                columns: ["status", "next_retry_at"]);

            migrationBuilder.CreateIndex(
                name: "IX_processing_steps_video_process_id_step_order",
                table: "processing_steps",
                columns: ["video_process_id", "step_order"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_processes_status",
                table: "video_processes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_video_processes_video_asset_id",
                table: "video_processes",
                column: "video_asset_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE media_assets
                SET storage_key = COALESCE(storage_key, raw_storage_key),
                    storage_prefix = COALESCE(storage_prefix, raw_storage_prefix),
                    storage_location = COALESCE(storage_location, raw_storage_location);
                """);

            migrationBuilder.DropTable(
                name: "processing_steps");

            migrationBuilder.DropTable(
                name: "video_processes");

            migrationBuilder.DropColumn(
                name: "direct_upload",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_storage_key",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_storage_location",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_storage_prefix",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "raw_storage_key",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "raw_storage_location",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "raw_storage_prefix",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "version",
                table: "media_assets");

            migrationBuilder.AlterColumn<string>(
                name: "storage_prefix",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "storage_location",
                table: "media_assets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "storage_key",
                table: "media_assets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
