"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { getDashboardStats, type DashboardStats } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from "recharts";

const PERIOD_COLORS = ["var(--chart-1)", "var(--chart-2)", "var(--chart-3)"];
const PIE_COLORS = [
  "var(--chart-1)",
  "var(--chart-2)",
  "var(--chart-3)",
  "var(--chart-4)",
  "var(--chart-5)",
  "#94a3b8",
  "#64748b",
];

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

  const periodData = stats
    ? [
        { name: tStats("today"), count: stats.countToday, fill: PERIOD_COLORS[0] },
        { name: tStats("thisMonth"), count: stats.countMonth, fill: PERIOD_COLORS[1] },
        { name: tStats("thisYear"), count: stats.countYear, fill: PERIOD_COLORS[2] },
      ]
    : [];

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

              {/* Period comparison bar chart */}
              {periodData.some((d) => d.count > 0) && (
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("byPeriod")}
                  </h3>
                  <div className="h-48 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={periodData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                        <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                        <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
                        <Tooltip
                          contentStyle={{
                            backgroundColor: "var(--card)",
                            border: "1px solid var(--border)",
                            borderRadius: "var(--radius)",
                          }}
                        />
                        <Bar dataKey="count" name={tStats("bookings")} radius={[4, 4, 0, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
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
                    <div className="h-48 w-full">
                      <ResponsiveContainer width="100%" height="100%">
                        <PieChart>
                          <Pie
                            data={stats.byCourier.map((c, i) => ({
                              name: c.key,
                              value: c.count,
                            }))}
                            cx="50%"
                            cy="50%"
                            innerRadius={40}
                            outerRadius={64}
                            paddingAngle={2}
                            dataKey="value"
                            nameKey="name"
                            label={({ name, percent }) =>
                              `${name} ${((percent ?? 0) * 100).toFixed(0)}%`
                            }
                          >
                            {stats.byCourier.map((_, i) => (
                              <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                            ))}
                          </Pie>
                          <Tooltip
                            formatter={(value: number | undefined) => [value ?? 0, tStats("bookings")]}
                            contentStyle={{
                              backgroundColor: "var(--card)",
                              border: "1px solid var(--border)",
                              borderRadius: "var(--radius)",
                            }}
                          />
                        </PieChart>
                      </ResponsiveContainer>
                    </div>
                  )}
                </div>
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("fromCities")}
                  </h3>
                  {stats.fromCities.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    <div className="h-48 w-full">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart
                          data={stats.fromCities.slice(0, 8)}
                          layout="vertical"
                          margin={{ top: 4, right: 8, left: 0, bottom: 4 }}
                        >
                          <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                          <XAxis type="number" allowDecimals={false} tick={{ fontSize: 11 }} />
                          <YAxis
                            type="category"
                            dataKey="key"
                            width={72}
                            tick={{ fontSize: 11 }}
                          />
                          <Tooltip
                            contentStyle={{
                              backgroundColor: "var(--card)",
                              border: "1px solid var(--border)",
                              borderRadius: "var(--radius)",
                            }}
                          />
                          <Bar dataKey="count" name={tStats("bookings")} fill="var(--chart-1)" radius={[0, 4, 4, 0]} />
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  )}
                </div>
                <div>
                  <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                    {tStats("toCities")}
                  </h3>
                  {stats.toCities.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{tStats("noData")}</p>
                  ) : (
                    <div className="h-48 w-full">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart
                          data={stats.toCities.slice(0, 8)}
                          layout="vertical"
                          margin={{ top: 4, right: 8, left: 0, bottom: 4 }}
                        >
                          <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                          <XAxis type="number" allowDecimals={false} tick={{ fontSize: 11 }} />
                          <YAxis
                            type="category"
                            dataKey="key"
                            width={72}
                            tick={{ fontSize: 11 }}
                          />
                          <Tooltip
                            contentStyle={{
                              backgroundColor: "var(--card)",
                              border: "1px solid var(--border)",
                              borderRadius: "var(--radius)",
                            }}
                          />
                          <Bar dataKey="count" name={tStats("bookings")} fill="var(--chart-2)" radius={[0, 4, 4, 0]} />
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
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
