import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import RegisterPage from "./page";

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
  register: vi.fn(),
}));

const apiRegister = await import("@/lib/api").then((m) => m.register);

describe("RegisterPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows registration form with companyId, email, userName, password", () => {
    render(<RegisterPage />);
    expect(screen.getByText("registerTitle")).toBeInTheDocument();
    expect(screen.getByLabelText(/companyId/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/userName/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /createAccount/i })).toBeInTheDocument();
  });

  it("shows companyIdRequired when API returns CompanyIdRequired", async () => {
    vi.mocked(apiRegister).mockImplementation(() =>
      Promise.resolve({ success: false, errorCode: "CompanyIdRequired" })
    );
    render(<RegisterPage />);
    fireEvent.change(screen.getByLabelText(/companyId/i), { target: { value: "123" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "a@b.com" } });
    fireEvent.change(screen.getByLabelText(/userName/i), { target: { value: "User" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "pass123" } });
    fireEvent.click(screen.getByRole("button", { name: /createAccount/i }));

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent(
      "companyIdRequired"
    );
  });

  it("shows companyNotFound when API returns CompanyNotFound", async () => {
    vi.mocked(apiRegister).mockImplementation(() =>
      Promise.resolve({ success: false, errorCode: "CompanyNotFound" })
    );
    render(<RegisterPage />);
    fireEvent.change(screen.getByLabelText(/companyId/i), { target: { value: "123" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "a@b.com" } });
    fireEvent.change(screen.getByLabelText(/userName/i), { target: { value: "User" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "pass123" } });
    fireEvent.click(screen.getByRole("button", { name: /createAccount/i }));

    await expect(screen.findByRole("alert")).resolves.toHaveTextContent(
      "companyNotFound"
    );
  });
});
