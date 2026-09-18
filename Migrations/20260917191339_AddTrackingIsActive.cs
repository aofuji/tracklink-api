using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackLink.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Trackings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Trackings");
        }
    }
}
