using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdCategoria { get; set; }

    public virtual Categoria IdCategoriaNavigation { get; set; } = null!;

    public virtual ICollection<Opinion> Opinion { get; set; } = new List<Opinion>();
}
