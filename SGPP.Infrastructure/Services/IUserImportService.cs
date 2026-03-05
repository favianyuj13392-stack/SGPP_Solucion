using Microsoft.AspNetCore.Http;
using SGPP.Domain.Common;

namespace SGPP.Infrastructure.Services;

public interface IUserImportService
{
    Task<ImportResult> ImportStudentsAsync(Stream fileStream);
    Task<ImportResult> ImportTeachersAsync(Stream fileStream);
    Task<ImportResult> ImportTutorsAsync(Stream fileStream);
}

public class ImportResult
{
    public int UsersCreated { get; set; }
    public int CompaniesCreated { get; set; }
    public List<string> Errors { get; set; } = new();
}
