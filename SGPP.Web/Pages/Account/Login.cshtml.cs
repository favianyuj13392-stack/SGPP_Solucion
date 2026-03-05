using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGPP.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
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

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    // Role-based redirection logic
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToPage("/Admin/Dashboard");
                    }
                    if (await _userManager.IsInRoleAsync(user, "Tutor"))
                    {
                        return RedirectToPage("/Tutor/Dashboard");
                    }
                    if (await _userManager.IsInRoleAsync(user, "Estudiante"))
                    {
                        return RedirectToPage("/Student/Dashboard");
                    }
                }
                
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión no válido.");
                return Page();
            }
        }

        return Page();
    }
    
    public async Task<IActionResult> OnPostLogoutAsync(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        if (returnUrl != null)
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            return RedirectToPage("/Index");
        }
    }
}
