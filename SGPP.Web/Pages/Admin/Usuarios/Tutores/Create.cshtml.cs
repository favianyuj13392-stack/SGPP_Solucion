using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Usuarios.Tutores;

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

    public SelectList? CentrosPractica { get; set; }

    public class InputModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        public int CentroPracticaId { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        CentrosPractica = new SelectList(await _context.CentrosPractica.ToListAsync(), "Id", "RazonSocial");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
             CentrosPractica = new SelectList(await _context.CentrosPractica.ToListAsync(), "Id", "RazonSocial");
             return Page();
        }

        // 1. Create User
        var user = new ApplicationUser
        {
            UserName = Input.Email, // Email as Username
            Email = Input.Email,
            Nombre = Input.Nombre,
            Apellido = Input.Apellido,
            EsActivo = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            CentrosPractica = new SelectList(await _context.CentrosPractica.ToListAsync(), "Id", "RazonSocial");
            return Page();
        }

        await _userManager.AddToRoleAsync(user, "Tutor");

        // 2. Create Tutor Entity
        var tutor = new TutorInstitucional
        {
            ApplicationUserId = user.Id,
            CentroPracticaId = Input.CentroPracticaId,
            Cargo = Input.Cargo,
            TelefonoContacto = Input.Telefono,
            Ci = Input.Ci
        };

        _context.TutoresInstitucionales.Add(tutor);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
