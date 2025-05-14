using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace budget_tracker_backend.Migrations
{
    /// <inheritdoc />
    public partial class PlanIdInTrans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BudgetPlanId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BudgetPlanId",
                table: "Transactions",
                column: "BudgetPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BudgetPlans_BudgetPlanId",
                table: "Transactions",
                column: "BudgetPlanId",
                principalTable: "BudgetPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BudgetPlans_BudgetPlanId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BudgetPlanId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetPlanId",
                table: "Transactions");
        }
    }
}
