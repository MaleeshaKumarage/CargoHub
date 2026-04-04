import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  login,
  register,
  getMe,
  updateTheme,
  getWaybillPdfBlobUrl,
  DESIGN_THEMES,
  requestPasswordReset,
  resetPassword,
  getBranding,
  getCouriers,
  getCourierCatalog,
  getCourierContracts,
  putCourierContracts,
  adminGetCompanies,
  adminGetUsers,
  adminPatchUser,
  createCompany,
  getAddressBook,
  addSender,
  addReceiver,
  bookingList,
  bookingGet,
  bookingCreate,
  draftList,
  draftGet,
  draftCreate,
  draftUpdate,
  draftConfirm,
  getDashboardStats,
  bookingsImport,
  bookingsImportPreview,
  bookingsImportAnalyze,
  bookingsImportApplyMapping,
  bookingsImportConfirm,
  bookingsWaybillsBulkDownload,
  bookingsExportDownload,
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

    it("returns failure when API returns ok but success false", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ success: false, errorCode: "Locked", message: "Account locked" }),
      });
      const result = await login("u@example.com", "pass");
      expect(result.success).toBe(false);
      expect(result.errorCode).toBe("Locked");
    });

    it("normalizes empty roles to empty array", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          userId: "u1",
          email: "u@x.com",
          displayName: "User",
          jwtToken: "jwt",
        }),
      });
      const result = await login("u@example.com", "pass");
      expect(result.success).toBe(true);
      expect(result.data?.roles).toEqual([]);
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

    it("returns failure with title when API returns non-ok with title", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ title: "Validation Error" }),
      });
      const result = await register({ email: "x@x.com", password: "p", userName: "X" } as RegisterBody);
      expect(result.success).toBe(false);
      expect(result.message).toBeDefined();
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

    it("returns failure with RegistrationFailed when API returns duplicate email", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          errorCode: "RegistrationFailed",
          message: "Email 'x@x.com' is already taken.",
        }),
      });

      const result = await register({
        email: "x@x.com",
        password: "pass",
        userName: "X",
        businessId: "1234567-8",
      } as RegisterBody);

      expect(result.success).toBe(false);
      expect(result.errorCode).toBe("RegistrationFailed");
      expect(result.message).toContain("already taken");
    });

    it("returns failure without throwing when response body is not JSON", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        statusText: "Internal Server Error",
        json: async () => {
          throw new SyntaxError("Unexpected token");
        },
      });

      const result = await register({
        email: "x@x.com",
        password: "pass",
        userName: "X",
        businessId: "1234567-8",
      } as RegisterBody);

      expect(result.success).toBe(false);
      expect(result.message).toBe("Internal Server Error");
    });

    it("normalizes PascalCase success payload", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          UserId: "u2",
          Email: "new@example.com",
          DisplayName: "New",
          JwtToken: "jwt-789",
          Roles: ["User"],
        }),
      });

      const result = await register({
        email: "new@example.com",
        password: "secret",
        userName: "New",
        businessId: "1234567-8",
      } as RegisterBody);

      expect(result.success).toBe(true);
      expect(result.data?.userId).toBe("u2");
      expect(result.data?.jwtToken).toBe("jwt-789");
      expect(result.data?.roles).toEqual(["User"]);
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

  describe("requestPasswordReset", () => {
    it("returns success when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, json: async () => ({}) });
      const result = await requestPasswordReset("u@example.com");
      expect(result.success).toBe(true);
    });
    it("returns failure with message when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "User not found" }),
      });
      const result = await requestPasswordReset("x@x.com");
      expect(result.success).toBe(false);
      expect(result.message).toBe("User not found");
    });
  });

  describe("resetPassword", () => {
    it("returns success when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, json: async () => ({}) });
      const result = await resetPassword("token", "newpass");
      expect(result.success).toBe(true);
    });
    it("returns failure when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Token expired" }),
      });
      const result = await resetPassword("t", "p");
      expect(result.success).toBe(false);
      expect(result.message).toBe("Token expired");
    });
  });

  describe("getBranding", () => {
    it("returns branding when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          appName: "CargoHub",
          logoUrl: "/logo.png",
          primaryColor: "#000",
          secondaryColor: "#fff",
        }),
      });
      const b = await getBranding();
      expect(b.appName).toBe("CargoHub");
      expect(b.logoUrl).toBe("/logo.png");
    });
    it("returns defaults when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
      const b = await getBranding();
      expect(b.appName).toBe("Portal");
      expect(b.logoUrl).toBe("");
    });
  });

  describe("getCouriers", () => {
    it("returns courierIds when API returns array", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ courierIds: ["DHL", "PostNord"] }),
      });
      const ids = await getCouriers("token");
      expect(ids).toEqual(["DHL", "PostNord"]);
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
      await expect(getCouriers("token")).rejects.toThrow("Failed to load couriers");
    });
  });

  describe("getCourierCatalog", () => {
    it("returns ids when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ courierIds: ["A", "B"] }),
      });
      const ids = await getCourierCatalog("token");
      expect(ids).toEqual(["A", "B"]);
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
      await expect(getCourierCatalog("token")).rejects.toThrow("Failed to load courier catalog");
    });
  });

  describe("getCourierContracts", () => {
    it("normalizes contracts from API", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          contracts: [{ id: "550e8400-e29b-41d4-a716-446655440000", courierId: "DHL", contractId: "X", service: "S" }],
        }),
      });
      const list = await getCourierContracts("token");
      expect(list).toHaveLength(1);
      expect(list[0].courierId).toBe("DHL");
      expect(list[0].contractId).toBe("X");
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
      await expect(getCourierContracts("token")).rejects.toThrow("Failed to load courier contracts");
    });
  });

  describe("putCourierContracts", () => {
    it("returns updated contracts when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          contracts: [{ courierId: "DHL", contractId: "N1" }],
        }),
      });
      const list = await putCourierContracts("token", [{ courierId: "DHL", contractId: "N1" }]);
      expect(list).toHaveLength(1);
      expect(list[0].contractId).toBe("N1");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/company/courier-contracts"),
        expect.objectContaining({ method: "PUT" })
      );
    });
    it("throws with message when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Duplicate courier: DHL" }),
      });
      await expect(putCourierContracts("token", [])).rejects.toThrow("Duplicate courier: DHL");
    });
  });

  describe("adminGetCompanies", () => {
    it("returns companies when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [{ id: "1", name: "Co", companyId: "cid" }],
      });
      const list = await adminGetCompanies("token");
      expect(list).toHaveLength(1);
      expect(list[0].name).toBe("Co");
    });
    it("throws with message when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 403,
        json: async () => ({ message: "Forbidden" }),
      });
      await expect(adminGetCompanies("token")).rejects.toThrow(/403|Forbidden/);
    });
  });

  describe("adminGetUsers", () => {
    it("returns users when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [{ userId: "u1", email: "a@b.com", displayName: "A", isActive: true, roles: [] }],
      });
      const list = await adminGetUsers("token");
      expect(list).toHaveLength(1);
      expect(list[0].userId).toBe("u1");
    });
    it("includes businessId in URL when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, json: async () => [] });
      await adminGetUsers("token", "1234567-8");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("businessId=1234567-8"),
        expect.any(Object)
      );
    });
  });

  describe("adminPatchUser", () => {
    it("succeeds when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true });
      await expect(adminPatchUser("token", "u1", { isActive: false })).resolves.toBeUndefined();
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Update failed" }),
      });
      await expect(adminPatchUser("token", "u1", {})).rejects.toThrow("Update failed");
    });
  });

  describe("createCompany", () => {
    it("returns created company when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "c1", companyId: "1234567-8", name: "New Co" }),
      });
      const c = await createCompany("token", { name: "New Co", businessId: "1234567-8" });
      expect(c.id).toBe("c1");
      expect(c.name).toBe("New Co");
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ message: "Invalid" }),
      });
      await expect(createCompany("token", {})).rejects.toThrow(/Invalid|400/);
    });
  });

  describe("getAddressBook", () => {
    it("returns address book when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ addressBooks: [{ companyId: "c1", senders: [], receivers: [] }] }),
      });
      const r = await getAddressBook("token");
      expect(r.addressBooks).toHaveLength(1);
      expect(r.addressBooks[0].companyId).toBe("c1");
    });
    it("includes companyId in URL when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, json: async () => ({ addressBooks: [] }) });
      await getAddressBook("token", { companyId: "c1" });
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("companyId=c1"), expect.any(Object));
    });
    it("throws Company not found on 404", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false, status: 404 });
      await expect(getAddressBook("token")).rejects.toThrow("Company not found");
    });
  });

  describe("addSender", () => {
    it("returns created entry when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "s1", name: "Sender" }),
      });
      const s = await addSender("token", { name: "Sender" });
      expect(s.id).toBe("s1");
    });
    it("includes companyId in URL when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, json: async () => ({ id: "s1" }) });
      await addSender("token", { name: "S" }, "c1");
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("companyId=c1"), expect.any(Object));
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Failed" }),
      });
      await expect(addSender("token", {})).rejects.toThrow("Failed");
    });
  });

  describe("addReceiver", () => {
    it("returns created entry when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "r1", name: "Receiver" }),
      });
      const r = await addReceiver("token", { name: "Receiver" });
      expect(r.id).toBe("r1");
    });
    it("includes companyId in URL when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, json: async () => ({ id: "r1" }) });
      await addReceiver("token", { name: "R" }, "c1");
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("companyId=c1"), expect.any(Object));
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Failed" }),
      });
      await expect(addReceiver("token", {})).rejects.toThrow("Failed");
    });
  });

  describe("bookingList", () => {
    it("returns bookings when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [{ id: "b1", createdAtUtc: "2024-01-01", enabled: true, isFavourite: false }],
      });
      const list = await bookingList("token");
      expect(list).toHaveLength(1);
      expect(list[0].id).toBe("b1");
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
      await expect(bookingList("token")).rejects.toThrow("Failed to load bookings");
    });
  });

  describe("bookingGet", () => {
    it("returns booking when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "b1", customerId: "c1", enabled: true, isTestBooking: false, isFavourite: false, header: {}, createdAtUtc: "", updatedAtUtc: "" }),
      });
      const b = await bookingGet("token", "b1");
      expect(b.id).toBe("b1");
    });
    it("throws Booking not found on 404", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false, status: 404 });
      await expect(bookingGet("token", "x")).rejects.toThrow("Booking not found");
    });
  });

  describe("bookingCreate", () => {
    it("returns created booking when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "b1", customerId: "c1", enabled: true, isTestBooking: false, isFavourite: false, header: {}, createdAtUtc: "", updatedAtUtc: "" }),
      });
      const b = await bookingCreate("token", { receiverName: "R" });
      expect(b.id).toBe("b1");
    });
    it("throws with migration hint when message includes IsDraft", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: "IsDraft column missing" }),
      });
      await expect(bookingCreate("token", {})).rejects.toThrow(/migration|IsDraft/);
    });
    it("throws with migration hint when message includes column", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: "column X does not exist" }),
      });
      await expect(bookingCreate("token", {})).rejects.toThrow(/migration|column/);
    });
    it("throws generic message when API returns non-ok without IsDraft", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ message: "Invalid receiver" }),
      });
      await expect(bookingCreate("token", {})).rejects.toThrow("Invalid receiver");
    });
  });

  describe("draftList", () => {
    it("returns drafts when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [{ id: "d1", createdAtUtc: "", enabled: true, isFavourite: false, isDraft: true }],
      });
      const list = await draftList("token");
      expect(list).toHaveLength(1);
      expect(list[0].isDraft).toBe(true);
    });
  });

  describe("draftGet", () => {
    it("returns draft when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "d1", customerId: "c1", enabled: true, isTestBooking: false, isFavourite: false, isDraft: true, header: {}, createdAtUtc: "", updatedAtUtc: "" }),
      });
      const d = await draftGet("token", "d1");
      expect(d.id).toBe("d1");
    });
    it("throws Draft not found on 404", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false, status: 404 });
      await expect(draftGet("token", "x")).rejects.toThrow("Draft not found");
    });
  });

  describe("draftCreate", () => {
    it("returns created draft when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "d1", customerId: "c1", enabled: true, isTestBooking: false, isFavourite: false, isDraft: true, header: {}, createdAtUtc: "", updatedAtUtc: "" }),
      });
      const d = await draftCreate("token", { receiverName: "R" });
      expect(d.id).toBe("d1");
    });
  });

  describe("draftUpdate", () => {
    it("returns updated draft when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "d1", customerId: "c1", enabled: true, isTestBooking: false, isFavourite: false, isDraft: true, header: {}, createdAtUtc: "", updatedAtUtc: "" }),
      });
      const d = await draftUpdate("token", "d1", { receiverName: "R2" });
      expect(d.id).toBe("d1");
    });
    it("throws Draft not found on 404", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false, status: 404 });
      await expect(draftUpdate("token", "x", {})).rejects.toThrow("Draft not found");
    });
    it("throws when API returns non-ok with message", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Validation failed" }),
      });
      await expect(draftUpdate("token", "d1", {})).rejects.toThrow("Validation failed");
    });
  });

  describe("draftConfirm", () => {
    it("returns completed booking when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ id: "b1", customerId: "c1", enabled: true, isTestBooking: false, isFavourite: false, header: {}, createdAtUtc: "", updatedAtUtc: "" }),
      });
      const b = await draftConfirm("token", "d1");
      expect(b.id).toBe("b1");
    });
    it("throws Draft not found on 404", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false, status: 404 });
      await expect(draftConfirm("token", "x")).rejects.toThrow("Draft not found");
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "Draft already confirmed" }),
      });
      await expect(draftConfirm("token", "d1")).rejects.toThrow("Draft already confirmed");
    });
  });

  describe("getDashboardStats", () => {
    it("returns stats when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          scope: "all",
          countToday: 5,
          countMonth: 20,
          countYear: 100,
          byCourier: [],
          fromCities: [],
          toCities: [],
          carrierServiceSunburst: null,
          laneSankey: { nodes: [], links: [] },
          bookingsPerDayLast30: [],
          bookingsPerDayCurrentMonth: [],
          kpi: {
            avgPerDayLast30: 0,
            avgPerDayThisMonth: 0,
            avgPerDayThisYear: 0,
            possiblyStuckCount: 0,
          },
          deliveryTime: {
            sampleSize: 0,
            minHours: 0,
            q1Hours: 0,
            medianHours: 0,
            q3Hours: 0,
            maxHours: 0,
            outlierCount: 0,
            sampleHours: [],
          },
          exceptionSignalsHeatmap: { cells: [], maxCount: 0 },
        }),
      });
      const s = await getDashboardStats("token");
      expect(s.countToday).toBe(5);
      expect(s.countMonth).toBe(20);
    });
    it("passes scope query param when not all", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          scope: "drafts",
          countToday: 0,
          countMonth: 0,
          countYear: 0,
          byCourier: [],
          fromCities: [],
          toCities: [],
          carrierServiceSunburst: null,
          laneSankey: { nodes: [], links: [] },
          bookingsPerDayLast30: [],
          bookingsPerDayCurrentMonth: [],
          kpi: {
            avgPerDayLast30: 0,
            avgPerDayThisMonth: 0,
            avgPerDayThisYear: 0,
            possiblyStuckCount: 0,
          },
          deliveryTime: {
            sampleSize: 0,
            minHours: 0,
            q1Hours: 0,
            medianHours: 0,
            q3Hours: 0,
            maxHours: 0,
            outlierCount: 0,
            sampleHours: [],
          },
          exceptionSignalsHeatmap: { cells: [], maxCount: 0 },
        }),
      });
      await getDashboardStats("token", "drafts");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/dashboard/stats?scope=drafts"),
        expect.anything(),
      );
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: false });
      await expect(getDashboardStats("token")).rejects.toThrow("Failed to load dashboard stats");
    });
  });

  describe("getMe with Roles/BusinessId/CompanyName", () => {
    it("uses Roles (PascalCase) when roles omitted", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          userId: "u1",
          email: "u@example.com",
          displayName: "User",
          Roles: ["Admin"],
          BusinessId: "123",
          CompanyName: "Acme",
        }),
      });
      const me = await getMe("token");
      expect(me.roles).toEqual(["Admin"]);
      expect(me.businessId).toBe("123");
      expect(me.companyName).toBe("Acme");
    });
  });

  describe("login with Roles/JwtToken", () => {
    it("uses JwtToken (PascalCase) when jwtToken omitted", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          userId: "u1",
          email: "u@example.com",
          displayName: "User",
          JwtToken: "jwt-xyz",
          Roles: ["User"],
        }),
      });
      const result = await login("u@example.com", "pass");
      expect(result.data?.jwtToken).toBe("jwt-xyz");
      expect(result.data?.roles).toEqual(["User"]);
    });
  });

  describe("bookingsImport", () => {
    it("returns counts when API returns 200", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          createdCount: 2,
          draftCount: 1,
          errors: ["row 2"],
          createdBookingIds: ["a", "b"],
          draftBookingIds: ["c"],
        }),
      });
      const file = new File(["a"], "t.csv", { type: "text/csv" });
      const r = await bookingsImport("tok", file);
      expect(r.createdCount).toBe(2);
      expect(r.draftCount).toBe(1);
      expect(r.errors).toEqual(["row 2"]);
      expect(r.createdBookingIds).toEqual(["a", "b"]);
      expect(r.draftBookingIds).toEqual(["c"]);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/import"),
        expect.objectContaining({ method: "POST", body: expect.any(FormData) }),
      );
    });

    it("throws with message when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "bad header" }),
      });
      const file = new File(["a"], "t.csv", { type: "text/csv" });
      await expect(bookingsImport("tok", file)).rejects.toThrow("bad header");
    });
  });

  describe("bookingsImportAnalyze", () => {
    it("returns analyze payload when headers match", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          needsMapping: false,
          sessionId: "sess-a",
          completedCount: 1,
          draftCount: 0,
          skippedEmptyRows: 0,
          totalDataRows: 1,
          hasSavedMapping: false,
        }),
      });
      const file = new File(["a"], "t.csv", { type: "text/csv" });
      const r = await bookingsImportAnalyze("tok", file);
      expect(r.needsMapping).toBe(false);
      expect(r.sessionId).toBe("sess-a");
      expect(r.totalDataRows).toBe(1);
      expect(r.hasSavedMapping).toBe(false);
      expect(r.savedColumnMap).toBeNull();
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/import/analyze"),
        expect.objectContaining({ method: "POST", body: expect.any(FormData) }),
      );
    });

    it("returns mapping metadata when needsMapping", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          needsMapping: true,
          sessionId: "sess-m",
          fileHeaders: ["A", "B"],
          bookingFields: ["X"],
          hasSavedMapping: true,
          savedColumnMap: { X: "A" },
        }),
      });
      const file = new File(["a"], "t.csv", { type: "text/csv" });
      const r = await bookingsImportAnalyze("tok", file);
      expect(r.needsMapping).toBe(true);
      expect(r.fileHeaders).toEqual(["A", "B"]);
      expect(r.bookingFields).toEqual(["X"]);
      expect(r.hasSavedMapping).toBe(true);
      expect(r.savedColumnMap).toEqual({ X: "A" });
    });
  });

  describe("bookingsImportApplyMapping", () => {
    it("returns preview-shaped payload", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          sessionId: "sess-m",
          completedCount: 0,
          draftCount: 1,
          skippedEmptyRows: 0,
          totalDataRows: 1,
        }),
      });
      const r = await bookingsImportApplyMapping("tok", {
        sessionId: "sess-m",
        columnMap: { ReferenceNumber: "Ref" },
        saveMappingForCompany: true,
      });
      expect(r.sessionId).toBe("sess-m");
      expect(r.totalDataRows).toBe(1);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/import/apply-mapping"),
        expect.objectContaining({
          method: "POST",
          body: expect.stringMatching(/"saveMappingForCompany":true/),
        }),
      );
    });
  });

  describe("bookingsImportPreview", () => {
    it("returns preview payload", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          sessionId: "sess-1",
          completedCount: 2,
          draftCount: 1,
          skippedEmptyRows: 3,
          totalDataRows: 3,
        }),
      });
      const file = new File(["a"], "t.csv", { type: "text/csv" });
      const r = await bookingsImportPreview("tok", file);
      expect(r.sessionId).toBe("sess-1");
      expect(r.completedCount).toBe(2);
      expect(r.skippedEmptyRows).toBe(3);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/import/preview"),
        expect.objectContaining({ method: "POST", body: expect.any(FormData) }),
      );
    });
  });

  describe("bookingsImportConfirm", () => {
    it("returns result with ids", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          createdCount: 1,
          draftCount: 0,
          errors: [],
          createdBookingIds: ["id1"],
          draftBookingIds: [],
        }),
      });
      const r = await bookingsImportConfirm("tok", {
        sessionId: "s",
        importCompleted: true,
        importDrafts: false,
      });
      expect(r.createdBookingIds).toEqual(["id1"]);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/import/confirm"),
        expect.objectContaining({
          method: "POST",
          body: expect.stringContaining('"sessionId":"s"'),
        }),
      );
    });
  });

  describe("bookingsWaybillsBulkDownload", () => {
    it("downloads pdf", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => new Blob(["%PDF"], { type: "application/pdf" }),
      });
      const createObjectURL = vi.fn(() => "blob:mock");
      vi.stubGlobal("URL", { ...globalThis.URL, createObjectURL, revokeObjectURL: vi.fn() });
      const anchor = { click: vi.fn(), href: "", download: "" };
      const ce = vi.spyOn(document, "createElement").mockReturnValue(anchor as unknown as HTMLElement);
      await bookingsWaybillsBulkDownload("tok", ["a", "b"]);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/waybills/bulk"),
        expect.objectContaining({
          method: "POST",
          body: expect.stringContaining("bookingIds"),
        }),
      );
      expect(anchor.click).toHaveBeenCalled();
      ce.mockRestore();
    });
  });

  describe("bookingsExportDownload", () => {
    it("fetches export and triggers download", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => new Blob(["x"], { type: "text/csv" }),
      });
      const createObjectURL = vi.fn(() => "blob:mock");
      vi.stubGlobal("URL", { ...globalThis.URL, createObjectURL, revokeObjectURL: vi.fn() });
      const anchor = { click: vi.fn(), href: "", download: "" };
      const ce = vi.spyOn(document, "createElement").mockReturnValue(anchor as unknown as HTMLElement);
      await bookingsExportDownload("tok", "csv");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/bookings/export?format=csv"),
        expect.objectContaining({ headers: { Authorization: "Bearer tok" } }),
      );
      expect(anchor.click).toHaveBeenCalled();
      expect(createObjectURL).toHaveBeenCalled();
      ce.mockRestore();
    });

    it("throws when export fails", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "no rows" }),
      });
      await expect(bookingsExportDownload("tok", "xlsx")).rejects.toThrow("no rows");
    });
  });
});
