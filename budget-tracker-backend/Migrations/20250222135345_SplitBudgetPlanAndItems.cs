using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace budget_tracker_backend.Migrations
{
    /// <inheritdoc />
    public partial class SplitBudgetPlanAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPlans_Categories_CategoryId",
                table: "BudgetPlans");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPlans_CategoryId",
                table: "BudgetPlans");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "BudgetPlans");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "BudgetPlans");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "BudgetPlans");

            migrationBuilder.AlterColumn<int>(
                name: "EventId",
                table: "Transactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "BudgetPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BudgetPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetPlanId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPlanItems_BudgetPlans_BudgetPlanId",
                        column: x => x.BudgetPlanId,
                        principalTable: "BudgetPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetPlanItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetPlanItems_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanItems_BudgetPlanId",
                table: "BudgetPlanItems",
                column: "BudgetPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanItems_CategoryId",
                table: "BudgetPlanItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanItems_CurrencyId",
                table: "BudgetPlanItems",
                column: "CurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetPlanItems");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "BudgetPlans");

            migrationBuilder.AlterColumn<int>(
                name: "EventId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "BudgetPlans",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "BudgetPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "BudgetPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlans_CategoryId",
                table: "BudgetPlans",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPlans_Categories_CategoryId",
                table: "BudgetPlans",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
