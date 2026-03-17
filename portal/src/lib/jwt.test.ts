import { describe, expect, it } from "vitest";
import { getRolesFromToken } from "./jwt";

describe("getRolesFromToken", () => {
  function b64(obj: Record<string, unknown>): string {
    return btoa(JSON.stringify(obj));
  }

  it("returns empty array for null", () => {
    expect(getRolesFromToken(null)).toEqual([]);
  });

  it("returns empty array for undefined", () => {
    expect(getRolesFromToken(undefined)).toEqual([]);
  });

  it("returns empty array for non-string", () => {
    expect(getRolesFromToken(123 as unknown as string)).toEqual([]);
  });

  it("returns empty array for invalid JWT (not 3 parts)", () => {
    expect(getRolesFromToken("a.b")).toEqual([]);
    expect(getRolesFromToken("a")).toEqual([]);
  });

  it("returns roles from 'role' claim when array", () => {
    const payload = b64({ role: ["Admin", "User"] });
    expect(getRolesFromToken(`header.${payload}.sig`)).toEqual(["Admin", "User"]);
  });

  it("returns roles from 'role' claim when single string", () => {
    const payload = b64({ role: "SuperAdmin" });
    expect(getRolesFromToken(`header.${payload}.sig`)).toEqual(["SuperAdmin"]);
  });

  it("returns roles from .NET ClaimTypes.Role URI", () => {
    const uri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    const payload = b64({ [uri]: ["Admin"] });
    expect(getRolesFromToken(`header.${payload}.sig`)).toEqual(["Admin"]);
  });

  it("returns roles from 'Role' (PascalCase)", () => {
    const payload = b64({ Role: "User" });
    expect(getRolesFromToken(`header.${payload}.sig`)).toEqual(["User"]);
  });

  it("filters non-string values from role array", () => {
    const payload = b64({ role: ["Admin", 1, null, "User", {}] });
    expect(getRolesFromToken(`header.${payload}.sig`)).toEqual(["Admin", "User"]);
  });

  it("handles base64url padding (replaces - and _)", () => {
    const obj = { role: ["User"] };
    const standard = btoa(JSON.stringify(obj));
    const base64url = standard.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
    const payload = base64url;
    expect(getRolesFromToken(`header.${payload}.sig`)).toEqual(["User"]);
  });

  it("returns empty array on JSON parse error", () => {
    expect(getRolesFromToken("header.invalid!!!.sig")).toEqual([]);
  });
});
