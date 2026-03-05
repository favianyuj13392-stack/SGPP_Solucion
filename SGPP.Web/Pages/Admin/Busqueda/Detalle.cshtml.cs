using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;
using SGPP.Infrastructure.Services; // Ensure namespace for IExcelExportService

namespace SGPP.Web.Pages.Admin.Busqueda;

[Authorize(Roles = "Admin")]
public class DetalleModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetalleModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string PersonName { get; set; } = string.Empty;
    public string PersonRole { get; set; } = string.Empty;
    
    public List<HistoryRecord> History { get; set; } = new();

    public class HistoryRecord
    {
        public int AsignacionId { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty; // Relevant for Tutor: Who they evaluated
        public string FormType { get; set; } = string.Empty; // "Formulario A", "Formulario B"
        public DateTime? FechaLlenado { get; set; }
        public int? Score { get; set; }
        public bool IsCompleted { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string type, int id)
    {
        if (string.IsNullOrEmpty(type) || id == 0) return RedirectToPage("./Index");

        if (type.ToLower() == "student")
        {
            await LoadStudentHistory(id);
        }
        else if (type.ToLower() == "tutor")
        {
            await LoadTutorHistory(id);
        }
        else
        {
            return RedirectToPage("./Index");
        }

        return Page();
    }

    private async Task LoadStudentHistory(int studentId)
    {
        var student = await _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.Id == studentId);
        
        if (student == null) return;

        PersonName = $"{student.ApplicationUser.Nombre} {student.ApplicationUser.Apellido}";
        PersonRole = "Estudiante (Autoevaluaciones)";

        var asignaciones = await _context.Asignaciones
            .Include(a => a.Periodo)
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Where(a => a.EstudianteId == studentId)
            .OrderByDescending(a => a.Periodo.FechaInicio)
            .ToListAsync();

        // Load Forms A manually
        var asigIds = asignaciones.Select(a => a.Id).ToList();
        var formsA = await _context.EvaluacionesEstudiante
            .Where(f => asigIds.Contains(f.AsignacionId))
            .ToListAsync();

        foreach(var a in asignaciones)
        {
            var form = formsA.FirstOrDefault(f => f.AsignacionId == a.Id);
            History.Add(new HistoryRecord
            {
                AsignacionId = a.Id,
                Periodo = a.Periodo.CodigoGestion,
                Subject = "Autoevaluación",
                FormType = "Formulario A",
                FechaLlenado = form?.FechaEvaluacion,
                Score = form != null ? (form.ScoreCentroBruto + form.ScoreTutorInstBruto + form.ScoreTutorAcadBruto) : null,
                IsCompleted = form != null
            });
        }
    }

    private async Task LoadTutorHistory(int tutorId)
    {
        var tutor = await _context.TutoresInstitucionales
            .Include(t => t.ApplicationUser)
            .Include(t => t.CentroPractica)
            .FirstOrDefaultAsync(t => t.Id == tutorId);

        if (tutor == null) return;

        PersonName = $"{tutor.ApplicationUser.Nombre} {tutor.ApplicationUser.Apellido}";
        PersonRole = $"Tutor Institucional - {tutor.CentroPractica.RazonSocial}";

        var asignaciones = await _context.Asignaciones
            .Include(a => a.Periodo)
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Where(a => a.TutorInstitucionalId == tutorId)
            .OrderByDescending(a => a.Periodo.FechaInicio)
            .ToListAsync();

        // Load Forms B
        var asigIds = asignaciones.Select(a => a.Id).ToList();
        var formsB = await _context.EvaluacionesEmpresa
            .Where(f => asigIds.Contains(f.AsignacionId))
            .ToListAsync();

        foreach(var a in asignaciones)
        {
            var form = formsB.FirstOrDefault(f => f.AsignacionId == a.Id);
            History.Add(new HistoryRecord
            {
                AsignacionId = a.Id,
                Periodo = a.Periodo.CodigoGestion,
                Subject = $"Est: {a.Estudiante.ApplicationUser.Apellido} {a.Estudiante.ApplicationUser.Nombre}",
                FormType = "Formulario B",
                FechaLlenado = form?.FechaEvaluacion,
                Score = form != null ? (form.ScoreTecnicoBruto + form.ScorePowerSkillsBruto) : null,
                IsCompleted = form != null
            });
        }
    }

    public IActionResult OnGetDownload(int id, string type, [FromServices] IExcelExportService excelService, [FromServices] IWebHostEnvironment env)
    {
         string rootPath = env.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
         
         if (type == "Formulario A")
         {
            var wb = excelService.GenerateFormA(id, rootPath);
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FormA_{id}.xlsx");
         }
         else // Formulario B
         {
            var wb = excelService.GenerateFormB(id, rootPath);
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FormB_{id}.xlsx");
         }
    }
}
