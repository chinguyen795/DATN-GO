using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class adadad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantCompositions_Products_ProductVariantId",
                table: "VariantCompositions");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantCompositions_ProductVariants_ProductVariantId",
                table: "VariantCompositions",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantCompositions_ProductVariants_ProductVariantId",
                table: "VariantCompositions");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantCompositions_Products_ProductVariantId",
                table: "VariantCompositions",
                column: "ProductVariantId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
