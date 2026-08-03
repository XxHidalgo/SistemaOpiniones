using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class Clasificacion
{
    public int IdClasificacion { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Opinion> Opinion { get; set; } = new List<Opinion>();
}
