using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatışProject.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceFilePathToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceFilePath",
                table: "Sales",
                type: "VarChar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceFilePath",
                table: "Sales");
        }
    }
}
