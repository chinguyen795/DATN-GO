using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class Viuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTradingPayment_Users_UserId",
                table: "UserTradingPayment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTradingPayment",
                table: "UserTradingPayment");

            migrationBuilder.RenameTable(
                name: "UserTradingPayment",
                newName: "UserTradingPayments");

            migrationBuilder.RenameIndex(
                name: "IX_UserTradingPayment_UserId",
                table: "UserTradingPayments",
                newName: "IX_UserTradingPayments_UserId");

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTradingPayments",
                table: "UserTradingPayments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTradingPayments_Users_UserId",
                table: "UserTradingPayments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTradingPayments_Users_UserId",
                table: "UserTradingPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTradingPayments",
                table: "UserTradingPayments");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "UserTradingPayments",
                newName: "UserTradingPayment");

            migrationBuilder.RenameIndex(
                name: "IX_UserTradingPayments_UserId",
                table: "UserTradingPayment",
                newName: "IX_UserTradingPayment_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTradingPayment",
                table: "UserTradingPayment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTradingPayment_Users_UserId",
                table: "UserTradingPayment",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
