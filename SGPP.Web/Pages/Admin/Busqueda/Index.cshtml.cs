using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Busqueda;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    public List<EstudianteDto> FoundStudents { get; set; } = new();
    public List<TutorDto> FoundTutors { get; set; } = new();

    public class EstudianteDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
    }

    public class TutorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm)) return;

        var term = SearchTerm.Trim().ToLower();

        // 1. Search Estudiantes
        FoundStudents = await _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .Where(e => e.ApplicationUser!.Nombre.ToLower().Contains(term) ||
                        e.ApplicationUser!.Apellido.ToLower().Contains(term) ||
                        e.CodigoEstudiante.ToLower().Contains(term) ||
                        e.ApplicationUser!.Email!.ToLower().Contains(term))
            .Select(e => new EstudianteDto
            {
                Id = e.Id,
                Nombre = $"{e.ApplicationUser.Apellido} {e.ApplicationUser.Nombre}",
                Codigo = e.CodigoEstudiante,
                Carrera = e.Carrera.ToString()
            })
            .ToListAsync();

        // 2. Search Tutores
        FoundTutors = await _context.TutoresInstitucionales
            .Include(t => t.ApplicationUser)
            .Include(t => t.CentroPractica)
            .Where(t => t.ApplicationUser!.Nombre.ToLower().Contains(term) ||
                        t.ApplicationUser!.Apellido.ToLower().Contains(term) ||
                        t.ApplicationUser!.Email!.ToLower().Contains(term))
            .Select(t => new TutorDto
            {
                Id = t.Id,
                Nombre = $"{t.ApplicationUser.Apellido} {t.ApplicationUser.Nombre}",
                Empresa = t.CentroPractica.RazonSocial,
                Cargo = t.Cargo ?? ""
            })
            .ToListAsync();
    }
}
