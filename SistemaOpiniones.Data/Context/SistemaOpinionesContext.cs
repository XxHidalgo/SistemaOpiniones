using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SistemaOpiniones.Data.Models.Domain;

namespace SistemaOpiniones.Data.Context;

public partial class SistemaOpinionesContext : DbContext
{
    public SistemaOpinionesContext(DbContextOptions<SistemaOpinionesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categoria { get; set; }

    public virtual DbSet<Clasificacion> Clasificacion { get; set; }

    public virtual DbSet<Cliente> Cliente { get; set; }

    public virtual DbSet<FuenteDatos> FuenteDatos { get; set; }

    public virtual DbSet<Opinion> Opinion { get; set; }

    public virtual DbSet<Producto> Producto { get; set; }

    public virtual DbSet<TipoFuente> TipoFuente { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
