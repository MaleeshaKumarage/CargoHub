import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@/test/test-utils";
import CourierContractsPage from "./page";

const mockReplace = vi.fn();

const mockGetContracts = vi.hoisted(() => vi.fn());
const mockGetCatalog = vi.hoisted(() => vi.fn());
const mockPutContracts = vi.hoisted(() => vi.fn());

vi.mock("@/lib/api", () => ({
  getCourierContracts: mockGetContracts,
  getCourierCatalog: mockGetCatalog,
  putCourierContracts: mockPutContracts,
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const stableT = vi.hoisted(() => {
  const t = (key: string) => key;
  return t;
});

vi.mock("next-intl", () => ({
  // Stable reference: page useEffect depends on `t`; a new function each render causes an infinite loop.
  useTranslations: () => stableT,
}));

const authMock = {
  token: "jwt-token",
  user: { roles: ["Admin"] as string[] },
  isAuthenticated: true,
  isLoading: false,
};

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => authMock,
}));

describe("CourierContractsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authMock.token = "jwt-token";
    authMock.user = { roles: ["Admin"] };
    authMock.isAuthenticated = true;
    authMock.isLoading = false;
    mockGetContracts.mockResolvedValue([]);
    mockGetCatalog.mockResolvedValue(["matkahuolto"]);
    mockPutContracts.mockResolvedValue([]);
  });

  it("redirects to login when not authenticated", async () => {
    authMock.isAuthenticated = false;
    render(<CourierContractsPage />);
    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalledWith("/login");
    });
  });

  it("shows super admin message and no contract editor", async () => {
    authMock.user = { roles: ["SuperAdmin"] };
    render(<CourierContractsPage />);
    expect(await screen.findByText("superAdminNoAccess")).toBeInTheDocument();
    expect(mockGetContracts).not.toHaveBeenCalled();
  });

  it("shows empty state for admin when there are no contracts", async () => {
    render(<CourierContractsPage />);
    expect(await screen.findByText("emptyList")).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("title");
    expect(mockGetContracts).toHaveBeenCalledWith("jwt-token");
    expect(mockGetCatalog).toHaveBeenCalledWith("jwt-token");
  });

  it("shows load error when contracts request fails", async () => {
    mockGetContracts.mockRejectedValueOnce(new Error("server down"));
    render(<CourierContractsPage />);
    expect(await screen.findByRole("alert")).toHaveTextContent("server down");
  });

  it("shows read-only hint for non-admin users", async () => {
    authMock.user = { roles: ["User"] };
    mockGetContracts.mockResolvedValue([
      { courierId: "dhl", contractId: "C-1", service: "Express" },
    ]);
    render(<CourierContractsPage />);
    expect(await screen.findByText("readOnlyHint")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /addCourier/i })).not.toBeInTheDocument();
    expect(await screen.findByText("dhl")).toBeInTheDocument();
  });

  it("saves contracts for admin", async () => {
    mockGetContracts.mockImplementation(() =>
      Promise.resolve([{ courierId: "matkahuolto", contractId: "", service: "" }])
    );
    mockPutContracts.mockResolvedValue([]);
    render(<CourierContractsPage />);
    const contractInput = await screen.findByPlaceholderText("contractIdPlaceholder");
    fireEvent.change(contractInput, { target: { value: "AGR-99" } });
    await waitFor(() => {
      expect((contractInput as HTMLInputElement).value).toBe("AGR-99");
    });
    const saveBtn = await screen.findByText("save", { selector: "button" });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(mockPutContracts).toHaveBeenCalledWith("jwt-token", [
        { courierId: "matkahuolto", contractId: "AGR-99", service: undefined },
      ]);
    });
  });

  it("shows catalog unavailable when catalog fetch fails for admin", async () => {
    mockGetCatalog.mockRejectedValueOnce(new Error("catalog down"));
    render(<CourierContractsPage />);
    await screen.findByText("catalogUnavailable");
  });
});
