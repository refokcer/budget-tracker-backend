using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace budget_tracker_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthCodeToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthCode",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthCode",
                table: "Transactions");
        }
    }
}
