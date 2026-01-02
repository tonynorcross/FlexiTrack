using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkingDaysAndBankHolidaySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankHolidayRegion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HoursPerDay",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WorkingDays",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankHolidayRegion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HoursPerDay",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                table: "AspNetUsers");
        }
    }
}
