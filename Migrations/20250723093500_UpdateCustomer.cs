using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Products_ProductNavId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_ProductNavId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "ProductNavId",
                table: "Carts");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Customers",
                newName: "FullName");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_Product",
                table: "Carts",
                column: "Product");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Products_Product",
                table: "Carts",
                column: "Product",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Products_Product",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_Product",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Customers",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "ProductNavId",
                table: "Carts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_ProductNavId",
                table: "Carts",
                column: "ProductNavId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Products_ProductNavId",
                table: "Carts",
                column: "ProductNavId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
