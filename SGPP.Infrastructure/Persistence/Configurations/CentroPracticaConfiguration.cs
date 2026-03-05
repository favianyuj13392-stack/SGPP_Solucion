using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGPP.Domain.Entities;

namespace SGPP.Infrastructure.Persistence.Configurations;

public class CentroPracticaConfiguration : IEntityTypeConfiguration<CentroPractica>
{
    public void Configure(EntityTypeBuilder<CentroPractica> builder)
    {
        builder.Property(x => x.EstadoConvenio)
               .HasConversion<string>()
               .HasMaxLength(20);
    }
}
