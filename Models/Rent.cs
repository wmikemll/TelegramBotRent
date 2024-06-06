using System;
using System.Collections.Generic;

namespace TekegramBotRent.Models;

public partial class Rent
{
    public string Id { get; set; } = null!;

    public string? TenantId { get; set; }

    public DateOnly? ArrivalDate { get; set; }

    public DateOnly? DepartureDate { get; set; }

    public bool? IsConfirmed { get; set; }

    public bool? IsCanceledOwner { get; set; }

    public bool? IsCanceledTenant { get; set; }

    public string? FlatId { get; set; }

    public virtual Flat? Flat { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
