import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@/test/test-utils";
import AcceptInvitePage from "./page";

const mockReplace = vi.fn();
const mockSetAuth = vi.fn();

const authMock = {
  login: mockSetAuth,
  isAuthenticated: false,
};

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => authMock,
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams({ token: "from-query" }),
}));

const mockAcceptInvite = vi.hoisted(() => vi.fn());

vi.mock("@/lib/api", () => ({
  acceptCompanyAdminInvite: mockAcceptInvite,
}));

describe("AcceptInvitePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authMock.isAuthenticated = false;
  });

  it("renders invite form with token from query string", async () => {
    render(<AcceptInvitePage />);
    const tokenInput = await screen.findByLabelText(/inviteToken/i);
    expect(tokenInput).toHaveValue("from-query");
    expect(screen.getByText("inviteTitle")).toBeInTheDocument();
  });

  it("shows password mismatch error", async () => {
    render(<AcceptInvitePage />);
    await screen.findByLabelText(/inviteToken/i);
    fireEvent.change(screen.getByLabelText(/userName/i), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText(/^password$/i), { target: { value: "secret12" } });
    fireEvent.change(screen.getByLabelText(/confirmPassword/i), {
      target: { value: "other12" },
    });
    fireEvent.click(screen.getByRole("button", { name: /inviteSubmit/i }));

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("passwordMismatch");
    expect(mockAcceptInvite).not.toHaveBeenCalled();
  });

  it("submits and signs in on success", async () => {
    mockAcceptInvite.mockResolvedValueOnce({
      success: true,
      data: {
        userId: "u1",
        email: "a@b.com",
        displayName: "Admin",
        businessId: "B1",
        customerMappingId: null,
        jwtToken: "jwt-here",
        roles: ["Admin"],
      },
    });

    render(<AcceptInvitePage />);
    await screen.findByLabelText(/inviteToken/i);
    fireEvent.change(screen.getByLabelText(/userName/i), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText(/^password$/i), { target: { value: "secret12" } });
    fireEvent.change(screen.getByLabelText(/confirmPassword/i), {
      target: { value: "secret12" },
    });
    fireEvent.click(screen.getByRole("button", { name: /inviteSubmit/i }));

    await waitFor(() => {
      expect(mockAcceptInvite).toHaveBeenCalledWith({
        token: "from-query",
        password: "secret12",
        userName: "admin",
      });
    });
    expect(mockSetAuth).toHaveBeenCalled();
    expect(mockReplace).toHaveBeenCalledWith("/dashboard");
  });

  it("shows API error message when invite is invalid", async () => {
    mockAcceptInvite.mockResolvedValueOnce({
      success: false,
      message: "Expired token",
    });

    render(<AcceptInvitePage />);
    await screen.findByLabelText(/inviteToken/i);
    fireEvent.change(screen.getByLabelText(/userName/i), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText(/^password$/i), { target: { value: "secret12" } });
    fireEvent.change(screen.getByLabelText(/confirmPassword/i), {
      target: { value: "secret12" },
    });
    fireEvent.click(screen.getByRole("button", { name: /inviteSubmit/i }));

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Expired token");
  });

  it("shows inviteInvalid when API throws", async () => {
    mockAcceptInvite.mockRejectedValueOnce(new Error("network"));

    render(<AcceptInvitePage />);
    await screen.findByLabelText(/inviteToken/i);
    fireEvent.change(screen.getByLabelText(/userName/i), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText(/^password$/i), { target: { value: "secret12" } });
    fireEvent.change(screen.getByLabelText(/confirmPassword/i), {
      target: { value: "secret12" },
    });
    fireEvent.click(screen.getByRole("button", { name: /inviteSubmit/i }));

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("inviteInvalid");
  });

  it("redirects to dashboard when already authenticated", async () => {
    authMock.isAuthenticated = true;
    render(<AcceptInvitePage />);
    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalledWith("/dashboard");
    });
  });
});
