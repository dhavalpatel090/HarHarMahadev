using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Khaparwada.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryImageUploadedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedByUserId",
                table: "GalleryImages",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryImages_UploadedByUserId",
                table: "GalleryImages",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GalleryImages_AspNetUsers_UploadedByUserId",
                table: "GalleryImages",
                column: "UploadedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GalleryImages_AspNetUsers_UploadedByUserId",
                table: "GalleryImages");

            migrationBuilder.DropIndex(
                name: "IX_GalleryImages_UploadedByUserId",
                table: "GalleryImages");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "GalleryImages");
        }
    }
}
