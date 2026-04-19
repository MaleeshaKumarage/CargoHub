import { defineConfig } from "vitest/config";
import path from "path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.test.{ts,tsx}"],
    globals: true,
    coverage: {
      provider: "v8",
      reporter: ["text", "json-summary"],
      reportsDirectory: "./coverage",
      // Avoid CI OOM: don't attempt to include every file in the project in the report.
      // We only gate coverage for code that is actually exercised by tests.
      all: false,
      // Match .github/workflows/pr-validation.yml (line & branch ≥75%; CI does not gate statements/functions)
      thresholds: {
        lines: 75,
        branches: 75,
      },
      // Keep coverage focused to avoid Node OOM on CI.
      // The Next.js app routes under src/app are large and are validated via tests, but excluded from coverage gating.
      include: [
        "src/lib/**/*.{ts,tsx}",
        "src/components/**/*.{ts,tsx}",
        "src/hooks/**/*.{ts,tsx}",
        "src/context/**/*.{ts,tsx}",
      ],
      exclude: [
        "src/**/*.test.{ts,tsx}",
        "src/test/**",
        // Large fetch wrapper (hundreds of branch points); behavior is covered via page/handler tests and narrower lib tests.
        "src/lib/api.ts",
        "src/proxy.ts",
        // Coverage scope does not include src/app/**, but keep excludes for safety if include patterns change.
        "src/app/**",
        "src/components/Navbar.tsx",
        "src/context/AuthContext.tsx",
        "src/i18n/**",
        "src/context/BrandingContext.tsx",
        "src/context/DesignThemeContext.tsx",
        "src/components/ui/dropdown-menu.tsx",
        "src/components/ui/table.tsx",
        "src/components/ui/tabs.tsx",
        "src/components/dashboard/BookingCalendarHeatmap.tsx",
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
