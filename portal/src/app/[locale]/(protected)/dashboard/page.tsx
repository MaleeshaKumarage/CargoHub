"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getDashboardStats,
  type DashboardScope,
  type DashboardStats,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ThemedECharts } from "@/components/charts/ThemedECharts";
import { useChartTheme } from "@/hooks/use-chart-theme";
import {
  buildPeriodBarOption,
  buildCourierPieOption,
  buildCityBarOption,
  buildSunburstOption,
  buildSankeyOption,
  buildDailyVolumeLineOption,
  buildDeliveryBoxplotOption,
  buildDayHourHeatmapOption,
  buildTodayVsAverageBulletOption,
} from "@/lib/dashboard-echarts";

export default function DashboardPage() {
  const { user, token, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("dashboard");
  const tStats = useTranslations("dashboard.stats");
  const tCards = useTranslations("dashboard.cards");
  const name = user?.displayName || user?.email || "";
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [statsError, setStatsError] = useState<string | null>(null);
  const [scope, setScope] = useState<DashboardScope>("all");
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");
  const chartTheme = useChartTheme();

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

  const courierSunburstOption = useMemo(() => {
    if (!stats?.carrierServiceSunburst?.children?.length) return null;
    return buildSunburstOption(
      stats.carrierServiceSunburst,
      [...chartTheme.chart],
      themeSlice,
      tStats("noData"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const courierPieFallbackOption = useMemo(() => {
    if (!stats?.byCourier.length) return null;
    return buildCourierPieOption(
      stats.byCourier,
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

  const volumeHeatmapOption = useMemo(() => {
    if (!stats?.bookingVolumeHeatmap?.cells?.length) return null;
    return buildDayHourHeatmapOption(
      stats.bookingVolumeHeatmap.cells,
      stats.bookingVolumeHeatmap.maxCount,
      themeSlice,
      dayLabels,
    );
  }, [stats, themeSlice, dayLabels]);

  const exceptionHeatmapOption = useMemo(() => {
    if (!stats?.exceptionSignalsHeatmap?.cells?.length) return null;
    return buildDayHourHeatmapOption(
      stats.exceptionSignalsHeatmap.cells,
      stats.exceptionSignalsHeatmap.maxCount,
      themeSlice,
      dayLabels,
    );
  }, [stats, themeSlice, dayLabels]);

  const bulletOption = useMemo(() => {
    if (!stats) return null;
    return buildTodayVsAverageBulletOption(
      stats.countToday,
      stats.kpi.avgPerDayLast30,
      themeSlice,
      tStats("today"),
      tStats("volumeLineTitle"),
    );
  }, [stats, themeSlice, tStats]);

  const stuckAlert =
    stats &&
    stats.kpi.possiblyStuckCount > 0 &&
    stats.countMonth > 0 &&
    stats.kpi.possiblyStuckCount / stats.countMonth >= 0.1;

  const fromCitiesOption = useMemo(() => {
    if (!stats?.fromCities.length) return null;
    return buildCityBarOption(
      stats.fromCities.slice(0, 8),
      chartTheme.chart[0],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const toCitiesOption = useMemo(() => {
    if (!stats?.toCities.length) return null;
    return buildCityBarOption(
      stats.toCities.slice(0, 8),
      chartTheme.chart[1],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

  const loadStats = useCallback(() => {
    if (!token) return;
    setStatsError(null);
    getDashboardStats(token, scope === "all" ? null : scope)
      .then(setStats)
      .catch((e) => setStatsError(e instanceof Error ? e.message : "Failed to load stats"));
  }, [token, scope]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token) return;
    loadStats();
  }, [token, isAuthenticated, isLoading, router, loadStats]);

  if (!isAuthenticated || isLoading) return null;

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("greeting", { name })}</h1>
        <p className="text-muted-foreground mt-1">{t("signedIn")}</p>
      </div>

      {/* Company booking stats */}
      <Card>
        <CardHeader>
          <CardTitle>{tStats("title")}</CardTitle>
          <CardDescription>
            {isSuperAdmin ? tStats("descriptionAll") : tStats("description")}
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
                    onClick={() => setScope("all")}
                  >
                    {tStats("scopeAll")}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant={scope === "drafts" ? "default" : "outline"}
                    onClick={() => setScope("drafts")}
                  >
                    {tStats("scopeDrafts")}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant={scope === "tests" ? "default" : "outline"}
                    onClick={() => setScope("tests")}
                  >
                    {tStats("scopeTests")}
                  </Button>
                </div>
              </div>

              {/* KPI cards — click scope above to filter charts */}
              <div className="grid gap-4 sm:grid-cols-3 lg:grid-cols-6">
                <button
                  type="button"
                  className="rounded-lg border bg-muted/30 p-4 text-center transition hover:bg-muted/50"
                  onClick={() => setScope("all")}
                >
                  <p className="text-2xl font-bold">{stats.countToday}</p>
                  <p className="text-sm text-muted-foreground">{tStats("today")}</p>
                </button>
                <button
                  type="button"
                  className="rounded-lg border bg-muted/30 p-4 text-center transition hover:bg-muted/50"
                  onClick={() => setScope("all")}
                >
                  <p className="text-2xl font-bold">{stats.countMonth}</p>
                  <p className="text-sm text-muted-foreground">{tStats("thisMonth")}</p>
                </button>
                <button
                  type="button"
                  className="rounded-lg border bg-muted/30 p-4 text-center transition hover:bg-muted/50"
                  onClick={() => setScope("all")}
                >
                  <p className="text-2xl font-bold">{stats.countYear}</p>
                  <p className="text-sm text-muted-foreground">{tStats("thisYear")}</p>
                </button>
                <button
                  type="button"
                  className="rounded-lg border bg-muted/30 p-4 text-center transition hover:bg-muted/50"
                  onClick={() => setScope("drafts")}
                >
                  <p className="text-2xl font-bold">{stats.kpi.draftCount}</p>
                  <p className="text-sm text-muted-foreground">{tStats("kpiDrafts")}</p>
                </button>
                <button
                  type="button"
                  className="rounded-lg border bg-muted/30 p-4 text-center transition hover:bg-muted/50"
                  onClick={() => setScope("tests")}
                >
                  <p className="text-2xl font-bold">{stats.kpi.testBookingCount}</p>
                  <p className="text-sm text-muted-foreground">{tStats("kpiTests")}</p>
                </button>
                <button
                  type="button"
                  className={`rounded-lg border p-4 text-center transition hover:bg-muted/50 ${
                    stuckAlert ? "border-destructive/80 bg-destructive/10 animate-pulse" : "bg-muted/30"
                  }`}
                  onClick={() => setScope("all")}
                >
                  <p className={`text-2xl font-bold ${stuckAlert ? "text-destructive" : ""}`}>
                    {stats.kpi.possiblyStuckCount}
                  </p>
                  <p className="text-sm text-muted-foreground">{tStats("kpiStuck")}</p>
                </button>
              </div>

              {/* Period comparison bar chart (Apache ECharts) */}
              {periodChartOption && (
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("byPeriod")}
                  </h3>
                  <div className="h-48 w-full min-h-[192px]">
                    <ThemedECharts option={periodChartOption} height={192} />
                  </div>
                </div>
              )}

              {/* Today vs 30d avg (bullet) */}
              {bulletOption && (
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("bulletTitle")}
                  </h3>
                  <div className="h-40 w-full min-h-[160px] max-w-md">
                    <ThemedECharts option={bulletOption} height={160} />
                  </div>
                </div>
              )}

              {/* Volume line (last 30 days, slider) */}
              {volumeLineOption && (
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("volumeLineTitle")}
                  </h3>
                  <div className="h-64 w-full min-h-[256px]">
                    <ThemedECharts option={volumeLineOption} height={256} />
                  </div>
                </div>
              )}

              {/* Sankey: lanes */}
              {sankeyOption && (
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("sankeyTitle")}
                  </h3>
                  <div className="h-80 w-full min-h-[320px]">
                    <ThemedECharts option={sankeyOption} height={320} />
                  </div>
                </div>
              )}

              {/* Charts row: Sunburst (or pie) | From cities | To cities */}
              <div className="grid gap-6 lg:grid-cols-3">
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {courierSunburstOption ? tStats("sunburstTitle") : tStats("byCourier")}
                  </h3>
                  {stats.byCourier.length === 0 && !courierSunburstOption ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    (courierSunburstOption || courierPieFallbackOption) && (
                      <div className="h-48 w-full min-h-[192px]">
                        <ThemedECharts
                          option={(courierSunburstOption ?? courierPieFallbackOption)!}
                          height={192}
                        />
                      </div>
                    )
                  )}
                </div>
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("fromCities")}
                  </h3>
                  {stats.fromCities.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    fromCitiesOption && (
                      <div className="h-48 w-full min-h-[192px]">
                        <ThemedECharts option={fromCitiesOption} height={192} />
                      </div>
                    )
                  )}
                </div>
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("toCities")}
                  </h3>
                  {stats.toCities.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    toCitiesOption && (
                      <div className="h-48 w-full min-h-[192px]">
                        <ThemedECharts option={toCitiesOption} height={192} />
                      </div>
                    )
                  )}
                </div>
              </div>

              <div className="grid gap-6 lg:grid-cols-2">
                {deliveryBoxOption && (
                  <div>
                    <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                      {tStats("deliveryBoxTitle")}
                    </h3>
                    <div className="h-56 w-full min-h-[224px]">
                      <ThemedECharts option={deliveryBoxOption} height={224} />
                    </div>
                  </div>
                )}
                {volumeHeatmapOption && (
                  <div>
                    <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                      {tStats("heatmapVolumeTitle")}
                    </h3>
                    <div className="h-72 w-full min-h-[288px]">
                      <ThemedECharts option={volumeHeatmapOption} height={288} />
                    </div>
                  </div>
                )}
              </div>

              {exceptionHeatmapOption && (
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("heatmapExceptionTitle")}
                  </h3>
                  <div className="h-72 w-full min-h-[288px]">
                    <ThemedECharts option={exceptionHeatmapOption} height={288} />
                  </div>
                </div>
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
