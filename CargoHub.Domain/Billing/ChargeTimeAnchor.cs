namespace CargoHub.Domain.Billing;

/// <summary>Which instant on the booking selects pricing period and volume ordering.</summary>
public enum ChargeTimeAnchor
{
    FirstBillableAtUtc = 0,
    CreatedAtUtc = 1,
}
