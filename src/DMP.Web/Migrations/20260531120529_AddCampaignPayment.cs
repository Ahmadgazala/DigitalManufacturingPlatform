using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "CampaignParticipants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "CampaignParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "CampaignParticipants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "CampaignParticipants",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "CampaignParticipants");
        }
    }
}
