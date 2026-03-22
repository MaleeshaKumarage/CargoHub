"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useMemo, useState } from "react";
import { getDashboardStats, type DashboardStats } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ThemedECharts } from "@/components/charts/ThemedECharts";
import { useChartTheme } from "@/hooks/use-chart-theme";
import {
  buildPeriodBarOption,
  buildCourierPieOption,
  buildCityBarOption,
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
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");
  const chartTheme = useChartTheme();

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

  const courierChartOption = useMemo(() => {
    if (!stats?.byCourier.length) return null;
    return buildCourierPieOption(
      stats.byCourier,
      [...chartTheme.chart],
      themeSlice,
      tStats("bookings"),
    );
  }, [stats, chartTheme.chart, themeSlice, tStats]);

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

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token) return;
    getDashboardStats(token)
      .then(setStats)
      .catch((e) => setStatsError(e instanceof Error ? e.message : "Failed to load stats"));
  }, [token, isAuthenticated, isLoading, router]);

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
              {/* KPI cards */}
              <div className="grid gap-4 sm:grid-cols-3">
                <div className="rounded-lg border bg-muted/30 p-4 text-center">
                  <p className="text-2xl font-bold">{stats.countToday}</p>
                  <p className="text-sm text-muted-foreground">{tStats("today")}</p>
                </div>
                <div className="rounded-lg border bg-muted/30 p-4 text-center">
                  <p className="text-2xl font-bold">{stats.countMonth}</p>
                  <p className="text-sm text-muted-foreground">{tStats("thisMonth")}</p>
                </div>
                <div className="rounded-lg border bg-muted/30 p-4 text-center">
                  <p className="text-2xl font-bold">{stats.countYear}</p>
                  <p className="text-sm text-muted-foreground">{tStats("thisYear")}</p>
                </div>
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

              {/* Charts row: By courier (pie) | From cities (bar) | To cities (bar) */}
              <div className="grid gap-6 lg:grid-cols-3">
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("byCourier")}
                  </h3>
                  {stats.byCourier.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    courierChartOption && (
                      <div className="h-48 w-full min-h-[192px]">
                        <ThemedECharts option={courierChartOption} height={192} />
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
