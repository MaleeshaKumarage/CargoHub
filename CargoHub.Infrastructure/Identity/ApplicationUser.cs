using Microsoft.AspNetCore.Identity;

namespace CargoHub.Infrastructure.Identity;

/// <summary>
/// Application user stored in Identity tables.
/// This extends the default IdentityUser with fields that are important
/// for the CargoHub domain such as business id and customer mapping id.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Human readable display name shown in the portal.
    /// Mirrors userNameRegister / userName from the existing system.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Business identifier (for example Finnish Y-tunnus).
    /// </summary>
    public string? BusinessId { get; set; }

    /// <summary>
    /// Optional GS1 / GSOne identifier carried over from registration.
    /// </summary>
    public string? GsOne { get; set; }

    /// <summary>
    /// Identifier linking the user to a customer record used in bookings.
    /// </summary>
    public string? CustomerMappingId { get; set; }

    /// <summary>
    /// When false, user cannot log in (deactivated by super admin).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UI design theme preference (skeuomorphism, neobrutalism, claymorphism, minimalism).
    /// Defaults to minimalism when null.
    /// </summary>
    public string? Theme { get; set; }
}

