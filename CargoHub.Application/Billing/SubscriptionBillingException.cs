namespace CargoHub.Application.Billing;

/// <summary>Business rule failure for subscription billing (e.g. trial exhausted).</summary>
public sealed class SubscriptionBillingException : Exception
{
    public string ErrorCode { get; }

    public SubscriptionBillingException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
