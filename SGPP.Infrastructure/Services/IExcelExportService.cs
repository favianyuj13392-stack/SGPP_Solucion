using ClosedXML.Excel;

namespace SGPP.Infrastructure.Services;

public interface IExcelExportService
{
    XLWorkbook GenerateFormA(int asignacionId, string wwwRootPath);
    XLWorkbook GenerateFormB(int asignacionId, string wwwRootPath);
}
