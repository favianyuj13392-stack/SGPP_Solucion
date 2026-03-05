using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Reports;

[Authorize(Roles = "Admin")]
public class DashboardDSSModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardDSSModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // KPI 1: Skills Gap Analysis
    public double AvgTechnicalScorePct { get; set; }
    public double AvgPowerSkillsScorePct { get; set; }

    // KPI 2: Company Health (Scatter Plot Data)
    public List<CompanyHealthMetric> CompanyMetrics { get; set; } = new();
    public List<CompanyHealthMetric> Top5Companies { get; set; } = new();

    // KPI 3: Market Demand
    public List<MarketDemandMetric> MarketDemand { get; set; } = new();

    public class CompanyHealthMetric
    {
        public string CompanyName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public double StudentSatisfaction { get; set; } // Form A (Max 36) -> Validated to 0-100 scale potentially? Keeping raw for now or scaled? User asked for Scatter X/Y. Let's provide raw averages but maybe mention scale. User said "Promedio de ScoreCentroBruto".
        public double CompanyEvaluation { get; set; } // Form B (Total Score? Or combined?) User said "Promedio de Nota Final". Let's use (Tech + Power) sum.
        public double CombinedScore => StudentSatisfaction + CompanyEvaluation; // For ranking
    }

    public class MarketDemandMetric
    {
        public string Area { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public async Task OnGetAsync()
    {
        // 1. Skills Gap Analysis
        // Max Technical = 64, Max PowerUsers = 36
        var completedAssignments = await _context.Asignaciones
            .Include(a => a.FormularioB)
            .Where(a => a.FormularioB != null)
            .ToListAsync();

        if (completedAssignments.Any())
        {
            double avgTechRaw = completedAssignments.Average(a => a.FormularioB!.ScoreTecnicoBruto);
            double avgPowerRaw = completedAssignments.Average(a => a.FormularioB!.ScorePowerSkillsBruto);

            AvgTechnicalScorePct = (avgTechRaw / 64.0) * 100.0;
            AvgPowerSkillsScorePct = (avgPowerRaw / 36.0) * 100.0;
        }

        // 2. Company Health Matrix
        // Group by CentroPractica. Need Form A (Student Eval) and Form B (Company Eval).
        var assignmentsWithFullData = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
                .ThenInclude(t => t.CentroPractica)
            .Include(a => a.FormularioA)
            .Include(a => a.FormularioB)
            .Where(a => a.TutorInstitucional != null && a.TutorInstitucional.CentroPractica != null)
            .ToListAsync();

        var companyGroups = assignmentsWithFullData
            .GroupBy(a => a.TutorInstitucional!.CentroPractica!.RazonSocial)
            .Select(g => new CompanyHealthMetric
            {
                CompanyName = g.Key,
                StudentCount = g.Count(),
                // Form A: ScoreCentroBruto. Filter nulls.
                StudentSatisfaction = g.Where(x => x.FormularioA != null)
                                       .Select(x => (double)x.FormularioA!.ScoreCentroBruto)
                                       .DefaultIfEmpty(0)
                                       .Average(),
                // Form B: Sum of Tech(64) + Power(36) = 100 max presumably? Or just sum.
                // User said "Nota Final". Let's assume Sum of Bruto scores for now as a proxy.
                CompanyEvaluation = g.Where(x => x.FormularioB != null)
                                     .Select(x => (double)(x.FormularioB!.ScoreTecnicoBruto + x.FormularioB!.ScorePowerSkillsBruto))
                                     .DefaultIfEmpty(0)
                                     .Average()
            })
            .Where(c => c.StudentCount > 0) // Filter out pure zeros if any
            .ToList();

        CompanyMetrics = companyGroups;
        Top5Companies = companyGroups.OrderByDescending(c => c.CombinedScore).Take(5).ToList();

        // 3. Market Demand
        // Group by AreaTrabajo from TutorInstitucional
        // We can use the same dataset or querie again. Let's query specifically to include even those without forms if they are assigned?
        // Usually demand is based on where they are doing practice.
        var marketGroups = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
            .Where(a => a.TutorInstitucional != null && !string.IsNullOrEmpty(a.TutorInstitucional.AreaTrabajo))
            .GroupBy(a => a.TutorInstitucional!.AreaTrabajo!)
            .Select(g => new MarketDemandMetric
            {
                Area = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        MarketDemand = marketGroups;
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        await OnGetAsync(); // Re-calculate data

        using var workbook = new XLWorkbook();

        // --- SHEET 1: Resumen Ejecutivo ---
        var ws1 = workbook.Worksheets.Add("Resumen Ejecutivo");
        ws1.Cell(1, 1).Value = "Indicador";
        ws1.Cell(1, 2).Value = "Valor (%)";
        
        ws1.Cell(2, 1).Value = "Promedio Competencias Técnicas";
        ws1.Cell(2, 2).Value = AvgTechnicalScorePct / 100.0; // For percentage format
        ws1.Cell(2, 2).Style.NumberFormat.Format = "0.00%";

        ws1.Cell(3, 1).Value = "Promedio Habilidades Blandas (Power Skills)";
        ws1.Cell(3, 2).Value = AvgPowerSkillsScorePct / 100.0;
        ws1.Cell(3, 2).Style.NumberFormat.Format = "0.00%";

        FormatHeader(ws1.Row(1));
        ws1.Columns().AdjustToContents();

        // --- SHEET 2: Ranking Empresas ---
        var ws2 = workbook.Worksheets.Add("Ranking Empresas");
        ws2.Cell(1, 1).Value = "Empresa";
        ws2.Cell(1, 2).Value = "Estudiantes Recibidos";
        ws2.Cell(1, 3).Value = "Satisfacción Estudiante (Promedio)";
        ws2.Cell(1, 4).Value = "Evaluación Empresa (Promedio)";

        int row = 2;
        foreach (var company in CompanyMetrics.OrderByDescending(c => c.StudentCount))
        {
            ws2.Cell(row, 1).Value = company.CompanyName;
            ws2.Cell(row, 2).Value = company.StudentCount;
            ws2.Cell(row, 3).Value = company.StudentSatisfaction;
            ws2.Cell(row, 3).Style.NumberFormat.Format = "0.00";
            ws2.Cell(row, 4).Value = company.CompanyEvaluation;
            ws2.Cell(row, 4).Style.NumberFormat.Format = "0.00";
            row++;
        }

        FormatHeader(ws2.Row(1));
        ws2.Columns().AdjustToContents();

        // --- SHEET 3: Distribución Áreas ---
        var ws3 = workbook.Worksheets.Add("Distribución Áreas");
        ws3.Cell(1, 1).Value = "Área de Trabajo";
        ws3.Cell(1, 2).Value = "Cantidad Estudiantes";

        row = 2;
        foreach (var item in MarketDemand)
        {
            ws3.Cell(row, 1).Value = item.Area;
            ws3.Cell(row, 2).Value = item.Count;
            row++;
        }

        FormatHeader(ws3.Row(1));
        ws3.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteGerencial_SGPP.xlsx");
    }

    private void FormatHeader(IXLRow row)
    {
        var cells = row.CellsUsed();
        cells.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366"); // UCB Blue
        cells.Style.Font.FontColor = XLColor.White;
        cells.Style.Font.Bold = true;
    }
}
