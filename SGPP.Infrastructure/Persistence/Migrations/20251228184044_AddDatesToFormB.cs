using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGPP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDatesToFormB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEvaluacion",
                table: "EvaluacionesEmpresa",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinPractica",
                table: "EvaluacionesEmpresa",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicioPractica",
                table: "EvaluacionesEmpresa",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaEvaluacion",
                table: "EvaluacionesEmpresa");

            migrationBuilder.DropColumn(
                name: "FechaFinPractica",
                table: "EvaluacionesEmpresa");

            migrationBuilder.DropColumn(
                name: "FechaInicioPractica",
                table: "EvaluacionesEmpresa");
        }
    }
}
