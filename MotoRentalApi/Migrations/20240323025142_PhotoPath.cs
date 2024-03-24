using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class PhotoPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverLicensePhotoPath",
                table: "Deliverers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverLicensePhotoPath",
                table: "Deliverers");
        }
    }
}
