using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    public bool EsActivo { get; set; } = true;
}
