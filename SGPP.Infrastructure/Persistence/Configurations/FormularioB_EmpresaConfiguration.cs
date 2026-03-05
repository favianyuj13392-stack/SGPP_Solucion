using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGPP.Domain.Entities;

namespace SGPP.Infrastructure.Persistence.Configurations;

public class FormularioB_EmpresaConfiguration : IEntityTypeConfiguration<FormularioB_Empresa>
{
    public void Configure(EntityTypeBuilder<FormularioB_Empresa> builder)
    {
        builder.HasIndex(x => x.AsignacionId).IsUnique();

        // CRITICAL: Decimal precision for HorasTrabajadas
        builder.Property(x => x.HorasTrabajadas)
               .HasColumnType("decimal(10,2)");

        builder.HasMany(x => x.Tareas)
               .WithOne(t => t.FormularioB)
               .HasForeignKey(t => t.FormularioBId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Detalles)
               .WithOne(d => d.FormularioB)
               .HasForeignKey(d => d.FormularioBId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
