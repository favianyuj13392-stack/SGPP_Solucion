using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Academic;

[Authorize(Roles = "Admin,TutorAcademico")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<AcademicSummaryDto> ResumenEstudiantes { get; set; } = new();

    public class AcademicSummaryDto
    {
        public string EstudianteNombre { get; set; } = string.Empty;
        public string EmpresaNombre { get; set; } = string.Empty;
        public string TutorInstNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        
        // Scores
        public int AsignacionId { get; set; }
        public int? NotaFormA { get; set; }
        public int? NotaFormB { get; set; }
        public double? PromedioFinal { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var query = _context.Asignaciones
            .Include(a => a.Estudiante)
                .ThenInclude(e => e.ApplicationUser)
            .Include(a => a.TutorInstitucional)
                .ThenInclude(t => t.ApplicationUser)
            .Include(a => a.TutorInstitucional)
                .ThenInclude(t => t.CentroPractica)
            .Include(a => a.FormularioB) // Eager load Form B
            .AsQueryable();

        // 1. Filter by Role
        if (!User.IsInRole("Admin"))
        {
            // If not admin, must be TutorAcademico, so filter by ID
            query = query.Where(a => a.TutorAcademicoId == user.Id);
        }

        // 2. Execute Query
        var asignaciones = await query
            .OrderByDescending(a => a.FechaCreacion)
            .ToListAsync();

        // 3. Proccess Data (Need to fetch Form A separately or adjust includes if relation exists)
        // Since Asignacion has FormularioA nav prop (if we added it correctly in Phase 5), use it.
        // Let's verify relation: In Phase 5 we updated Asignacion to have props?
        // Checking Asignacion.cs -> Yes, public FormularioA_Estudiante? FormularioA { get; set; }
        // BUT FormularioA IS NOT directly linked in EF if we didn't add DBSet relation configuration or foreign keys
        // Let's assume we need to fetch manually if nav prop isn't configured, or use Include if it is.
        // Based on Phase 4, FormA has AsignacionId.
        
        // Let's fetch IDs to load FormAs in batch
        var asignacionIds = asignaciones.Select(a => a.Id).ToList();
        var formulariosA = await _context.EvaluacionesEstudiante
            .Where(f => asignacionIds.Contains(f.AsignacionId))
            .ToListAsync();

        foreach (var asignacion in asignaciones)
        {
            var formA = formulariosA.FirstOrDefault(f => f.AsignacionId == asignacion.Id);
            var formB = asignacion.FormularioB; // Loaded via Include

            // Calculate Score B
            int? scoreB = null;
            if (formB != null)
            {
                scoreB = formB.ScoreTecnicoBruto + formB.ScorePowerSkillsBruto;
            }

            // Calculate Score A
            int? scoreA = null;
            if (formA != null)
            {
                scoreA = formA.ScoreCentroBruto + formA.ScoreTutorInstBruto + formA.ScoreTutorAcadBruto;
            }

            double? promedio = null;
            if (scoreB.HasValue)
            {
                promedio = (double)scoreB.Value;
            }

            ResumenEstudiantes.Add(new AcademicSummaryDto
            {
                AsignacionId = asignacion.Id,
                EstudianteNombre = $"{asignacion.Estudiante.ApplicationUser.Nombre} {asignacion.Estudiante.ApplicationUser.Apellido}",
                EmpresaNombre = asignacion.TutorInstitucional.CentroPractica.RazonSocial,
                TutorInstNombre = $"{asignacion.TutorInstitucional.ApplicationUser.Nombre} {asignacion.TutorInstitucional.ApplicationUser.Apellido}",
                Estado = asignacion.Estado.ToString(),
                NotaFormA = scoreA,
                NotaFormB = scoreB,
                PromedioFinal = promedio
            });
        }

        return Page();
    }
    
    public IActionResult OnGetExportFormA(int id, [FromServices] SGPP.Infrastructure.Services.IExcelExportService excelService, [FromServices] IWebHostEnvironment env)
    {
        // DEBUG: Unmasked error + Safe Path
        string rootPath = env.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
        
        var wb = excelService.GenerateFormA(id, rootPath);
        using (var stream = new MemoryStream())
        {
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FormA_Asignacion_{id}.xlsx");
        }
    }

    public IActionResult OnGetExportFormB(int id, [FromServices] SGPP.Infrastructure.Services.IExcelExportService excelService, [FromServices] IWebHostEnvironment env)
    {
        // DEBUG: Unmasked error + Safe Path
        string rootPath = env.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");

        var wb = excelService.GenerateFormB(id, rootPath);
        using (var stream = new MemoryStream())
        {
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FormB_Asignacion_{id}.xlsx");
        }
    }
}
