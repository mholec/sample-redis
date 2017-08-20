using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RedisEshop.Migrations
{
    public partial class PostalCodesPK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PostalCodes",
                table: "PostalCodes");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PostalCodes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostalCodes",
                table: "PostalCodes",
                columns: new[] { "Code", "Name" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PostalCodes",
                table: "PostalCodes");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PostalCodes",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostalCodes",
                table: "PostalCodes",
                column: "Code");
        }
    }
}
