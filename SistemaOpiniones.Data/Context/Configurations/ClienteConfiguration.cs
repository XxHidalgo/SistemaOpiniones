using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> entity)
    {
        entity.HasKey(e => e.IdCliente);

        entity.Property(e => e.IdCliente).ValueGeneratedNever();
        entity.Property(e => e.Email).HasMaxLength(256);
        entity.Property(e => e.Nombre).HasMaxLength(100);
    }
}
