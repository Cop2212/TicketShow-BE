using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketShow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingModeToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ticketing_mode",
                table: "events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ticketing_mode",
                table: "events");
        }
    }
}
