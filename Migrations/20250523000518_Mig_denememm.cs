using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatışProject.Migrations
{
    /// <inheritdoc />
    public partial class Mig_denememm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Invoices",
                type: "VarChar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Invoices");
        }
    }
}
