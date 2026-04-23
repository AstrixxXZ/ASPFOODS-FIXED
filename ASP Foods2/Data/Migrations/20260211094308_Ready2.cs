using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP_Foods2.Data.Migrations
{
    /// <inheritdoc />
    public partial class Ready2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_TypeProducts_TypeProductsId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TypeProduct",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "TypeProductsId",
                table: "Products",
                newName: "TypeProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_TypeProductsId",
                table: "Products",
                newName: "IX_Products_TypeProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TypeProducts_TypeProductId",
                table: "Products",
                column: "TypeProductId",
                principalTable: "TypeProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_TypeProducts_TypeProductId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "TypeProductId",
                table: "Products",
                newName: "TypeProductsId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_TypeProductId",
                table: "Products",
                newName: "IX_Products_TypeProductsId");

            migrationBuilder.AddColumn<int>(
                name: "TypeProduct",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TypeProducts_TypeProductsId",
                table: "Products",
                column: "TypeProductsId",
                principalTable: "TypeProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
