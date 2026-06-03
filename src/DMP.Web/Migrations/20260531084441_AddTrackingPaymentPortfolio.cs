using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingPaymentPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "ManufacturingRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "ManufacturingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "ManufacturingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "ManufacturingRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrackingStage",
                table: "ManufacturingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderUpdates_ManufacturingRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ManufacturingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManufacturerId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioItems_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderUpdates_RequestId",
                table: "OrderUpdates",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_ManufacturerId",
                table: "PortfolioItems",
                column: "ManufacturerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderUpdates");

            migrationBuilder.DropTable(
                name: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "ManufacturingRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "ManufacturingRequests");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "ManufacturingRequests");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "ManufacturingRequests");

            migrationBuilder.DropColumn(
                name: "TrackingStage",
                table: "ManufacturingRequests");
        }
    }
}
