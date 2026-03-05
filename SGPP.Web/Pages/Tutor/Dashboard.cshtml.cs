using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Infrastructure.Persistence;

namespace SGPP.Web.Pages.Tutor;

[Authorize(Roles = "Tutor")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Asignacion> Asignaciones { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        // Find the Tutor profile linked to this User
        var tutorProfile = await _context.TutoresInstitucionales
            .FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);

        if (tutorProfile != null)
        {
            Asignaciones = await _context.Asignaciones
                .Include(a => a.Estudiante)
                    .ThenInclude(e => e.ApplicationUser)
                .Include(a => a.Periodo)
                .Where(a => a.TutorInstitucionalId == tutorProfile.Id)
                .ToListAsync();
        }
    }
}
