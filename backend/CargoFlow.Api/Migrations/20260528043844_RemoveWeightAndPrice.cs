using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWeightAndPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "TakeCargos");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "TakeCargos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Price",
                table: "TakeCargos",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "TakeCargos",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
