using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuurtApplicatie.Migrations
{
    public partial class AnonymousTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Posts");

            migrationBuilder.CreateTable(
                name: "AnonUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnonPosts",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Content = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    AnonUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonPosts_AnonUsers_AnonUserId",
                        column: x => x.AnonUserId,
                        principalTable: "AnonUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AnonImages",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Data = table.Column<byte[]>(nullable: true),
                    PostId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonImages_AnonPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "AnonPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonImages_PostId",
                table: "AnonImages",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnonPosts_AnonUserId",
                table: "AnonPosts",
                column: "AnonUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnonUsers_UserId",
                table: "AnonUsers",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonImages");

            migrationBuilder.DropTable(
                name: "AnonPosts");

            migrationBuilder.DropTable(
                name: "AnonUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Posts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
