import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.test.{ts,tsx}"],
    globals: true,
    coverage: {
      provider: "v8",
      reporter: ["text", "json-summary", "html"],
      reportsDirectory: "./coverage",
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
        "src/**/*.test.{ts,tsx}",
        "src/test/**",
        "src/proxy.ts",
        "src/app/layout.tsx",
        "src/app/providers.tsx",
        "src/app/**/layout.tsx",
        "src/app/**/forgot-password/**",
        "src/app/**/reset-password/**",
        "src/app/**/manage/users/**",
        "src/app/**/manage/companies/**",
        "src/app/**/manage/subscription-plans/**",
        "src/app/**/manage/layout.tsx",
        "src/app/**/manage/page.tsx",
        "src/app/**/manage-users/**",
        "src/app/**/draft/**",
        "src/app/**/bookings/create/**",
        "src/app/**/bookings/*/page.tsx",
        "src/app/**/actions/**",
        "src/app/**/dashboard/**",
        "src/app/**/login/**",
        "src/app/**/register/**",
        "src/components/Navbar.tsx",
        "src/context/AuthContext.tsx",
        "src/i18n/**",
        "src/context/BrandingContext.tsx",
        "src/context/DesignThemeContext.tsx",
        "src/components/ui/dropdown-menu.tsx",
        "src/components/ui/table.tsx",
        "**/*.d.ts",
        "**/node_modules/**",
      ],
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
