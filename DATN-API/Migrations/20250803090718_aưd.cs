using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_API.Migrations
{
    /// <inheritdoc />
    public partial class aưd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Decorates");

            migrationBuilder.AddColumn<string>(
                name: "Description1",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description2",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSlide1",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSlide2",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSlide3",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSlide4",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSlide5",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image1",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image2",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slide1",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slide2",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slide3",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slide4",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slide5",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title1",
                table: "Decorates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title2",
                table: "Decorates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleSlide1",
                table: "Decorates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleSlide2",
                table: "Decorates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleSlide3",
                table: "Decorates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleSlide4",
                table: "Decorates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleSlide5",
                table: "Decorates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Video",
                table: "Decorates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "Carts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description1",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Description2",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "DescriptionSlide1",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "DescriptionSlide2",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "DescriptionSlide3",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "DescriptionSlide4",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "DescriptionSlide5",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Image1",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Image2",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Slide1",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Slide2",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Slide3",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Slide4",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Slide5",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Title1",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Title2",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "TitleSlide1",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "TitleSlide2",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "TitleSlide3",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "TitleSlide4",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "TitleSlide5",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "Video",
                table: "Decorates");

            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "Carts");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Decorates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Decorates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
