using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Empresas;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<CentroPractica> Centros { get; set; } = new List<CentroPractica>();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.CentrosPractica.AsQueryable();

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            query = query.Where(c => c.RazonSocial.Contains(SearchTerm) || (c.Nit != null && c.Nit.Contains(SearchTerm)));
        }

        Centros = await query.OrderBy(c => c.RazonSocial).ToListAsync();
    }
}
