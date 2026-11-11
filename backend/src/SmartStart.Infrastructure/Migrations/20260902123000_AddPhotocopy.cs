using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SmartStart.Infrastructure.Data;

#nullable disable

namespace SmartStart.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260902123000_AddPhotocopy")]
    public partial class AddPhotocopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAccessPhotocopy",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PhotocopySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaintenanceCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ElectricityCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotocopySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotocopyPaperTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    WholesalePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotocopyPaperTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotocopyInkLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WholesalePerPage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SellingPerPage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingPages = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotocopyInkLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotocopySales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CashierId = table.Column<int>(type: "integer", nullable: false),
                    PaperTypeId = table.Column<int>(type: "integer", nullable: false),
                    PaperName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InkLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    PaperWholesalePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PaperSellingPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InkWholesalePerPage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InkSellingPerPage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MaintenanceCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ElectricityCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProfitAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProfitPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotocopySales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotocopySales_Users_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhotocopySales_PhotocopyPaperTypes_PaperTypeId",
                        column: x => x.PaperTypeId,
                        principalTable: "PhotocopyPaperTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotocopyPaperTypes_Name",
                table: "PhotocopyPaperTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotocopyInkLevels_Level",
                table: "PhotocopyInkLevels",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotocopySales_SaleNumber",
                table: "PhotocopySales",
                column: "SaleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotocopySales_CashierId",
                table: "PhotocopySales",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotocopySales_PaperTypeId",
                table: "PhotocopySales",
                column: "PaperTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PhotocopySales");
            migrationBuilder.DropTable(name: "PhotocopyInkLevels");
            migrationBuilder.DropTable(name: "PhotocopyPaperTypes");
            migrationBuilder.DropTable(name: "PhotocopySettings");

            migrationBuilder.DropColumn(
                name: "CanAccessPhotocopy",
                table: "Users");
        }
    }
}
