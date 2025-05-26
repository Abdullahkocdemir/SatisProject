using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatışProject.Migrations
{
    /// <inheritdoc />
    public partial class Mig_SonDüzenleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "popularProduct",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "popularCategory",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "popularProduct",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "popularCategory",
                table: "Categories");
        }
    }
}
