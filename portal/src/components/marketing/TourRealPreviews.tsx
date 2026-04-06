"use client";

import { BookingMilestoneBar } from "@/components/BookingMilestoneBar";
import { ThemedECharts } from "@/components/charts/ThemedECharts";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import {
  aggregateCouriersForPie,
  buildCourierPieOption,
  buildLast7DaysGroupedBarOption,
  buildPeriodBarOption,
} from "@/lib/dashboard-echarts";
import { useTranslations } from "next-intl";
import type { ComponentProps } from "react";
import {
  Building2,
  Download,
  LayoutDashboard,
  MoreHorizontal,
  Package,
  Puzzle,
  Settings2,
  Users,
  Zap,
} from "lucide-react";
import { TOUR_DEMO_CHART_COLORS, TOUR_DEMO_ECHARTS_THEME } from "./tour-demo-theme";

/** Renders real portal UI primitives with sample data; matches app dark + minimalism tokens. */
function TourUiSurface({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "dark max-h-[min(440px,70vh)] min-h-[220px] overflow-auto rounded-xl border border-border/80 bg-background text-foreground shadow-2xl",
        className,
      )}
      data-theme="minimalism"
      aria-label="Product preview (sample data)"
    >
      <div className="min-w-0 p-4">{children}</div>
    </div>
  );
}

function ChartPanel({
  title,
  children,
  className,
}: {
  title: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "rounded-2xl border border-border/70 bg-card/95 p-4 shadow-sm",
        "dark:border-blue-500/20 dark:bg-gradient-to-br dark:from-card/95 dark:to-blue-950/20 dark:shadow-[0_0_36px_-14px_rgba(59,130,246,0.45)]",
        className,
      )}
    >
      <h3 className="mb-3 text-sm font-semibold tracking-tight text-foreground">{title}</h3>
      {children}
    </div>
  );
}

export function TourBookingPreview() {
  const t = useTranslations("bookings");
  const f = useTranslations("bookings.fields");
  const tM = useTranslations("milestones");

  const demoItem = {
    isDraft: false,
    waybillNumber: "WB-918273",
    statusHistory: [
      { status: "Draft", occurredAtUtc: "2025-03-10T08:00:00Z" },
      { status: "CompletedBooking", occurredAtUtc: "2025-03-10T09:12:00Z" },
      { status: "Waybill", occurredAtUtc: "2025-03-10T09:30:00Z" },
      { status: "SendBooking", occurredAtUtc: "2025-03-10T10:00:00Z" },
    ] as const,
  };

  return (
    <TourUiSurface>
      <Card className="border-border/80 shadow-none">
        <CardHeader className="pb-3">
          <CardTitle className="text-base">{t("createTitle")}</CardTitle>
          <CardDescription>{t("sections.headerDescription")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="tour-ref">{f("referenceNumber")}</Label>
              <Input id="tour-ref" readOnly value="REF-2025-8841" className="font-mono text-sm" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="tour-carrier">{f("postalService")}</Label>
              <Input id="tour-carrier" readOnly value="Posti" />
            </div>
          </div>
          <div className="space-y-2">
            <p className="text-xs font-medium text-muted-foreground">{tM("draft")}</p>
            <BookingMilestoneBar item={demoItem} />
          </div>
          <div className="space-y-1.5">
            <Label>{f("receiver")}</Label>
            <Input readOnly value="Nordic Retail Oy" />
            <div className="grid grid-cols-2 gap-2 pt-1">
              <Input readOnly value="00100" className="text-sm" />
              <Input readOnly value="Helsinki" className="text-sm" />
            </div>
          </div>
          <div className="flex flex-wrap gap-2 pt-1">
            <Button type="button" variant="secondary" size="sm" disabled>
              {t("saveAsDraft")}
            </Button>
            <Button type="button" size="sm" disabled>
              {tHome("createBooking")}
            </Button>
          </div>
        </CardContent>
      </Card>
    </TourUiSurface>
  );
}

export function TourDashboardPreview() {
  const tStats = useTranslations("dashboard.stats");
  const theme = TOUR_DEMO_ECHARTS_THEME;
  const colors = [...TOUR_DEMO_CHART_COLORS];

  const dowLabels = [
    tStats("dowSun"),
    tStats("dowMon"),
    tStats("dowTue"),
    tStats("dowWed"),
    tStats("dowThu"),
    tStats("dowFri"),
    tStats("dowSat"),
  ];

  const daily = Array.from({ length: 28 }, (_, i) => {
    const d = new Date(Date.UTC(2025, 2, 1 + i));
    const date = d.toISOString().slice(0, 10);
    const count = 8 + ((i * 5 + 11) % 19);
    return { date, count };
  });

  const last7Option = buildLast7DaysGroupedBarOption(
    daily,
    theme,
    tStats("bookings"),
    tStats("sameWeekdayAvg30"),
    colors[0]!,
    colors[1]!,
    dowLabels,
  );

  const periodOption = buildPeriodBarOption(
    [tStats("today"), tStats("thisMonth"), tStats("thisYear")],
    [12, 184, 2140],
    colors,
    theme,
    tStats("bookings"),
  );

  const pieRows = aggregateCouriersForPie(
    [
      { key: "Posti", count: 42 },
      { key: "DHL Express", count: 28 },
      { key: "DB Schenker", count: 19 },
      { key: "UPS", count: 11 },
    ],
    3,
    tStats("otherCouriers"),
  );
  const pieOption = buildCourierPieOption(pieRows, colors, theme, tStats("bookings"));

  return (
    <TourUiSurface>
      <div className="space-y-4">
        <div className="grid grid-cols-3 gap-2">
          {[
            { label: tStats("today"), value: "12" },
            { label: tStats("thisMonth"), value: "184" },
            { label: tStats("thisYear"), value: "2140" },
          ].map((x) => (
            <div
              key={x.label}
              className="rounded-xl border border-border/70 bg-card/90 px-2 py-2 text-center"
            >
              <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
                {x.label}
              </p>
              <p className="font-mono text-lg font-semibold tabular-nums text-foreground">{x.value}</p>
            </div>
          ))}
        </div>
        {last7Option ? (
          <ChartPanel title={tStats("last7DaysTitle")}>
            <ThemedECharts option={last7Option} height={168} />
          </ChartPanel>
        ) : null}
        <ChartPanel title={tStats("byPeriod")}>
          <ThemedECharts option={periodOption} height={160} />
        </ChartPanel>
        <ChartPanel title={tStats("courierShareTitle")}>
          <ThemedECharts option={pieOption} height={200} />
        </ChartPanel>
      </div>
    </TourUiSurface>
  );
}

export function TourWorkflowPreview() {
  const tNav = useTranslations("nav");
  const tCards = useTranslations("dashboard.cards");
  const tStats = useTranslations("dashboard.stats");

  const tiles = [
    { icon: LayoutDashboard, title: tNav("dashboard"), desc: tStats("title") },
    { icon: Package, title: tNav("bookings"), desc: tCards("createDescription") },
    { icon: Zap, title: tNav("actions"), desc: tCards("actionsDescription") },
    { icon: Puzzle, title: tNav("plugin"), desc: tCards("pluginDescription") },
    { icon: Download, title: tNav("releaseNotes"), desc: tCards("pluginDescription") },
    { icon: MoreHorizontal, title: tNav("more"), desc: tCards("moreDescription") },
  ];

  return (
    <TourUiSurface>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {tiles.map(({ icon: Icon, title, desc }) => (
          <Card key={title} className="border-border/80 shadow-sm">
            <CardHeader className="space-y-1 pb-2">
              <div className="flex items-center gap-2">
                <Icon className="size-4 text-primary" strokeWidth={1.75} aria-hidden />
                <CardTitle className="text-sm font-medium">{title}</CardTitle>
              </div>
              <CardDescription className="line-clamp-2 text-xs">{desc}</CardDescription>
            </CardHeader>
            <CardContent className="pt-0">
              <Button variant="outline" size="sm" className="w-full" disabled>
                {title}
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </TourUiSurface>
  );
}

export function TourCarriersPreview() {
  const tNav = useTranslations("nav");

  const rows = [
    { name: "Posti", id: "CNT-8821-A" },
    { name: "DHL Express", id: "EXT-10492" },
    { name: "DB Schenker", id: "SCH-7712" },
  ];

  return (
    <TourUiSurface>
      <Card className="border-border/80 shadow-none">
        <CardHeader className="pb-2">
          <CardTitle className="text-base">{tNav("courierContracts")}</CardTitle>
          <CardDescription>{tTour("previewDisclaimer")}</CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Carrier</TableHead>
                <TableHead className="text-right">Contract ID</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((r) => (
                <TableRow key={r.id}>
                  <TableCell className="font-medium">{r.name}</TableCell>
                  <TableCell className="text-right font-mono text-xs">{r.id}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </TourUiSurface>
  );
}

export function TourAdminPreview() {
  const tNav = useTranslations("nav");

  return (
    <TourUiSurface>
      <div className="flex flex-col gap-4 sm:flex-row">
        <div className="flex shrink-0 flex-col gap-1 sm:w-40">
          <Button variant="secondary" size="sm" className="justify-start" disabled>
            <Users className="mr-2 size-4" aria-hidden />
            {tNav("users")}
          </Button>
          <Button variant="ghost" size="sm" className="justify-start" disabled>
            <Building2 className="mr-2 size-4" aria-hidden />
            {tNav("companies")}
          </Button>
          <Button variant="ghost" size="sm" className="justify-start" disabled>
            <Settings2 className="mr-2 size-4" aria-hidden />
            {tNav("subscriptionPlans")}
          </Button>
        </div>
        <Card className="min-w-0 flex-1 border-border/80 shadow-none">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">{tNav("companies")}</CardTitle>
            <CardDescription>{tTour("previewAdminNote")}</CardDescription>
          </CardHeader>
          <CardContent className="pt-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Company</TableHead>
                  <TableHead className="text-right">Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {["Nordic Freight Oy", "Baltic Cargo AB", "Arctic Logistics"].map((name, i) => (
                  <TableRow key={name}>
                    <TableCell className="font-medium">{name}</TableCell>
                    <TableCell className="text-right">
                      <span
                        className={cn(
                          "rounded-md px-2 py-0.5 text-xs font-medium",
                          i === 0
                            ? "bg-emerald-500/15 text-emerald-600 dark:text-emerald-400"
                            : "bg-muted text-muted-foreground",
                        )}
                      >
                        {i === 0 ? tTour("previewActive") : tTour("previewTrial")}
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </TourUiSurface>
  );
}
