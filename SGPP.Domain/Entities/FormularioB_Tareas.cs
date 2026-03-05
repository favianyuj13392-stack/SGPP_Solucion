using SGPP.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class FormularioB_Tareas : BaseEntity
{
    public int FormularioBId { get; set; }
    [ForeignKey("FormularioBId")]
    public FormularioB_Empresa FormularioB { get; set; } = null!;

    [MaxLength(500)]
    public string? DescripcionTarea { get; set; }
    public int Cumplimiento { get; set; }
    [MaxLength(500)]
    public string? AspectosPositivos { get; set; }
    [MaxLength(500)]
    public string? AspectosMejorar { get; set; }
}
