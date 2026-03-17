/**
 * Parse JWT payload (no verification; used only to read role claims for UI).
 * Supports .NET ClaimTypes.Role in payload as "role" or full URI.
 */
export function getRolesFromToken(token: string | null | undefined): string[] {
  if (!token || typeof token !== 'string') return [];
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return [];
    const payload = parts[1];
    if (!payload) return [];
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    const parsed = JSON.parse(decoded) as Record<string, unknown>;
    const roleClaimUri = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    const raw =
      parsed['role'] ??
      parsed[roleClaimUri] ??
      (parsed as Record<string, unknown>)['Role'] ??
      (parsed as Record<string, unknown>)[roleClaimUri];
    if (Array.isArray(raw)) return raw.filter((r): r is string => typeof r === 'string');
    if (typeof raw === 'string') return [raw];
    return [];
  } catch {
    return [];
  }
}
