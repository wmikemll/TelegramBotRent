using System;
using System.Collections.Generic;

namespace TekegramBotRent.Models;

public partial class Owner
{
    public string Id { get; set; } = null!;

    public virtual ICollection<Flat> Flats { get; set; } = new List<Flat>();

    public virtual User IdNavigation { get; set; } = null!;
}
