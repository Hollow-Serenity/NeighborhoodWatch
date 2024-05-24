using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuurtApplicatie.Migrations
{
    public partial class AlterImageTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "ImageData",
                table: "Images",
                type: "MediumBlob",
                maxLength: 2097152,
                nullable: false);
            
            migrationBuilder.RenameColumn(
                name: "ImageData",
                table: "Images",
                newName: "Data");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "Images",
                type: "longblob",
                nullable: true);
            
            migrationBuilder.RenameColumn(
                name: "Data",
                table: "Images",
                newName: "ImageData");
        }
    }
}
