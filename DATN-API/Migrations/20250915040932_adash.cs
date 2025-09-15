using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class adash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Categories_CategoryId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CategoryId",
                table: "Vouchers");

            migrationBuilder.CreateTable(
                name: "CategoriesVouchers",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "int", nullable: false),
                    VouchersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesVouchers", x => new { x.CategoriesId, x.VouchersId });
                    table.ForeignKey(
                        name: "FK_CategoriesVouchers_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoriesVouchers_Vouchers_VouchersId",
                        column: x => x.VouchersId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesVouchers_VouchersId",
                table: "CategoriesVouchers",
                column: "VouchersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriesVouchers");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CategoryId",
                table: "Vouchers",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Categories_CategoryId",
                table: "Vouchers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
