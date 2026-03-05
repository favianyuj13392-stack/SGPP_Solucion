using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGPP.Domain.Entities;

namespace SGPP.Infrastructure.Persistence.Configurations;

public class AsignacionConfiguration : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> builder)
    {
        builder.Property(x => x.Estado)
               .HasConversion<string>()
               .HasMaxLength(20);

        // Relationships
        builder.HasOne(x => x.Periodo)
               .WithMany()
               .HasForeignKey(x => x.PeriodoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Estudiante)
               .WithMany()
               .HasForeignKey(x => x.EstudianteId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TutorInstitucional)
               .WithMany()
               .HasForeignKey(x => x.TutorInstitucionalId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
