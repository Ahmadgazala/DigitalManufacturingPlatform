using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupBuyingDepositPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "CampaignParticipants",
                newName: "FullPaidAt");

            migrationBuilder.RenameColumn(
                name: "PaidAmount",
                table: "CampaignParticipants",
                newName: "RemainingAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "CampaignParticipants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositPaidAt",
                table: "CampaignParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositReceiptPath",
                table: "CampaignParticipants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReviewNote",
                table: "CampaignParticipants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemainingReceiptPath",
                table: "CampaignParticipants",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "DepositPaidAt",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "DepositReceiptPath",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "PaymentReviewNote",
                table: "CampaignParticipants");

            migrationBuilder.DropColumn(
                name: "RemainingReceiptPath",
                table: "CampaignParticipants");

            migrationBuilder.RenameColumn(
                name: "RemainingAmount",
                table: "CampaignParticipants",
                newName: "PaidAmount");

            migrationBuilder.RenameColumn(
                name: "FullPaidAt",
                table: "CampaignParticipants",
                newName: "PaidAt");
        }
    }
}
