using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Web.Pages.Admin.Docentes;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string DocenteId { get; set; } = string.Empty;

    public class InputModel
    {
        [Required(ErrorMessage = "El nombre es requerido.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido.")]
        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        public string Email { get; set; } = string.Empty;

        public bool EsActivo { get; set; } = true;
    }

    [BindProperty]
    public string? NewPassword { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        DocenteId = user.Id;
        Input = new InputModel
        {
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email ?? "",
            EsActivo = user.EsActivo
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(DocenteId);
        if (user == null) return NotFound();

        if (!ModelState.IsValid) return Page();

        string emailClean = (Input.Email ?? "").Trim().ToLowerInvariant();

        // Validar si el correo cambió y pertenece a otro usuario
        if (!string.Equals(user.Email, emailClean, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userManager.FindByEmailAsync(emailClean);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("Input.Email", $"El correo '{emailClean}' ya pertenece a otro usuario en el sistema ({existingUser.Nombre} {existingUser.Apellido}).");
                return Page();
            }

            user.Email = emailClean;
            user.UserName = emailClean;
            await _userManager.UpdateNormalizedEmailAsync(user);
            await _userManager.UpdateNormalizedUserNameAsync(user);
        }

        user.Nombre = Input.Nombre?.Trim() ?? "";
        user.Apellido = Input.Apellido?.Trim() ?? "";
        user.EsActivo = Input.EsActivo;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Docente {user.Nombre} {user.Apellido} actualizado correctamente.";
            return RedirectToPage("./Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            TempData["ErrorMessage"] = "Debe ingresar una nueva contraseña.";
            return RedirectToPage(new { id = DocenteId });
        }

        var user = await _userManager.FindByIdAsync(DocenteId);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, NewPassword);

        if (result.Succeeded)
        {
            user.DebeCambiarPassword = true;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Contraseña restablecida correctamente. El docente deberá cambiarla al iniciar sesión.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al restablecer contraseña: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage(new { id = DocenteId });
    }
}
