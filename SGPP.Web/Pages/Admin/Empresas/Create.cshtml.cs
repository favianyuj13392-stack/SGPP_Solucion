using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Empresas;

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
        return Page();
    }

    [BindProperty]
    public CentroPractica CentroPractica { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.CentrosPractica.Add(CentroPractica);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
