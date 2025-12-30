using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyHoursTarget",
                table: "AspNetUsers",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DefaultStartTime",
                table: "AspNetUsers",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyHoursTarget",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultStartTime",
                table: "AspNetUsers");
        }
    }
}
