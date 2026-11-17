using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketShow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStandingTicketsAndOrderSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_standing",
                table: "ticket_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "event_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reserved_items_json",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_event_id",
                table: "orders",
                column: "event_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_events_event_id",
                table: "orders",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_events_event_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_event_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_standing",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "reserved_items_json",
                table: "orders");
        }
    }
}
