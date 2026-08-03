using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class FuenteDatos
{
    public string IdFuente { get; set; } = null!;

    public int IdTipoFuente { get; set; }

    public DateOnly FechaCarga { get; set; }

    public virtual TipoFuente IdTipoFuenteNavigation { get; set; } = null!;

    public virtual ICollection<Opinion> Opinion { get; set; } = new List<Opinion>();
}
