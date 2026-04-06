import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import { MarketingTourHeader } from "./MarketingTourHeader";

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
  it("renders default marketing logo and alt from tour namespace", () => {
    render(<MarketingTourHeader />);
    const img = screen.getByRole("img", { name: "tour.logoAlt" });
    expect(img).toHaveAttribute("src", "/branding/cargohub-logo-on-dark.svg");
  });

  it("renders anchor nav links for each tour section", () => {
    render(<MarketingTourHeader />);
    const nav = screen.getByRole("navigation", { name: /Tour sections/i });
    expect(nav.querySelector('a[href="#top"]')).toBeTruthy();
    expect(nav.querySelector('a[href="#platform"]')).toBeTruthy();
    expect(nav.querySelector('a[href="#integrations"]')).toBeTruthy();
    expect(nav.querySelector('a[href="#admin"]')).toBeTruthy();
    expect(nav.querySelector('a[href="#subscriptions"]')).toBeTruthy();
  });

  it("renders register link in header", () => {
    render(<MarketingTourHeader />);
    expect(screen.getByRole("link", { name: "tour.cta.register" })).toHaveAttribute("href", "/register");
  });

  it("does not render start or sign-in in section nav", () => {
    render(<MarketingTourHeader />);
    const nav = screen.getByRole("navigation", { name: /Tour sections/i });
    expect(nav.querySelector('a[href="#start"]')).toBeNull();
    expect(screen.queryByRole("link", { name: "tour.cta.signIn" })).toBeNull();
  });
});
