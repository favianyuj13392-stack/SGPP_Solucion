using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;
using System.Security.Claims;

namespace SGPP.Web.Pages.Student;

[Authorize(Roles = "Estudiante")]
public class FormularioAModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FormularioAModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public FormularioA_Estudiante Input { get; set; } = new();

    [BindProperty]
    public Dictionary<int, QuestionAnswer> CentroQuestions { get; set; } = new();

    [BindProperty]
    public Dictionary<int, QuestionAnswer> TutorInstQuestions { get; set; } = new();

    [BindProperty]
    public Dictionary<int, QuestionAnswer> TutorAcadQuestions { get; set; } = new();

    public class QuestionAnswer
    {
        public int Value { get; set; }
        public string? Justificacion { get; set; }
        public string? Observaciones { get; set; }
    }

    public Asignacion? Asignacion { get; set; }

    public async Task<IActionResult> OnGetAsync(int? asignacionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Base Query filtered by Student User ID
        var query = _context.Asignaciones
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.ApplicationUser)
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Include(a => a.Periodo)
            .Where(a => a.Estudiante.ApplicationUserId == user.Id);

        if (asignacionId.HasValue)
        {
            Asignacion = await query.FirstOrDefaultAsync(a => a.Id == asignacionId.Value);
        }
        else
        {
            // Fallback: Get the latest assignment if none specified
            Asignacion = await query.OrderByDescending(a => a.FechaCreacion).FirstOrDefaultAsync();
        }

        if (Asignacion == null) 
        {
             TempData["ErrorMessage"] = "No se encontró una asignación válida para realizar la evaluación.";
             return RedirectToPage("./Dashboard");
        }

        // Check if already filled?
        bool exists = await _context.EvaluacionesEstudiante.AnyAsync(e => e.AsignacionId == Asignacion.Id);
        if (exists)
        {
             TempData["InfoMessage"] = "Ya has completado esta autoevaluación.";
             return RedirectToPage("./Dashboard"); 
        }

        // Pre-fill default dates for the View
        Input.FechaInicio = Asignacion.Periodo.FechaInicio;
        Input.FechaFin = Asignacion.Periodo.FechaFin;
        Input.FechaEvaluacion = DateTime.Now;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int asignacionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // 1. Validate Ownership again
        var asignacion = await _context.Asignaciones
            .Include(a => a.Estudiante)
            .FirstOrDefaultAsync(a => a.Id == asignacionId && a.Estudiante.ApplicationUserId == user.Id);
        
        if (asignacion == null) return NotFound();

        // 2. Calculate Scores
        // Rule: Centro x1, TutorInst x2, TutorAcad x1
        
        int scoreCentro = CentroQuestions.Sum(q => q.Value.Value);
        int scoreTutorInst = TutorInstQuestions.Sum(q => q.Value.Value) * 2; // Weighting applied here
        int scoreTutorAcad = TutorAcadQuestions.Sum(q => q.Value.Value);

        // 3. Create Entity
        var evaluacion = new FormularioA_Estudiante
        {
            AsignacionId = asignacionId,
            ScoreCentroBruto = scoreCentro,
            ScoreTutorInstBruto = scoreTutorInst,
            ScoreTutorAcadBruto = scoreTutorAcad,
            HorasTrabajadas = Input.HorasTrabajadas,
            AreaAsignada = Input.AreaAsignada,
            FechaInicio = Input.FechaInicio,
            FechaFin = Input.FechaFin,
            FechaEvaluacion = Input.FechaEvaluacion,
            FortalezasCentro = Input.FortalezasCentro,
            LimitacionesCentro = Input.LimitacionesCentro,
            RecomendacionesCentro = Input.RecomendacionesCentro,
            FortalezasTutor = Input.FortalezasTutor,
            LimitacionesTutor = Input.LimitacionesTutor,
            RecomendacionesTutor = Input.RecomendacionesTutor,
            FechaEnvio = DateTime.Now
        };

        // We use ranges to avoid key collision in the single Details table
        // Centro: 1-9
        foreach(var q in CentroQuestions)
        {
            evaluacion.Detalles.Add(new FormularioA_DetalleRespuestas { 
                PreguntaKey = q.Key, 
                Valor = q.Value.Value, 
                Justificacion = q.Value.Justificacion, 
                Observaciones = q.Value.Observaciones 
            });
        }

        // Tutor Inst: Maps to 10-14 (View keys 1-5 -> +9)
        foreach(var q in TutorInstQuestions)
        {
            evaluacion.Detalles.Add(new FormularioA_DetalleRespuestas { 
                PreguntaKey = 9 + q.Key, // 1->10, 5->14
                Valor = q.Value.Value, 
                Justificacion = q.Value.Justificacion, 
                Observaciones = q.Value.Observaciones 
            });
        }

         // Tutor Acad: Maps to 15-20 (View keys 1-6 -> +14)
        foreach(var q in TutorAcadQuestions)
        {
            evaluacion.Detalles.Add(new FormularioA_DetalleRespuestas { 
                PreguntaKey = 14 + q.Key, // 1->15, 6->20
                Valor = q.Value.Value, 
                Justificacion = q.Value.Justificacion, 
                Observaciones = q.Value.Observaciones 
            });
        }

        _context.EvaluacionesEstudiante.Add(evaluacion);
        
        // IMPORTANT: Do NOT update Asignacion.Estado
        
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Autoevaluación guardada correctamente.";

        return RedirectToPage("./Dashboard");
    }
}
