using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Usuarios.Tutores;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<TutorItem> Tutores { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CarreraFilter { get; set; }

    public class TutorItem
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
        public List<string> CarrerasAsignadas { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        var query = _context.TutoresInstitucionales
            .Include(t => t.ApplicationUser)
            .Include(t => t.CentroPractica)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(t => t.ApplicationUser.Nombre.Contains(term)
                                  || t.ApplicationUser.Apellido.Contains(term)
                                  || (t.ApplicationUser.Email != null && t.ApplicationUser.Email.Contains(term))
                                  || t.CentroPractica.RazonSocial.Contains(term)
                                  || (t.Ci != null && t.Ci.Contains(term))
                                  || (t.Cargo != null && t.Cargo.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(CarreraFilter) && Enum.TryParse<Carrera>(CarreraFilter, out var parsedCarrera))
        {
            var tutorIdsConCarrera = _context.Asignaciones
                .Where(a => a.Estudiante.Carrera == parsedCarrera)
                .Select(a => a.TutorInstitucionalId);

            query = query.Where(t => tutorIdsConCarrera.Contains(t.Id));
        }

        var tutoresDb = await query
            .OrderBy(t => t.ApplicationUser.Apellido)
            .ThenBy(t => t.ApplicationUser.Nombre)
            .ToListAsync();

        var tutorIds = tutoresDb.Select(t => t.Id).ToList();

        // Obtener carreras asignadas a estos tutores en periodos activos
        var asignacionesInfo = await _context.Asignaciones
            .Where(a => tutorIds.Contains(a.TutorInstitucionalId))
            .Select(a => new { a.TutorInstitucionalId, Carrera = a.Estudiante.Carrera.ToString() })
            .ToListAsync();

        var asignacionesPorTutor = asignacionesInfo
            .GroupBy(a => a.TutorInstitucionalId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Carrera).Distinct().ToList());

        Tutores = tutoresDb.Select(t => new TutorItem
        {
            Id = t.Id,
            NombreCompleto = t.ApplicationUser.Apellido + " " + t.ApplicationUser.Nombre,
            Email = t.ApplicationUser.Email ?? "",
            Empresa = t.CentroPractica.RazonSocial,
            Cargo = t.Cargo ?? "",
            Ci = t.Ci ?? "",
            EsActivo = t.ApplicationUser.EsActivo,
            CarrerasAsignadas = asignacionesPorTutor.ContainsKey(t.Id) ? asignacionesPorTutor[t.Id] : new List<string>()
        }).ToList();
    }
}
