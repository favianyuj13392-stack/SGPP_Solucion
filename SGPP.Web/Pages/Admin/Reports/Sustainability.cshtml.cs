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

        // 2. Data for Areas based on Assignments (Macro-áreas normalizadas)
        var assignmentsForArea = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
            .Include(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Where(a => a.TutorInstitucional != null
                     && a.TutorInstitucional.CentroPractica.RazonSocial != "SoftCorp SRL"
                     && (a.Estudiante == null || a.Estudiante.ApplicationUser.Email == null || !a.Estudiante.ApplicationUser.Email.Contains("test")))
            .Select(a => new 
            {
                RawArea = a.TutorInstitucional.AreaTrabajo,
                Carrera = a.Estudiante != null ? (SGPP.Domain.Enums.Carrera?)a.Estudiante.Carrera : null
            })
            .ToListAsync();
            
        // Agrupar y normalizar a sectores canónicos
        var areaGrouped = assignmentsForArea
            .Select(a => GetMacroArea(a.RawArea, a.Carrera))
            .GroupBy(area => area)
            .Select(g => new 
            {
                Area = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        AreaChart.Labels = areaGrouped.Select(x => x.Area).ToList();
        AreaChart.Values = areaGrouped.Select(x => (double)x.Count).ToList();
    }

    public static string GetMacroArea(string? rawArea, SGPP.Domain.Enums.Carrera? carrera)
    {
        if (!string.IsNullOrWhiteSpace(rawArea))
        {
            string norm = rawArea.ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Trim();

            if (norm.Contains("software") || norm.Contains("desarrollo") || norm.Contains("sistema") 
                || norm.Contains("tecnolog") || norm.Contains("informatic") || norm.Contains("redes") 
                || norm.Contains("cibersegur") || norm.Contains("data") || norm.Contains("analit") 
                || norm.Contains("comput") || norm.Contains("inteligencia") || norm.Contains("tic")
                || norm.Contains("programac") || norm.Contains("ti"))
            {
                return "Tecnología e Informática";
            }

            if (norm.Contains("comercial") || norm.Contains("ventas") || norm.Contains("marketing") 
                || norm.Contains("mercado") || norm.Contains("publicidad") || norm.Contains("trade")
                || norm.Contains("negocio"))
            {
                return "Comercial y Marketing";
            }

            if (norm.Contains("produccion") || norm.Contains("operacion") || norm.Contains("logistica") 
                || norm.Contains("suministro") || norm.Contains("calidad") || norm.Contains("mantenimiento")
                || norm.Contains("planta") || norm.Contains("industrial"))
            {
                return "Operaciones y Logística";
            }

            if (norm.Contains("finanz") || norm.Contains("contab") || norm.Contains("banc") 
                || norm.Contains("credito") || norm.Contains("cartera") || norm.Contains("auditor")
                || norm.Contains("tesorer") || norm.Contains("riesgo"))
            {
                return "Finanzas y Banca";
            }

            if (norm.Contains("talento") || norm.Contains("rrhh") || norm.Contains("recursos humanos") 
                || norm.Contains("personal") || norm.Contains("organizacional") || norm.Contains("capacitac"))
            {
                return "Gestión del Talento Humano";
            }

            if (norm.Contains("salud") || norm.Contains("psico") || norm.Contains("medic") 
                || norm.Contains("clinica") || norm.Contains("rehabilitacion") || norm.Contains("social")
                || norm.Contains("humanit") || norm.Contains("nutric"))
            {
                return "Salud y Desarrollo Humano";
            }

            if (norm.Contains("diseno") || norm.Contains("comunicac") || norm.Contains("audiovisual") 
                || norm.Contains("prensa") || norm.Contains("medios") || norm.Contains("grafic"))
            {
                return "Comunicación y Diseño";
            }

            if (norm.Contains("obra") || norm.Contains("civil") || norm.Contains("infraestruct") 
                || norm.Contains("proyect") || norm.Contains("arquitect") || norm.Contains("construc"))
            {
                return "Proyectos e Infraestructura";
            }
        }

        if (carrera.HasValue)
        {
            return carrera.Value switch
            {
                SGPP.Domain.Enums.Carrera.SIS or SGPP.Domain.Enums.Carrera.TEL or SGPP.Domain.Enums.Carrera.IMU or SGPP.Domain.Enums.Carrera.DIG => "Tecnología e Informática",
                SGPP.Domain.Enums.Carrera.ICO or SGPP.Domain.Enums.Carrera.MKD or SGPP.Domain.Enums.Carrera.ADM or SGPP.Domain.Enums.Carrera.ADT or SGPP.Domain.Enums.Carrera.IIE => "Comercial y Marketing",
                SGPP.Domain.Enums.Carrera.IND or SGPP.Domain.Enums.Carrera.IGL or SGPP.Domain.Enums.Carrera.IMT or SGPP.Domain.Enums.Carrera.IQM or SGPP.Domain.Enums.Carrera.IEN => "Operaciones y Logística",
                SGPP.Domain.Enums.Carrera.ECO or SGPP.Domain.Enums.Carrera.EIN or SGPP.Domain.Enums.Carrera.CPA => "Finanzas y Banca",
                SGPP.Domain.Enums.Carrera.PSI or SGPP.Domain.Enums.Carrera.PSP or SGPP.Domain.Enums.Carrera.INB or SGPP.Domain.Enums.Carrera.IBB or SGPP.Domain.Enums.Carrera.MED or SGPP.Domain.Enums.Carrera.NDI => "Salud y Desarrollo Humano",
                SGPP.Domain.Enums.Carrera.COM or SGPP.Domain.Enums.Carrera.CDM or SGPP.Domain.Enums.Carrera.DGR => "Comunicación y Diseño",
                SGPP.Domain.Enums.Carrera.CIV or SGPP.Domain.Enums.Carrera.ARQ or SGPP.Domain.Enums.Carrera.IAM => "Proyectos e Infraestructura",
                _ => "Gestión y Consultoría"
            };
        }

        return "Gestión y Consultoría";
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
        var worksheet2 = workbook.Worksheets.Add("Sectores Laborales");
        worksheet2.Cell(1, 1).Value = "Sector Laboral / Área";
        worksheet2.Cell(1, 2).Value = "Cantidad Estudiantes";

        var areaStats = await _context.Asignaciones
            .Include(a => a.TutorInstitucional)
            .Include(a => a.Estudiante)
            .Where(a => a.TutorInstitucional != null 
                     && a.TutorInstitucional.CentroPractica.RazonSocial != "SoftCorp SRL")
            .ToListAsync();

        var areaGrouped = areaStats
            .Select(a => GetMacroArea(a.TutorInstitucional.AreaTrabajo, a.Estudiante?.Carrera))
            .GroupBy(area => area)
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
