using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace concertbackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Concerts",
                columns: table => new
                {
                    ConcertId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConcertTitle = table.Column<string>(type: "text", nullable: true),
                    Venue = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    RegularPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RegularStripeId = table.Column<string>(type: "text", nullable: true),
                    VipPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VipStripeId = table.Column<string>(type: "text", nullable: true),
                    IsSoldOut = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concerts", x => x.ConcertId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    ResetCode = table.Column<string>(type: "text", nullable: true),
                    ResetCodeExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    PaymentId = table.Column<string>(type: "text", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: true),
                    TicketType = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Purchases_Users_UserEmail",
                        column: x => x.UserEmail,
                        principalTable: "Users",
                        principalColumn: "Email");
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ConcertId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_Tickets_Concerts_ConcertId",
                        column: x => x.ConcertId,
                        principalTable: "Concerts",
                        principalColumn: "ConcertId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tickets_Purchases_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Purchases",
                        principalColumn: "PaymentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserEmail",
                table: "Purchases",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ConcertId",
                table: "Tickets",
                column: "ConcertId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PaymentId",
                table: "Tickets",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Concerts");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
