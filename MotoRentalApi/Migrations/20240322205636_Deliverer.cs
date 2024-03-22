using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class Deliverer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deliverers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CNPJ = table.Column<string>(type: "text", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DriverLicenseNumber = table.Column<string>(type: "text", nullable: false),
                    DriverLicenseType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliverers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deliverers_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Motos_Plate",
                table: "Motos",
                column: "Plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliverers_CNPJ",
                table: "Deliverers",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliverers_DriverLicenseNumber",
                table: "Deliverers",
                column: "DriverLicenseNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deliverers");

            migrationBuilder.DropIndex(
                name: "IX_Motos_Plate",
                table: "Motos");
        }
    }
}
