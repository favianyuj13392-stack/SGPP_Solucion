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
public class VincularModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VincularModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Properties for UI
    public List<CentroPractica> Centros { get; set; } = new();
    
    [BindProperty]
    public int SelectedCentroId { get; set; }
    
    [BindProperty]
    public int SelectedTutorId { get; set; }
    
    [BindProperty]
    public string SelectedTutorAcademicoId { get; set; } = string.Empty;

    public List<ApplicationUser> TutoresAcademicos { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);
        if (estudiante == null) return Forbid(); // Should not happen for role Estudiante

        // 1. Check if there is an ACTIVE period
        var activePeriod = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
        
        // If there is an active period, check if the student ALREADY has an assignment for IT
        if (activePeriod != null)
        {
            bool hasAssignmentInActivePeriod = await _context.Asignaciones
                .AnyAsync(a => a.EstudianteId == estudiante.Id 
                            && a.PeriodoId == activePeriod.Id 
                            && a.Estado != EstadoAsignacion.NoHabilitado);

            if (hasAssignmentInActivePeriod)
            {
                return RedirectToPage("./Dashboard");
            }
        }
        // If no active period, or student has no assignment in it, allow them to stay on Vincular (unless we want to block if no active period exists at all)

        // 2. Load Centros
        Centros = await _context.CentrosPractica
            .OrderBy(c => c.RazonSocial)
            .ToListAsync();
            
        // 3. Load Academic Tutors
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
        if (estudiante == null) return NotFound("Perfil de estudiante no encontrado.");
        
        // 0. Validate Manual Selection
        if (string.IsNullOrEmpty(SelectedTutorAcademicoId))
        {
             ModelState.AddModelError("", "Debe seleccionar un Tutor Académico (Docente).");
             return await ReloadPageOnError();
        }

        // 1. Obtener Periodo Activo (ESTRICTO)
        var periodo = await _context.Periodos
            .FirstOrDefaultAsync(p => p.Activo);

        if (periodo == null)
        {
            ModelState.AddModelError("", "No hay un periodo académico activo. Contacte al Administrador.");
            return await ReloadPageOnError();
        }

        // 3. Create Assignment (with Manual Academic Tutor)
        var asignacion = new Asignacion
        {
            PeriodoId = periodo.Id,
            EstudianteId = estudiante.Id,
            TutorInstitucionalId = SelectedTutorId,
            TutorAcademicoId = SelectedTutorAcademicoId, // Manual Selection
            Estado = EstadoAsignacion.Pendiente,
            FechaCreacion = DateTime.Now
        };

        _context.Asignaciones.Add(asignacion);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Vinculación exitosa. Su práctica ha sido registrada y notificada a su Tutor Institucional y Académico.";
        return RedirectToPage("./Dashboard");
    }

    private async Task<IActionResult> ReloadPageOnError()
    {
        Centros = await _context.CentrosPractica.OrderBy(c => c.RazonSocial).ToListAsync();
        var usersInRole = await _userManager.GetUsersInRoleAsync("TutorAcademico");
        TutoresAcademicos = usersInRole.OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToList();
        return Page();
    }
}
