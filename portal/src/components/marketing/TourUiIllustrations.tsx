import {
  Building2,
  Download,
  LayoutDashboard,
  MoreHorizontal,
  Package,
  Puzzle,
  Settings2,
  Truck,
  Users,
  Zap,
} from "lucide-react";
import { TourWindowFrame } from "./TourWindowFrame";

const muted = "text-zinc-500";
const line = "h-2 rounded bg-zinc-700/80";
const field = "space-y-1";

/** Stylized booking editor + milestone strip (illustration only). */
export function IllustrationBookings({ label }: { label: string }) {
  const sections = ["Header", "Shipper", "Receiver", "Pickup", "Shipment", "Packages"];
  return (
    <TourWindowFrame label={label}>
      <div className="flex aspect-[16/10] min-h-[200px] text-[10px]">
        <aside className="w-[28%] shrink-0 border-r border-zinc-800/90 bg-zinc-900/80 p-2">
          <p className={`mb-2 font-mono ${muted}`}>Sections</p>
          <ul className="space-y-1">
            {sections.map((s, i) => (
              <li
                key={s}
                className={`rounded px-2 py-1 font-mono ${
                  i === 2 ? "bg-cyan-500/15 text-cyan-200" : "text-zinc-400"
                }`}
              >
                {s}
              </li>
            ))}
          </ul>
        </aside>
        <div className="flex min-w-0 flex-1 flex-col p-3">
          <div className="mb-3 flex gap-1">
            {[1, 2, 3, 4, 5, 6].map((step) => (
              <div
                key={step}
                className={`h-1.5 flex-1 rounded-full ${
                  step <= 3 ? "bg-cyan-500/90" : "bg-zinc-800"
                }`}
              />
            ))}
          </div>
          <p className={`mb-2 font-mono ${muted}`}>Reference & carrier</p>
          <div className={field}>
            <div className={`${line} w-3/5`} />
            <div className={`${line} w-2/5`} />
          </div>
          <p className={`mb-2 mt-3 font-mono ${muted}`}>Receiver</p>
          <div className={field}>
            <div className={`${line} w-full`} />
            <div className={`${line} w-4/5`} />
            <div className="grid grid-cols-2 gap-2 pt-1">
              <div className={`${line} w-full`} />
              <div className={`${line} w-full`} />
            </div>
          </div>
          <div className="mt-auto flex items-center justify-between border-t border-zinc-800/80 pt-2">
            <span className="font-mono text-zinc-500">Draft</span>
            <span className="rounded border border-cyan-500/40 bg-cyan-500/10 px-2 py-0.5 font-mono text-cyan-200/90">
              Save draft
            </span>
          </div>
        </div>
      </div>
    </TourWindowFrame>
  );
}

/** KPI row + bar activity + heatmap grid (illustration only). */
export function IllustrationDashboard({ label }: { label: string }) {
  const bars = [40, 65, 45, 80, 55, 90, 50];
  return (
    <TourWindowFrame label={label}>
      <div className="flex aspect-[16/10] min-h-[200px] flex-col gap-3 p-3">
        <div className="grid grid-cols-3 gap-2">
          {[
            { k: "Today", v: "24" },
            { k: "Month", v: "412" },
            { k: "Year", v: "3.8k" },
          ].map((x) => (
            <div
              key={x.k}
              className="rounded-lg border border-zinc-700/80 bg-zinc-900/60 px-2 py-2 text-center"
            >
              <p className="font-mono text-[9px] uppercase tracking-wide text-zinc-500">{x.k}</p>
              <p className="font-mono text-lg font-semibold tabular-nums text-cyan-200/95">{x.v}</p>
            </div>
          ))}
        </div>
        <div>
          <p className={`mb-1.5 font-mono text-[9px] uppercase tracking-wide ${muted}`}>
            Recent activity
          </p>
          <div className="flex h-16 items-end gap-1.5 rounded-lg border border-zinc-800/90 bg-zinc-950/50 px-2 pb-2 pt-3">
            {bars.map((h, i) => (
              <div
                key={i}
                className="flex-1 rounded-sm bg-gradient-to-t from-cyan-600/50 to-cyan-400/80"
                style={{ height: `${h}%` }}
              />
            ))}
          </div>
        </div>
        <div>
          <p className={`mb-1.5 font-mono text-[9px] uppercase tracking-wide ${muted}`}>
            Calendar signals
          </p>
          <div className="grid grid-cols-12 gap-0.5 rounded border border-zinc-800/80 bg-zinc-950/40 p-1.5">
            {Array.from({ length: 48 }).map((_, i) => {
              const intensity = (i * 17 + 31) % 100;
              return (
                <div
                  key={i}
                  className="aspect-square rounded-[1px] bg-cyan-400/90"
                  style={{ opacity: 0.08 + (intensity / 100) * 0.55 }}
                />
              );
            })}
          </div>
        </div>
      </div>
    </TourWindowFrame>
  );
}

/** App hub tiles (illustration only). */
export function IllustrationWorkflow({ label }: { label: string }) {
  const tiles = [
    { icon: LayoutDashboard, t: "Dashboard" },
    { icon: Package, t: "Bookings" },
    { icon: Zap, t: "Actions" },
    { icon: Puzzle, t: "Plugin" },
    { icon: Download, t: "Release notes" },
    { icon: MoreHorizontal, t: "More" },
  ];
  return (
    <TourWindowFrame label={label}>
      <div className="grid aspect-[16/10] min-h-[200px] grid-cols-3 gap-2 p-4 sm:grid-cols-3">
        {tiles.map(({ icon: Icon, t }) => (
          <div
            key={t}
            className="flex flex-col items-center justify-center gap-2 rounded-xl border border-zinc-700/70 bg-zinc-900/50 p-3 transition-colors hover:border-cyan-500/30"
          >
            <Icon className="size-7 text-cyan-400/85" strokeWidth={1.5} />
            <span className="text-center font-mono text-[9px] text-zinc-400">{t}</span>
          </div>
        ))}
      </div>
    </TourWindowFrame>
  );
}

/** Carrier contract list (illustration only). */
export function IllustrationCarriers({ label }: { label: string }) {
  const rows = [
    { name: "Posti", id: "CNT-8821-A" },
    { name: "DHL Express", id: "EXT-10492" },
    { name: "DB Schenker", id: "SCH-7712" },
  ];
  return (
    <TourWindowFrame label={label}>
      <div className="aspect-[16/10] min-h-[200px] p-3">
        <div className="mb-2 flex items-center gap-2">
          <Truck className="size-4 text-cyan-400/80" strokeWidth={1.5} />
          <span className="font-mono text-[10px] text-zinc-400">Courier contracts</span>
        </div>
        <div className="overflow-hidden rounded-lg border border-zinc-800/90">
          <div className="grid grid-cols-[1fr_auto] gap-2 border-b border-zinc-800 bg-zinc-900/80 px-2 py-1.5 font-mono text-[9px] uppercase tracking-wide text-zinc-500">
            <span>Carrier</span>
            <span>Contract ID</span>
          </div>
          {rows.map((r) => (
            <div
              key={r.id}
              className="grid grid-cols-[1fr_auto] gap-2 border-b border-zinc-800/60 px-2 py-2 font-mono text-[10px] last:border-0"
            >
              <span className="text-zinc-300">{r.name}</span>
              <span className="rounded bg-zinc-800/80 px-1.5 py-0.5 text-cyan-200/90">{r.id}</span>
            </div>
          ))}
        </div>
      </div>
    </TourWindowFrame>
  );
}

/** Super-admin style panel (illustration only). */
export function IllustrationAdmin({ label }: { label: string }) {
  return (
    <TourWindowFrame label={label}>
      <div className="flex aspect-[16/10] min-h-[200px] gap-2 p-2">
        <div className="flex w-[32%] shrink-0 flex-col gap-1 border-r border-zinc-800/90 pr-2 pt-1">
          {[
            { icon: Users, t: "Users" },
            { icon: Building2, t: "Companies" },
            { icon: Settings2, t: "Plans" },
          ].map(({ icon: Icon, t }) => (
            <div
              key={t}
              className="flex items-center gap-2 rounded-md px-2 py-1.5 font-mono text-[10px] text-zinc-400 hover:bg-zinc-800/50"
            >
              <Icon className="size-3.5 text-cyan-500/70" strokeWidth={1.5} />
              {t}
            </div>
          ))}
        </div>
        <div className="min-w-0 flex-1 pt-1">
          <p className={`mb-2 font-mono text-[9px] uppercase tracking-wide ${muted}`}>Companies</p>
          <div className="space-y-1.5">
            {["Nordic Freight Oy", "Baltic Cargo AB", "Arctic Logistics"].map((name, i) => (
              <div
                key={name}
                className="flex items-center justify-between rounded-md border border-zinc-800/70 bg-zinc-900/40 px-2 py-1.5"
              >
                <span className="truncate font-mono text-[10px] text-zinc-300">{name}</span>
                <span
                  className={`shrink-0 rounded px-1.5 py-0.5 font-mono text-[9px] ${
                    i === 0 ? "bg-emerald-500/20 text-emerald-300/90" : "bg-zinc-800 text-zinc-400"
                  }`}
                >
                  {i === 0 ? "Active" : "Trial"}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </TourWindowFrame>
  );
}
