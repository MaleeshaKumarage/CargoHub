import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@/test/test-utils";
import ManageInvoicesPage from "./page";

const mockUseAuth = vi.fn();

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => mockUseAuth(),
}));

const mockGetCompanies = vi.fn();
const mockGetPeriods = vi.fn();
const mockGetDetail = vi.fn();
const mockGetUsers = vi.fn();

vi.mock("@/lib/api", async () => {
  const actual = await vi.importActual<typeof import("@/lib/api")>("@/lib/api");
  return {
    ...actual,
    adminGetCompanies: (...a: unknown[]) => mockGetCompanies(...a),
    adminGetCompanyBillingPeriods: (...a: unknown[]) => mockGetPeriods(...a),
    adminGetBillingPeriodDetail: (...a: unknown[]) => mockGetDetail(...a),
    adminGetUsers: (...a: unknown[]) => mockGetUsers(...a),
    adminDownloadBillingPeriodInvoicePdf: vi.fn(),
    adminSendBillingPeriodInvoiceEmail: vi.fn(),
    adminPatchBillingLineItem: vi.fn(),
  };
});

describe("ManageInvoicesPage", () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ token: "jwt" });
    mockGetUsers.mockResolvedValue([
      { userId: "u1", email: "a@x.com", displayName: "", businessId: "123", isActive: true, roles: ["Admin"] },
    ]);
    mockGetCompanies.mockResolvedValue([
      { id: "c1", name: "Acme", companyId: "x", businessId: "123" },
    ]);
    mockGetPeriods.mockResolvedValue([
      {
        id: "per1",
        companyId: "c1",
        yearUtc: 2026,
        monthUtc: 4,
        currency: "EUR",
        status: "Open",
        lineItemCount: 1,
        payableTotal: 10,
      },
    ]);
    mockGetDetail.mockResolvedValue({
      id: "per1",
      companyId: "c1",
      yearUtc: 2026,
      monthUtc: 4,
      currency: "EUR",
      status: "Open",
      payableTotal: 10,
      lineItems: [
        {
          id: "l1",
          bookingId: null,
          lineType: "PerBooking",
          component: null,
          amount: 10,
          currency: "EUR",
          excludedFromInvoice: false,
          createdAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
    });
  });

  it("renders heading and loads companies", async () => {
    render(<ManageInvoicesPage />);
    expect(screen.getByRole("heading", { name: "title" })).toBeInTheDocument();
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalledWith("jwt"));
  });

  it("loads periods when company selected and opens line detail", async () => {
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    const select = screen.getByRole("combobox", { name: "selectCompany" });
    fireEvent.change(select, { target: { value: "c1" } });
    await waitFor(() => expect(mockGetPeriods).toHaveBeenCalledWith("jwt", "c1"));
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(mockGetDetail).toHaveBeenCalledWith("jwt", "per1"));
    await waitFor(() => expect(screen.getByText("PerBooking")).toBeInTheDocument());
  });

  it("shows error when billing periods request fails", async () => {
    mockGetPeriods.mockRejectedValueOnce(new Error("periods failed"));
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByText(/periods failed/)).toBeInTheDocument());
  });

  it("shows excluded marker for invoice-excluded lines", async () => {
    mockGetDetail.mockResolvedValueOnce({
      id: "per1",
      companyId: "c1",
      yearUtc: 2026,
      monthUtc: 4,
      currency: "EUR",
      status: "Open",
      payableTotal: 0,
      lineItems: [
        {
          id: "l1",
          bookingId: null,
          lineType: "Adjustment",
          component: null,
          amount: 1,
          currency: "EUR",
          excludedFromInvoice: true,
          createdAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
    });
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(mockGetPeriods).toHaveBeenCalled());
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => {
      expect(screen.getByRole("switch", { name: "excluded" })).toBeChecked();
    });
  });

  it("renders nothing when token is missing", () => {
    mockUseAuth.mockReturnValue({ token: null });
    const { container } = render(<ManageInvoicesPage />);
    expect(container.firstChild).toBeNull();
  });

  it("shows error when billing period detail fails", async () => {
    mockGetDetail.mockRejectedValueOnce(new Error("detail failed"));
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(mockGetPeriods).toHaveBeenCalled());
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(screen.getByText(/detail failed/)).toBeInTheDocument());
  });

  it("shows no periods message when company has no billing periods", async () => {
    mockGetPeriods.mockResolvedValueOnce([]);
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByText("noPeriods")).toBeInTheDocument());
  });

  it("shows dialog loading then detail after fetch resolves", async () => {
    let resolveDetail!: (value: {
      id: string;
      companyId: string;
      yearUtc: number;
      monthUtc: number;
      currency: string;
      status: string;
      payableTotal: number;
      lineItems: Array<{
        id: string;
        bookingId: string | null;
        lineType: string;
        component: string | null;
        amount: number;
        currency: string;
        excludedFromInvoice: boolean;
        createdAtUtc: string;
      }>;
    }) => void;
    mockGetDetail.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveDetail = resolve;
        })
    );
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(mockGetPeriods).toHaveBeenCalled());
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(screen.getByText("…")).toBeInTheDocument());
    resolveDetail({
      id: "per1",
      companyId: "c1",
      yearUtc: 2026,
      monthUtc: 4,
      currency: "EUR",
      status: "Open",
      payableTotal: 0,
      lineItems: [],
    });
    await waitFor(() => expect(screen.queryByText("…")).not.toBeInTheDocument());
  });
});
