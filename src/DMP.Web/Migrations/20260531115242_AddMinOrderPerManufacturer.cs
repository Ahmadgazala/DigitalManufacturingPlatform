using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMinOrderPerManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinOrderPerManufacturer",
                table: "GroupBuyingCampaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinOrderPerManufacturer",
                table: "GroupBuyingCampaigns");
        }
    }
}
