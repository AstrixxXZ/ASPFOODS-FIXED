using ASP_Foods2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP_Foods2.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521125053_RenameProductImageUrlToImageLink")]
    public partial class RenameProductImageUrlToImageLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Products', 'ImageLink') IS NULL
                BEGIN
                    ALTER TABLE [Products] ADD [ImageLink] nvarchar(max) NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Products', 'ImageUrl') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE [Products] SET [ImageLink] = [ImageUrl] WHERE [ImageLink] IS NULL;');
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Products', 'ImageUrl') IS NOT NULL
                BEGIN
                    ALTER TABLE [Products] DROP COLUMN [ImageUrl];
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Products', 'ImageUrl') IS NULL
                BEGIN
                    ALTER TABLE [Products] ADD [ImageUrl] nvarchar(max) NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Products', 'ImageLink') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE [Products] SET [ImageUrl] = [ImageLink] WHERE [ImageUrl] IS NULL;');
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Products', 'ImageLink') IS NOT NULL
                BEGIN
                    ALTER TABLE [Products] DROP COLUMN [ImageLink];
                END
                """);
        }
    }
}
