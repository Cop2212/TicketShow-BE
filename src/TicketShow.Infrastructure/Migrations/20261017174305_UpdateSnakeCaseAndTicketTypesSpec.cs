using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketShow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSnakeCaseAndTicketTypesSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckInLogs_Tickets_TicketId",
                table: "CheckInLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInLogs_Users_ScannedByUserId",
                table: "CheckInLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Users_OrganizerId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Events_EventId",
                table: "Seats");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_TicketTiers_TicketTierId",
                table: "Seats");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Events_EventId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Orders_OrderId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Seats_SeatId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TicketTiers_TicketTierId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_CheckedByStaffId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketTiers_Events_EventId",
                table: "TicketTiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_EventId",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Seats",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Events",
                table: "Events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TicketTiers",
                table: "TicketTiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CheckInLogs",
                table: "CheckInLogs");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "tickets");

            migrationBuilder.RenameTable(
                name: "Seats",
                newName: "seats");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "orders");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "events");

            migrationBuilder.RenameTable(
                name: "TicketTiers",
                newName: "ticket_types");

            migrationBuilder.RenameTable(
                name: "CheckInLogs",
                newName: "check_in_logs");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenExpiryTime",
                table: "users",
                newName: "refresh_token_expiry_time");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "users",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "users",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "users",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "users",
                newName: "full_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "users",
                newName: "ix_users_email");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "tickets",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tickets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "tickets",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TicketTierId",
                table: "tickets",
                newName: "ticket_tier_id");

            migrationBuilder.RenameColumn(
                name: "TicketCode",
                table: "tickets",
                newName: "ticket_code");

            migrationBuilder.RenameColumn(
                name: "SeatId",
                table: "tickets",
                newName: "seat_id");

            migrationBuilder.RenameColumn(
                name: "QrCodeSignature",
                table: "tickets",
                newName: "qr_code_signature");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "tickets",
                newName: "order_id");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "tickets",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "tickets",
                newName: "event_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tickets",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CheckedInAt",
                table: "tickets",
                newName: "checked_in_at");

            migrationBuilder.RenameColumn(
                name: "CheckedByStaffId",
                table: "tickets",
                newName: "checked_by_staff_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_TicketTierId",
                table: "tickets",
                newName: "ix_tickets_ticket_tier_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_TicketCode",
                table: "tickets",
                newName: "ix_tickets_ticket_code");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_SeatId",
                table: "tickets",
                newName: "ix_tickets_seat_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_OrderId",
                table: "tickets",
                newName: "ix_tickets_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_CheckedByStaffId",
                table: "tickets",
                newName: "ix_tickets_checked_by_staff_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "seats",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "seats",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "seats",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TicketTierId",
                table: "seats",
                newName: "ticket_tier_id");

            migrationBuilder.RenameColumn(
                name: "SeatNumber",
                table: "seats",
                newName: "seat_number");

            migrationBuilder.RenameColumn(
                name: "RowName",
                table: "seats",
                newName: "row_name");

            migrationBuilder.RenameColumn(
                name: "LockedUntil",
                table: "seats",
                newName: "locked_until");

            migrationBuilder.RenameColumn(
                name: "LockedByUserId",
                table: "seats",
                newName: "locked_by_user_id");

            migrationBuilder.RenameColumn(
                name: "LockedBySessionId",
                table: "seats",
                newName: "locked_by_session_id");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "seats",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "seats",
                newName: "event_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "seats",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Seats_TicketTierId",
                table: "seats",
                newName: "ix_seats_ticket_tier_id");

            migrationBuilder.RenameIndex(
                name: "IX_Seats_EventId_RowName_SeatNumber",
                table: "seats",
                newName: "ix_seats_event_id_row_name_seat_number");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "orders",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "orders",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "orders",
                newName: "transaction_id");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "orders",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "orders",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "PaymentGateway",
                table: "orders",
                newName: "payment_gateway");

            migrationBuilder.RenameColumn(
                name: "OrderCode",
                table: "orders",
                newName: "order_code");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "orders",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "orders",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CustomerPhone",
                table: "orders",
                newName: "customer_phone");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "orders",
                newName: "customer_name");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "orders",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CustomerEmail",
                table: "orders",
                newName: "customer_email");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "orders",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderCode",
                table: "orders",
                newName: "ix_orders_order_code");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "events",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "events",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "events",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "events",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VenueName",
                table: "events",
                newName: "venue_name");

            migrationBuilder.RenameColumn(
                name: "VenueAddress",
                table: "events",
                newName: "venue_address");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "events",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "events",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "SalesStartTime",
                table: "events",
                newName: "sales_start_time");

            migrationBuilder.RenameColumn(
                name: "SalesEndTime",
                table: "events",
                newName: "sales_end_time");

            migrationBuilder.RenameColumn(
                name: "OrganizerId",
                table: "events",
                newName: "organizer_id");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "events",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "events",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "events",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BannerUrl",
                table: "events",
                newName: "banner_url");

            migrationBuilder.RenameIndex(
                name: "IX_Events_Slug",
                table: "events",
                newName: "ix_events_slug");

            migrationBuilder.RenameIndex(
                name: "IX_Events_OrganizerId",
                table: "events",
                newName: "ix_events_organizer_id");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ticket_types",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ticket_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ticket_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ticket_types",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalQuantity",
                table: "ticket_types",
                newName: "total_quantity");

            migrationBuilder.RenameColumn(
                name: "SoldQuantity",
                table: "ticket_types",
                newName: "sold_quantity");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "ticket_types",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "ticket_types",
                newName: "event_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ticket_types",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ColorHex",
                table: "ticket_types",
                newName: "color_hex");

            migrationBuilder.RenameIndex(
                name: "IX_TicketTiers_EventId",
                table: "ticket_types",
                newName: "ix_ticket_types_event_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "check_in_logs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "check_in_logs",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "check_in_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "check_in_logs",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TicketId",
                table: "check_in_logs",
                newName: "ticket_id");

            migrationBuilder.RenameColumn(
                name: "ScannedByUserId",
                table: "check_in_logs",
                newName: "scanned_by_user_id");

            migrationBuilder.RenameColumn(
                name: "ScannedAt",
                table: "check_in_logs",
                newName: "scanned_at");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "check_in_logs",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "check_in_logs",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_CheckInLogs_TicketId",
                table: "check_in_logs",
                newName: "ix_check_in_logs_ticket_id");

            migrationBuilder.RenameIndex(
                name: "IX_CheckInLogs_ScannedByUserId",
                table: "check_in_logs",
                newName: "ix_check_in_logs_scanned_by_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tickets",
                table: "tickets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seats",
                table: "seats",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_orders",
                table: "orders",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_events",
                table: "events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ticket_types",
                table: "ticket_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_check_in_logs",
                table: "check_in_logs",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_event_id_status",
                table: "tickets",
                columns: new[] { "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_seats_event_id_status",
                table: "seats",
                columns: new[] { "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id_payment_status",
                table: "orders",
                columns: new[] { "customer_id", "payment_status" });

            migrationBuilder.AddForeignKey(
                name: "fk_check_in_logs_tickets_ticket_id",
                table: "check_in_logs",
                column: "ticket_id",
                principalTable: "tickets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_check_in_logs_users_scanned_by_user_id",
                table: "check_in_logs",
                column: "scanned_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_users_organizer_id",
                table: "events",
                column: "organizer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_users_customer_id",
                table: "orders",
                column: "customer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_seats_events_event_id",
                table: "seats",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seats_ticket_tiers_ticket_tier_id",
                table: "seats",
                column: "ticket_tier_id",
                principalTable: "ticket_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_types_events_event_id",
                table: "ticket_types",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_events_event_id",
                table: "tickets",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_orders_order_id",
                table: "tickets",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_seats_seat_id",
                table: "tickets",
                column: "seat_id",
                principalTable: "seats",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_ticket_tiers_ticket_tier_id",
                table: "tickets",
                column: "ticket_tier_id",
                principalTable: "ticket_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_users_checked_by_staff_id",
                table: "tickets",
                column: "checked_by_staff_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_check_in_logs_tickets_ticket_id",
                table: "check_in_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_check_in_logs_users_scanned_by_user_id",
                table: "check_in_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_events_users_organizer_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_users_customer_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_seats_events_event_id",
                table: "seats");

            migrationBuilder.DropForeignKey(
                name: "fk_seats_ticket_tiers_ticket_tier_id",
                table: "seats");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_types_events_event_id",
                table: "ticket_types");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_events_event_id",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_orders_order_id",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_seats_seat_id",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_ticket_tiers_ticket_tier_id",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_users_checked_by_staff_id",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tickets",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "ix_tickets_event_id_status",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seats",
                table: "seats");

            migrationBuilder.DropIndex(
                name: "ix_seats_event_id_status",
                table: "seats");

            migrationBuilder.DropPrimaryKey(
                name: "pk_orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_customer_id_payment_status",
                table: "orders");

            migrationBuilder.DropPrimaryKey(
                name: "pk_events",
                table: "events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_ticket_types",
                table: "ticket_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_check_in_logs",
                table: "check_in_logs");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "tickets",
                newName: "Tickets");

            migrationBuilder.RenameTable(
                name: "seats",
                newName: "Seats");

            migrationBuilder.RenameTable(
                name: "orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "events",
                newName: "Events");

            migrationBuilder.RenameTable(
                name: "ticket_types",
                newName: "TicketTiers");

            migrationBuilder.RenameTable(
                name: "check_in_logs",
                newName: "CheckInLogs");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "refresh_token_expiry_time",
                table: "Users",
                newName: "RefreshTokenExpiryTime");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "Users",
                newName: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "Users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Users",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "Users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_users_email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Tickets",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tickets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Tickets",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ticket_tier_id",
                table: "Tickets",
                newName: "TicketTierId");

            migrationBuilder.RenameColumn(
                name: "ticket_code",
                table: "Tickets",
                newName: "TicketCode");

            migrationBuilder.RenameColumn(
                name: "seat_id",
                table: "Tickets",
                newName: "SeatId");

            migrationBuilder.RenameColumn(
                name: "qr_code_signature",
                table: "Tickets",
                newName: "QrCodeSignature");

            migrationBuilder.RenameColumn(
                name: "order_id",
                table: "Tickets",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Tickets",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "Tickets",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Tickets",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "checked_in_at",
                table: "Tickets",
                newName: "CheckedInAt");

            migrationBuilder.RenameColumn(
                name: "checked_by_staff_id",
                table: "Tickets",
                newName: "CheckedByStaffId");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_ticket_tier_id",
                table: "Tickets",
                newName: "IX_Tickets_TicketTierId");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_ticket_code",
                table: "Tickets",
                newName: "IX_Tickets_TicketCode");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_seat_id",
                table: "Tickets",
                newName: "IX_Tickets_SeatId");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_order_id",
                table: "Tickets",
                newName: "IX_Tickets_OrderId");

            migrationBuilder.RenameIndex(
                name: "ix_tickets_checked_by_staff_id",
                table: "Tickets",
                newName: "IX_Tickets_CheckedByStaffId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Seats",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Seats",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Seats",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ticket_tier_id",
                table: "Seats",
                newName: "TicketTierId");

            migrationBuilder.RenameColumn(
                name: "seat_number",
                table: "Seats",
                newName: "SeatNumber");

            migrationBuilder.RenameColumn(
                name: "row_name",
                table: "Seats",
                newName: "RowName");

            migrationBuilder.RenameColumn(
                name: "locked_until",
                table: "Seats",
                newName: "LockedUntil");

            migrationBuilder.RenameColumn(
                name: "locked_by_user_id",
                table: "Seats",
                newName: "LockedByUserId");

            migrationBuilder.RenameColumn(
                name: "locked_by_session_id",
                table: "Seats",
                newName: "LockedBySessionId");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Seats",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "Seats",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Seats",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seats_ticket_tier_id",
                table: "Seats",
                newName: "IX_Seats_TicketTierId");

            migrationBuilder.RenameIndex(
                name: "ix_seats_event_id_row_name_seat_number",
                table: "Seats",
                newName: "IX_Seats_EventId_RowName_SeatNumber");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Orders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Orders",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "transaction_id",
                table: "Orders",
                newName: "TransactionId");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "Orders",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "payment_gateway",
                table: "Orders",
                newName: "PaymentGateway");

            migrationBuilder.RenameColumn(
                name: "order_code",
                table: "Orders",
                newName: "OrderCode");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Orders",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "Orders",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "customer_phone",
                table: "Orders",
                newName: "CustomerPhone");

            migrationBuilder.RenameColumn(
                name: "customer_name",
                table: "Orders",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "Orders",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "customer_email",
                table: "Orders",
                newName: "CustomerEmail");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Orders",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_orders_order_code",
                table: "Orders",
                newName: "IX_Orders_OrderCode");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Events",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Events",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "Events",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Events",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Events",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "venue_name",
                table: "Events",
                newName: "VenueName");

            migrationBuilder.RenameColumn(
                name: "venue_address",
                table: "Events",
                newName: "VenueAddress");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Events",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "Events",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "sales_start_time",
                table: "Events",
                newName: "SalesStartTime");

            migrationBuilder.RenameColumn(
                name: "sales_end_time",
                table: "Events",
                newName: "SalesEndTime");

            migrationBuilder.RenameColumn(
                name: "organizer_id",
                table: "Events",
                newName: "OrganizerId");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Events",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "Events",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Events",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "banner_url",
                table: "Events",
                newName: "BannerUrl");

            migrationBuilder.RenameIndex(
                name: "ix_events_slug",
                table: "Events",
                newName: "IX_Events_Slug");

            migrationBuilder.RenameIndex(
                name: "ix_events_organizer_id",
                table: "Events",
                newName: "IX_Events_OrganizerId");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "TicketTiers",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "TicketTiers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TicketTiers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "TicketTiers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_quantity",
                table: "TicketTiers",
                newName: "TotalQuantity");

            migrationBuilder.RenameColumn(
                name: "sold_quantity",
                table: "TicketTiers",
                newName: "SoldQuantity");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "TicketTiers",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "TicketTiers",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "TicketTiers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "color_hex",
                table: "TicketTiers",
                newName: "ColorHex");

            migrationBuilder.RenameIndex(
                name: "ix_ticket_types_event_id",
                table: "TicketTiers",
                newName: "IX_TicketTiers_EventId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "CheckInLogs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "CheckInLogs",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CheckInLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "CheckInLogs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ticket_id",
                table: "CheckInLogs",
                newName: "TicketId");

            migrationBuilder.RenameColumn(
                name: "scanned_by_user_id",
                table: "CheckInLogs",
                newName: "ScannedByUserId");

            migrationBuilder.RenameColumn(
                name: "scanned_at",
                table: "CheckInLogs",
                newName: "ScannedAt");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "CheckInLogs",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "CheckInLogs",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_check_in_logs_ticket_id",
                table: "CheckInLogs",
                newName: "IX_CheckInLogs_TicketId");

            migrationBuilder.RenameIndex(
                name: "ix_check_in_logs_scanned_by_user_id",
                table: "CheckInLogs",
                newName: "IX_CheckInLogs_ScannedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Seats",
                table: "Seats",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Events",
                table: "Events",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TicketTiers",
                table: "TicketTiers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CheckInLogs",
                table: "CheckInLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EventId",
                table: "Tickets",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInLogs_Tickets_TicketId",
                table: "CheckInLogs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInLogs_Users_ScannedByUserId",
                table: "CheckInLogs",
                column: "ScannedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Users_OrganizerId",
                table: "Events",
                column: "OrganizerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Events_EventId",
                table: "Seats",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_TicketTiers_TicketTierId",
                table: "Seats",
                column: "TicketTierId",
                principalTable: "TicketTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Events_EventId",
                table: "Tickets",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Orders_OrderId",
                table: "Tickets",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Seats_SeatId",
                table: "Tickets",
                column: "SeatId",
                principalTable: "Seats",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TicketTiers_TicketTierId",
                table: "Tickets",
                column: "TicketTierId",
                principalTable: "TicketTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_CheckedByStaffId",
                table: "Tickets",
                column: "CheckedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketTiers_Events_EventId",
                table: "TicketTiers",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
