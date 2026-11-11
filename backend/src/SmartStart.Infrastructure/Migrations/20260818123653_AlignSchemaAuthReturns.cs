using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignSchemaAuthReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent repair for partially-applied migration + duplicate barcodes
            migrationBuilder.Sql("""
                ALTER TABLE "InvoiceItems" DROP CONSTRAINT IF EXISTS "FK_InvoiceItems_Products_ProductId";
                ALTER TABLE "Invoices" DROP CONSTRAINT IF EXISTS "FK_Invoices_Users_UserId";
                ALTER TABLE "Invoices" DROP CONSTRAINT IF EXISTS "FK_Invoices_Users_CashierId";
                ALTER TABLE "Returns" DROP CONSTRAINT IF EXISTS "FK_Returns_InvoiceItems_InvoiceItemId";

                DROP INDEX IF EXISTS "IX_Invoices_UserId";
                DROP INDEX IF EXISTS "IX_Users_Username";
                DROP INDEX IF EXISTS "IX_Products_Barcode";
                DROP INDEX IF EXISTS "IX_Products_Name";
                DROP INDEX IF EXISTS "IX_Invoices_CashierId";

                ALTER TABLE "Invoices" DROP COLUMN IF EXISTS "UserId";

                DO $$ BEGIN
                    DROP TYPE IF EXISTS user_role;
                EXCEPTION WHEN dependent_objects_still_exist THEN
                    NULL;
                END $$;

                ALTER TABLE "Users"
                    ALTER COLUMN "Role" TYPE character varying(20)
                    USING ("Role"::text);

                ALTER TABLE "Invoices"
                    ADD COLUMN IF NOT EXISTS "CashierId" integer NOT NULL DEFAULT 0;

                ALTER TABLE "Invoices"
                    ADD COLUMN IF NOT EXISTS "TotalAmount" numeric NOT NULL DEFAULT 0.0;

                -- Make duplicate barcodes unique before creating unique index
                UPDATE "Products" p
                SET "Barcode" = 'FIX-' || p."Id"::text
                WHERE p."Id" IN (
                    SELECT "Id" FROM (
                        SELECT "Id",
                               ROW_NUMBER() OVER (PARTITION BY "Barcode" ORDER BY "Id") AS rn
                        FROM "Products"
                    ) t
                    WHERE rn > 1
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Products_Barcode" ON "Products" ("Barcode");
                CREATE INDEX IF NOT EXISTS "IX_Products_Name" ON "Products" ("Name");
                CREATE INDEX IF NOT EXISTS "IX_Invoices_CashierId" ON "Invoices" ("CashierId");

                -- Remove invoices that still point to invalid cashier (from default 0)
                DELETE FROM "Returns" r
                USING "InvoiceItems" ii, "Invoices" inv
                WHERE r."InvoiceItemId" = ii."Id"
                  AND ii."InvoiceId" = inv."Id"
                  AND NOT EXISTS (SELECT 1 FROM "Users" u WHERE u."Id" = inv."CashierId");

                DELETE FROM "InvoiceItems" ii
                USING "Invoices" inv
                WHERE ii."InvoiceId" = inv."Id"
                  AND NOT EXISTS (SELECT 1 FROM "Users" u WHERE u."Id" = inv."CashierId");

                DELETE FROM "Invoices" inv
                WHERE NOT EXISTS (SELECT 1 FROM "Users" u WHERE u."Id" = inv."CashierId");

                ALTER TABLE "InvoiceItems"
                    DROP CONSTRAINT IF EXISTS "FK_InvoiceItems_Products_ProductId";
                ALTER TABLE "Invoices"
                    DROP CONSTRAINT IF EXISTS "FK_Invoices_Users_CashierId";
                ALTER TABLE "Returns"
                    DROP CONSTRAINT IF EXISTS "FK_Returns_InvoiceItems_InvoiceItemId";

                ALTER TABLE "InvoiceItems"
                    ADD CONSTRAINT "FK_InvoiceItems_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id")
                    ON DELETE RESTRICT;

                ALTER TABLE "Invoices"
                    ADD CONSTRAINT "FK_Invoices_Users_CashierId"
                    FOREIGN KEY ("CashierId") REFERENCES "Users" ("Id")
                    ON DELETE RESTRICT;

                ALTER TABLE "Returns"
                    ADD CONSTRAINT "FK_Returns_InvoiceItems_InvoiceItemId"
                    FOREIGN KEY ("InvoiceItemId") REFERENCES "InvoiceItems" ("Id")
                    ON DELETE RESTRICT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "InvoiceItems" DROP CONSTRAINT IF EXISTS "FK_InvoiceItems_Products_ProductId";
                ALTER TABLE "Invoices" DROP CONSTRAINT IF EXISTS "FK_Invoices_Users_CashierId";
                ALTER TABLE "Returns" DROP CONSTRAINT IF EXISTS "FK_Returns_InvoiceItems_InvoiceItemId";
                DROP INDEX IF EXISTS "IX_Users_Username";
                DROP INDEX IF EXISTS "IX_Products_Barcode";
                DROP INDEX IF EXISTS "IX_Products_Name";
                DROP INDEX IF EXISTS "IX_Invoices_CashierId";
                ALTER TABLE "Invoices" DROP COLUMN IF EXISTS "CashierId";
                ALTER TABLE "Invoices" DROP COLUMN IF EXISTS "TotalAmount";
                """);
        }
    }
}
