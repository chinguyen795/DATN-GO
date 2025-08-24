using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class IntialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductsId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Orders_OrdersId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_OrdersId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductsId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "OrdersId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProductsId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "GhtkStatusCode",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Reviews");

            migrationBuilder.AddColumn<int>(
                name: "OrdersId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductsId",
                table: "ProductVariants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GhtkStatusCode",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrdersId",
                table: "Reviews",
                column: "OrdersId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductsId",
                table: "ProductVariants",
                column: "ProductsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductsId",
                table: "ProductVariants",
                column: "ProductsId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Orders_OrdersId",
                table: "Reviews",
                column: "OrdersId",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
