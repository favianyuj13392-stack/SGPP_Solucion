using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Tutor;

[Authorize(Roles = "Tutor")]
public class FormularioBModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FormularioBModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int AsignacionId { get; set; }

    public Asignacion? Asignacion { get; set; }

    [BindProperty]
    public FormularioB_Empresa Input { get; set; } = new();

    // List binding is more robust with .Index helper than Dictionary
    [BindProperty]
    public List<FormularioB_Tareas> Tareas { get; set; } = new();

    // Custom binding for dynamically indexed lists -- Dictionary is prone to binding errors with keys
    [BindProperty]
    public List<QuestionAnswer> TechQuestions { get; set; } = new();

    [BindProperty]
    public List<QuestionAnswer> PowerQuestions { get; set; } = new();

    public class QuestionAnswer
    {
        public int QuestionId { get; set; } // Previously Key
        public int Value { get; set; }
        public string? Justificacion { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (AsignacionId == 0) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Security Check: Assignation must belong to Tutor
        var tutorProfile = await _context.TutoresInstitucionales
            .Include(t => t.CentroPractica)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
        if (tutorProfile == null) return Forbid();

        Asignacion = await _context.Asignaciones
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Include(a => a.Periodo)
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .FirstOrDefaultAsync(a => a.Id == AsignacionId && a.TutorInstitucionalId == tutorProfile.Id);

        if (Asignacion == null) return NotFound("Asignación no encontrada o no pertenece a su usuario.");
        
        // Validation Rule: Locked if already completed? 
        if (Asignacion.Estado == EstadoAsignacion.Completado)
        {
             // Optional: Read-only logic
        }

        // Initialize Lists for View Reuse
        TechQuestions = new List<QuestionAnswer>();
        for(int i=1; i<=8; i++) TechQuestions.Add(new QuestionAnswer { QuestionId = i });

        PowerQuestions = new List<QuestionAnswer>();
        for(int i=9; i<=17; i++) PowerQuestions.Add(new QuestionAnswer { QuestionId = i });

        return Page();
    }

    public PartialViewResult OnGetRowTemplate()
    {
        return Partial("Partials/_RowTask", new FormularioB_Tareas());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Re-validate Security
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        var tutorProfile = await _context.TutoresInstitucionales.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
        if (tutorProfile == null) return Forbid(); // Fix CS8602 warning by checking null
        
        var asignacionDb = await _context.Asignaciones
            .FirstOrDefaultAsync(a => a.Id == AsignacionId && a.TutorInstitucionalId == tutorProfile.Id);

        if (asignacionDb == null) return NotFound();

        // 2. Calculate Scores
        int sumTech = TechQuestions.Sum(q => q.Value);
        int scoreTech = sumTech * 2;

        int sumPower = PowerQuestions.Sum(q => q.Value);
        int scorePower = sumPower;

        // 3. Map Data to Entities
        // Check if exists
        var existingForm = await _context.EvaluacionesEmpresa
            .Include(f => f.Tareas)
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.AsignacionId == AsignacionId);

        if (existingForm != null)
        {
            _context.EvaluacionesEmpresa.Remove(existingForm);
             await _context.SaveChangesAsync();
        }

        var form = new FormularioB_Empresa
        {
            AsignacionId = AsignacionId,
            HorasTrabajadas = Input.HorasTrabajadas,
            FechaInicioPractica = Input.FechaInicioPractica,
            FechaFinPractica = Input.FechaFinPractica,
            FechaEvaluacion = Input.FechaEvaluacion,
            ScoreTecnicoBruto = scoreTech,
            ScorePowerSkillsBruto = scorePower,
            FortalezasTexto = Input.FortalezasTexto,
            AreasMejoraTexto = Input.AreasMejoraTexto,
            FechaEnvio = DateTime.Now
        };

        // Add Tasks
        foreach (var tarea in Tareas)
        {
            if (!string.IsNullOrWhiteSpace(tarea.DescripcionTarea))
            {
                form.Tareas.Add(new FormularioB_Tareas
                {
                    DescripcionTarea = tarea.DescripcionTarea,
                    AspectosPositivos = tarea.AspectosPositivos,
                    AspectosMejorar = tarea.AspectosMejorar
                });
            }
        }

        // Add Details (Questions)
        foreach (var q in TechQuestions)
        {
            form.Detalles.Add(new FormularioB_DetalleRespuestas { 
                PreguntaKey = q.QuestionId, 
                Valor = q.Value, 
                Justificacion = q.Justificacion 
            }); 
        }
         foreach (var q in PowerQuestions)
        {
             form.Detalles.Add(new FormularioB_DetalleRespuestas { 
                 PreguntaKey = q.QuestionId, 
                 Valor = q.Value, 
                 Justificacion = q.Justificacion 
             });
        }
        
        _context.EvaluacionesEmpresa.Add(form);

        // 4. Update Assignation State
        asignacionDb.Estado = EstadoAsignacion.Completado;
        asignacionDb.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Dashboard");
    }
}
