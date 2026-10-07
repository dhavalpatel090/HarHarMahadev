using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Khaparwada.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonConsentAndReviewNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsentNote",
                table: "People",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsentRecordedAt",
                table: "People",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "People",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentNote",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ConsentRecordedAt",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "People");
        }
    }
}
