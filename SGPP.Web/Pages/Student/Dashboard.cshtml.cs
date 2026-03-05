using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Student;

[Authorize(Roles = "Estudiante")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Asignacion? Asignacion { get; set; }
    public Periodo? ActivePeriod { get; set; }
    public bool HasCompletedFormA { get; set; } = false;

    // Propiedad centralizada para verificar ventana de evaluación
    public bool IsEvaluationWindowOpen 
    {
        get 
        {
            // Si hay asignación, manda el periodo de esa asignación
            if (Asignacion != null && Asignacion.Periodo != null)
            {
                return Asignacion.Periodo.IsEvaluationOpen;
            }
            return false;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // 1. Obtener Periodo Activo Global (para info o vincular)
        ActivePeriod = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);

        // 2. Buscar perfil de estudiante
        var estudianteProfile = await _context.Estudiantes
            .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

        if (estudianteProfile != null)
        {
            // 3. Buscar ÚLTIMA asignación (Solo 1 activa por lógica de negocio)
            Asignacion = await _context.Asignaciones
                .Include(a => a.TutorInstitucional).ThenInclude(t => t.ApplicationUser)
                .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
                .Include(a => a.Periodo)
                .Where(a => a.EstudianteId == estudianteProfile.Id)
                .OrderByDescending(a => a.FechaCreacion)
                .FirstOrDefaultAsync();

            if (Asignacion != null)
            {
                // YA tiene práctica. Verificar si completó Form A
                HasCompletedFormA = await _context.EvaluacionesEstudiante
                    .AnyAsync(f => f.AsignacionId == Asignacion.Id);
            }
            // Si Asignacion == null, la vista mostrará el botón de Vincular (si hay Periodo Activo)
        }
        
        return Page();
    }
}
