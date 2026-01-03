using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTrace.DataEf.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Products");

            migrationBuilder.EnsureSchema(
                name: "Logging");

            migrationBuilder.EnsureSchema(
                name: "User");

            migrationBuilder.CreateTable(
                name: "tbLogs",
                schema: "Logging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Originator = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbProduct",
                schema: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Names = table.Column<string[]>(type: "text[]", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    ProductDescription = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbProduct", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbUser",
                schema: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbEshopPrice",
                schema: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbEshopPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbEshopPrice_tbProduct_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Products",
                        principalTable: "tbProduct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbListing",
                schema: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RetrievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    CreatorName = table.Column<string>(type: "text", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbListing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbListing_tbProduct_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Products",
                        principalTable: "tbProduct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbSearchRequest",
                schema: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SoftUpLimit = table.Column<int>(type: "integer", nullable: true),
                    SoftDownLimit = table.Column<int>(type: "integer", nullable: true),
                    HardUpLimit = table.Column<int>(type: "integer", nullable: true),
                    HardDownLimit = table.Column<int>(type: "integer", nullable: true),
                    IntervalInSeconds = table.Column<long>(type: "bigint", nullable: false),
                    FleaMarketWebEnum = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbSearchRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbSearchRequest_tbProduct_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Products",
                        principalTable: "tbProduct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbSearchRequest_tbUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "User",
                        principalTable: "tbUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbEshopPrice_ProductId",
                schema: "Products",
                table: "tbEshopPrice",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbListing_ProductId",
                schema: "Products",
                table: "tbListing",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbSearchRequest_ProductId",
                schema: "User",
                table: "tbSearchRequest",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbSearchRequest_UserId",
                schema: "User",
                table: "tbSearchRequest",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbEshopPrice",
                schema: "Products");

            migrationBuilder.DropTable(
                name: "tbListing",
                schema: "Products");

            migrationBuilder.DropTable(
                name: "tbLogs",
                schema: "Logging");

            migrationBuilder.DropTable(
                name: "tbSearchRequest",
                schema: "User");

            migrationBuilder.DropTable(
                name: "tbProduct",
                schema: "Products");

            migrationBuilder.DropTable(
                name: "tbUser",
                schema: "User");
        }
    }
}
