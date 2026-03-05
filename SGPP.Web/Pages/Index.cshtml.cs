using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using SGPP.Domain.Entities;

namespace SGPP.Web.Pages;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
             var user = await _userManager.GetUserAsync(User);
             if (user != null)
             {
                if (await _userManager.IsInRoleAsync(user, "Admin")) return RedirectToPage("/Admin/Dashboard");
                if (await _userManager.IsInRoleAsync(user, "Tutor")) return RedirectToPage("/Tutor/Dashboard");
                if (await _userManager.IsInRoleAsync(user, "TutorAcademico")) return RedirectToPage("/Academic/Dashboard");
                if (await _userManager.IsInRoleAsync(user, "Estudiante")) return RedirectToPage("/Student/Dashboard");
             }
        }
        return Page();
    }
}
