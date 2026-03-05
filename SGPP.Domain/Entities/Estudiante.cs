using SGPP.Domain.Common;
using SGPP.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class Estudiante : BaseEntity
{
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    [ForeignKey("ApplicationUserId")]
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [MaxLength(20)]
    public string CodigoEstudiante { get; set; } = string.Empty;

    public Carrera Carrera { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string EmailInstitucional { get; set; } = string.Empty;

    public EstadoAcademico EstadoAcademico { get; set; } = EstadoAcademico.Habilitado;
}
