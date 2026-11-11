using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTrace.DataEf.Migrations
{
    /// <inheritdoc />
    public partial class AddListingProductUrlUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbListing_ProductId",
                schema: "Products",
                table: "tbListing");

            migrationBuilder.CreateIndex(
                name: "IX_tbListing_ProductId_Url",
                schema: "Products",
                table: "tbListing",
                columns: new[] { "ProductId", "Url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbListing_ProductId_Url",
                schema: "Products",
                table: "tbListing");

            migrationBuilder.CreateIndex(
                name: "IX_tbListing_ProductId",
                schema: "Products",
                table: "tbListing",
                column: "ProductId");
        }
    }
}
