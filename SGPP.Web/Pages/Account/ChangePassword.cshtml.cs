using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGPP.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Web.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "La contraseña actual es requerida.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual (temporal)")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida.")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("NewPassword", ErrorMessage = "La nueva contraseña y la confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("./Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("./Login");
        }

        if (Input.CurrentPassword == Input.NewPassword)
        {
            ModelState.AddModelError(string.Empty, "La nueva contraseña no puede ser idéntica a la contraseña temporal.");
            return Page();
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        user.DebeCambiarPassword = false;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        // Redirect to role dashboard
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToPage("/Admin/Dashboard");
        }
        if (await _userManager.IsInRoleAsync(user, "Tutor"))
        {
            return RedirectToPage("/Tutor/Dashboard");
        }
        if (await _userManager.IsInRoleAsync(user, "TutorAcademico"))
        {
            return RedirectToPage("/Academic/Dashboard");
        }
        if (await _userManager.IsInRoleAsync(user, "Estudiante"))
        {
            return RedirectToPage("/Student/Dashboard");
        }

        return RedirectToPage("/Index");
    }
}
