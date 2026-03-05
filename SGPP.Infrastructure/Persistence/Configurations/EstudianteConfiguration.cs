using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGPP.Domain.Entities;

namespace SGPP.Infrastructure.Persistence.Configurations;

public class EstudianteConfiguration : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> builder)
    {
        builder.Property(x => x.Carrera)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(x => x.EstadoAcademico)
               .HasConversion<string>()
               .HasMaxLength(20);
        
        // Relationship with identity is handled by FK property on entity
    }
}
