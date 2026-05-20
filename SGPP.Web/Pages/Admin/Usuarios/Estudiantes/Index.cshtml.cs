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

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CarreraFilter { get; set; }

    public class EstudianteItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
    }

    public async Task OnGetAsync()
    {
        var query = _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(e => e.ApplicationUser.Nombre.Contains(SearchTerm) 
                                  || e.ApplicationUser.Apellido.Contains(SearchTerm) 
                                  || e.CodigoEstudiante.Contains(SearchTerm));
        }

        if (!string.IsNullOrWhiteSpace(CarreraFilter) && Enum.TryParse<SGPP.Domain.Enums.Carrera>(CarreraFilter, out var parsedCarrera))
        {
            query = query.Where(e => e.Carrera == parsedCarrera);
        }

        var estudiantesDb = await query
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
            Telefono = e.ApplicationUser.PhoneNumber ?? "-",
            EsActivo = e.ApplicationUser.EsActivo
        }).ToList();
    }
}
