using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SGPP.Infrastructure.Persistence;
using SGPP.Domain.Entities;

namespace SGPP.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly ApplicationDbContext _context;

    public ExcelExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public XLWorkbook GenerateFormA(int asignacionId, string wwwRootPath)
    {
        var form = _context.EvaluacionesEstudiante
            .Include(f => f.Asignacion).ThenInclude(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Include(f => f.Asignacion).ThenInclude(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Include(f => f.Asignacion).ThenInclude(a => a.TutorInstitucional).ThenInclude(t => t.ApplicationUser)
            .Include(f => f.Asignacion).ThenInclude(a => a.Periodo)
            .Include(f => f.Detalles)
            .FirstOrDefault(f => f.AsignacionId == asignacionId);

        if (form == null) throw new Exception("Formulario A no encontrado");

        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Formulario A");

        // A. Visual Config
        // Col A(65), B(12), C(30), D(30)
        ws.Column("A").Width = 65;
        ws.Column("B").Width = 12;
        ws.Column("C").Width = 30;
        ws.Column("D").Width = 30;

        // Base Font: Arial 10
        var globalStyle = ws.Style;
        globalStyle.Font.FontName = "Arial";
        globalStyle.Font.FontSize = 10;
        globalStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // B. Header (Rows 1-14)
        // Logo
        var logoPath = Path.Combine(wwwRootPath, "img", "logo_ucb.png");
        if (File.Exists(logoPath))
        {
            try {
                var picture = ws.AddPicture(logoPath).MoveTo(ws.Cell("A1"));
                picture.Width = 150; 
                picture.Height = 50; 
            } catch { ws.Cell("A1").Value = "[LOGO]"; }
        }

        // Titles
        ws.Range("A2:D2").Merge().Value = "FORMULARIO DE EVALUACIÓN A LOS CENTROS DE PRÁCTICA";
        ws.Range("A2:D2").Style.Font.Bold = true; ws.Range("A2:D2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range("A3:D3").Merge().Value = "PRÁCTICA PRE PROFESIONAL";
        ws.Range("A3:D3").Style.Font.Bold = true; ws.Range("A3:D3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range("A4:D4").Merge().Value = "(V 3.0/2025)";
        ws.Range("A4:D4").Style.Font.Bold = true; ws.Range("A4:D4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Student Data
        ws.Cell("A6").Value = "Centro de Práctica:"; ws.Cell("A6").Style.Font.Bold = true;
        ws.Range("B6:D6").Merge().Value = form.Asignacion.TutorInstitucional.CentroPractica.RazonSocial;
        ws.Range("A6:D6").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        ws.Cell("A7").Value = "Tutor Institucional:"; ws.Cell("A7").Style.Font.Bold = true;
        ws.Range("B7:D7").Merge().Value = $"{form.Asignacion.TutorInstitucional.ApplicationUser.Nombre} {form.Asignacion.TutorInstitucional.ApplicationUser.Apellido}";
        ws.Range("A7:D7").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        ws.Cell("A8").Value = "Área asignada:"; ws.Cell("A8").Style.Font.Bold = true;
        ws.Range("B8:D8").Merge().Value = form.AreaAsignada ?? "N/A";
        ws.Range("A8:D8").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        // Row 9 
        string sem = form.Asignacion.Periodo?.CodigoGestion ?? "N/A";
        int yr = DateTime.Now.Year;
        double hrs = form.HorasTrabajadas;
        ws.Cell("A9").Value = $"Semestre: {sem} / Año: {yr} / Horas trabajadas: {hrs}";
        ws.Range("A9:D9").Merge();
        ws.Range("A9:D9").Style.Font.Bold = true;
        ws.Range("A9:D9").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        // Row 10
        string dStart = form.FechaInicio.HasValue ? form.FechaInicio.Value.ToString("dd/MM/yyyy") : "--";
        string dEnd = form.FechaFin.HasValue ? form.FechaFin.Value.ToString("dd/MM/yyyy") : "--";
        string dEval = form.FechaEvaluacion.ToString("dd/MM/yyyy");
        ws.Cell("A10").Value = $"Fecha Inicio: {dStart} | Fecha Fin: {dEnd} | Fecha Evaluación: {dEval}";
        ws.Range("A10:D10").Merge();
        ws.Range("A10:D10").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        // Legal Text (12-14)
        var legal = ws.Range("A12:D14");
        legal.Merge().Value = "ESTE FORMULARIO DEBE SER LLENADO DE FORMA EXCLUSIVA POR EL ESTUDIANTE. Su objetivo es evaluar el desempeño del Centro de Práctica y del Tutor Institucional. Califique de 1 a 4 donde: 4=Totalmente Satisfecho, 1=Muy Insatisfecho.";
        legal.Style.Font.Italic = true;
        legal.Style.Font.FontSize = 9;
        legal.Style.Alignment.WrapText = true;
        legal.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // C. Questions Loop Setup
        int r = 16;
        ws.Cell(r, 1).Value = "Criterios / Preguntas";
        ws.Cell(r, 2).Value = "Calificación";
        ws.Cell(r, 3).Value = "Justificación";
        ws.Cell(r, 4).Value = "Observaciones";
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Dictionary
        var questionsDict = new Dictionary<int, string>
        {
            {1, "1. El centro de prácticas te proporcionó un espacio físico para desarrollar tus actividades."},
            {2, "2. Contabas con equipo computacional y/o recursos tecnológicos adecuados para tu trabajo."},
            {3, "3. El ambiente laboral fue positivo con relación al trato recibido por el supervisor (Tutor institucional)."},
            {4, "4. Las relaciones laborales con los compañeros de trabajo fueron agradables."},
            {5, "5. La cultura organizacional del centro de práctica apoya el crecimiento de su personal."},
            {6, "6. Las actividades que realizaste en el centro de práctica fueron relevantes para tu formación académica."},
            {7, "7. El centro de práctica ofreció oportunidades adicionales para tu desarrollo profesional (capacitaciones, talleres, similares)."},
            {8, "8. El centro de práctica cumplió con tus expectativas iniciales."},
            {9, "9. Se reconoce espacios de crecimiento profesional (puesto laboral) adicionales a las prácticas pre-profesionales."},
            {10, "10. Recibiste la información necesaria para el desarrollo de las tareas asignadas."},
            {11, "11. Recibiste un seguimiento adecuado por parte de tu supervisor (Tutor(a) institucional) en el centro de práctica."},
            {12, "12. Se realizaron evaluaciones claras y secuenciales con relación a las tareas asignadas."},
            {13, "13. Tuviste una retroalimentación clara y constructiva sobre el desempeño en el centro de práctica."},
            {14, "14. Consideras que las tareas asignadas en el centro de práctica estuvieron relacionadas con tu carrera."},
            {15, "15. El profesor de la asignatura (Tutor(a) académico(a)) te ofreció posibles centros de práctica acordes a tus intereses profesionales."},
            {16, "16. Consideras que la orientación del profesor de la asignatura (Tutor(a) académico(a)) te ayudó a encontrar una práctica adecuada."},
            {17, "17.  El/la tutor(a) académico(a) daba explicaciones claras en las sesiones teóricas de la asignatura."},
            {18, "18. Existen horarios accesibles para consultas con el/la tutor(a) académico(a)."},
            {19, "19. El/la Tutor(a) académico(a) apoyo a resolver dudas sobre las tareas asignadas en el centro de práctica."},
            {20, "20. El/la tutor(a) académico(a) te proporcionó retroalimentación útil durante tu experiencia de práctica."}
        };

        // --- SECTION 1 ---
        r++;
        ws.Cell(r, 1).Value = "1. SOBRE EL CENTRO DE PRÁCTICA";
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        ws.Range(r, 1, r, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        // Loop 1-9
        for (int i = 1; i <= 9; i++) RenderQuestionRow(ws, ref r, i, questionsDict[i], form);

        // --- SECTION 2 ---
        r++;
        ws.Cell(r, 1).Value = "2. SOBRE EL TUTOR INSTITUCIONAL";
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        ws.Range(r, 1, r, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        // Loop 10-14
        for (int i = 10; i <= 14; i++) RenderQuestionRow(ws, ref r, i, questionsDict[i], form);

        // --- SECTION 3 ---
        r++;
        ws.Cell(r, 1).Value = "3. SOBRE EL TUTOR ACADÉMICO";
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        ws.Range(r, 1, r, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        // Loop 15-20
        for (int i = 15; i <= 20; i++) RenderQuestionRow(ws, ref r, i, questionsDict[i], form);
        
        // --- NEW FOOTER LAYOUT (Step 1497) ---
        r++; // Spacer

        // 1. CENTRO DE PRACTICA
        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "CENTRO DE PRÁCTICA";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Range(r, 1, r, 2).Merge().Value = "Principales Fortalezas";
        ws.Range(r, 3, r, 4).Merge().Value = "Principales Limitaciones";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Range(r, 1, r+2, 2).Merge().Value = form.FortalezasCentro ?? "";
        ws.Range(r, 3, r+2, 4).Merge().Value = form.LimitacionesCentro ?? "";
        ws.Range(r, 1, r+2, 4).Style.Alignment.WrapText = true;
        ws.Range(r, 1, r+2, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range(r, 1, r+2, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r+2, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        r += 2;

        r++; // Spacer
        
        // 2. TUTOR(A) INSTITUCIONAL
        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "TUTOR(A) INSTITUCIONAL";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Range(r, 1, r, 2).Merge().Value = "Principales Fortalezas";
        ws.Range(r, 3, r, 4).Merge().Value = "Principales Limitaciones";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Range(r, 1, r+2, 2).Merge().Value = form.FortalezasTutor ?? "";
        ws.Range(r, 3, r+2, 4).Merge().Value = form.LimitacionesTutor ?? "";
        ws.Range(r, 1, r+2, 4).Style.Alignment.WrapText = true;
        ws.Range(r, 1, r+2, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range(r, 1, r+2, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r+2, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        r += 2;

        r++; // Spacer

        // 3. RECOMENDACIONES
        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "RECOMENDACIONES";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Range(r, 1, r, 2).Merge().Value = "Centro de Práctica";
        ws.Range(r, 3, r, 4).Merge().Value = "Tutor(a) Institucional";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Range(r, 1, r+2, 2).Merge().Value = form.RecomendacionesCentro ?? "";
        ws.Range(r, 3, r+2, 4).Merge().Value = form.RecomendacionesTutor ?? "";
        ws.Range(r, 1, r+2, 4).Style.Alignment.WrapText = true;
        ws.Range(r, 1, r+2, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range(r, 1, r+2, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r+2, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        r += 2;


        // --- SIGNATURES ---
        r += 5;
        ws.Cell(r, 1).Value = "__________________________";
        ws.Cell(r, 3).Value = "__________________________";
        ws.Range(r, 1, r, 2).Merge(); 
        ws.Range(r, 3, r, 4).Merge();

        r++;
        ws.Cell(r, 1).Value = "Estudiante";
        ws.Range(r, 1, r, 2).Merge();
        
        ws.Cell(r, 3).Value = "Recibido por (UCB)";
        ws.Range(r, 3, r, 4).Merge();

        ws.Range(r-1, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        return wb;
    }

    private void RenderQuestionRow(IXLWorksheet ws, ref int r, int id, string text, FormularioA_Estudiante form)
    {
        r++;
        var ans = form.Detalles.FirstOrDefault(d => d.PreguntaKey == id);
        
        ws.Cell(r, 1).Value = text;
        ws.Cell(r, 1).Style.Alignment.WrapText = true;
        ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        if (ans != null)
        {
            ws.Cell(r, 2).Value = ans.Valor;
            ws.Cell(r, 3).Value = ans.Justificacion;
            ws.Cell(r, 4).Value = ans.Observaciones;
        }
        else
        {
            ws.Cell(r, 2).Value = "-";
        }

        ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(r, 3).Style.Alignment.WrapText = true;
        ws.Cell(r, 4).Style.Alignment.WrapText = true;
        
        ws.Range(r, 1, r, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    public XLWorkbook GenerateFormB(int asignacionId, string wwwRootPath)
    {
        var form = _context.EvaluacionesEmpresa
            .Include(f => f.Asignacion).ThenInclude(a => a.Estudiante).ThenInclude(e => e.ApplicationUser)
            .Include(f => f.Asignacion).ThenInclude(a => a.TutorInstitucional).ThenInclude(t => t.CentroPractica)
            .Include(f => f.Asignacion).ThenInclude(a => a.TutorInstitucional).ThenInclude(t => t.ApplicationUser)
            .Include(f => f.Asignacion).ThenInclude(a => a.Periodo)
            .Include(f => f.Detalles)
            .Include(f => f.Tareas)
            .FirstOrDefault(f => f.AsignacionId == asignacionId);

        if (form == null) throw new Exception("Formulario B no encontrado");

        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Formulario B");

        // --- A. VISUAL CONFIG & HEADER ---
        ws.Column(1).Width = 50; // A
        ws.Column(2).Width = 12; // B
        ws.Column(3).Width = 35; // C
        ws.Column(4).Width = 35; // D
        
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 10;

        // Logo A1
        var logoPath = Path.Combine(wwwRootPath, "img", "logo_ucb.png");
        if (File.Exists(logoPath))
        {
            try { ws.AddPicture(logoPath).MoveTo(ws.Cell("A1")).Scale(0.6); } catch { }
        }

        // Titles
        ws.Range("A2:D2").Merge().Value = "FORMULARIO DE EVALUACIÓN AL PROFESIONAL EN FORMACIÓN";
        ws.Range("A3:D3").Merge().Value = "PRÁCTICA PRE PROFESIONAL";
        ws.Range("A2:D3").Style.Font.Bold = true;
        ws.Range("A2:D3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range("A2:D3").Style.Font.FontSize = 12;

        int r = 5; 
        // Student Info Block
        // Row 6
        r++;
        ws.Cell(r, 1).Value = $"Profesional en formación: {form.Asignacion.Estudiante.ApplicationUser.Nombre} {form.Asignacion.Estudiante.ApplicationUser.Apellido}";
        ws.Range(r, 1, r, 4).Merge(); // Span entire width for long names
        
        // Row 7
        r++;
        ws.Cell(r, 1).Value = $"Tutor(a) Académico(a): (Pendiente)"; // Ideally fetch from relation
        ws.Cell(r, 3).Value = $"Departamento: {form.Asignacion.Estudiante.Carrera}"; 
        
        // Row 8
        r++;
        ws.Cell(r, 1).Value = $"Tutor(a) Institucional: {form.Asignacion.TutorInstitucional.ApplicationUser.Nombre} {form.Asignacion.TutorInstitucional.ApplicationUser.Apellido}";
        ws.Cell(r, 3).Value = $"Centro de Práctica: {form.Asignacion.TutorInstitucional.CentroPractica.RazonSocial}";

        // Row 9
        r++;
        ws.Cell(r, 1).Value = $"Semestre: {form.Asignacion.Periodo.CodigoGestion}"; 
        ws.Cell(r, 2).Value = $"Año: {DateTime.Now.Year}";
        ws.Cell(r, 3).Value = $"Horas trabajadas: {form.HorasTrabajadas}";
        
        // Row 10
        r++;
        ws.Cell(r, 1).Value = $"Fecha Inicio: {form.Asignacion.Periodo.FechaInicio:dd/MM/yyyy}";
        ws.Cell(r, 2).Value = $"Fecha Fin: {form.Asignacion.Periodo.FechaFin:dd/MM/yyyy}";
        ws.Cell(r, 3).Value = $"Fecha Evaluación: {form.FechaEvaluacion:dd/MM/yyyy}";

        // Styling Info Block (Rows 6-10)
        ws.Range(6, 1, 10, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(6, 1, 10, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Legal Text (12-14)
        r += 2; // 12
        var legalRange = ws.Range(r, 1, r+2, 4);
        legalRange.Merge().Value = "* ESTE FORMULARIO DEBE SER LLENADO DE FORMA EXCLUSIVA POR EL SUPERVISOR, ENCARGADO, RESPONSABLE DE LA PRÁCTICA PREPROFESIONAL (TUTOR INSTITUCIONAL). La información brindada ayuda a la U.C.B. a mejorar sus procesos académicos. Lea cuidadosamente cada criterio y ponga la calificación del 1 al 4 de acuerdo a: 4 es Totalmente Satisfecho, 3 es Satisfecho, 2 es Insatisfecho, 1 es Muy Insatisfecho.";
        legalRange.Style.Font.Italic = true;
        legalRange.Style.Font.FontSize = 9;
        legalRange.Style.Alignment.WrapText = true;
        legalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        r += 2;

        // --- B. SECCIÓN I: DETALLE DE TAREAS ---
        r++; // Spacer
        r++; 
        ws.Range(r, 1, r, 4).Merge().Value = "I. DETALLE DE TAREAS REALIZADAS";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Cell(r, 1).Value = "Tareas encargadas al/a la profesional en formación";
        ws.Cell(r, 2).Value = "% Cumplim."; // Abbreviated for space
        ws.Cell(r, 3).Value = "Aspectos Positivos";
        ws.Cell(r, 4).Value = "Aspectos a Mejorar";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Tasks Loop (Min 5 rows)
        int tasksCount = form.Tareas.Count;
        int rowsToRender = Math.Max(tasksCount, 5);

        for (int i = 0; i < rowsToRender; i++)
        {
            r++;
            if (i < tasksCount)
            {
                var t = form.Tareas.ElementAt(i);
                ws.Cell(r, 1).Value = $"{i+1}. {t.DescripcionTarea}";
                ws.Cell(r, 2).Value = t.Cumplimiento + "%";
                ws.Cell(r, 3).Value = t.AspectosPositivos;
                ws.Cell(r, 4).Value = t.AspectosMejorar;
            }
            else
            {
                ws.Cell(r, 1).Value = $"{i+1}.";
            }
            // Style row
             ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
             ws.Range(r, 1, r, 4).Style.Alignment.WrapText = true;
             ws.Range(r, 1, r, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }

        // --- C. SECCIÓN II: CONOCIMIENTOS TÉCNICOS ---
        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "II. EVALUACIÓN DE CONOCIMIENTOS TÉCNICOS";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Cell(r, 1).Value = "Competencias del Proceso (Criterios)";
        ws.Cell(r, 2).Value = "Valoración";
        ws.Range(r, 3, r, 4).Merge().Value = "Justificación (Opcional)";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var techQuestions = new Dictionary<int, string> {
            {1, "1. El practicante posee los conocimientos técnicos necesarios para las tareas asignadas."},
            {2, "2. Aplica correctamente los conocimientos teóricos en la práctica"},
            {3, "3. Tiene capacidad para identificar y resolver problemas técnicos"},
            {4, "4. Demuestra habilidades en el uso de herramientas o software especializado"},
            {5, "5. Se adapta a nuevos procesos o metodologías en el área de trabajo"},
            {6, "6. Entrega sus trabajos con calidad y precisión"},
            {7, "7. Muestra iniciativa en la búsqueda de soluciones técnicas"},
            {8, "8. Sigue adecuadamente los protocolos y normativas de la institución"}
        };

        foreach (var kvp in techQuestions)
        {
            r++;
            RenderEvalRow(ws, r, kvp.Key, kvp.Value, form.Detalles);
        }

        // Partial Score Technical
        r++;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; 
        ws.Cell(r, 1).Value = "Total Conocimientos Técnicos";
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        
        // Sum values matching keys 1-8
        var techScoreRaw = form.Detalles.Where(d => d.PreguntaKey <= 8).Sum(d => d.Valor);
        ws.Cell(r, 2).Value = techScoreRaw; 
        ws.Cell(r, 2).Style.Font.Bold = true;
        ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // --- D. SECCIÓN III: POWER SKILLS ---
        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "III. EVALUACIÓN DE COMPETENCIAS BLANDAS (POWER SKILLS)";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Cell(r, 1).Value = "Detalle";
        ws.Cell(r, 2).Value = "Valoración";
        ws.Range(r, 3, r, 4).Merge().Value = "Justificación (Opcional)";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var softQuestions = new Dictionary<int, string> {
            {9, "1. El practicante se comunica de manera clara y efectiva con sus compañeros y superiores."},
            {10, "2. Trabaja bien en equipo y contribuye a un ambiente colaborativo."},
            {11, "3. Maneja adecuadamente la presión y se adapta a cambios."},
            {12, "4. Muestra una actitud proactiva en el desarrollo de sus funciones."},
            {13, "5. Es puntual y cumple con los plazos establecidos"},
            {14, "6. Es organizado con las tareas asignadas y productos entregados."},
            {15, "7. Acepta y aplica retroalimentación de manera positiva."},
            {16, "8. Demuestra compromiso con las tareas asignadas."},
            {17, "9. Tiene iniciativa para proponer mejoras en el trabajo"}
        };

        foreach (var kvp in softQuestions)
        {
            r++;
            RenderEvalRow(ws, r, kvp.Key, kvp.Value, form.Detalles);
        }

        // Partial Score Power Skills
        r++;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Cell(r, 1).Value = "Total Power Skills";
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var skillScore = form.Detalles.Where(d => d.PreguntaKey >= 9 && d.PreguntaKey <= 18).Sum(d => d.Valor);
        ws.Cell(r, 2).Value = skillScore;
        ws.Cell(r, 2).Style.Font.Bold = true;
        ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // --- E. SECCIÓN FINAL (RESUMEN) ---
        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "EVALUACIÓN FINAL";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        r++;
        ws.Cell(r, 1).Value = "EVALUACIÓN 1: Conocimientos técnicos (Ponderado)";
        
        var techWeighted = techScoreRaw * 2;
        
        ws.Range(r, 1, r, 3).Merge();
        ws.Cell(r, 4).Value = techWeighted; 
        ws.Cell(r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        r++;
        ws.Cell(r, 1).Value = "EVALUACIÓN 2: Power Skills (Ponderado)";
        ws.Range(r, 1, r, 3).Merge();
        ws.Cell(r, 4).Value = skillScore;
        ws.Cell(r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        r++;
        ws.Cell(r, 1).Value = "TOTAL EVALUACIÓN";
        ws.Range(r, 1, r, 3).Merge(); 
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        
        ws.Cell(r, 4).Value = techWeighted + skillScore;
        ws.Cell(r, 4).Style.Font.Bold = true;
        ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;


        // Qualitative
        r += 2;
        ws.Range(r, 1, r, 4).Merge().Value = "Principales FORTALEZAS:";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        r++;
        ws.Range(r, 1, r+1, 4).Merge().Value = form.FortalezasTexto ?? "";
        ws.Range(r, 1, r+1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r+1, 4).Style.Alignment.WrapText = true;
        ws.Range(r, 1, r+1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        r += 2;

        r++;
        ws.Range(r, 1, r, 4).Merge().Value = "Áreas a FORTALECER:";
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        r++;
        ws.Range(r, 1, r+1, 4).Merge().Value = form.AreasMejoraTexto ?? "";
        ws.Range(r, 1, r+1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r+1, 4).Style.Alignment.WrapText = true;
        ws.Range(r, 1, r+1, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        r += 2;

        // Signatures
        r += 5;
        ws.Cell(r, 1).Value = "__________________________";
        ws.Cell(r, 4).Value = "__________________________";
        ws.Range(r, 1, r, 2).Merge(); 
        // ws.Range(r, 3, r, 4).Merge();

        r++;
        ws.Cell(r, 1).Value = "Firma Tutor(a) Académico(a)";
        ws.Range(r, 1, r, 2).Merge();
        
        ws.Cell(r, 4).Value = "Firma Tutor(a) Institucional";
        // ws.Range(r, 3, r, 4).Merge();

        ws.Range(r-1, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        r += 3;
        ws.Cell(r, 2).Value = "__________________________";
        ws.Range(r, 2, r, 3).Merge();
         ws.Range(r, 2, r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r++;
        ws.Cell(r, 2).Value = "Firma Profesional en formación";
        ws.Range(r, 2, r, 3).Merge();
        ws.Range(r, 2, r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        wb.CalculateMode = XLCalculateMode.Auto;
        return wb;
    }

    private void RenderEvalRow(IXLWorksheet ws, int r, int id, string text, ICollection<SGPP.Domain.Entities.FormularioB_DetalleRespuestas> answers)
    {
        var ans = answers.FirstOrDefault(a => a.PreguntaKey == id);
        ws.Cell(r, 1).Value = text;
        ws.Cell(r, 1).Style.Alignment.WrapText = true;
        
        if (ans != null)
        {
            ws.Cell(r, 2).Value = ans.Valor;
            ws.Range(r, 3, r, 4).Merge().Value = ans.Justificacion;
        }
        else
        {
            ws.Cell(r, 2).Value = "-";
        }
        
        ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(r, 3, r, 4).Style.Alignment.WrapText = true;

        ws.Range(r, 1, r, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }
}
