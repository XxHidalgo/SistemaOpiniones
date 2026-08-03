using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class Categoria
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
