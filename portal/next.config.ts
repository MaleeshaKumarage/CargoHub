import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

/**
 * When the portal runs on :3000 with NEXT_PUBLIC_API_URL unset or __SAME_ORIGIN__, browser fetch uses
 * window.location.origin, so /api/* would hit Next.js and return 404. Proxy /api to the .NET API in dev.
 */
const publicApiUrl = process.env.NEXT_PUBLIC_API_URL?.trim() ?? "";
const useDevApiRewrite =
  process.env.NODE_ENV === "development" &&
  (publicApiUrl === "" || publicApiUrl === "__SAME_ORIGIN__");

const nextConfig: NextConfig = {
  async rewrites() {
    if (!useDevApiRewrite) return [];
    const target = (process.env.API_PROXY_TARGET ?? "http://localhost:5299").replace(/\/$/, "");
    return [{ source: "/api/:path*", destination: `${target}/api/:path*` }];
  },
};

export default withNextIntl(nextConfig);
