using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketShow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHoldQuantityToTicketTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "hold_quantity",
                table: "ticket_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hold_quantity",
                table: "ticket_types");
        }
    }
}
