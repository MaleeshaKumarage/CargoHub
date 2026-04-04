import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import LocaleRootPage from "./page";

const mockReplace = vi.fn();

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

const authMock = {
  isAuthenticated: false,
  isLoading: true,
};

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => authMock,
}));

describe("LocaleRootPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authMock.isAuthenticated = false;
    authMock.isLoading = true;
  });

  it("shows redirecting text while loading", () => {
    authMock.isLoading = true;
    render(<LocaleRootPage />);
    expect(screen.getByText("redirecting")).toBeInTheDocument();
  });

  it("redirects to dashboard when authenticated", async () => {
    authMock.isLoading = false;
    authMock.isAuthenticated = true;
    render(<LocaleRootPage />);
    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalledWith("/dashboard");
    });
  });

  it("redirects to login when not authenticated", async () => {
    authMock.isLoading = false;
    authMock.isAuthenticated = false;
    render(<LocaleRootPage />);
    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalledWith("/login");
    });
  });
});
