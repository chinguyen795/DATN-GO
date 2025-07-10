using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prices_VariantCompositions_VariantCompositionId",
                table: "Prices");

            migrationBuilder.DropIndex(
                name: "IX_Prices_VariantCompositionId",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "colorHex",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "PackageSize",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VariantCompositionId",
                table: "Prices");

            migrationBuilder.RenameColumn(
                name: "ValueName",
                table: "VariantValues",
                newName: "VariantValueName");

            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "VariantValues",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "VariantValues",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VariantValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "VariantValues",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Length",
                table: "VariantValues",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "VariantValues",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "VariantValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "VariantValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "VariantsId",
                table: "VariantValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "VariantValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Width",
                table: "VariantValues",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "Products",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Length",
                table: "Products",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Width",
                table: "Products",
                type: "real",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Prices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateAt",
                table: "Prices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateAt",
                table: "Prices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "VariantValuesId",
                table: "Prices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantValues_VariantsId",
                table: "VariantValues",
                column: "VariantsId");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_VariantValuesId",
                table: "Prices",
                column: "VariantValuesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prices_VariantValues_VariantValuesId",
                table: "Prices",
                column: "VariantValuesId",
                principalTable: "VariantValues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantValues_Variants_VariantsId",
                table: "VariantValues",
                column: "VariantsId",
                principalTable: "Variants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prices_VariantValues_VariantValuesId",
                table: "Prices");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantValues_Variants_VariantsId",
                table: "VariantValues");

            migrationBuilder.DropIndex(
                name: "IX_VariantValues_VariantsId",
                table: "VariantValues");

            migrationBuilder.DropIndex(
                name: "IX_Prices_VariantValuesId",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "VariantsId",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "VariantValues");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreateAt",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "UpdateAt",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "VariantValuesId",
                table: "Prices");

            migrationBuilder.RenameColumn(
                name: "VariantValueName",
                table: "VariantValues",
                newName: "ValueName");

            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "VariantValues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "VariantValues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "colorHex",
                table: "VariantValues",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageSize",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Prices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantCompositionId",
                table: "Prices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Prices_VariantCompositionId",
                table: "Prices",
                column: "VariantCompositionId");

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
