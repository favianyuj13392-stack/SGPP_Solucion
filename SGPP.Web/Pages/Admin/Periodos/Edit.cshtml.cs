using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Periodos;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Periodo Periodo { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var periodo = await _context.Periodos.FirstOrDefaultAsync(m => m.Id == id);
        if (periodo == null) return NotFound();

        Periodo = periodo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Handle Active Toggle
        if (Periodo.Activo)
        {
             using var transaction = await _context.Database.BeginTransactionAsync();
             try 
             {
                 await _context.Database.ExecuteSqlRawAsync("UPDATE Periodos SET Activo = 0");
                 
                 _context.Attach(Periodo).State = EntityState.Modified;
                 await _context.SaveChangesAsync();

                 await transaction.CommitAsync();
             }
             catch
             {
                 await transaction.RollbackAsync();
                 throw;
             }
        }
        else
        {
            _context.Attach(Periodo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
