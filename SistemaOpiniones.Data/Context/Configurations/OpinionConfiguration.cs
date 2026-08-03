using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class OpinionConfiguration : IEntityTypeConfiguration<Opinion>
{
    public void Configure(EntityTypeBuilder<Opinion> entity)
    {
        entity.HasKey(e => e.IdOpinion);

        entity.Property(e => e.Comentario).HasMaxLength(1000);
        entity.Property(e => e.IdFuente).HasMaxLength(10);

        entity.HasOne(d => d.IdClasificacionNavigation).WithMany(p => p.Opinion)
            .HasForeignKey(d => d.IdClasificacion)
            .HasConstraintName("FK_Opinion_Clasificacion");

        entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Opinion)
            .HasForeignKey(d => d.IdCliente)
            .HasConstraintName("FK_Opinion_Cliente");

        entity.HasOne(d => d.IdFuenteNavigation).WithMany(p => p.Opinion)
            .HasForeignKey(d => d.IdFuente)
            .HasConstraintName("FK_Opinion_Fuente");

        entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Opinion)
            .HasForeignKey(d => d.IdProducto)
            .HasConstraintName("FK_Opinion_Producto");
    }
}
