using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class Price1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prices_VariantCompositions_VariantCompositionId",
                table: "Prices");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Variants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "VariantCompositionId",
                table: "Prices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Prices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Prices_VariantCompositions_VariantCompositionId",
                table: "Prices",
                column: "VariantCompositionId",
                principalTable: "VariantCompositions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prices_VariantCompositions_VariantCompositionId",
                table: "Prices");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Variants",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VariantCompositionId",
                table: "Prices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Prices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Prices_VariantCompositions_VariantCompositionId",
                table: "Prices",
                column: "VariantCompositionId",
                principalTable: "VariantCompositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
