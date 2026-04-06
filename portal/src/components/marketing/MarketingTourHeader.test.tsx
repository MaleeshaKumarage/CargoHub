import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import { MarketingTourHeader } from "./MarketingTourHeader";

let mockBranding: { appName: string; logoUrl: string; primaryColor: string; secondaryColor: string } = {
  appName: "",
  logoUrl: "",
  primaryColor: "",
  secondaryColor: "",
};

vi.mock("@/context/BrandingContext", () => ({
  useBranding: () => mockBranding,
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/tour",
  Link: ({ children, href, locale: _locale }: { children: React.ReactNode; href: string; locale?: string }) => (
    <a href={href} data-locale={_locale}>
      {children}
    </a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: (namespace?: string) => (key: string) => (namespace ? `${namespace}.${key}` : key),
  useLocale: () => "en",
}));

describe("MarketingTourHeader", () => {
  beforeEach(() => {
    mockBranding = { appName: "", logoUrl: "", primaryColor: "", secondaryColor: "" };
  });

  it("renders default marketing logo and alt from tour namespace", () => {
    render(<MarketingTourHeader />);
    const img = screen.getByRole("img", { name: "tour.logoAlt" });
    expect(img).toHaveAttribute("src", "/branding/cargohub-logo-on-dark.svg");
  });

  it("uses common.appName when branding has no appName", () => {
    render(<MarketingTourHeader />);
    expect(screen.getByText("common.appName")).toBeInTheDocument();
  });

  it("uses branding appName in header when set", () => {
    mockBranding = { appName: "FleetHub", logoUrl: "", primaryColor: "", secondaryColor: "" };
    render(<MarketingTourHeader />);
    expect(screen.getByText("FleetHub")).toBeInTheDocument();
  });

  it("renders anchor nav links for each tour section", () => {
    render(<MarketingTourHeader />);
    const nav = screen.getByRole("navigation", { name: /Tour sections/i });
    expect(nav.querySelector('a[href="#top"]')).toBeTruthy();
    expect(nav.querySelector('a[href="#platform"]')).toBeTruthy();
    expect(nav.querySelector('a[href="#admin"]')).toBeTruthy();
  });

  it("renders sign-in and register links", () => {
    render(<MarketingTourHeader />);
    expect(screen.getByRole("link", { name: "tour.cta.register" })).toHaveAttribute("href", "/register");
    expect(screen.getByRole("link", { name: "tour.cta.signIn" })).toHaveAttribute("href", "/login");
  });
});
