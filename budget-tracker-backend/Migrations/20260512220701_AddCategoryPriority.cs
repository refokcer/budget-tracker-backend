using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace budget_tracker_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Categories",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Flexible");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Categories");
        }
    }
}
