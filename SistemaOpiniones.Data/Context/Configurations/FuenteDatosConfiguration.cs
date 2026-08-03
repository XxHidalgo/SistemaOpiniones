using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class FuenteDatosConfiguration : IEntityTypeConfiguration<FuenteDatos>
{
    public void Configure(EntityTypeBuilder<FuenteDatos> entity)
    {
        entity.HasKey(e => e.IdFuente);

        entity.Property(e => e.IdFuente).HasMaxLength(10);

        entity.HasOne(d => d.IdTipoFuenteNavigation).WithMany(p => p.FuenteDatos)
            .HasForeignKey(d => d.IdTipoFuente)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_FuenteDatos_TipoFuente");
    }
}
