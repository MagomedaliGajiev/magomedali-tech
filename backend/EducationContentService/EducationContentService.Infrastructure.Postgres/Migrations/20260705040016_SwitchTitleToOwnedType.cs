using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationContentService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class SwitchTitleToOwnedType : Migration
    {
        // Title is now mapped as an owned type instead of a value converter so that
        // string search (LIKE) can be translated to SQL. The physical schema is unchanged:
        // the "title" column and the "ix_lessons_title" unique index stay identical.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}