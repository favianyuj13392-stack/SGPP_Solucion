using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
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

    public class TutorItem
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
    }

    public async Task OnGetAsync()
    {
        var tutoresDb = await _context.TutoresInstitucionales
            .Include(t => t.ApplicationUser)
            .Include(t => t.CentroPractica)
            .OrderBy(t => t.ApplicationUser.Apellido)
            .ThenBy(t => t.ApplicationUser.Nombre)
            .ToListAsync();

        Tutores = tutoresDb.Select(t => new TutorItem
        {
            Id = t.Id,
            NombreCompleto = t.ApplicationUser.Apellido + " " + t.ApplicationUser.Nombre,
            Email = t.ApplicationUser.Email ?? "",
            Empresa = t.CentroPractica.RazonSocial,
            Cargo = t.Cargo ?? "",
            Ci = t.Ci ?? "",
            EsActivo = t.ApplicationUser.EsActivo
        }).ToList();
    }
}
