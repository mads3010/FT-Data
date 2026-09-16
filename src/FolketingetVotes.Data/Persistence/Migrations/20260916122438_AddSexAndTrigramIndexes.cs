using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolketingetVotes.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSexAndTrigramIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_keywords_name",
                table: "keywords");

            migrationBuilder.DropIndex(
                name: "ix_actors_name",
                table: "actors");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "sex",
                table: "actors",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_keywords_name_trgm",
                table: "keywords",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_cases_title_trgm",
                table: "cases",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_actors_name_trgm",
                table: "actors",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_keywords_name_trgm",
                table: "keywords");

            migrationBuilder.DropIndex(
                name: "ix_cases_title_trgm",
                table: "cases");

            migrationBuilder.DropIndex(
                name: "ix_actors_name_trgm",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "sex",
                table: "actors");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "ix_keywords_name",
                table: "keywords",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_actors_name",
                table: "actors",
                column: "name");
        }
    }
}
