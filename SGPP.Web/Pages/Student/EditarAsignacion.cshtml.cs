using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Student;

[Authorize(Roles = "Estudiante")]
public class EditarAsignacionModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditarAsignacionModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Dropdowns
    public List<CentroPractica> Centros { get; set; } = new();
    public List<ApplicationUser> TutoresAcademicos { get; set; } = new();

    // Inputs PRE-FILLED
    [BindProperty]
    public int SelectedCentroId { get; set; }
    
    [BindProperty]
    public int SelectedTutorId { get; set; }
    
    [BindProperty]
    public string SelectedTutorAcademicoId { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);
        if (estudiante == null) return Forbid();

        var asignacion = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
            .Include(a => a.TutorAcademico)
            .OrderByDescending(a => a.FechaCreacion)
            .FirstOrDefaultAsync(a => a.EstudianteId == estudiante.Id && a.Estado != EstadoAsignacion.NoHabilitado);

        if (asignacion == null) return RedirectToPage("./Vincular");
        // if (asignacion.Estado == EstadoAsignacion.Completado) return RedirectToPage("./Dashboard"); // Allow editing for correction

        // Pre-fill
        SelectedCentroId = asignacion.TutorInstitucional.CentroPracticaId;
        SelectedTutorId = asignacion.TutorInstitucionalId;
        SelectedTutorAcademicoId = asignacion.TutorAcademicoId!;

        // Load Lists
        Centros = await _context.CentrosPractica.OrderBy(c => c.RazonSocial).ToListAsync();
        var usersInRole = await _userManager.GetUsersInRoleAsync("TutorAcademico");
        TutoresAcademicos = usersInRole.OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToList();

        return Page();
    }
    
    public async Task<JsonResult> OnGetTutoresAsync(int centroId)
    {
        var tutores = await _context.TutoresInstitucionales
            .Where(t => t.CentroPracticaId == centroId)
            .Include(t => t.ApplicationUser)
            .Select(t => new {
                id = t.Id,
                nombre = $"{t.ApplicationUser.Nombre} {t.ApplicationUser.Apellido} - {t.Cargo}"
            })
            .ToListAsync();
        return new JsonResult(tutores);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);
        
        var asignacion = await _context.Asignaciones
            .OrderByDescending(a => a.FechaCreacion)
            .FirstOrDefaultAsync(a => a.EstudianteId == estudiante!.Id && a.Estado != EstadoAsignacion.NoHabilitado);

        if (asignacion == null) return RedirectToPage("./Vincular");

        // Update
        asignacion.TutorInstitucionalId = SelectedTutorId;
        asignacion.TutorAcademicoId = SelectedTutorAcademicoId;
        
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Su asignación ha sido actualizada correctamente.";
        return RedirectToPage("./Dashboard");
    }
}
