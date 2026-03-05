using SGPP.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class TutorInstitucional : BaseEntity
{
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    [ForeignKey("ApplicationUserId")]
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [MaxLength(20)]
    public string Ci { get; set; } = string.Empty;

    public int CentroPracticaId { get; set; }
    [ForeignKey("CentroPracticaId")]
    public CentroPractica CentroPractica { get; set; } = null!;

    [MaxLength(100)]
    public string? Cargo { get; set; }

    [MaxLength(100)]
    public string? AreaTrabajo { get; set; }

    [MaxLength(50)]
    public string? TelefonoContacto { get; set; }
}
