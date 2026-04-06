import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import { Navbar } from "./Navbar";

const mockReplace = vi.fn();
const mockLogout = vi.fn();

let mockUser: { displayName?: string; email?: string; businessId?: string | null } = {
  displayName: "Test User",
  email: "u@test.com",
  businessId: null,
};
let mockRolesFromToken: string[] = [];
let mockBranding: { appName: string; logoUrl: string; primaryColor: string; secondaryColor: string } = {
  appName: "appName",
  logoUrl: "",
  primaryColor: "",
  secondaryColor: "",
};

vi.mock("@/context/DesignThemeContext", () => ({
  useDesignTheme: () => ({ theme: "minimalism", setTheme: vi.fn(), isLoading: false }),
}));

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    user: mockUser,
    token: "token",
    logout: mockLogout,
  }),
}));

vi.mock("@/lib/api", () => ({
  getMe: vi.fn().mockResolvedValue({ roles: [] }),
  DESIGN_THEMES: ["skeuomorphism", "neobrutalism", "claymorphism", "minimalism"],
}));

vi.mock("@/context/BrandingContext", () => ({
  useBranding: () => mockBranding,
}));

vi.mock("@/lib/jwt", () => ({
  getRolesFromToken: () => mockRolesFromToken,
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
  usePathname: () => "/dashboard",
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
  useLocale: () => "en",
}));

describe("Navbar", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUser = { displayName: "Test User", email: "u@test.com", businessId: null };
    mockRolesFromToken = [];
    mockBranding = { appName: "appName", logoUrl: "", primaryColor: "", secondaryColor: "" };
  });

  it("renders app name link to dashboard", () => {
    render(<Navbar />);
    const appLink = screen.getByRole("link", { name: /appName/i });
    expect(appLink).toBeInTheDocument();
    expect(appLink).toHaveAttribute("href", "/dashboard");
  });

  it("renders custom logo image when branding.logoUrl is set", () => {
    mockBranding = {
      appName: "Acme Ship",
      logoUrl: "https://cdn.example/logo.png",
      primaryColor: "",
      secondaryColor: "",
    };
    render(<Navbar />);
    const img = screen.getByRole("img", { name: "Acme Ship" });
    expect(img).toHaveAttribute("src", "https://cdn.example/logo.png");
    expect(screen.getByRole("link", { name: /Acme Ship/i })).toHaveAttribute("href", "/dashboard");
  });

  it("does not render logo img when branding.logoUrl is empty", () => {
    mockBranding = { appName: "NoLogo Co", logoUrl: "", primaryColor: "", secondaryColor: "" };
    render(<Navbar />);
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /NoLogo Co/i })).toBeInTheDocument();
  });

  it("renders nav tabs: Dashboard, Bookings, Actions, Plugin, More", () => {
    render(<Navbar />);
    expect(screen.getByRole("link", { name: /dashboard/i })).toHaveAttribute("href", "/dashboard");
    expect(screen.getByRole("link", { name: /bookings/i })).toHaveAttribute("href", "/bookings");
    expect(screen.getByRole("link", { name: /actions/i })).toHaveAttribute("href", "/actions");
    expect(screen.getByRole("link", { name: /plugin/i })).toHaveAttribute("href", "/plugin");
    expect(screen.getByRole("link", { name: /more/i })).toHaveAttribute("href", "/more");
  });

  it("does not show Booking form link when user has no businessId", () => {
    mockUser = { displayName: "Test", email: "u@x.com", businessId: null };
    render(<Navbar />);
    expect(screen.queryByRole("link", { name: /bookingForm/i })).not.toBeInTheDocument();
  });

  it("shows Manage button when user is SuperAdmin", () => {
    mockRolesFromToken = ["SuperAdmin"];
    render(<Navbar />);
    expect(screen.getByRole("button", { name: /manage/i })).toBeInTheDocument();
  });

  it("renders account button with display name", () => {
    render(<Navbar />);
    const accountButton = screen.getByRole("button", { name: /Test User|account/i });
    expect(accountButton).toBeInTheDocument();
  });
});
