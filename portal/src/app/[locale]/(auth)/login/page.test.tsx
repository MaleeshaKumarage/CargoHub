import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import LoginPage from "./page";

const mockReplace = vi.fn();
const mockSetAuth = vi.fn();

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    login: mockSetAuth,
    isAuthenticated: false,
  }),
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

vi.mock("@/lib/api", () => ({
  login: vi.fn(),
}));

const apiLogin = await import("@/lib/api").then((m) => m.login);

describe("LoginPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows sign in form with account and password fields", () => {
    render(<LoginPage />);
    expect(screen.getByText("signInTitle")).toBeInTheDocument();
    expect(screen.getByLabelText(/account/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /signInButton/i })).toBeInTheDocument();
  });

  it("shows error message when login returns success false", async () => {
    vi.mocked(apiLogin).mockResolvedValueOnce({
      success: false,
      message: "Invalid credentials",
    });
    render(<LoginPage />);
    const account = screen.getByLabelText(/account/i);
    const password = screen.getByLabelText(/password/i);
    const submit = screen.getByRole("button", { name: /signInButton/i });

    fireEvent.change(account, { target: { value: "user@test.com" } });
    fireEvent.change(password, { target: { value: "wrong" } });
    fireEvent.click(submit);

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent(
      "Invalid credentials"
    );
  });

  it("shows network error when login throws", async () => {
    vi.mocked(apiLogin).mockRejectedValueOnce(new Error("Network error"));
    render(<LoginPage />);
    fireEvent.change(screen.getByLabelText(/account/i), { target: { value: "u@u.com" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "pass" } });
    fireEvent.click(screen.getByRole("button", { name: /signInButton/i }));

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent(
      "networkError"
    );
  });
});
