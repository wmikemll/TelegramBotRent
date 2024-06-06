using System;
using System.Collections.Generic;

namespace TekegramBotRent.Models;

public partial class Flat
{
    public string Id { get; set; } = null!;

    public string? Adress { get; set; }

    public string? Zone { get; set; }

    public short? Floor { get; set; }

    public short? CountOfRooms { get; set; }

    public float? Area { get; set; }

    public string? Description { get; set; }

    public string? OwnerId { get; set; }

    public bool? IsActive { get; set; }

    public decimal? Price { get; set; }

    public virtual Owner? Owner { get; set; }

    public virtual ICollection<Rent> Rents { get; set; } = new List<Rent>();
}
