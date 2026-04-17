/** True when the user should see the freelance rider portal only (no company booking UI). */
export function isRiderOnlyPortal(roles: string[] | null | undefined): boolean {
  if (!roles || !Array.isArray(roles)) return false;
  return (
    roles.includes("Rider") &&
    !roles.includes("SuperAdmin") &&
    !roles.includes("Admin") &&
    !roles.includes("User")
  );
}
