"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  getDashboardStats,
  adminGetPlatformEarningsSeries,
  type DailyCount,
  type DashboardScope,
  type DashboardStats,
  type PlatformEarningsSeriesPoint,
  type PlatformEarningsSeriesRangeParam,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ThemedECharts } from "@/components/charts/ThemedECharts";
import { useChartTheme } from "@/hooks/use-chart-theme";
import {
  aggregateCouriersForPie,
  buildPeriodBarOption,
  buildSankeyOption,
  buildDailyVolumeLineOption,
  buildDeliveryBoxplotOption,
  buildDayHourHeatmapOption,
  buildLast7DaysGroupedBarOption,
  buildCourierPieOption,
  buildMonthlyEarningsLineOption,
} from "@/lib/dashboard-echarts";
import { buildWordCloudOption } from "@/lib/dashboard-wordcloud";
import { cn } from "@/lib/utils";
import { isRiderOnlyPortal } from "@/lib/rider-role";
import { BookingCalendarHeatmapGrid } from "@/components/dashboard/BookingCalendarHeatmap";
import { ChevronLeft, ChevronRight } from "lucide-react";

function ChartPanel({
  title,
  children,
  className,
  headerRight,
}: {
  title: string;
  children: React.ReactNode;
  className?: string;
  headerRight?: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "rounded-2xl border border-border/70 bg-card/95 p-4 shadow-sm",
        "dark:border-blue-500/20 dark:bg-gradient-to-br dark:from-card/95 dark:to-blue-950/20 dark:shadow-[0_0_36px_-14px_rgba(59,130,246,0.45)]",
        className,
      )}
    >
      <div className="mb-3 flex flex-wrap items-start justify-between gap-2 gap-y-2">
        <h3 className="min-w-0 flex-1 text-sm font-semibold tracking-tight text-foreground">{title}</h3>
        {headerRight}
      </div>
      {children}
    </div>
  );
}

export default function DashboardPage() {
  const { user, token, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const locale = useLocale();
  const t = useTranslations("dashboard");
  const tStats = useTranslations("dashboard.stats");
  const tCards = useTranslations("dashboard.cards");
  const tEarnings = useTranslations("dashboard.earnings");
  const name = user?.displayName || user?.email || "";
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [statsError, setStatsError] = useState<string | null>(null);
  const [scope, setScope] = useState<DashboardScope>("all");
  const [heatmapUtc, setHeatmapUtc] = useState(() => {
    const d = new Date();
    return { year: d.getUTCFullYear(), month: d.getUTCMonth() + 1 };
  });
  const [avgMode, setAvgMode] = useState<"roll30" | "month" | "year">("roll30");
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");
  const chartTheme = useChartTheme();

  useEffect(() => {
    const roles = user?.roles;
    if (!isLoading && isAuthenticated && roles && isRiderOnlyPortal(roles)) {
      router.replace("/rider/deliveries");
    }
  }, [isLoading, isAuthenticated, user?.roles, router]);

  const [earningsRange, setEarningsRange] = useState<PlatformEarningsSeriesRangeParam>("lastMonth");
  const [earningsSeries, setEarningsSeries] = useState<PlatformEarningsSeriesPoint[]>([]);
  const [earningsSeriesError, setEarningsSeriesError] = useState<string | null>(null);
  const [earningsSeriesLoading, setEarningsSeriesLoading] = useState(false);

  const formatEur = useCallback(
    (n: number) =>
      new Intl.NumberFormat(locale, { style: "currency", currency: "EUR", maximumFractionDigits: 2 }).format(n),
    [locale],
  );

  const formatEarningsPeriodLabel = useCallback(
    (period: string) => {
      const dayMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(period);
      if (dayMatch) {
        const d = new Date(
          Date.UTC(Number(dayMatch[1]), Number(dayMatch[2]) - 1, Number(dayMatch[3])),
        );
        return new Intl.DateTimeFormat(locale, {
          month: "short",
          day: "numeric",
          timeZone: "UTC",
        }).format(d);
      }
      const monthMatch = /^(\d{4})-(\d{2})$/.exec(period);
      if (monthMatch) {
        const d = new Date(Date.UTC(Number(monthMatch[1]), Number(monthMatch[2]) - 1, 1));
        return new Intl.DateTimeFormat(locale, {
          month: "short",
          year: "numeric",
          timeZone: "UTC",
        }).format(d);
      }
      return period;
    },
    [locale],
  );

  const dayLabels = useMemo(
    () => [
      tStats("dowSun"),
      tStats("dowMon"),
      tStats("dowTue"),
      tStats("dowWed"),
      tStats("dowThu"),
      tStats("dowFri"),
      tStats("dowSat"),
    ],
    [tStats],
  );

  const mondayFirstDowLabels = useMemo(
    () => [
      tStats("dowMon"),
      tStats("dowTue"),
      tStats("dowWed"),
      tStats("dowThu"),
      tStats("dowFri"),
      tStats("dowSat"),
      tStats("dowSun"),
    ],
    [tStats],
  );

  const themeSlice = useMemo(
    () => ({
      border: chartTheme.border,
      foreground: chartTheme.foreground,
      mutedForeground: chartTheme.mutedForeground,
      card: chartTheme.card,
    }),
    [chartTheme],
  );

  const periodChartOption = useMemo(() => {
    if (!stats) return null;
    const values = [stats.countToday, stats.countMonth, stats.countYear];
    if (!values.some((v) => v > 0)) return null;
    const categories = [tStats("today"), tStats("thisMonth"), tStats("thisYear")];
    const barColors = [chartTheme.chart[0], chartTheme.chart[1], chartTheme.chart[2]];
    return buildPeriodBarOption(categories, values, barColors, themeSlice, tStats("bookings"));
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const courierWordCloudOption = useMemo(() => {
    if (!stats?.byCourier.length) return null;
    return buildWordCloudOption(
      stats.byCourier,
      [...chartTheme.chart],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const fromCitiesWordCloudOption = useMemo(() => {
    if (!stats?.fromCities.length) return null;
    return buildWordCloudOption(
      stats.fromCities,
      [...chartTheme.chart],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const toCitiesWordCloudOption = useMemo(() => {
    if (!stats?.toCities.length) return null;
    return buildWordCloudOption(
      stats.toCities,
      [...chartTheme.chart],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const sankeyOption = useMemo(() => {
    if (!stats?.laneSankey?.links?.length) return null;
    return buildSankeyOption(stats.laneSankey, themeSlice, tStats("noData"));
  }, [stats, themeSlice, tStats]);

  const volumeLineOption = useMemo(() => {
    if (!stats?.bookingsPerDayLast30?.length) return null;
    return buildDailyVolumeLineOption(
      stats.bookingsPerDayLast30,
      chartTheme.chart[2],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const deliveryBoxOption = useMemo(() => {
    if (!stats?.deliveryTime) return null;
    return buildDeliveryBoxplotOption(
      {
        sampleSize: stats.deliveryTime.sampleSize,
        minHours: stats.deliveryTime.minHours,
        q1Hours: stats.deliveryTime.q1Hours,
        medianHours: stats.deliveryTime.medianHours,
        q3Hours: stats.deliveryTime.q3Hours,
        maxHours: stats.deliveryTime.maxHours,
        outlierCount: stats.deliveryTime.outlierCount,
        sampleHours: stats.deliveryTime.sampleHours,
      },
      themeSlice,
      tStats("noData"),
    );
  }, [stats, themeSlice, tStats]);

  const heatmapMonthLabel = useMemo(() => {
    const fmt = new Intl.DateTimeFormat(locale, {
      month: "long",
      year: "numeric",
      timeZone: "UTC",
    });
    return fmt.format(new Date(Date.UTC(heatmapUtc.year, heatmapUtc.month - 1, 1)));
  }, [locale, heatmapUtc.year, heatmapUtc.month]);

  const heatmapNavNow = (() => {
    const d = new Date();
    return { year: d.getUTCFullYear(), month: d.getUTCMonth() + 1 };
  })();

  const canHeatmapGoNext =
    heatmapUtc.year < heatmapNavNow.year ||
    (heatmapUtc.year === heatmapNavNow.year && heatmapUtc.month < heatmapNavNow.month);

  const goHeatmapPrevMonth = useCallback(() => {
    setHeatmapUtc(({ year, month }) => {
      let m = month - 1;
      let y = year;
      if (m < 1) {
        m = 12;
        y -= 1;
      }
      return { year: y, month: m };
    });
  }, []);

  const goHeatmapNextMonth = useCallback(() => {
    setHeatmapUtc(({ year, month }) => {
      let m = month + 1;
      let y = year;
      if (m > 12) {
        m = 1;
        y += 1;
      }
      const d = new Date();
      const ny = d.getUTCFullYear();
      const nm = d.getUTCMonth() + 1;
      if (y > ny || (y === ny && m > nm)) return { year: ny, month: nm };
      return { year: y, month: m };
    });
  }, []);

  const setScopeAndResetHeatmap = useCallback((next: DashboardScope) => {
    setScope(next);
    const d = new Date();
    setHeatmapUtc({ year: d.getUTCFullYear(), month: d.getUTCMonth() + 1 });
  }, []);

  const exceptionHeatmapOption = useMemo(() => {
    if (!stats?.exceptionSignalsHeatmap?.cells?.length) return null;
    return buildDayHourHeatmapOption(
      stats.exceptionSignalsHeatmap.cells,
      stats.exceptionSignalsHeatmap.maxCount,
      themeSlice,
      dayLabels,
    );
  }, [stats, themeSlice, dayLabels]);

  const last7GroupedOption = useMemo(() => {
    if (!stats?.bookingsPerDayLast30?.length) return null;
    const benchColor = chartTheme.chart[2] ?? chartTheme.chart[1];
    return buildLast7DaysGroupedBarOption(
      stats.bookingsPerDayLast30,
      themeSlice,
      tStats("bookings"),
      tStats("sameWeekdayAvg30"),
      chartTheme.chart[0],
      benchColor,
      dayLabels,
    );
  }, [stats, themeSlice, chartTheme.chart, tStats, dayLabels]);

  const heatmapTooltipSeries = useMemo(() => {
    if (!stats) return { completed: [] as DailyCount[], drafts: [] as DailyCount[] };
    const completed =
      stats.completedBookingsPerDayCurrentMonth ??
      (scope === "all" ? stats.bookingsPerDayCurrentMonth : []);
    const drafts =
      stats.draftsPerDayCurrentMonth ?? (scope === "drafts" ? stats.bookingsPerDayCurrentMonth : []);
    return { completed, drafts };
  }, [stats, scope]);

  const formatHeatmapCellTooltip = useCallback(
    (p: { date: string; bookingCount: number; draftCount: number }) => tStats("heatmapCellTooltip", p),
    [tStats],
  );

  const courierPieOption = useMemo(() => {
    if (!stats?.byCourier.length) return null;
    const rows = aggregateCouriersForPie(stats.byCourier, 5, tStats("otherCouriers"));
    if (!rows.length) return null;
    return buildCourierPieOption(rows, [...chartTheme.chart], themeSlice, tStats("bookings"));
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const stuckAlert =
    stats &&
    scope === "all" &&
    stats.kpi.possiblyStuckCount > 0 &&
    stats.countMonth > 0 &&
    stats.kpi.possiblyStuckCount / stats.countMonth >= 0.1;

  /** Monotonic id so an older /dashboard/stats response cannot overwrite a newer one (e.g. April finishing after March). */
  const statsRequestSeqRef = useRef(0);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token) return;

    const id = ++statsRequestSeqRef.current;
    const ac = new AbortController();
    setStatsError(null);

    getDashboardStats(token, scope === "all" ? null : scope, heatmapUtc, ac.signal)
      .then((data) => {
        if (id !== statsRequestSeqRef.current) return;
        setStatsError(null);
        setStats(data);
      })
      .catch((e) => {
        if (e instanceof DOMException && e.name === "AbortError") return;
        if (id !== statsRequestSeqRef.current) return;
        setStatsError(e instanceof Error ? e.message : "Failed to load stats");
      });

    return () => ac.abort();
  }, [token, isAuthenticated, isLoading, router, scope, heatmapUtc]);

  const earningsLineOption = useMemo(() => {
    if (!earningsSeries.length) return null;
    const points = earningsSeries.map((r) => ({
      label: formatEarningsPeriodLabel(r.period),
      totalEur: r.totalEur,
    }));
    return buildMonthlyEarningsLineOption(points, chartTheme.chart[0], themeSlice, formatEur);
  }, [earningsSeries, chartTheme.chart, themeSlice, formatEur, formatEarningsPeriodLabel]);

  useEffect(() => {
    if (!isSuperAdmin || !token) return;
    const ac = new AbortController();
    setEarningsSeriesError(null);
    setEarningsSeriesLoading(true);
    adminGetPlatformEarningsSeries(token, earningsRange, ac.signal)
      .then((rows) => {
        if (ac.signal.aborted) return;
        setEarningsSeries(rows);
      })
      .catch((e) => {
        if (e instanceof DOMException && e.name === "AbortError") return;
        if (!ac.signal.aborted) {
          setEarningsSeriesError(e instanceof Error ? e.message : "Failed to load earnings");
        }
      })
      .finally(() => {
        if (!ac.signal.aborted) setEarningsSeriesLoading(false);
      });
    return () => ac.abort();
  }, [isSuperAdmin, token, earningsRange]);

  if (!isAuthenticated || isLoading) return null;
  if (user?.roles && isRiderOnlyPortal(user.roles)) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-muted-foreground">Redirecting…</p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("greeting", { name })}</h1>
        <p className="text-muted-foreground mt-1">{t("signedIn")}</p>
      </div>

      {isSuperAdmin && token ? (
        <Card className="overflow-hidden rounded-3xl border-border/70 shadow-md dark:border-amber-500/15">
          <CardHeader>
            <CardTitle>{tEarnings("title")}</CardTitle>
            <CardDescription>{tEarnings("description")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {earningsSeriesError ? (
              <p className="text-sm text-destructive" role="alert">
                {earningsSeriesError}
              </p>
            ) : null}
            <div className="flex flex-wrap items-center gap-3">
              <label htmlFor="earnings-range" className="text-sm font-medium text-muted-foreground">
                {tEarnings("rangeLabel")}
              </label>
              <select
                id="earnings-range"
                className="h-9 rounded-md border border-input bg-background px-3 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={earningsRange}
                onChange={(e) => setEarningsRange(e.target.value as PlatformEarningsSeriesRangeParam)}
              >
                <option value="yesterday">{tEarnings("rangeYesterday")}</option>
                <option value="last7days">{tEarnings("rangeLast7Days")}</option>
                <option value="lastMonth">{tEarnings("rangeLastMonth")}</option>
                <option value="last6months">{tEarnings("rangeLast6Months")}</option>
                <option value="lastYear">{tEarnings("rangeLastYear")}</option>
              </select>
              {earningsSeriesLoading ? (
                <span className="text-sm text-muted-foreground">{tEarnings("loading")}</span>
              ) : null}
            </div>

            <ChartPanel title={tEarnings("trendTitle")}>
              {earningsLineOption ? (
                <div className="h-64 w-full min-h-[256px]">
                  <ThemedECharts option={earningsLineOption} height={256} />
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">{tEarnings("noData")}</p>
              )}
            </ChartPanel>
          </CardContent>
        </Card>
      ) : null}

      {/* Company booking stats */}
      <Card className="overflow-hidden rounded-3xl border-border/70 shadow-md dark:border-blue-500/20">
        <CardHeader>
          <CardTitle>{tStats("title")}</CardTitle>
          <CardDescription>
            {isSuperAdmin ? tStats("descriptionAll") : tStats("description")}
            {typeof process.env.NEXT_PUBLIC_BUILD_REF === "string" &&
              process.env.NEXT_PUBLIC_BUILD_REF.length > 0 &&
              process.env.NEXT_PUBLIC_BUILD_REF !== "local" && (
                <span className="mt-1 block font-mono text-xs text-muted-foreground">
                  {tStats("buildRef", {
                    ref:
                      process.env.NEXT_PUBLIC_BUILD_REF.length > 12
                        ? `${process.env.NEXT_PUBLIC_BUILD_REF.slice(0, 7)}…`
                        : process.env.NEXT_PUBLIC_BUILD_REF,
                  })}
                </span>
              )}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {statsError && (
            <p className="text-sm text-destructive" role="alert">
              {statsError}
            </p>
          )}
          {stats && (
            <>
              <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
                <p className="text-sm text-muted-foreground">{tStats("scopeHint")}</p>
                <div className="flex flex-wrap gap-2">
                  <Button
                    type="button"
                    size="sm"
                    variant={scope === "all" ? "default" : "outline"}
                    onClick={() => setScopeAndResetHeatmap("all")}
                  >
                    {tStats("scopeAll")}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant={scope === "drafts" ? "default" : "outline"}
                    onClick={() => setScopeAndResetHeatmap("drafts")}
                  >
                    {tStats("scopeDrafts")}
                  </Button>
                </div>
              </div>

              {/* KPI row — card tiles with stronger radius / depth (admin-style dashboard) */}
              <div
                className={`grid gap-4 sm:grid-cols-2 ${scope === "all" ? "lg:grid-cols-5" : "lg:grid-cols-4"}`}
              >
                <div className="rounded-2xl border border-border/80 bg-gradient-to-br from-muted/50 to-muted/15 p-5 text-center shadow-sm dark:border-blue-500/25 dark:from-blue-950/40 dark:to-card/60">
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{tStats("today")}</p>
                  <p className="mt-2 text-3xl font-bold tabular-nums tracking-tight text-foreground">{stats.countToday}</p>
                </div>
                <div className="rounded-2xl border border-border/80 bg-gradient-to-br from-muted/50 to-muted/15 p-5 text-center shadow-sm dark:border-blue-500/25 dark:from-blue-950/40 dark:to-card/60">
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{tStats("thisMonth")}</p>
                  <p className="mt-2 text-3xl font-bold tabular-nums tracking-tight text-foreground">{stats.countMonth}</p>
                </div>
                <div className="rounded-2xl border border-border/80 bg-gradient-to-br from-muted/50 to-muted/15 p-5 text-center shadow-sm dark:border-blue-500/25 dark:from-blue-950/40 dark:to-card/60">
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{tStats("thisYear")}</p>
                  <p className="mt-2 text-3xl font-bold tabular-nums tracking-tight text-foreground">{stats.countYear}</p>
                </div>
                <div className="rounded-2xl border border-border/80 bg-gradient-to-br from-muted/50 to-muted/15 p-5 shadow-sm dark:border-blue-500/25 dark:from-blue-950/40 dark:to-card/60">
                  <p className="mb-2 text-center text-xs font-medium uppercase tracking-wide text-muted-foreground">{tStats("avgTitle")}</p>
                  <div className="mb-2 flex flex-wrap justify-center gap-1">
                    <Button
                      type="button"
                      size="sm"
                      variant={avgMode === "roll30" ? "default" : "outline"}
                      className="h-7 px-2 text-xs"
                      onClick={() => setAvgMode("roll30")}
                    >
                      {tStats("avgModeRoll30")}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant={avgMode === "month" ? "default" : "outline"}
                      className="h-7 px-2 text-xs"
                      onClick={() => setAvgMode("month")}
                    >
                      {tStats("avgModeMonth")}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant={avgMode === "year" ? "default" : "outline"}
                      className="h-7 px-2 text-xs"
                      onClick={() => setAvgMode("year")}
                    >
                      {tStats("avgModeYear")}
                    </Button>
                  </div>
                  <p className="text-center text-3xl font-bold tabular-nums tracking-tight">
                    {avgMode === "roll30" && stats.kpi.avgPerDayLast30}
                    {avgMode === "month" && stats.kpi.avgPerDayThisMonth}
                    {avgMode === "year" && stats.kpi.avgPerDayThisYear}
                  </p>
                  <p className="text-center text-xs text-muted-foreground">{tStats("avgPerDaySuffix")}</p>
                </div>
                {scope === "all" && (
                  <div
                    className={`rounded-2xl border p-5 text-center shadow-sm ${
                      stuckAlert ? "border-destructive/80 bg-destructive/10 animate-pulse" : "border-border/80 bg-gradient-to-br from-muted/50 to-muted/15 dark:border-blue-500/25 dark:from-blue-950/40 dark:to-card/60"
                    }`}
                  >
                    <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{tStats("kpiStuck")}</p>
                    <p className={`mt-2 text-3xl font-bold tabular-nums tracking-tight ${stuckAlert ? "text-destructive" : ""}`}>
                      {stats.kpi.possiblyStuckCount}
                    </p>
                  </div>
                )}
              </div>

              {/* Recent activity (grouped bars) + carrier share (donut) */}
              <div className="grid gap-6 lg:grid-cols-2">
                <ChartPanel title={tStats("last7DaysTitle")}>
                  {last7GroupedOption ? (
                    <div className="h-60 w-full min-h-[240px]">
                      <ThemedECharts option={last7GroupedOption} height={240} />
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  )}
                </ChartPanel>
                <ChartPanel title={tStats("courierShareTitle")}>
                  {courierPieOption ? (
                    <div className="h-56 w-full min-h-[224px]">
                      <ThemedECharts option={courierPieOption} height={224} />
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  )}
                </ChartPanel>
              </div>

              {periodChartOption && (
                <ChartPanel title={tStats("byPeriod")}>
                  <div className="h-48 w-full min-h-[192px]">
                    <ThemedECharts option={periodChartOption} height={192} />
                  </div>
                </ChartPanel>
              )}

              {volumeLineOption && (
                <ChartPanel title={tStats("volumeLineTitle")}>
                  <div className="h-64 w-full min-h-[256px]">
                    <ThemedECharts option={volumeLineOption} height={256} />
                  </div>
                </ChartPanel>
              )}

              {sankeyOption && (
                <ChartPanel title={tStats("sankeyTitle")}>
                  <div className="h-80 w-full min-h-[320px]">
                    <ThemedECharts option={sankeyOption} height={320} />
                  </div>
                </ChartPanel>
              )}

              <div className="grid gap-6 lg:grid-cols-3">
                <ChartPanel title={tStats("wordCloudCarriers")}>
                  {stats.byCourier.length === 0 || !courierWordCloudOption ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    <div className="h-56 w-full min-h-[224px]">
                      <ThemedECharts option={courierWordCloudOption} height={224} />
                    </div>
                  )}
                </ChartPanel>
                <ChartPanel title={tStats("wordCloudFrom")}>
                  {stats.fromCities.length === 0 || !fromCitiesWordCloudOption ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    <div className="h-56 w-full min-h-[224px]">
                      <ThemedECharts option={fromCitiesWordCloudOption} height={224} />
                    </div>
                  )}
                </ChartPanel>
                <ChartPanel title={tStats("wordCloudTo")}>
                  {stats.toCities.length === 0 || !toCitiesWordCloudOption ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    <div className="h-56 w-full min-h-[224px]">
                      <ThemedECharts option={toCitiesWordCloudOption} height={224} />
                    </div>
                  )}
                </ChartPanel>
              </div>

              <div className="grid gap-6 lg:grid-cols-2">
                {deliveryBoxOption && (
                  <ChartPanel title={tStats("deliveryBoxTitle")}>
                    <div className="h-56 w-full min-h-[224px]">
                      <ThemedECharts option={deliveryBoxOption} height={224} />
                    </div>
                  </ChartPanel>
                )}
                {stats.bookingsPerDayCurrentMonth.length > 0 && (
                  <ChartPanel
                    title={tStats("calendarHeatmapTitle")}
                    headerRight={
                      <div className="flex shrink-0 items-center gap-0.5">
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-muted-foreground hover:text-foreground"
                          onClick={goHeatmapPrevMonth}
                          aria-label={tStats("heatmapPrevMonth")}
                        >
                          <ChevronLeft className="h-4 w-4" />
                        </Button>
                        <span className="min-w-[9rem] text-center text-sm font-medium tabular-nums text-foreground sm:min-w-[11rem]">
                          {heatmapMonthLabel}
                        </span>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-muted-foreground hover:text-foreground"
                          disabled={!canHeatmapGoNext}
                          onClick={goHeatmapNextMonth}
                          aria-label={tStats("heatmapNextMonth")}
                        >
                          <ChevronRight className="h-4 w-4" />
                        </Button>
                      </div>
                    }
                  >
                    <div className="w-full pt-1">
                      <BookingCalendarHeatmapGrid
                        daily={stats.bookingsPerDayCurrentMonth}
                        completedDaily={heatmapTooltipSeries.completed}
                        draftsDaily={heatmapTooltipSeries.drafts}
                        targetYear={heatmapUtc.year}
                        targetMonth={heatmapUtc.month}
                        dowLabels={mondayFirstDowLabels}
                        weekLabel={(week, isoYear) =>
                          tStats("heatmapIsoWeekWithYear", { week, year: isoYear })
                        }
                        formatCellTooltip={formatHeatmapCellTooltip}
                      />
                    </div>
                  </ChartPanel>
                )}
              </div>

              {exceptionHeatmapOption && (
                <ChartPanel title={tStats("heatmapExceptionTitle")}>
                  <div className="h-72 w-full min-h-[288px]">
                    <ThemedECharts option={exceptionHeatmapOption} height={288} />
                  </div>
                </ChartPanel>
              )}
            </>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>{t("bookings")}</CardTitle>
            <CardDescription>{t("bookingsList")}</CardDescription>
          </CardHeader>
          <CardContent>
            <Link href="/bookings">
              <Button className="w-full">{t("bookingsList")}</Button>
            </Link>
          </CardContent>
        </Card>
        {!isSuperAdmin && (
        <Card>
          <CardHeader>
            <CardTitle>{t("createBooking")}</CardTitle>
            <CardDescription>{tCards("createDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <Link href="/bookings/create">
              <Button variant="outline" className="w-full">
                {t("createBooking")}
              </Button>
            </Link>
          </CardContent>
        </Card>
        )}
        {!isSuperAdmin && (
          <Card>
            <CardHeader>
              <CardTitle>{t("courierContracts")}</CardTitle>
              <CardDescription>{tCards("courierContractsDescription")}</CardDescription>
            </CardHeader>
            <CardContent>
              <Link href="/company/courier-contracts">
                <Button variant="outline" className="w-full">
                  {t("courierContracts")}
                </Button>
              </Link>
            </CardContent>
          </Card>
        )}
        <Card>
          <CardHeader>
            <CardTitle>{t("actions")}</CardTitle>
            <CardDescription>{tCards("actionsDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <Link href="/actions">
              <Button variant="outline" className="w-full">
                {t("actions")}
              </Button>
            </Link>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>{t("plugin")}</CardTitle>
            <CardDescription>{tCards("pluginDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <Link href="/plugin">
              <Button variant="outline" className="w-full">
                {t("plugin")}
              </Button>
            </Link>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>{t("more")}</CardTitle>
            <CardDescription>{tCards("moreDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <Link href="/more">
              <Button variant="outline" className="w-full">
                {t("more")}
              </Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
