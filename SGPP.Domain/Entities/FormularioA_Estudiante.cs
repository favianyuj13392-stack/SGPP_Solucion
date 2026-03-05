using SGPP.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class FormularioA_Estudiante : BaseEntity
{
    public int AsignacionId { get; set; }
    [ForeignKey("AsignacionId")]
    public Asignacion Asignacion { get; set; } = null!;

    public int ScoreCentroBruto { get; set; }
    public int ScoreTutorInstBruto { get; set; }
    public int ScoreTutorAcadBruto { get; set; }

    public string? FortalezasCentro { get; set; }
    public string? LimitacionesCentro { get; set; }
    public string? FortalezasTutor { get; set; }
    public string? LimitacionesTutor { get; set; }
    public string? RecomendacionesCentro { get; set; }
    public string? RecomendacionesTutor { get; set; }

    public double HorasTrabajadas { get; set; }
    public string? AreaAsignada { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime FechaEvaluacion { get; set; } = DateTime.Now;

    public DateTime FechaEnvio { get; set; } = DateTime.Now;

    public ICollection<FormularioA_DetalleRespuestas> Detalles { get; set; } = new List<FormularioA_DetalleRespuestas>();
}
