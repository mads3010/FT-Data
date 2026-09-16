using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FolketingetVotes.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "actor_relations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    from_actor_id = table.Column<int>(type: "integer", nullable: false),
                    to_actor_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_relations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "actors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    group_short_name = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    biography_xml = table.Column<string>(type: "text", nullable: true),
                    period_id = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    picture_url = table.Column<string>(type: "text", nullable: true),
                    biography_party_short_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ballots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    vote_id = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ballots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bill_summaries",
                columns: table => new
                {
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    content_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_summaries", x => x.case_id);
                });

            migrationBuilder.CreateTable(
                name: "biography_memberships",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    party_name = table.Column<string>(type: "text", nullable: false),
                    party_short_name = table.Column<string>(type: "text", nullable: true),
                    constituency = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_biography_memberships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "case_actors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_actors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "case_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    folketingstidende_url = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_steps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    short_title = table.Column<string>(type: "text", nullable: true),
                    number = table.Column<string>(type: "text", nullable: true),
                    number_prefix = table.Column<string>(type: "text", nullable: true),
                    number_numeric = table.Column<int>(type: "integer", nullable: true),
                    number_postfix = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    voting_conclusion = table.Column<string>(type: "text", nullable: true),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    law_number = table.Column<int>(type: "integer", nullable: true),
                    law_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    retsinformation_url = table.Column<string>(type: "text", nullable: true),
                    is_budget_case = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookups",
                columns: table => new
                {
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lookups", x => new { x.kind, x.id });
                });

            migrationBuilder.CreateTable(
                name: "meetings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    room = table.Column<string>(type: "text", nullable: true),
                    number = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meetings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    short_name = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    latest_group_actor_id = table.Column<int>(type: "integer", nullable: true),
                    first_seen = table.Column<DateOnly>(type: "date", nullable: true),
                    last_seen = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parties", x => x.short_name);
                });

            migrationBuilder.CreateTable(
                name: "party_accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    year = table.Column<int>(type: "integer", nullable: false),
                    party_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    party_short_name = table.Column<string>(type: "text", nullable: true),
                    source_file = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_page = table.Column<int>(type: "integer", nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_memberships",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<int>(type: "integer", nullable: false),
                    group_actor_id = table.Column<int>(type: "integer", nullable: false),
                    party_short_name = table.Column<string>(type: "text", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_memberships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_states",
                columns: table => new
                {
                    entity_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_id = table.Column<int>(type: "integer", nullable: true),
                    full_load_completed = table.Column<bool>(type: "boolean", nullable: false),
                    last_run_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rows_upserted = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_states", x => x.entity_name);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    conclusion = table.Column<string>(type: "text", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    meeting_id = table.Column<int>(type: "integer", nullable: false),
                    case_step_id = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_donations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    party_account_id = table.Column<int>(type: "integer", nullable: false),
                    donor_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    donor_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    source_page = table.Column<int>(type: "integer", nullable: true),
                    raw_text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_donations", x => x.id);
                    table.ForeignKey(
                        name: "fk_party_donations_party_accounts_party_account_id",
                        column: x => x.party_account_id,
                        principalTable: "party_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_actor_relations_from_actor_id_role_id",
                table: "actor_relations",
                columns: new[] { "from_actor_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ix_actor_relations_to_actor_id_role_id",
                table: "actor_relations",
                columns: new[] { "to_actor_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ix_actors_name",
                table: "actors",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_actors_type_id",
                table: "actors",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_type_id_group_short_name",
                table: "actors",
                columns: new[] { "type_id", "group_short_name" });

            migrationBuilder.CreateIndex(
                name: "ix_ballots_actor_id_vote_id",
                table: "ballots",
                columns: new[] { "actor_id", "vote_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ballots_vote_id",
                table: "ballots",
                column: "vote_id");

            migrationBuilder.CreateIndex(
                name: "ix_biography_memberships_person_id_start_date",
                table: "biography_memberships",
                columns: new[] { "person_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_case_actors_actor_id_role_id",
                table: "case_actors",
                columns: new[] { "actor_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ix_case_actors_case_id",
                table: "case_actors",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_case_steps_case_id",
                table: "case_steps",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_cases_period_id",
                table: "cases",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_cases_type_id_period_id",
                table: "cases",
                columns: new[] { "type_id", "period_id" });

            migrationBuilder.CreateIndex(
                name: "ix_meetings_date",
                table: "meetings",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_meetings_period_id",
                table: "meetings",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_party_accounts_year_party_name",
                table: "party_accounts",
                columns: new[] { "year", "party_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_party_donations_donor_name",
                table: "party_donations",
                column: "donor_name");

            migrationBuilder.CreateIndex(
                name: "ix_party_donations_party_account_id",
                table: "party_donations",
                column: "party_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_party_memberships_party_short_name_period_id",
                table: "party_memberships",
                columns: new[] { "party_short_name", "period_id" });

            migrationBuilder.CreateIndex(
                name: "ix_party_memberships_person_id_start_date",
                table: "party_memberships",
                columns: new[] { "person_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_periods_code",
                table: "periods",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_votes_case_step_id",
                table: "votes",
                column: "case_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_votes_meeting_id",
                table: "votes",
                column: "meeting_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actor_relations");

            migrationBuilder.DropTable(
                name: "actors");

            migrationBuilder.DropTable(
                name: "ballots");

            migrationBuilder.DropTable(
                name: "bill_summaries");

            migrationBuilder.DropTable(
                name: "biography_memberships");

            migrationBuilder.DropTable(
                name: "case_actors");

            migrationBuilder.DropTable(
                name: "case_steps");

            migrationBuilder.DropTable(
                name: "cases");

            migrationBuilder.DropTable(
                name: "lookups");

            migrationBuilder.DropTable(
                name: "meetings");

            migrationBuilder.DropTable(
                name: "parties");

            migrationBuilder.DropTable(
                name: "party_donations");

            migrationBuilder.DropTable(
                name: "party_memberships");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "sync_states");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "party_accounts");
        }
    }
}
