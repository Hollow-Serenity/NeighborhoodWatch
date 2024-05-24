using Microsoft.EntityFrameworkCore.Migrations;

namespace BuurtApplicatie.Migrations
{
    public partial class cascadeDeleteDeletedPosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeletedPosts_AspNetUsers_UserId",
                table: "DeletedPosts");

            migrationBuilder.AddForeignKey(
                name: "FK_DeletedPosts_AspNetUsers_UserId",
                table: "DeletedPosts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeletedPosts_AspNetUsers_UserId",
                table: "DeletedPosts");

            migrationBuilder.AddForeignKey(
                name: "FK_DeletedPosts_AspNetUsers_UserId",
                table: "DeletedPosts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
