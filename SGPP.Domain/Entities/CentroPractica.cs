using SGPP.Domain.Common;
using SGPP.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Domain.Entities;

public class CentroPractica : AuditableEntity
{
    [MaxLength(200)]
    public string RazonSocial { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Nit { get; set; }

    [MaxLength(300)]
    public string? Direccion { get; set; }

    [MaxLength(100)]
    public string? Rubro { get; set; }

    public EstadoConvenio EstadoConvenio { get; set; } = EstadoConvenio.Activo;

    // Navigation Properties
    public ICollection<TutorInstitucional> Tutores { get; set; } = new List<TutorInstitucional>();
}
