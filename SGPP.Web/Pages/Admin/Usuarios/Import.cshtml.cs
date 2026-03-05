using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGPP.Infrastructure.Services;

namespace SGPP.Web.Pages.Admin.Usuarios;

[Authorize(Roles = "Admin")]
public class ImportModel : PageModel
{
    private readonly IUserImportService _importService;

    public ImportModel(IUserImportService importService)
    {
        _importService = importService;
    }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    public ImportResult? Result { get; set; }

    // To persist the active tab after post
    [TempData]
    public string ActiveTab { get; set; } = "students"; 

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostStudentsAsync()
    {
        ActiveTab = "students";
        return await ProcessImport(file => _importService.ImportStudentsAsync(file));
    }

    public async Task<IActionResult> OnPostTeachersAsync()
    {
        ActiveTab = "teachers";
        return await ProcessImport(file => _importService.ImportTeachersAsync(file));
    }

    public async Task<IActionResult> OnPostTutorsAsync()
    {
        ActiveTab = "tutors";
        return await ProcessImport(file => _importService.ImportTutorsAsync(file));
    }

    private async Task<IActionResult> ProcessImport(Func<Stream, Task<ImportResult>> importFunc)
    {
        if (UploadFile == null || UploadFile.Length == 0)
        {
            ModelState.AddModelError("", "Por favor seleccione un archivo Excel.");
            return Page();
        }

        if (!Path.GetExtension(UploadFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
             ModelState.AddModelError("", "El archivo debe ser un Excel (.xlsx).");
             return Page();
        }

        try
        {
            using var stream = UploadFile.OpenReadStream();
            Result = await importFunc(stream);
            
            if (Result.Errors.Count == 0 && Result.UsersCreated == 0 && Result.CompaniesCreated == 0)
            {
                ModelState.AddModelError("", "El archivo parece estar vacío o no se crearon registros.");
            }
            else if (Result.Errors.Any())
            {
                TempData["ImportWarning"] = "La importación finalizó con algunos errores.";
            }
            else
            {
                TempData["SuccessMessage"] = "Importación completada exitosamente.";
            }

            return Page();
        }
        catch (Exception ex)
        {
             ModelState.AddModelError("", $"Error crítico al procesar el archivo: {ex.Message}");
             return Page();
        }
    }
}
