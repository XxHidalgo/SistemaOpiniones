using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class ClasificacionConfiguration : IEntityTypeConfiguration<Clasificacion>
{
    public void Configure(EntityTypeBuilder<Clasificacion> entity)
    {
        entity.HasKey(e => e.IdClasificacion);

        entity.HasIndex(e => e.Nombre, "UQ_Clasificacion_Nombre").IsUnique();

        entity.Property(e => e.Nombre).HasMaxLength(10);
    }
}
