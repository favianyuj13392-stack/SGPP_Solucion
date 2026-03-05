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
    public int TutorId { get; set; }

    public SelectList? CentrosPractica { get; set; }

    public class InputModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; 
        
        public int CentroPracticaId { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        
        public bool EsActivo { get; set; }
    }

    [BindProperty]
    public string? NewPassword { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var tutor = await _context.TutoresInstitucionales
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tutor == null) return NotFound();

        TutorId = tutor.Id;
        Input = new InputModel
        {
            Nombre = tutor.ApplicationUser.Nombre,
            Apellido = tutor.ApplicationUser.Apellido,
            Email = tutor.ApplicationUser.Email ?? "",
            CentroPracticaId = tutor.CentroPracticaId,
            Cargo = tutor.Cargo ?? "",
            Telefono = tutor.TelefonoContacto ?? "",
            Ci = tutor.Ci ?? "",
            EsActivo = tutor.ApplicationUser.EsActivo
        };

        CentrosPractica = new SelectList(await _context.CentrosPractica.ToListAsync(), "Id", "RazonSocial");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tutor = await _context.TutoresInstitucionales
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == TutorId);

        if (tutor == null) return NotFound();

        // Update User
        tutor.ApplicationUser.Nombre = Input.Nombre;
        tutor.ApplicationUser.Apellido = Input.Apellido;
        tutor.ApplicationUser.EsActivo = Input.EsActivo;

        if (tutor.ApplicationUser.Email != Input.Email)
        {
            tutor.ApplicationUser.Email = Input.Email;
            tutor.ApplicationUser.UserName = Input.Email;
            await _userManager.UpdateNormalizedEmailAsync(tutor.ApplicationUser);
            await _userManager.UpdateNormalizedUserNameAsync(tutor.ApplicationUser);
        }

        // Update Tutor
        tutor.CentroPracticaId = Input.CentroPracticaId;
        tutor.Cargo = Input.Cargo;
        tutor.TelefonoContacto = Input.Telefono;
        tutor.Ci = Input.Ci;

        await _context.SaveChangesAsync();
        await _userManager.UpdateAsync(tutor.ApplicationUser);

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ModelState.AddModelError("NewPassword", "Contraseña requerida.");
            return await OnGetAsync(TutorId);
        }

        var tutor = await _context.TutoresInstitucionales.Include(t => t.ApplicationUser).FirstOrDefaultAsync(t => t.Id == TutorId);
        if (tutor == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(tutor.ApplicationUser);
        var result = await _userManager.ResetPasswordAsync(tutor.ApplicationUser, token, NewPassword);

        if (result.Succeeded) TempData["Message"] = "Contraseña cambiada.";
        else foreach(var err in result.Errors) ModelState.AddModelError("", err.Description);

        return await OnGetAsync(TutorId);
    }
}
