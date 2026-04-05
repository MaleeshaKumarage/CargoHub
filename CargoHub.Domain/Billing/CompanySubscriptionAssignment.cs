namespace CargoHub.Domain.Billing;

/// <summary>Append-only history of which subscription plan applied to a company from an effective UTC instant.</summary>
public class CompanySubscriptionAssignment
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid SubscriptionPlanId { get; set; }

    /// <summary>UTC inclusive; latest row with EffectiveFromUtc &lt;= anchor wins.</summary>
    public DateTime EffectiveFromUtc { get; set; }

    public string? SetByUserId { get; set; }
}
