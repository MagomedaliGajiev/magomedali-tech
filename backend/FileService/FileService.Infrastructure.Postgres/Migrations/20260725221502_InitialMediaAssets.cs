using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialMediaAssets : Migration
    {
        private static readonly string[] OwnerIndexColumns = ["owner_context", "owner_entity_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    file_extension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    media_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    expected_chunks_count = table.Column<int>(type: "integer", nullable: false),
                    asset_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_prefix = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_location = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_owner",
                table: "media_assets",
                columns: OwnerIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}