using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackLink.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Trackings_Token",
                table: "Trackings",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trackings_Token",
                table: "Trackings");
        }
    }
}
