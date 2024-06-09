using System;
using System.Collections.Generic;

namespace TelegramBotRent.Models;

public partial class Tenant
{
    public string Id { get; set; } = null!;

    public virtual User IdNavigation { get; set; } = null!;

    public virtual ICollection<Rent> Rents { get; set; } = new List<Rent>();

    public virtual ICollection<SearchSession> SearchSessions { get; set; } = new List<SearchSession>();
}
