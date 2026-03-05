using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Usuarios.Estudiantes;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<EstudianteItem> Estudiantes { get; set; } = new();

    public class EstudianteItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
    }

    public async Task OnGetAsync()
    {
        var estudiantesDb = await _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .OrderBy(e => e.ApplicationUser.Apellido)
            .ThenBy(e => e.ApplicationUser.Nombre)
            .ToListAsync();

        Estudiantes = estudiantesDb.Select(e => new EstudianteItem
        {
            Id = e.Id,
            UserId = e.ApplicationUserId,
            NombreCompleto = e.ApplicationUser.Apellido + " " + e.ApplicationUser.Nombre,
            Email = e.ApplicationUser.Email ?? "",
            Codigo = e.CodigoEstudiante,
            Carrera = e.Carrera.ToString(),
            EsActivo = e.ApplicationUser.EsActivo
        }).ToList();
    }
}
