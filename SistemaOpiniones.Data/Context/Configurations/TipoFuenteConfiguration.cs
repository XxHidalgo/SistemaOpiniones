using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class TipoFuenteConfiguration : IEntityTypeConfiguration<TipoFuente>
{
    public void Configure(EntityTypeBuilder<TipoFuente> entity)
    {
        entity.HasKey(e => e.IdTipoFuente);

        entity.HasIndex(e => e.Nombre, "UQ_TipoFuente_Nombre").IsUnique();

        entity.Property(e => e.Nombre).HasMaxLength(20);
    }
}
