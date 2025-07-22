using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class Chage2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageSize",
                table: "ProductVariants");

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "ProductVariants",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Length",
                table: "ProductVariants",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Width",
                table: "ProductVariants",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "PackageSize",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
