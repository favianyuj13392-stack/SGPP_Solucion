using SGPP.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class FormularioB_Empresa : BaseEntity
{
    public int AsignacionId { get; set; }
    [ForeignKey("AsignacionId")]
    public Asignacion Asignacion { get; set; } = null!;

    // Input critico - debe ser decimal(10,2) en DB
    public decimal HorasTrabajadas { get; set; }

    public int ScoreTecnicoBruto { get; set; }
    public int ScorePowerSkillsBruto { get; set; }

    public string? FortalezasTexto { get; set; }
    public string? AreasMejoraTexto { get; set; }

    public DateTime? FechaInicioPractica { get; set; }
    public DateTime? FechaFinPractica { get; set; }
    
    // Editable date of evaluation (default to now but changeable)
    public DateTime FechaEvaluacion { get; set; } = DateTime.Now;

    public DateTime FechaEnvio { get; set; } = DateTime.Now; // System timestamp of submission

    public ICollection<FormularioB_Tareas> Tareas { get; set; } = new List<FormularioB_Tareas>();
    public ICollection<FormularioB_DetalleRespuestas> Detalles { get; set; } = new List<FormularioB_DetalleRespuestas>();
}
