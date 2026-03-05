using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Usuarios.Estudiantes;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public CreateModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public Carrera Carrera { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // 1. Create ApplicationUser
        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            Nombre = Input.Nombre,
            Apellido = Input.Apellido,
            EsActivo = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        // 2. Assign Role
        await _userManager.AddToRoleAsync(user, "Estudiante");

        // 3. Create Estudiante Profile
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

        return RedirectToPage("./Index");
    }
}
