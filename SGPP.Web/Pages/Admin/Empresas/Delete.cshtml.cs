using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Empresas;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public CentroPractica CentroPractica { get; set; } = null!;
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var centro = await _context.CentrosPractica.FirstOrDefaultAsync(m => m.Id == id);

        if (centro == null) return NotFound();
        CentroPractica = centro;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null) return NotFound();

        var centro = await _context.CentrosPractica
            .Include(c => c.Tutores) // Check relationships
            .FirstOrDefaultAsync(m => m.Id == id);

        if (centro == null) return NotFound();

        // Validation: Cannot delete if Tutors exist
        if (centro.Tutores.Any())
        {
            CentroPractica = centro;
            ErrorMessage = "No se puede eliminar la empresa porque tiene tutores o prácticas vinculadas.";
            return Page();
        }

        CentroPractica = centro;
        _context.CentrosPractica.Remove(CentroPractica);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
