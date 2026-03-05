using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;

namespace SGPP.Web.Pages.Admin.Docentes;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IList<ApplicationUser> Docentes { get; set; } = new List<ApplicationUser>();

    public async Task OnGetAsync()
    {
        // Get users in role "TutorAcademico"
        var usersInRole = await _userManager.GetUsersInRoleAsync("TutorAcademico");
        Docentes = usersInRole.OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToList();
    }
}
