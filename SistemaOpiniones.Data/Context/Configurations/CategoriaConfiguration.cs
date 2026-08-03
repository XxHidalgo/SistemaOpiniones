using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> entity)
    {
        entity.HasKey(e => e.IdCategoria);

        entity.HasIndex(e => e.Nombre, "UQ_Categoria_Nombre").IsUnique();

        entity.Property(e => e.Nombre).HasMaxLength(50);
    }
}
