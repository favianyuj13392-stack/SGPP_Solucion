using SGPP.Domain.Common;
using SGPP.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class Asignacion : AuditableEntity
{
    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public int TutorInstitucionalId { get; set; }
    public TutorInstitucional TutorInstitucional { get; set; } = null!;

    public EstadoAsignacion Estado { get; set; } = EstadoAsignacion.Pendiente;

    // The Professor responsible at the moment of assignment
    public string? TutorAcademicoId { get; set; }
    [ForeignKey("TutorAcademicoId")]
    public ApplicationUser? TutorAcademico { get; set; }

    // Nav properties for forms (to be added)
    public FormularioB_Empresa? FormularioB { get; set; }
    public FormularioA_Estudiante? FormularioA { get; set; }
}
