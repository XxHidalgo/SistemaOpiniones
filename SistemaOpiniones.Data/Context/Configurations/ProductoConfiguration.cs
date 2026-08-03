using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> entity)
    {
        entity.HasKey(e => e.IdProducto);

        entity.Property(e => e.IdProducto).ValueGeneratedNever();
        entity.Property(e => e.Nombre).HasMaxLength(100);

        entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Producto)
            .HasForeignKey(d => d.IdCategoria)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Producto_Categoria");
    }
}
