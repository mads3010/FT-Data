using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FolketingetVotes.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordsRolePeriodsAndBorn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_temporary",
                table: "biography_memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "born",
                table: "actors",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "case_keywords",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    keyword_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "keywords",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_periods", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_case_keywords_case_id",
                table: "case_keywords",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_case_keywords_keyword_id",
                table: "case_keywords",
                column: "keyword_id");

            migrationBuilder.CreateIndex(
                name: "ix_keywords_name",
                table: "keywords",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_role_periods_person_id_kind_start_date",
                table: "role_periods",
                columns: new[] { "person_id", "kind", "start_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_keywords");

            migrationBuilder.DropTable(
                name: "keywords");

            migrationBuilder.DropTable(
                name: "role_periods");

            migrationBuilder.DropColumn(
                name: "is_temporary",
                table: "biography_memberships");

            migrationBuilder.DropColumn(
                name: "born",
                table: "actors");
        }
    }
}
