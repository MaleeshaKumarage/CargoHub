namespace HiavaNet.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL schema names. Each domain has its own schema for clarity and access control.
/// </summary>
public static class DbSchemas
{
    /// <summary>ASP.NET Core Identity tables (users, roles, claims, logins, tokens).</summary>
    public const string Auth = "auth";

    /// <summary>Company configuration, address books, agreement numbers.</summary>
    public const string Companies = "companies";

    /// <summary>Bookings and related data.</summary>
    public const string Bookings = "bookings";

    /// <summary>Couriers (reserved for future use).</summary>
    public const string Couriers = "couriers";
}
