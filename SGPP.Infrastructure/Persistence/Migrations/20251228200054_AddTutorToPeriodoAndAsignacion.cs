using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorToPeriodoAndAsignacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TutorAcademicoId",
                table: "Periodos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorAcademicoId",
                table: "Asignaciones",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Periodos_TutorAcademicoId",
                table: "Periodos",
                column: "TutorAcademicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_TutorAcademicoId",
                table: "Asignaciones",
                column: "TutorAcademicoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_AspNetUsers_TutorAcademicoId",
                table: "Asignaciones",
                column: "TutorAcademicoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Periodos_AspNetUsers_TutorAcademicoId",
                table: "Periodos",
                column: "TutorAcademicoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_AspNetUsers_TutorAcademicoId",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Periodos_AspNetUsers_TutorAcademicoId",
                table: "Periodos");

            migrationBuilder.DropIndex(
                name: "IX_Periodos_TutorAcademicoId",
                table: "Periodos");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_TutorAcademicoId",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "TutorAcademicoId",
                table: "Periodos");

            migrationBuilder.DropColumn(
                name: "TutorAcademicoId",
                table: "Asignaciones");
        }
    }
}
