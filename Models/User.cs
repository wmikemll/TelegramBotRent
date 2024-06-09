using System;
using System.Collections.Generic;

namespace TelegramBotRent.Models;

public partial class User
{
    public string Id { get; set; } = null!;

    public string? Username { get; set; }

    public string? Contact { get; set; }

    public virtual Owner? Owner { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
