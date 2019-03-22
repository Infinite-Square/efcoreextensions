using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCore.Extensions.SqlServer.UnitTests.Migrations
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Persons",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Groups",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "GroupPersons",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "GroupPersons");
        }
    }
}
