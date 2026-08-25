using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestIsApproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "ManufacturingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "ManufacturingRequests");
        }
    }
}
