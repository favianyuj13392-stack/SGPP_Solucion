using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using SGPP.Domain.Enums;
using SGPP.Infrastructure.Persistence;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace SGPP.Infrastructure.Services;

public class UserImportService : IUserImportService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserImportService(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ImportResult> ImportStudentsAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip Header

        foreach (var row in rows)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Columns: NOMBRE | APELLIDO | EMAIL | CODIGO | CARRERA | TELEFONO
                string nombre = row.Cell(1).GetValue<string>().Trim();
                string apellido = row.Cell(2).GetValue<string>().Trim();
                string email = row.Cell(3).GetValue<string>().Trim();
                
                if (string.IsNullOrEmpty(email)) continue;

                string codigo = row.Cell(4).GetValue<string>().Trim();
                string passwordDefault = string.IsNullOrEmpty(codigo) ? "Ucb.2026!" : $"Ucb.{codigo}!";

                var user = await EnsureUserAsync(nombre, apellido, email, row.Cell(6).GetValue<string>().Trim(), passwordDefault, result);
                if (user == null) continue; // Error already added to result

                await EnsureRole(user, "Estudiante");

                var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);
                if (estudiante == null)
                {
                    string carreraStr = row.Cell(5).GetValue<string>().Trim();
                    Carrera carreraEnum = ParseCarrera(carreraStr);

                    estudiante = new Estudiante
                    {
                        ApplicationUserId = user.Id,
                        CodigoEstudiante = codigo,
                        Carrera = carreraEnum,
                        EmailInstitucional = email,
                        EstadoAcademico = EstadoAcademico.Habilitado
                    };
                    _context.Estudiantes.Add(estudiante);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Errors.Add($"Fila {row.RowNumber()}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<ImportResult> ImportTeachersAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip Header

        foreach (var row in rows)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Columns: NOMBRE | APELLIDO | EMAIL | TELEFONO | CARRERA
                string nombre = row.Cell(1).GetValue<string>().Trim();
                string apellido = row.Cell(2).GetValue<string>().Trim();
                string email = row.Cell(3).GetValue<string>().Trim();

                if (string.IsNullOrEmpty(email)) continue;

                var user = await EnsureUserAsync(nombre, apellido, email, row.Cell(4).GetValue<string>().Trim(), "Ucb.2026!", result);
                if (user == null) continue;

                await EnsureRole(user, "TutorAcademico");

                var docente = await _context.TutoresAcademicos.FirstOrDefaultAsync(d => d.ApplicationUserId == user.Id);
                if (docente == null)
                {
                    string carreraStr = row.Cell(5).GetValue<string>().Trim();
                    Carrera carreraEnum = ParseCarrera(carreraStr);

                    docente = new TutorAcademico
                    {
                        ApplicationUserId = user.Id,
                        Carrera = carreraEnum
                    };
                    _context.TutoresAcademicos.Add(docente);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Errors.Add($"Fila {row.RowNumber()}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<ImportResult> ImportTutorsAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip Header

        foreach (var row in rows)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Columns: NOMBRE | APELLIDO | EMAIL | TELEFONO | EMPRESA | CARGO | AREA_TRABAJO
                string nombre = row.Cell(1).GetValue<string>().Trim();
                string apellido = row.Cell(2).GetValue<string>().Trim();
                string email = row.Cell(3).GetValue<string>().Trim();

                if (string.IsNullOrEmpty(email)) continue;

                var user = await EnsureUserAsync(nombre, apellido, email, row.Cell(4).GetValue<string>().Trim(), "Ucb.2026!", result);
                if (user == null) continue;

                await EnsureRole(user, "Tutor");

                string empresaNombre = row.Cell(5).GetValue<string>().Trim();
                if (string.IsNullOrEmpty(empresaNombre)) throw new Exception("Nombre de empresa requerido.");

                // Normalize Company
                string empresaNorm = empresaNombre.ToUpper();
                var centro = await _context.CentrosPractica.FirstOrDefaultAsync(c => c.RazonSocial.ToUpper() == empresaNorm);

                if (centro == null)
                {
                    centro = new CentroPractica 
                    { 
                        RazonSocial = empresaNombre,
                        EstadoConvenio = EstadoConvenio.Activo,
                        Direccion = "Sin dirección registrada",
                        Rubro = null
                    };
                    _context.CentrosPractica.Add(centro);
                    await _context.SaveChangesAsync();
                    result.CompaniesCreated++;
                }

                var tutor = await _context.TutoresInstitucionales.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
                if (tutor == null)
                {
                    tutor = new TutorInstitucional
                    {
                        ApplicationUserId = user.Id,
                        CentroPracticaId = centro.Id,
                        Cargo = row.Cell(6).GetValue<string>().Trim(),
                        AreaTrabajo = row.Cell(7).GetValue<string>().Trim()
                    };
                    _context.TutoresInstitucionales.Add(tutor);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Errors.Add($"Fila {row.RowNumber()}: {ex.Message}");
            }
        }
        return result;
    }

    private async Task<ApplicationUser?> EnsureUserAsync(string nombre, string apellido, string email, string phone, string password, ImportResult result)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nombre = nombre,
                Apellido = apellido,
                EmailConfirmed = true,
                EsActivo = true,
                PhoneNumber = phone,
                DebeCambiarPassword = true
            };
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                result.Errors.Add($"Error creando usuario {email} - {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                return null;
            }
            result.UsersCreated++;
        }
        else
        {
             // Update logic if needed, e.g. phone
             if (!string.IsNullOrEmpty(phone) && user.PhoneNumber != phone)
             {
                 user.PhoneNumber = phone;
                 await _userManager.UpdateAsync(user);
             }
        }
        return user;
    }

    private async Task EnsureRole(ApplicationUser user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }
    }

    private Carrera ParseCarrera(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Carrera.OTHER;
        input = input.Trim();
        
        // 1. Intento rápido: Validar si escribieron el código corto (ej. "SIS")
        if (Enum.TryParse<Carrera>(input, true, out var carreraEnum))
            return carreraEnum;

        // 2. Intento profundo: Buscar por el nombre completo "Display(Name)"
        foreach (var field in typeof(Carrera).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) is DisplayAttribute attribute)
            {
                // Ignorar mayúsculas, minúsculas y tildes básicas
                if (string.Equals(attribute.Name, input, StringComparison.OrdinalIgnoreCase))
                {
                    return (Carrera)field.GetValue(null)!;
                }
            }
        }

        return Carrera.OTHER;
    }
}
