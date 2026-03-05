using SGPP.Domain.Common;
using SGPP.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGPP.Domain.Entities;

public class TutorAcademico : BaseEntity
{
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    [ForeignKey("ApplicationUserId")]
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public Carrera Carrera { get; set; } = Carrera.OTHER;

    // Optional: extra fields if needed later
}
