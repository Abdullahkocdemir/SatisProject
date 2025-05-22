using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatışProject.Migrations
{
    /// <inheritdoc />
    public partial class Mig_addsa_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotal",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotal",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrandTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxTotal",
                table: "Sales");
        }
    }
}
