using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP_Foods2.Data.Migrations
{
    public partial class AddOrderStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Orders', N'Status') IS NULL " +
                "BEGIN " +
                "ALTER TABLE [Orders] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'\u041f\u0440\u0438\u0435\u0442\u0430'; " +
                "END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.Orders', N'Status') IS NOT NULL " +
                "BEGIN " +
                "ALTER TABLE [Orders] DROP COLUMN [Status]; " +
                "END");
        }
    }
}
