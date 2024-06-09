using System;
using System.Collections.Generic;

namespace TelegramBotRent.Models;

public partial class SearchSession
{
    public string? TenantId { get; set; }

    public string? Dates { get; set; }

    public string Id { get; set; } = null!;

    public virtual Tenant? Tenant { get; set; }
}
