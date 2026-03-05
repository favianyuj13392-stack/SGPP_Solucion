using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SGPP.Domain.Entities;
using System.Reflection;

namespace SGPP.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<CentroPractica> CentrosPractica { get; set; }
    public DbSet<TutorInstitucional> TutoresInstitucionales { get; set; }
    public DbSet<TutorAcademico> TutoresAcademicos { get; set; }
    public DbSet<Estudiante> Estudiantes { get; set; }
    public DbSet<Periodo> Periodos { get; set; }
    public DbSet<Asignacion> Asignaciones { get; set; }
    
    // Forms
    public DbSet<FormularioB_Empresa> EvaluacionesEmpresa { get; set; } // Mapping to Table Name convention if desired, or property name
    public DbSet<FormularioB_Tareas> EvaluacionEmpresa_Tareas { get; set; }
    public DbSet<FormularioB_DetalleRespuestas> FormularioB_Detalles { get; set; }
    
    public DbSet<FormularioA_Estudiante> EvaluacionesEstudiante { get; set; }
    public DbSet<FormularioA_DetalleRespuestas> FormularioA_Detalles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply all configurations from the current assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Renaming tables to match SQL Schema reference where appropriate or clean names
        builder.Entity<CentroPractica>().ToTable("CentrosPractica");
        builder.Entity<TutorInstitucional>().ToTable("TutoresInstitucionales");
        builder.Entity<Estudiante>().ToTable("Estudiantes");
        builder.Entity<Periodo>().ToTable("Periodos");
        builder.Entity<Asignacion>().ToTable("Asignaciones");
        
        builder.Entity<FormularioB_Empresa>().ToTable("EvaluacionesEmpresa");
        builder.Entity<FormularioB_Tareas>().ToTable("EvaluacionEmpresa_Tareas");
        
        builder.Entity<FormularioA_Estudiante>().ToTable("EvaluacionesEstudiante");
    }
}
