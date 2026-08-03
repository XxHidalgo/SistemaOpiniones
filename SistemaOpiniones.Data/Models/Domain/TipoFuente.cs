using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class TipoFuente
{
    public int IdTipoFuente { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<FuenteDatos> FuenteDatos { get; set; } = new List<FuenteDatos>();
}
