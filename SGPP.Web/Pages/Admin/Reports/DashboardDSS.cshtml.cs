using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Infrastructure.Persistence;
using System.Globalization;

namespace SGPP.Web.Pages.Admin.Reports;

[Authorize(Roles = "Admin")]
public class DashboardDSSModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardDSSModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ConvenioHealthMetric> ConvenioHealthMetrics { get; set; } = new();
    public List<BrechaCurricularMetric> BrechaMetrics { get; set; } = new();
    public List<EmpleabilidadMetric> EmpleabilidadMetrics { get; set; } = new();
    public List<TendenciaCalidadMetric> TendenciaMetrics { get; set; } = new();

    public double Resumen_TechPct { get; set; }
    public double Resumen_PowerPct { get; set; }
    public double Resumen_FormAPct { get; set; }
    public double Resumen_FormBPct { get; set; }

    public class ConvenioHealthMetric
    {
        public string Empresa { get; set; } = string.Empty;
        public int Count { get; set; }
        public double SatisfaccionPct { get; set; }
        public double EvaluacionPct { get; set; }
    }

    public class BrechaCurricularMetric
    {
        public string Carrera { get; set; } = string.Empty;
        public double TechPct { get; set; }
        public double PowerPct { get; set; }
    }

    public class EmpleabilidadMetric
    {
        public string Carrera { get; set; } = string.Empty;
        public string AreaTrabajo { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TendenciaCalidadMetric
    {
        public string Periodo { get; set; } = string.Empty;
        public double PromedioA_Pct { get; set; }
        public double PromedioB_Pct { get; set; }
    }

    public async Task OnGetAsync()
    {
        // Traer todas las asignaciones validadas con ambos formularios
        var validAssignments = await _context.Asignaciones
            .Include(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Include(a => a.FormularioA)
            .Include(a => a.FormularioB)
            .Include(a => a.Estudiante)
            .Include(a => a.Periodo)
            .Where(a => a.FormularioA != null && a.FormularioB != null)
            .ToListAsync();

        if (validAssignments.Any())
        {
            Resumen_TechPct = Math.Min(100.0, validAssignments.Average(a => (a.FormularioB!.ScoreTecnicoBruto / 32.0) * 100.0));
            Resumen_PowerPct = Math.Min(100.0, validAssignments.Average(a => (a.FormularioB!.ScorePowerSkillsBruto / 40.0) * 100.0));
            Resumen_FormAPct = Math.Min(100.0, validAssignments.Average(a => (a.FormularioA!.ScoreCentroBruto / 36.0) * 100.0));
            Resumen_FormBPct = Math.Min(100.0, validAssignments.Average(a => (((a.FormularioB!.ScoreTecnicoBruto / 32.0) * 100.0) + ((a.FormularioB!.ScorePowerSkillsBruto / 40.0) * 100.0)) / 2.0));
        }

        // --- KPI 1: Salud de Convenios ---
        ConvenioHealthMetrics = validAssignments
            .Where(a => a.TutorInstitucional?.CentroPractica != null)
            .GroupBy(a => a.TutorInstitucional!.CentroPractica!.RazonSocial)
            .Select(g => new ConvenioHealthMetric
            {
                Empresa = g.Key,
                Count = g.Count(),
                SatisfaccionPct = Math.Min(100.0, g.Average(a => (a.FormularioA!.ScoreCentroBruto / 36.0) * 100.0)),
                EvaluacionPct = Math.Min(100.0, g.Average(a => (((a.FormularioB!.ScoreTecnicoBruto / 32.0) * 100.0) + ((a.FormularioB!.ScorePowerSkillsBruto / 40.0) * 100.0)) / 2.0))
            })
            .ToList();

        // --- KPI 2: Brechas Curriculares ---
        BrechaMetrics = validAssignments
            .Where(a => a.Estudiante != null)
            .GroupBy(a => a.Estudiante!.Carrera.ToString())
            .Select(g => new BrechaCurricularMetric
            {
                Carrera = g.Key,
                TechPct = Math.Min(100.0, g.Average(a => (a.FormularioB!.ScoreTecnicoBruto / 32.0) * 100.0)),
                PowerPct = Math.Min(100.0, g.Average(a => (a.FormularioB!.ScorePowerSkillsBruto / 40.0) * 100.0))
            })
            .ToList();

        // --- KPI 4: Tendencias de Calidad ---
        TendenciaMetrics = validAssignments
            .Where(a => a.Periodo != null && !string.IsNullOrEmpty(a.Periodo.CodigoGestion))
            .GroupBy(a => a.Periodo!.CodigoGestion)
            .AsEnumerable()
            .Select(g => {
                string rawCodigo = g.Key;
                string cleanPeriodo = rawCodigo;
                var match = System.Text.RegularExpressions.Regex.Match(rawCodigo, @"^(I{1,2}-\d{4})");
                if (match.Success) 
                {
                    cleanPeriodo = match.Groups[1].Value;
                }
                
                return new TendenciaCalidadMetric
                {
                    Periodo = cleanPeriodo,
                    PromedioA_Pct = Math.Min(100.0, g.Average(a => (a.FormularioA!.ScoreCentroBruto / 36.0) * 100.0)),
                    PromedioB_Pct = Math.Min(100.0, g.Average(a => (((a.FormularioB!.ScoreTecnicoBruto / 32.0) * 100.0) + ((a.FormularioB!.ScorePowerSkillsBruto / 40.0) * 100.0)) / 2.0))
                };
            })
            .OrderBy(t => t.Periodo)
            .ToList();

        // --- KPI 3: Distribución de Empleabilidad ---
        // Se usa todo el universo de asignaciones con tutores y áreas definidas para reflejar inserción, 
        // indistinto de si terminaron la evaluación o no.
        var allAssignments = await _context.Asignaciones
            .Include(a => a.Estudiante)
            .Include(a => a.TutorInstitucional)
            .Where(a => a.TutorInstitucional != null && a.TutorInstitucional.AreaTrabajo != null && a.Estudiante != null)
            .ToListAsync();

        EmpleabilidadMetrics = allAssignments
            .GroupBy(a => new { Carrera = a.Estudiante!.Carrera.ToString(), Area = a.TutorInstitucional!.AreaTrabajo })
            .Select(g => new EmpleabilidadMetric
            {
                Carrera = g.Key.Carrera,
                AreaTrabajo = g.Key.Area!,
                Count = g.Count()
            })
            .ToList();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        await OnGetAsync(); // Re-calculate data

        using var workbook = new XLWorkbook();

        // --- SHEET 1: Resumen Ejecutivo ---
        var ws1 = workbook.Worksheets.Add("Resumen Ejecutivo");
        ws1.Cell(1, 1).Value = "Indicador Clave (KPI)";
        ws1.Cell(1, 2).Value = "Logro General (%)";
        
        ws1.Cell(2, 1).Value = "Satisfacción del Estudiante (Formulario A)";
        ws1.Cell(2, 2).Value = Resumen_FormAPct / 100.0;
        ws1.Cell(2, 2).Style.NumberFormat.Format = "0.00%";

        ws1.Cell(3, 1).Value = "Evaluación Global de las Empresas (Formulario B)";
        ws1.Cell(3, 2).Value = Resumen_FormBPct / 100.0;
        ws1.Cell(3, 2).Style.NumberFormat.Format = "0.00%";

        ws1.Cell(4, 1).Value = "Promedio Competencias Técnicas";
        ws1.Cell(4, 2).Value = Resumen_TechPct / 100.0; 
        ws1.Cell(4, 2).Style.NumberFormat.Format = "0.00%";

        ws1.Cell(5, 1).Value = "Promedio Habilidades Blandas (Power Skills)";
        ws1.Cell(5, 2).Value = Resumen_PowerPct / 100.0;
        ws1.Cell(5, 2).Style.NumberFormat.Format = "0.00%";

        FormatHeader(ws1.Row(1));
        ws1.Columns().AdjustToContents();

        // --- SHEET 2: Ranking Empresas ---
        var ws2 = workbook.Worksheets.Add("Ranking Empresas");
        ws2.Cell(1, 1).Value = "Empresa";
        ws2.Cell(1, 2).Value = "Asignaciones Evaluadas";
        ws2.Cell(1, 3).Value = "Satisfacción Estudiante (%)";
        ws2.Cell(1, 4).Value = "Desempeño Estudiante (%)";

        int row = 2;
        foreach (var company in ConvenioHealthMetrics.OrderByDescending(c => c.EvaluacionPct))
        {
            ws2.Cell(row, 1).Value = company.Empresa;
            ws2.Cell(row, 2).Value = company.Count;
            
            ws2.Cell(row, 3).Value = company.SatisfaccionPct / 100.0;
            ws2.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
            
            ws2.Cell(row, 4).Value = company.EvaluacionPct / 100.0;
            ws2.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
            row++;
        }

        FormatHeader(ws2.Row(1));
        ws2.Columns().AdjustToContents();

        // --- SHEET 3: Distribución Áreas ---
        var ws3 = workbook.Worksheets.Add("Distribución Áreas");
        ws3.Cell(1, 1).Value = "Carrera";
        ws3.Cell(1, 2).Value = "Área de Trabajo";
        ws3.Cell(1, 3).Value = "Cantidad Estudiantes";

        row = 2;
        foreach (var item in EmpleabilidadMetrics.OrderBy(e => e.Carrera).ThenByDescending(e => e.Count))
        {
            ws3.Cell(row, 1).Value = item.Carrera;
            ws3.Cell(row, 2).Value = item.AreaTrabajo;
            ws3.Cell(row, 3).Value = item.Count;
            row++;
        }

        FormatHeader(ws3.Row(1));
        ws3.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DashboardEstrategico_BI_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private void FormatHeader(IXLRow row)
    {
        var cells = row.CellsUsed();
        cells.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366"); // UCB Blue
        cells.Style.Font.FontColor = XLColor.White;
        cells.Style.Font.Bold = true;
    }
}
