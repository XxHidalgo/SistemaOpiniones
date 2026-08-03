using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class Opinion
{
    public int IdOpinion { get; set; }

    public int? IdCliente { get; set; }

    public int? IdProducto { get; set; }

    public string? IdFuente { get; set; }

    public int? IdClasificacion { get; set; }

    public DateOnly Fecha { get; set; }

    public string? Comentario { get; set; }

    public byte? PuntajeSatisfaccion { get; set; }

    public virtual Clasificacion? IdClasificacionNavigation { get; set; }

    public virtual Cliente? IdClienteNavigation { get; set; }

    public virtual FuenteDatos? IdFuenteNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
