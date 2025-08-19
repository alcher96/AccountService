using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFrozenToAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFrozen",
                table: "Accounts");
        }
    }
}
