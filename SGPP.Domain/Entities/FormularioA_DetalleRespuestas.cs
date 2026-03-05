using SGPP.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class FormularioA_DetalleRespuestas : BaseEntity
{
    public int FormularioAId { get; set; }
    [ForeignKey("FormularioAId")]
    public FormularioA_Estudiante FormularioA { get; set; } = null!;

    public int PreguntaKey { get; set; }
    public int Valor { get; set; } // 1-4
    public string? Justificacion { get; set; }
    public string? Observaciones { get; set; }
}

