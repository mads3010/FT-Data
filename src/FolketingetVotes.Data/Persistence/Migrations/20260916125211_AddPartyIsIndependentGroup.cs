using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolketingetVotes.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyIsIndependentGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_independent_group",
                table: "parties",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_independent_group",
                table: "parties");
        }
    }
}
