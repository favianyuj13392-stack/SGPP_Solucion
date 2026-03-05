using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCiToTutorInstitucional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ci",
                table: "TutoresInstitucionales",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ci",
                table: "TutoresInstitucionales");
        }
    }
}
