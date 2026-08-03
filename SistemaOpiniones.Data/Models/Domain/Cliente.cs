using System;
using System.Collections.Generic;

namespace SistemaOpiniones.Data.Models.Domain;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Email { get; set; }

    public virtual ICollection<Opinion> Opinion { get; set; } = new List<Opinion>();
}
