using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGPP.Domain.Entities;

namespace SGPP.Infrastructure.Persistence.Configurations;

public class FormularioA_EstudianteConfiguration : IEntityTypeConfiguration<FormularioA_Estudiante>
{
    public void Configure(EntityTypeBuilder<FormularioA_Estudiante> builder)
    {
        builder.HasIndex(x => x.AsignacionId).IsUnique();

        builder.HasMany(x => x.Detalles)
               .WithOne(d => d.FormularioA)
               .HasForeignKey(d => d.FormularioAId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
