using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Reports;

[Authorize(Roles = "Admin")]
public class SustainabilityModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SustainabilityModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ChartData CompanyChart { get; set; } = new();
    public ChartData AreaChart { get; set; } = new();

    public class ChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<double> Values { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        // 1. Data for Companies (Top 10 by Volume - Excluyendo datos mock de prueba)
        var companiesData = await _context.Asignaciones
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Where(a => a.TutorInstitucional != null 
                     && a.TutorInstitucional.CentroPractica != null
                     && a.TutorInstitucional.CentroPractica.RazonSocial != "SoftCorp SRL"
                     && (a.Estudiante == null || a.Estudiante.ApplicationUser.Email == null || !a.Estudiante.ApplicationUser.Email.Contains("test")))
            .GroupBy(a => a.TutorInstitucional.CentroPractica.RazonSocial)
            .Select(g => new
            {
                Empresa = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        CompanyChart.Labels = companiesData.Select(x => x.Empresa).ToList();
        CompanyChart.Values = companiesData.Select(x => (double)x.Count).ToList();

        // 2. Data for Areas based on Assignments (Excluyendo datos mock)
        var assignmentsByArea = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Where(a => a.TutorInstitucional != null
                     && a.TutorInstitucional.CentroPractica.RazonSocial != "SoftCorp SRL"
                     && (a.Estudiante == null || a.Estudiante.ApplicationUser.Email == null || !a.Estudiante.ApplicationUser.Email.Contains("test")))
            .GroupBy(a => a.TutorInstitucional.AreaTrabajo)
            .Select(g => new 
            {
                Area = string.IsNullOrWhiteSpace(g.Key) ? "SIN DEFINIR" : g.Key.Trim().ToUpper(),
                Count = g.Count()
            })
            .ToListAsync();
            
        // Agrupar y normalizar en memoria
        var areaGrouped = assignmentsByArea
            .GroupBy(x => x.Area)
            .Select(g => new 
            {
                Area = g.Key,
                Count = g.Sum(x => x.Count)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Para presentación ejecutiva: Top 6 áreas principales + "OTRAS ÁREAS"
        var topAreas = areaGrouped.Take(6).ToList();
        var remainingCount = areaGrouped.Skip(6).Sum(x => x.Count);
        if (remainingCount > 0)
        {
            topAreas.Add(new { Area = "OTRAS ÁREAS", Count = remainingCount });
        }

        AreaChart.Labels = topAreas.Select(x => x.Area).ToList();
        AreaChart.Values = topAreas.Select(x => (double)x.Count).ToList();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        using var workbook = new XLWorkbook();
        
        // --- Sheet 1: Empresas ---
        var worksheet1 = workbook.Worksheets.Add("Empresas");
        
        // Headers
        worksheet1.Cell(1, 1).Value = "Empresa";
        worksheet1.Cell(1, 2).Value = "Total Histórico";
        worksheet1.Cell(1, 3).Value = "Estudiantes Activos";
        worksheet1.Cell(1, 4).Value = "Promedio Satisfacción (0-36)";

        // Data Fetch (Excluyendo mock)
        var companyStats = await _context.Asignaciones
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Include(a => a.FormularioA)
            .Include(a => a.Periodo)
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Where(a => a.TutorInstitucional != null 
                     && a.TutorInstitucional.CentroPractica != null
                     && a.TutorInstitucional.CentroPractica.RazonSocial != "SoftCorp SRL"
                     && (a.Estudiante == null || a.Estudiante.ApplicationUser.Email == null || !a.Estudiante.ApplicationUser.Email.Contains("test")))
            .GroupBy(a => a.TutorInstitucional.CentroPractica.RazonSocial)
            .Select(g => new
            {
                Empresa = g.Key,
                Total = g.Count(),
                Activos = g.Count(x => x.Periodo.Activo),
                // Calculate Average only for those who have FormularioA
                Promedio = g.Where(x => x.FormularioA != null).Average(x => (double?)x.FormularioA!.ScoreCentroBruto) ?? 0
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        int row = 2;
        foreach (var item in companyStats)
        {
            worksheet1.Cell(row, 1).Value = item.Empresa;
            worksheet1.Cell(row, 2).Value = item.Total;
            worksheet1.Cell(row, 3).Value = item.Activos;
            worksheet1.Cell(row, 4).Value = Math.Round(item.Promedio, 1);
            row++;
        }

        // Style
        worksheet1.Columns().AdjustToContents();

        // --- Sheet 2: Áreas ---
        var worksheet2 = workbook.Worksheets.Add("Áreas de Trabajo");
        worksheet2.Cell(1, 1).Value = "Área";
        worksheet2.Cell(1, 2).Value = "Cantidad Estudiantes";

        var areaStats = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
            .ToListAsync(); // Fetch all to categorize in memory safely

        var areaGrouped = areaStats
            .GroupBy(a => a.TutorInstitucional.AreaTrabajo != null ? a.TutorInstitucional.AreaTrabajo.ToUpper().Trim() : "SIN DEFINIR")
            .Select(g => new
            {
                Area = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        row = 2;
        foreach (var item in areaGrouped)
        {
            worksheet2.Cell(row, 1).Value = item.Area;
            worksheet2.Cell(row, 2).Value = item.Count;
            row++;
        }
         worksheet2.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteSostenibilidad.xlsx");
    }
}
