import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  login,
  register,
  getMe,
  getCompanySubscription,
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
  adminCreateCompany,
  adminGetSubscriptionPlans,
  adminPatchCompany,
  adminSendTestEmail,
  adminSendReleaseNotes,
  adminGetCompanyBillingPeriods,
  adminGetBillableMonths,
  adminGetBillingMonthBreakdown,
  adminGetBillingBreakdownByDateRange,
  adminGetPlatformEarningsMonthly,
  adminGetPlatformEarningsSeries,
  adminGetPlatformEarningsByCompany,
  adminGetPlatformEarningsBySubscription,
  adminSetBookingInvoiceExcluded,
  AdminCompanyLimitReductionRequiredError,
  DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID,
  adminGetBillingPeriodDetail,
  adminGetUsers,
  adminPatchUser,
  createCompany,
  getAddressBook,
  addSender,
  addReceiver,
  bookingList,
  bookingGet,
  bookingCreate,
  SubscriptionBillingConflictError,
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
  getBookingFieldRules,
  putBookingFieldRules,
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

  describe("getCompanySubscription", () => {
    it("normalizes PascalCase subscription payload", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          PlanName: "Trial",
          PlanKind: "Trial",
          Currency: "EUR",
          TrialBookingAllowance: 5,
        }),
      });

      const sub = await getCompanySubscription("token");

      expect(sub.planName).toBe("Trial");
      expect(sub.planKind).toBe("Trial");
      expect(sub.currency).toBe("EUR");
      expect(sub.trialBookingAllowance).toBe(5);
    });

    it("throws subscription_not_found on 404", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 404,
      });

      await expect(getCompanySubscription("token")).rejects.toThrow("subscription_not_found");
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
    it("normalizes SubscriptionPlanId from PascalCase API", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          {
            Id: "1",
            Name: "Co",
            CompanyId: "cid",
            SubscriptionPlanId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
          },
        ],
      });
      const list = await adminGetCompanies("token");
      expect(list[0].subscriptionPlanId).toBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    });

    it("returns empty list when response body is not an array", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ items: [] }),
      });
      const list = await adminGetCompanies("token");
      expect(list).toEqual([]);
    });

    it("normalizes PascalCase counts, invites, and empty subscription id", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          {
            Id: "1",
            CompanyId: "cid",
            ActiveUserCount: 4,
            AdminCount: 2,
            InitialAdminInviteEmail: "adm@co.com",
            InitialAdminInviteEmails: ["a1@co.com", "a2@co.com"],
            SubscriptionPlanId: "",
          },
        ],
      });
      const list = await adminGetCompanies("token");
      expect(list[0].activeUserCount).toBe(4);
      expect(list[0].adminCount).toBe(2);
      expect(list[0].initialAdminInviteEmail).toBe("adm@co.com");
      expect(list[0].initialAdminInviteEmails).toEqual(["a1@co.com", "a2@co.com"]);
      expect(list[0].subscriptionPlanId).toBeNull();
    });
  });

  describe("adminCreateCompany", () => {
    it("returns normalized company when API succeeds", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: "new1",
          name: "NewCo",
          companyId: "nc",
          businessId: "B-9",
          subscriptionPlanId: DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID,
        }),
      });
      const c = await adminCreateCompany("jwt", {
        name: "NewCo",
        businessId: "B-9",
        subscriptionPlanId: DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID,
      });
      expect(c.id).toBe("new1");
      expect(c.subscriptionPlanId).toBe(DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/companies"),
        expect.objectContaining({ method: "POST" })
      );
    });

    it("throws with message when API returns 400", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ message: "BusinessIdExists" }),
      });
      await expect(adminCreateCompany("jwt", { name: "X", businessId: "dup" })).rejects.toThrow("BusinessIdExists");
    });

    it("throws with status fallback when error body has no message", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 422,
        statusText: "Nope",
        json: async () => ({}),
      });
      await expect(adminCreateCompany("jwt", { name: "X", businessId: "y" })).rejects.toThrow("Nope");
    });

    it("throws generic create failed when message and title are missing", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 418,
        statusText: "",
        json: async () => ({}),
      });
      await expect(adminCreateCompany("jwt", { name: "X", businessId: "y" })).rejects.toThrow(/Create failed \(418\)/);
    });
  });

  describe("adminGetSubscriptionPlans", () => {
    it("returns normalized plans", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          { Id: "p1", Name: "Trial", Kind: "Trial", Currency: "EUR", IsActive: true },
        ],
      });
      const list = await adminGetSubscriptionPlans("token");
      expect(list).toHaveLength(1);
      expect(list[0].id).toBe("p1");
      expect(list[0].kind).toBe("Trial");
    });
    it("returns empty array when response is not an array", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      const list = await adminGetSubscriptionPlans("token");
      expect(list).toEqual([]);
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: "Server error" }),
      });
      await expect(adminGetSubscriptionPlans("token")).rejects.toThrow(/Server error|500/);
    });
  });

  describe("DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID", () => {
    it("matches backend default trial plan id", () => {
      expect(DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID).toBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    });
  });

  describe("adminPatchCompany", () => {
    it("returns normalized company on success", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: "c1",
          name: "Acme",
          companyId: "x",
          businessId: "B1",
          subscriptionPlanId: DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID,
        }),
      });
      const c = await adminPatchCompany("jwt", "c1", { subscriptionPlanId: DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID });
      expect(c.id).toBe("c1");
      expect(c.subscriptionPlanId).toBe(DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/companies/c1"),
        expect.objectContaining({ method: "PATCH" })
      );
    });

    it("throws AdminCompanyLimitReductionRequiredError on 409 LimitReductionRequired", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({
          errorCode: "LimitReductionRequired",
          message: "Pick users",
          activeUserCount: 5,
          proposedMaxUserAccounts: 2,
          adminCount: 2,
          proposedMaxAdminAccounts: 1,
          minimumUsersToDeactivate: 3,
          minimumAdminsToDemote: 1,
          businessId: "BIZ",
        }),
      });
      await expect(adminPatchCompany("jwt", "c1", { maxUserAccounts: 2 })).rejects.toMatchObject({
        name: "AdminCompanyLimitReductionRequiredError",
        details: expect.objectContaining({
          businessId: "BIZ",
          minimumUsersToDeactivate: 3,
          minimumAdminsToDemote: 1,
        }),
      });
    });

    it("throws AdminCompanyLimitReductionRequiredError with PascalCase conflict payload", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({
          ErrorCode: "LimitReductionRequired",
          ActiveUserCount: 1,
          MinimumUsersToDeactivate: 0,
          MinimumAdminsToDemote: 0,
          BusinessId: "X",
        }),
      });
      try {
        await adminPatchCompany("jwt", "c1", { maxAdminAccounts: 1 });
        expect.fail("expected throw");
      } catch (e) {
        expect(e).toBeInstanceOf(AdminCompanyLimitReductionRequiredError);
        expect((e as AdminCompanyLimitReductionRequiredError).details.activeUserCount).toBe(1);
        expect((e as AdminCompanyLimitReductionRequiredError).details.businessId).toBe("X");
      }
    });

    it("throws generic Error on 409 with other errorCode", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({ errorCode: "Conflict", message: "Other" }),
      });
      await expect(adminPatchCompany("jwt", "c1", {})).rejects.toThrow("Other");
    });

    it("uses title fallback when message missing on error", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ title: "Bad" }),
      });
      await expect(adminPatchCompany("jwt", "c1", {})).rejects.toThrow("Bad");
    });

    it("normalizes PascalCase fields on success", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          Id: "c9",
          Name: "Pascal Co",
          CompanyId: "cid9",
          BusinessId: "B9",
          SubscriptionPlanId: null,
        }),
      });
      const c = await adminPatchCompany("jwt", "c9", { maxUserAccounts: 5 });
      expect(c.id).toBe("c9");
      expect(c.name).toBe("Pascal Co");
      expect(c.subscriptionPlanId).toBeNull();
    });
  });

  describe("adminSendTestEmail", () => {
    it("returns payload when API succeeds", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ ok: true, message: "sent" }),
      });
      const r = await adminSendTestEmail("jwt", "a@b.com");
      expect(r.ok).toBe(true);
      expect(r.message).toBe("sent");
    });

    it("throws with message when API fails", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: "SMTP down" }),
      });
      await expect(adminSendTestEmail("jwt", "a@b.com")).rejects.toThrow("SMTP down");
    });

    it("throws with title when message missing", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 503,
        json: async () => ({ title: "Unavailable" }),
      });
      await expect(adminSendTestEmail("jwt", "a@b.com")).rejects.toThrow("Unavailable");
    });

    it("throws generic test email failed when no message, title, or statusText", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 502,
        statusText: "",
        json: async () => ({}),
      });
      await expect(adminSendTestEmail("jwt", "a@b.com")).rejects.toThrow(/Test email failed \(502\)/);
    });
  });

  describe("adminSendReleaseNotes", () => {
    it("returns normalized result when API succeeds", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          recipientCount: 2,
          sentCount: 2,
          failures: [],
        }),
      });
      const r = await adminSendReleaseNotes("jwt", {
        subject: "S",
        body: "B",
        allCompanies: true,
        allRoles: true,
      });
      expect(r.recipientCount).toBe(2);
      expect(r.sentCount).toBe(2);
      expect(r.failures).toEqual([]);
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/email/release-notes"), expect.any(Object));
    });

    it("parses PascalCase failures", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          RecipientCount: 1,
          SentCount: 0,
          Failures: [{ Email: "a@b.com", Message: "bounce" }],
        }),
      });
      const r = await adminSendReleaseNotes("jwt", {
        subject: "S",
        body: "B",
        allCompanies: true,
        allRoles: true,
      });
      expect(r.recipientCount).toBe(1);
      expect(r.failures).toEqual([{ email: "a@b.com", message: "bounce" }]);
    });

    it("throws when API fails", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ message: "No recipients" }),
      });
      await expect(
        adminSendReleaseNotes("jwt", {
          subject: "S",
          body: "B",
          allCompanies: true,
          allRoles: true,
        })
      ).rejects.toThrow("No recipients");
    });
  });

  describe("adminGetCompanyBillingPeriods", () => {
    it("returns normalized periods", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          {
            Id: "per1",
            CompanyId: "c1",
            YearUtc: 2026,
            MonthUtc: 4,
            Currency: "EUR",
            Status: "Open",
            LineItemCount: 2,
            PayableTotal: 10.5,
          },
        ],
      });
      const list = await adminGetCompanyBillingPeriods("token", "c1");
      expect(list).toHaveLength(1);
      expect(list[0].payableTotal).toBe(10.5);
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/companies/c1/billing-periods"), expect.any(Object));
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({ message: "Nope" }),
      });
      await expect(adminGetCompanyBillingPeriods("token", "c1")).rejects.toThrow(/Nope|404/);
    });
  });

  describe("adminGetBillableMonths", () => {
    it("returns normalized months", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          {
            YearUtc: 2026,
            MonthUtc: 4,
            BillableBookingCount: 2,
            BillingPeriodId: "per1",
          },
        ],
      });
      const list = await adminGetBillableMonths("token", "c1");
      expect(list).toHaveLength(1);
      expect(list[0].yearUtc).toBe(2026);
      expect(list[0].monthUtc).toBe(4);
      expect(list[0].billableBookingCount).toBe(2);
      expect(list[0].billingPeriodId).toBe("per1");
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/companies/c1/billable-months"), expect.any(Object));
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: "Bad" }),
      });
      await expect(adminGetBillableMonths("token", "c1")).rejects.toThrow(/Bad/);
    });
  });

  describe("adminGetBillingMonthBreakdown", () => {
    it("returns normalized breakdown", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          CompanyId: "c1",
          YearUtc: 2026,
          MonthUtc: 4,
          BillingPeriodId: "per1",
          RangeStartUtc: "2026-04-01T00:00:00Z",
          RangeEndExclusiveUtc: "2026-05-01T00:00:00Z",
          Currency: "EUR",
          BillableBookingCount: 1,
          PayableTotal: 9.5,
          LedgerTotal: 10,
          Segments: [
            {
              Label: "Tier 1",
              BookingCount: 1,
              UnitRate: 9.5,
              Subtotal: 9.5,
              PlanKind: "Tiered",
              SubscriptionPlanId: "p1",
            },
          ],
          Bookings: [
            {
              BookingId: "b1",
              ShipmentNumber: null,
              ReferenceNumber: "R1",
              PlanLabel: "Pro",
              Description: "Leg",
              Amount: 9.5,
              ExcludedFromInvoice: false,
            },
          ],
        }),
      });
      const b = await adminGetBillingMonthBreakdown("token", "c1", 2026, 4);
      expect(b.billingPeriodId).toBe("per1");
      expect(b.rangeStartUtc).toContain("2026-04-01");
      expect(b.payableTotal).toBe(9.5);
      expect(b.segments).toHaveLength(1);
      expect(b.segments[0].label).toBe("Tier 1");
      expect(b.bookings[0].bookingId).toBe("b1");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/companies/c1/billing-months/2026/4/breakdown"),
        expect.any(Object)
      );
    });
  });

  describe("adminGetBillingBreakdownByDateRange", () => {
    it("calls billing-breakdown with from and to", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          CompanyId: "c1",
          YearUtc: 2026,
          MonthUtc: 3,
          BillingPeriodId: null,
          RangeStartUtc: "2026-03-01T00:00:00Z",
          RangeEndExclusiveUtc: "2026-03-16T00:00:00Z",
          Currency: "EUR",
          BillableBookingCount: 0,
          PayableTotal: 0,
          LedgerTotal: 0,
          Segments: [],
          Bookings: [],
        }),
      });
      const b = await adminGetBillingBreakdownByDateRange("token", "c1", "2026-03-01", "2026-03-15");
      expect(b.billingPeriodId).toBeNull();
      expect(b.billableBookingCount).toBe(0);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/companies/c1/billing-breakdown?"),
        expect.any(Object)
      );
      const url = (fetch as ReturnType<typeof vi.fn>).mock.calls[0][0] as string;
      expect(url).toContain("from=2026-03-01");
      expect(url).toContain("to=2026-03-15");
    });
  });

  describe("adminSetBookingInvoiceExcluded", () => {
    it("PATCHes excluded flag", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, status: 204 });
      await adminSetBookingInvoiceExcluded("token", "per1", "b1", true);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/billing-periods/per1/bookings/b1/invoice-excluded"),
        expect.objectContaining({ method: "PATCH" })
      );
    });
  });

  describe("adminDownloadBillingPeriodInvoicePdf", () => {
    it("returns blob when ok", async () => {
      const blob = new Blob([new Uint8Array([1, 2])], { type: "application/pdf" });
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => blob,
      });
      const { adminDownloadBillingPeriodInvoicePdf } = await import("@/lib/api");
      const out = await adminDownloadBillingPeriodInvoicePdf("token", "per1");
      expect(out).toBe(blob);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/billing-periods/per1/invoice.pdf"),
        expect.any(Object)
      );
    });
    it("appends from and to query when provided", async () => {
      const blob = new Blob([new Uint8Array([1])], { type: "application/pdf" });
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => blob,
      });
      const { adminDownloadBillingPeriodInvoicePdf } = await import("@/lib/api");
      await adminDownloadBillingPeriodInvoicePdf("token", "per1", { from: "2026-04-01", to: "2026-04-15" });
      expect(fetch).toHaveBeenCalledWith(
        expect.stringMatching(/\/billing-periods\/per1\/invoice\.pdf\?from=2026-04-01&to=2026-04-15$/),
        expect.any(Object)
      );
    });

    it("appends only from when to omitted", async () => {
      const blob = new Blob([new Uint8Array([1])], { type: "application/pdf" });
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => blob,
      });
      const { adminDownloadBillingPeriodInvoicePdf } = await import("@/lib/api");
      await adminDownloadBillingPeriodInvoicePdf("token", "per1", { from: "2026-04-01" });
      expect(fetch).toHaveBeenCalledWith(
        expect.stringMatching(/\/billing-periods\/per1\/invoice\.pdf\?from=2026-04-01$/),
        expect.any(Object)
      );
    });

    it("throws when PDF response not ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 403,
        statusText: "Forbidden",
        json: async () => ({ message: "No access" }),
      });
      const { adminDownloadBillingPeriodInvoicePdf } = await import("@/lib/api");
      await expect(adminDownloadBillingPeriodInvoicePdf("token", "per1")).rejects.toThrow("No access");
    });
  });

  describe("adminSendBillingPeriodInvoiceEmail", () => {
    it("posts recipient id", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ ok: true }),
      });
      const { adminSendBillingPeriodInvoiceEmail } = await import("@/lib/api");
      await adminSendBillingPeriodInvoiceEmail("token", "per1", "u1");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/billing-periods/per1/send-invoice-email"),
        expect.objectContaining({ method: "POST" })
      );
    });
    it("includes from and to in body when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ ok: true }),
      });
      const { adminSendBillingPeriodInvoiceEmail } = await import("@/lib/api");
      await adminSendBillingPeriodInvoiceEmail("token", "per1", "u1", {
        from: "2026-04-01",
        to: "2026-04-15",
      });
      const call = (fetch as ReturnType<typeof vi.fn>).mock.calls[0];
      const init = call[1] as RequestInit;
      expect(JSON.parse(init.body as string)).toEqual({
        recipientAdminUserId: "u1",
        from: "2026-04-01",
        to: "2026-04-15",
      });
    });

    it("throws using PascalCase Message on failure", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: "Bad",
        json: async () => ({ Message: "Not ready" }),
      });
      const { adminSendBillingPeriodInvoiceEmail } = await import("@/lib/api");
      await expect(adminSendBillingPeriodInvoiceEmail("token", "per1", "u1")).rejects.toThrow("Not ready");
    });
  });

  describe("adminPatchBillingLineItem", () => {
    it("patches excluded flag", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true, status: 204 });
      const { adminPatchBillingLineItem } = await import("@/lib/api");
      await adminPatchBillingLineItem("token", "line1", true);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/billing-line-items/line1"),
        expect.objectContaining({ method: "PATCH" })
      );
    });

    it("throws using message from error body", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: "Bad Req",
        json: async () => ({ message: "Line gone" }),
      });
      const { adminPatchBillingLineItem } = await import("@/lib/api");
      await expect(adminPatchBillingLineItem("t", "x", false)).rejects.toThrow("Line gone");
    });

    it("throws using PascalCase Message when message missing", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: "Bad",
        json: async () => ({ Message: "From server" }),
      });
      const { adminPatchBillingLineItem } = await import("@/lib/api");
      await expect(adminPatchBillingLineItem("t", "x", false)).rejects.toThrow("From server");
    });

    it("throws using status text when JSON empty", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 502,
        statusText: "Bad Gateway",
        json: async () => ({}),
      });
      const { adminPatchBillingLineItem } = await import("@/lib/api");
      await expect(adminPatchBillingLineItem("t", "x", false)).rejects.toThrow("Bad Gateway");
    });
  });

  describe("adminGetSubscriptionPlanDetail", () => {
    it("normalizes pricing periods and tiers", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          Id: "p1",
          Name: "Trial",
          Kind: "Trial",
          ChargeTimeAnchor: "CreatedAtUtc",
          TrialBookingAllowance: 3,
          Currency: "EUR",
          IsActive: true,
          PricingPeriods: [
            {
              Id: "pp1",
              EffectiveFromUtc: "2026-01-01T00:00:00Z",
              ChargePerBooking: 1,
              Tiers: [{ Id: "t1", Ordinal: 1, InclusiveMaxBookingsInPeriod: 5, ChargePerBooking: 2, MonthlyFee: null }],
            },
          ],
        }),
      });
      const { adminGetSubscriptionPlanDetail } = await import("@/lib/api");
      const d = await adminGetSubscriptionPlanDetail("token", "p1");
      expect(d.id).toBe("p1");
      expect(d.pricingPeriods).toHaveLength(1);
      expect(d.pricingPeriods[0].tiers[0].ordinal).toBe(1);
    });
  });

  describe("adminGetBillingPeriodDetail", () => {
    it("normalizes LineItems from PascalCase API", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          Id: "p1",
          CompanyId: "c1",
          YearUtc: 2026,
          MonthUtc: 4,
          Currency: "EUR",
          Status: "Open",
          PayableTotal: 5,
          LineItems: [
            {
              Id: "l1",
              BookingId: null,
              LineType: "PerBooking",
              Amount: 5,
              Currency: "EUR",
              ExcludedFromInvoice: false,
              CreatedAtUtc: "2026-01-01T00:00:00Z",
            },
          ],
        }),
      });
      const d = await adminGetBillingPeriodDetail("token", "p1");
      expect(d.lineItems).toHaveLength(1);
      expect(d.lineItems[0].lineType).toBe("PerBooking");
      expect(d.lineItems[0].bookingId).toBeNull();
    });
    it("throws when API returns non-ok", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({ message: "Missing" }),
      });
      await expect(adminGetBillingPeriodDetail("token", "x")).rejects.toThrow(/Missing|404/);
    });
    it("accepts camelCase lineItems with bookingId and component", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: "p1",
          companyId: "c1",
          yearUtc: 2026,
          monthUtc: 4,
          currency: "EUR",
          status: "Open",
          payableTotal: 1,
          lineItems: [
            {
              id: "l1",
              bookingId: "b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b",
              lineType: "Overage",
              component: "x",
              amount: 2,
              currency: "USD",
              excludedFromInvoice: true,
              createdAtUtc: "2026-02-01T00:00:00Z",
            },
          ],
        }),
      });
      const d = await adminGetBillingPeriodDetail("token", "p1");
      expect(d.lineItems[0].bookingId).toBe("b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b");
      expect(d.lineItems[0].component).toBe("x");
      expect(d.lineItems[0].excludedFromInvoice).toBe(true);
    });
    it("treats missing lineItems as empty", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: "p1",
          companyId: "c1",
          yearUtc: 2026,
          monthUtc: 4,
          currency: "EUR",
          status: "Open",
          payableTotal: 0,
        }),
      });
      const d = await adminGetBillingPeriodDetail("token", "p1");
      expect(d.lineItems).toEqual([]);
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
    it("throws SubscriptionBillingConflictError on 409 TrialBookingLimitExceeded", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({
          errorCode: "TrialBookingLimitExceeded",
          message: "Trial booking allowance has been used.",
        }),
      });
      try {
        await bookingCreate("token", { receiverName: "R" });
        expect.fail("expected throw");
      } catch (e) {
        expect(e).toBeInstanceOf(SubscriptionBillingConflictError);
        expect((e as SubscriptionBillingConflictError).errorCode).toBe("TrialBookingLimitExceeded");
        expect((e as SubscriptionBillingConflictError).message).toBe("Trial booking allowance has been used.");
        expect((e as SubscriptionBillingConflictError).isTrialBookingLimitExceeded).toBe(true);
      }
    });
    it("throws SubscriptionBillingConflictError on 409 with ErrorCode/Message casing", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({
          ErrorCode: "TrialBookingLimitExceeded",
          Message: "Trial booking allowance has been used.",
        }),
      });
      await expect(bookingCreate("token", {})).rejects.toBeInstanceOf(SubscriptionBillingConflictError);
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
    it("throws SubscriptionBillingConflictError on 409 TrialBookingLimitExceeded", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({
          errorCode: "TrialBookingLimitExceeded",
          message: "Trial booking allowance has been used.",
        }),
      });
      await expect(draftConfirm("token", "d1")).rejects.toBeInstanceOf(SubscriptionBillingConflictError);
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
    it("passes heatmap year and month when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          scope: "all",
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
      await getDashboardStats("token", null, { year: 2024, month: 6 });
      const url = (fetch as ReturnType<typeof vi.fn>).mock.calls[0][0] as string;
      expect(url).toContain("heatmapYear=2024");
      expect(url).toContain("heatmapMonth=6");
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

  describe("admin platform earnings", () => {
    it("monthly maps PascalCase and passes months query", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [{ YearUtc: 2025, MonthUtc: 3, TotalEur: 1.5 }],
      });
      const r = await adminGetPlatformEarningsMonthly("tok", 12);
      expect(r).toEqual([{ yearUtc: 2025, monthUtc: 3, totalEur: 1.5 }]);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/dashboard/earnings/monthly?months=12"),
        expect.objectContaining({ headers: { Authorization: "Bearer tok" } }),
      );
    });

    it("monthly returns empty when response is not an array", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ x: 1 }),
      });
      const r = await adminGetPlatformEarningsMonthly("tok");
      expect(r).toEqual([]);
    });

    it("monthly throws with message from error body", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "forbidden" }),
      });
      await expect(adminGetPlatformEarningsMonthly("tok")).rejects.toThrow("forbidden");
    });

    it("by company maps fields and query params", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          { CompanyId: "a1", CompanyName: "Acme", AmountEur: 42 },
        ],
      });
      const r = await adminGetPlatformEarningsByCompany("tok", 2024, 7);
      expect(r).toEqual([{ companyId: "a1", companyName: "Acme", amountEur: 42 }]);
      const url = String((fetch as ReturnType<typeof vi.fn>).mock.calls[0]?.[0] ?? "");
      expect(url).toContain("yearUtc=2024");
      expect(url).toContain("monthUtc=7");
    });

    it("by subscription maps plan fields", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [
          { PlanId: "p1", PlanName: "Pro", AmountEur: 10, Percent: 50.5 },
        ],
      });
      const r = await adminGetPlatformEarningsBySubscription("tok", 2023, 5);
      expect(r[0].planId).toBe("p1");
      expect(r[0].percent).toBe(50.5);
    });

    it("series maps PascalCase and encodes range", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => [{ Period: "2025-03-01", TotalEur: 9.25 }],
      });
      const r = await adminGetPlatformEarningsSeries("tok", "lastMonth");
      expect(r).toEqual([{ period: "2025-03-01", totalEur: 9.25 }]);
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/dashboard/earnings/series?range=lastMonth"),
        expect.objectContaining({ headers: { Authorization: "Bearer tok" } }),
      );
    });

    it("series returns empty when response is not an array", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      const r = await adminGetPlatformEarningsSeries("tok", "yesterday");
      expect(r).toEqual([]);
    });
  });

  describe("booking field rules", () => {
    it("get without companyId", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ version: 1, sections: {}, fields: {} }),
      });
      await getBookingFieldRules("tok");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringMatching(/\/company\/booking-field-rules$/),
        expect.objectContaining({ headers: { Authorization: "Bearer tok" } }),
      );
    });

    it("get appends companyId when provided", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ version: 2, sections: { s: "x" }, fields: {} }),
      });
      const r = await getBookingFieldRules("tok", "co-1");
      expect(r.version).toBe(2);
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining("companyId=co-1"), expect.any(Object));
    });

    it("put sends body and optional company query", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ version: 1, sections: { a: "m" }, fields: { f: "o" } }),
      });
      const body = { version: 1, sections: { a: "m" }, fields: { f: "o" } };
      const r = await putBookingFieldRules("tok", body, "cid");
      expect(r.fields.f).toBe("o");
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("companyId=cid"),
        expect.objectContaining({
          method: "PUT",
          body: JSON.stringify(body),
        }),
      );
    });

    it("put throws on failure", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: "bad" }),
      });
      await expect(
        putBookingFieldRules("tok", { version: 1, sections: {}, fields: {} }),
      ).rejects.toThrow("bad");
    });
  });

  describe("adminGetSubscriptionPlanDetail normalization branches", () => {
    it("handles missing tiers array and null numeric fields on periods", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: "p1",
          name: "P",
          kind: "TieredPayPerBooking",
          chargeTimeAnchor: "FirstBillableAtUtc",
          trialBookingAllowance: "",
          currency: "SEK",
          isActive: false,
          pricingPeriods: [
            {
              id: "pp1",
              effectiveFromUtc: "2020-01-01T00:00:00Z",
              tiers: null,
            },
          ],
        }),
      });
      const { adminGetSubscriptionPlanDetail } = await import("@/lib/api");
      const d = await adminGetSubscriptionPlanDetail("tok", "p1");
      expect(d.pricingPeriods[0].tiers).toEqual([]);
      expect(d.pricingPeriods[0].chargePerBooking).toBeNull();
      expect(d.trialBookingAllowance).toBeNull();
    });

    it("normalizes tier with only monthly fee set", async () => {
      (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: "p2",
          name: "P",
          kind: "TieredMonthlyByUsage",
          chargeTimeAnchor: "FirstBillableAtUtc",
          currency: "EUR",
          isActive: true,
          pricingPeriods: [
            {
              id: "pp1",
              effectiveFromUtc: "2020-01-01T00:00:00Z",
              monthlyFee: 99,
              includedBookingsPerMonth: 10,
              overageChargePerBooking: 1.5,
              tiers: [
                {
                  id: "t1",
                  ordinal: 2,
                  inclusiveMaxBookingsInPeriod: null,
                  monthlyFee: 40,
                },
              ],
            },
          ],
        }),
      });
      const { adminGetSubscriptionPlanDetail } = await import("@/lib/api");
      const d = await adminGetSubscriptionPlanDetail("tok", "p2");
      const tier = d.pricingPeriods[0].tiers[0];
      expect(tier.inclusiveMaxBookingsInPeriod).toBeNull();
      expect(tier.monthlyFee).toBe(40);
      expect(tier.chargePerBooking).toBeNull();
      expect(d.pricingPeriods[0].monthlyFee).toBe(99);
      expect(d.pricingPeriods[0].includedBookingsPerMonth).toBe(10);
      expect(d.pricingPeriods[0].overageChargePerBooking).toBe(1.5);
    });
  });
});
