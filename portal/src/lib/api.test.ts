import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  login,
  register,
  getMe,
  updateTheme,
  getWaybillPdfBlobUrl,
  DESIGN_THEMES,
  type RegisterBody,
} from "./api";

describe("api", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe("login", () => {
    it("returns success and data when API returns 200 with user and token", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          userId: "u1",
          email: "u@example.com",
          displayName: "User",
          jwtToken: "jwt-123",
          roles: ["User"],
        }),
      });

      const result = await login("u@example.com", "password");

      expect(result.success).toBe(true);
      expect(result.data?.userId).toBe("u1");
      expect(result.data?.jwtToken).toBe("jwt-123");
      expect(result.data?.roles).toEqual(["User"]);
    });

    it("returns failure with errorCode and message when API returns 400", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          errorCode: "InvalidCredentials",
          message: "Wrong password",
        }),
      });

      const result = await login("u@example.com", "wrong");

      expect(result.success).toBe(false);
      expect(result.errorCode).toBe("InvalidCredentials");
      expect(result.message).toBe("Wrong password");
    });

    it("returns failure when API returns non-ok without errorCode", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({}),
      });

      const result = await login("u@example.com", "x");

      expect(result.success).toBe(false);
      expect(result.message).toBeDefined();
    });
  });

  describe("register", () => {
    it("returns success when API returns 200 with userId and jwtToken", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          userId: "u2",
          email: "new@example.com",
          displayName: "New",
          jwtToken: "jwt-456",
        }),
      });

      const result = await register({
        email: "new@example.com",
        password: "secret",
        userName: "New",
        businessId: "1234567-8",
      } as RegisterBody);

      expect(result.success).toBe(true);
      expect(result.data?.jwtToken).toBe("jwt-456");
    });

    it("returns failure with CompanyIdRequired when errorCode is CompanyIdRequired", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          errorCode: "CompanyIdRequired",
          message: "Company ID is required",
        }),
      });

      const result = await register({
        email: "x@x.com",
        password: "pass",
        userName: "X",
      } as RegisterBody);

      expect(result.success).toBe(false);
      expect(result.errorCode).toBe("CompanyIdRequired");
    });

    it("returns failure with CompanyNotFound when errorCode is CompanyNotFound", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          errorCode: "CompanyNotFound",
          message: "No company found",
        }),
      });

      const result = await register({
        email: "x@x.com",
        password: "pass",
        userName: "X",
        businessId: "9999999-9",
      } as RegisterBody);

      expect(result.success).toBe(false);
      expect(result.errorCode).toBe("CompanyNotFound");
    });
  });

  describe("getMe", () => {
    it("returns user with theme when API returns theme", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          userId: "u1",
          email: "u@example.com",
          displayName: "User",
          roles: ["User"],
          theme: "neobrutalism",
        }),
      });

      const me = await getMe("token");

      expect(me.userId).toBe("u1");
      expect(me.theme).toBe("neobrutalism");
    });

    it("returns null theme when API omits theme", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          userId: "u1",
          email: "u@example.com",
          displayName: "User",
          roles: [],
        }),
      });

      const me = await getMe("token");

      expect(me.theme).toBeNull();
    });
  });

  describe("updateTheme", () => {
    it("succeeds when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ theme: "claymorphism" }),
      });

      await expect(updateTheme("token", "claymorphism")).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/me/preferences"),
        expect.objectContaining({
          method: "PATCH",
          body: JSON.stringify({ theme: "claymorphism" }),
        })
      );
    });

    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Invalid theme" }),
      });

      await expect(updateTheme("token", "invalid")).rejects.toThrow("Invalid theme");
    });
  });

  describe("DESIGN_THEMES", () => {
    it("includes all four design themes", () => {
      expect(DESIGN_THEMES).toEqual([
        "skeuomorphism",
        "neobrutalism",
        "claymorphism",
        "minimalism",
      ]);
    });
  });

  describe("getWaybillPdfBlobUrl", () => {
    it("returns blob URL when API returns 200 with PDF blob", async () => {
      const mockBlob = new Blob(["pdf content"], { type: "application/pdf" });
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => mockBlob,
      });
      const createObjectURL = vi.fn(() => "blob:mock-url");
      vi.stubGlobal("URL", { ...globalThis.URL, createObjectURL, revokeObjectURL: vi.fn() });

      const url = await getWaybillPdfBlobUrl("token", "booking-123");

      expect(url).toBe("blob:mock-url");
      expect(createObjectURL).toHaveBeenCalledWith(mockBlob);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/booking-123/waybill"),
        expect.objectContaining({ headers: expect.objectContaining({ Authorization: "Bearer token" }) })
      );
    });

    it("throws with API message when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({ message: "Waybill not ready" }),
      });

      await expect(getWaybillPdfBlobUrl("token", "b1")).rejects.toThrow("Waybill not ready");
    });

    it("throws generic message when API returns error without body message", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({}),
      });

      await expect(getWaybillPdfBlobUrl("token", "b1")).rejects.toThrow(/Failed to load waybill|waybill/);
    });
  });
});
