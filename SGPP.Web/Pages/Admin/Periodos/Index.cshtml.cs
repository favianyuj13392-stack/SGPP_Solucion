using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Periodos;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Periodo> Periodos { get;set; } = default!;

    public async Task OnGetAsync()
    {
        Periodos = await _context.Periodos
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSetActiveAsync(int id)
    {
        // 1. Deactivate ALL active periods using EF Core (Safe & Tracked)
        var activePeriods = await _context.Periodos.Where(p => p.Activo).ToListAsync();
        foreach (var p in activePeriods)
        {
            p.Activo = false;
        }

        // 2. Activate the selected one
        var periodo = await _context.Periodos.FindAsync(id);
        if (periodo != null)
        {
            periodo.Activo = true;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}
