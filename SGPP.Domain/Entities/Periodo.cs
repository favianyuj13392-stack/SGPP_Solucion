using SGPP.Domain.Common;

namespace SGPP.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using SGPP.Domain.Common;

public class Periodo : BaseEntity
{
    public string CodigoGestion { get; set; } = string.Empty; // Nombre del periodo (Ej: "I-2025")
    
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; } // Anteriormente FechaCierre

    // Rango de fechas habilitado para evaluaciones (Form A, B, C)
    public DateTime FechaInicioEvaluacion { get; set; }
    public DateTime FechaFinEvaluacion { get; set; }

    public bool Activo { get; set; } = false;
    public bool PermitirExtemporaneos { get; set; } = false;

    // Helper de Validación
    [NotMapped]
    public bool IsEvaluationOpen 
    {
        get 
        {
            var now = DateTime.Now;
            return Activo && now >= FechaInicioEvaluacion && now <= FechaFinEvaluacion;
        }
    }

    // The Professor responsible for this Period
    public string? TutorAcademicoId { get; set; }
    [ForeignKey("TutorAcademicoId")]
    public ApplicationUser? TutorAcademico { get; set; }
}
