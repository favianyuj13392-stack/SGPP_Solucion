using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Admin.Usuarios.Estudiantes;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public EditModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public int EstudianteId { get; set; }

    public class InputModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // Read-only mostly? Or editable?
        public string CodigoEstudiante { get; set; } = string.Empty;
        public Carrera Carrera { get; set; }
        public bool EsActivo { get; set; }
    }

    [BindProperty]
    public string? NewPassword { get; set; } // Only for Reset

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var estudiante = await _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estudiante == null) return NotFound();

        EstudianteId = estudiante.Id;
        Input = new InputModel
        {
            Nombre = estudiante.ApplicationUser.Nombre,
            Apellido = estudiante.ApplicationUser.Apellido,
            Email = estudiante.ApplicationUser.Email ?? "",
            CodigoEstudiante = estudiante.CodigoEstudiante,
            Carrera = estudiante.Carrera,
            EsActivo = estudiante.ApplicationUser.EsActivo
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var estudiante = await _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.Id == EstudianteId);

        if (estudiante == null) return NotFound();

        // Update User Info
        estudiante.ApplicationUser.Nombre = Input.Nombre;
        estudiante.ApplicationUser.Apellido = Input.Apellido;
        estudiante.ApplicationUser.EsActivo = Input.EsActivo;
        
        // Update Email (Handle UserName too)
        if (estudiante.ApplicationUser.Email != Input.Email)
        {
             estudiante.ApplicationUser.Email = Input.Email;
             estudiante.ApplicationUser.UserName = Input.Email;
             estudiante.EmailInstitucional = Input.Email;
             await _userManager.UpdateNormalizedEmailAsync(estudiante.ApplicationUser);
             await _userManager.UpdateNormalizedUserNameAsync(estudiante.ApplicationUser);
        }

        // Update Student Info
        estudiante.CodigoEstudiante = Input.CodigoEstudiante;
        estudiante.Carrera = Input.Carrera;

        await _context.SaveChangesAsync();
        await _userManager.UpdateAsync(estudiante.ApplicationUser);

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ModelState.AddModelError("NewPassword", "La nueva contraseña es requerida.");
            // Reload basic info to show page again
            return await OnGetAsync(EstudianteId);
        }

        var estudiante = await _context.Estudiantes
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.Id == EstudianteId);
            
        if (estudiante == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(estudiante.ApplicationUser);
        var result = await _userManager.ResetPasswordAsync(estudiante.ApplicationUser, token, NewPassword);

        if (result.Succeeded)
        {
            TempData["Message"] = "Contraseña restablecida correctamente.";
        }
        else
        {
            foreach(var err in result.Errors) 
                ModelState.AddModelError(string.Empty, err.Description);
        }
        
        return await OnGetAsync(EstudianteId); // Stay on page
    }
}
