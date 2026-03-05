using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToFormARetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaAsignada",
                table: "EvaluacionesEstudiante",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEvaluacion",
                table: "EvaluacionesEstudiante",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
                table: "EvaluacionesEstudiante",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "EvaluacionesEstudiante",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HorasTrabajadas",
                table: "EvaluacionesEstudiante",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaAsignada",
                table: "EvaluacionesEstudiante");

            migrationBuilder.DropColumn(
                name: "FechaEvaluacion",
                table: "EvaluacionesEstudiante");

            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "EvaluacionesEstudiante");

            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "EvaluacionesEstudiante");

            migrationBuilder.DropColumn(
                name: "HorasTrabajadas",
                table: "EvaluacionesEstudiante");
        }
    }
}
