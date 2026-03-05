using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Empresas;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public CentroPractica CentroPractica { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var centro = await _context.CentrosPractica.FirstOrDefaultAsync(m => m.Id == id);
        if (centro == null) return NotFound();
        
        CentroPractica = centro;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _context.Attach(CentroPractica).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CentroExists(CentroPractica.Id)) return NotFound();
            else throw;
        }

        return RedirectToPage("./Index");
    }

    private bool CentroExists(int id)
    {
        return _context.CentrosPractica.Any(e => e.Id == id);
    }
}
