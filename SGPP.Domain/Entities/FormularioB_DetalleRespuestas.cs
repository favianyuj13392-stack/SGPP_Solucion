using SGPP.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class FormularioB_DetalleRespuestas : BaseEntity
{
    public int FormularioBId { get; set; }
    [ForeignKey("FormularioBId")]
    public FormularioB_Empresa FormularioB { get; set; } = null!;

    public int PreguntaKey { get; set; }
    public int Valor { get; set; } // 1-4
    public string? Justificacion { get; set; }
    public string? Observaciones { get; set; }
}
