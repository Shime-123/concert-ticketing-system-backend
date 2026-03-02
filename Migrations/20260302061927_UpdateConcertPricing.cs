using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace concertbackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConcertPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Concerts",
                newName: "VipPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "RegularPrice",
                table: "Concerts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RegularStripeId",
                table: "Concerts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VipStripeId",
                table: "Concerts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegularPrice",
                table: "Concerts");

            migrationBuilder.DropColumn(
                name: "RegularStripeId",
                table: "Concerts");

            migrationBuilder.DropColumn(
                name: "VipStripeId",
                table: "Concerts");

            migrationBuilder.RenameColumn(
                name: "VipPrice",
                table: "Concerts",
                newName: "Price");
        }
    }
}
