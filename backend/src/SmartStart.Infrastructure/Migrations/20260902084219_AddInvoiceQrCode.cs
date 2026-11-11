using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceQrCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Invoices"
                SET "QrCode" = 'SSINV-' || lpad("Id"::text, 10, '0')
                WHERE "QrCode" IS NULL OR "QrCode" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_QrCode",
                table: "Invoices",
                column: "QrCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_QrCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Invoices");
        }
    }
}
