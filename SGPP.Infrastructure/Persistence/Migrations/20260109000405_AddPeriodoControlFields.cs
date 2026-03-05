using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodoControlFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FechaCierre",
                table: "Periodos",
                newName: "FechaInicioEvaluacion");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Periodos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
                table: "Periodos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinEvaluacion",
                table: "Periodos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Periodos");

            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "Periodos");

            migrationBuilder.DropColumn(
                name: "FechaFinEvaluacion",
                table: "Periodos");

            migrationBuilder.RenameColumn(
                name: "FechaInicioEvaluacion",
                table: "Periodos",
                newName: "FechaCierre");
        }
    }
}
