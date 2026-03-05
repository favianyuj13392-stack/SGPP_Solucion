using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFormBFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "FormularioB_Detalles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cumplimiento",
                table: "EvaluacionEmpresa_Tareas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "FormularioB_Detalles");

            migrationBuilder.DropColumn(
                name: "Cumplimiento",
                table: "EvaluacionEmpresa_Tareas");
        }
    }
}
