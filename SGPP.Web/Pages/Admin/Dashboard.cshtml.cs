using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Periodo? ActivePeriod { get; set; }

    public async Task OnGetAsync()
    {
        ActivePeriod = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
    }

    public async Task<IActionResult> OnPostTogglePeriodAsync(int id)
    {
        var period = await _context.Periodos.FindAsync(id);
        if (period == null) return RedirectToPage();

        if (period.Activo)
        {
            // Deactivate
            period.Activo = false;
            await _context.SaveChangesAsync();
        }
        else
        {
            // Activate (Exclusive)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("UPDATE Periodos SET Activo = 0");
                period.Activo = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return RedirectToPage();
    }
}
