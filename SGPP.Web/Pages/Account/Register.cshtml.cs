using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Correo Institucional")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "El {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "El {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 2)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Código Estudiante")]
        public string CodigoEstudiante { get; set; } = string.Empty;

        [Required]
        public Carrera Carrera { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Student/Dashboard");

        if (ModelState.IsValid)
        {
            // Validate Domain
            if (!Input.Email.ToLower().EndsWith("@ucb.edu.bo"))
            {
                ModelState.AddModelError(string.Empty, "Solo se permiten correos institucionales (@ucb.edu.bo).");
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                Nombre = Input.Nombre,
                Apellido = Input.Apellido,
                EsActivo = true,
                EmailConfirmed = true // Auto-confirm for now
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                // Assign Role
                await _userManager.AddToRoleAsync(user, "Estudiante");

                // Create Profile
                var estudiante = new Estudiante
                {
                    ApplicationUserId = user.Id,
                    CodigoEstudiante = Input.CodigoEstudiante,
                    Carrera = Input.Carrera,
                    EmailInstitucional = Input.Email,
                    EstadoAcademico = EstadoAcademico.Habilitado
                };
                _context.Estudiantes.Add(estudiante);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
    }
}
