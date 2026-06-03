using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCliqPaymentReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentReceiptPath",
                table: "ManufacturingRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReviewNote",
                table: "ManufacturingRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentReceiptPath",
                table: "ManufacturingRequests");

            migrationBuilder.DropColumn(
                name: "PaymentReviewNote",
                table: "ManufacturingRequests");
        }
    }
}
