using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Periodos;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        // Default values
        Periodo = new Periodo
        {
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddMonths(4),
            FechaInicioEvaluacion = DateTime.Today.AddMonths(3),
            FechaFinEvaluacion = DateTime.Today.AddMonths(4)
        };
        return Page();
    }

    [BindProperty]
    public Periodo Periodo { get; set; } = default!;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Periodo.Activo)
        {
            // Transactional deactivation
             using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                 // 1. Deactivate all
                await _context.Database.ExecuteSqlRawAsync("UPDATE Periodos SET Activo = 0");
                
                // 2. Add new
                _context.Periodos.Add(Periodo);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch(Exception)
            {
                 await transaction.RollbackAsync();
                 throw;
            }
        }
        else
        {
             _context.Periodos.Add(Periodo);
             await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
