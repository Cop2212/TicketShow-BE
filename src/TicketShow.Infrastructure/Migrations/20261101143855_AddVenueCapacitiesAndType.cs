using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketShow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueCapacitiesAndType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "seated_capacity",
                table: "venues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "standing_capacity",
                table: "venues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "venue_type",
                table: "venues",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "seated_capacity",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "standing_capacity",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "venue_type",
                table: "venues");
        }
    }
}
