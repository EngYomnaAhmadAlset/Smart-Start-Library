using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowZeroWholesalePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ProfitMarginPercent",
                table: "Products",
                type: "numeric",
                nullable: false,
                computedColumnSql: "COALESCE(ROUND(((\"SellingPrice\" - \"WholesalePrice\") / NULLIF(\"WholesalePrice\", 0)) * 100, 2), 0)",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldComputedColumnSql: "ROUND(((\"SellingPrice\" - \"WholesalePrice\") / NULLIF(\"WholesalePrice\", 0)) * 100, 2)",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ProfitMarginPercent",
                table: "Products",
                type: "numeric",
                nullable: false,
                computedColumnSql: "ROUND(((\"SellingPrice\" - \"WholesalePrice\") / NULLIF(\"WholesalePrice\", 0)) * 100, 2)",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldComputedColumnSql: "COALESCE(ROUND(((\"SellingPrice\" - \"WholesalePrice\") / NULLIF(\"WholesalePrice\", 0)) * 100, 2), 0)",
                oldStored: true);
        }
    }
}
