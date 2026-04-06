import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

/** Decorative browser-style frame for marketing tour UI illustrations (not live product data). */
export function TourWindowFrame({
  label,
  children,
  className,
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "overflow-hidden rounded-xl border border-cyan-500/25 bg-zinc-900/90 shadow-[0_0_48px_-14px_rgba(34,211,238,0.35)]",
        className,
      )}
      aria-hidden
    >
      <div className="flex h-9 items-center gap-2 border-b border-zinc-700/90 bg-zinc-900/95 px-3">
        <span className="h-2.5 w-2.5 rounded-full bg-red-500/90" />
        <span className="h-2.5 w-2.5 rounded-full bg-amber-400/90" />
        <span className="h-2.5 w-2.5 rounded-full bg-emerald-500/90" />
        <span className="ml-2 truncate font-mono text-[10px] tracking-wide text-zinc-500">{label}</span>
      </div>
      <div className="bg-gradient-to-br from-zinc-900 via-zinc-950 to-zinc-900">{children}</div>
    </div>
  );
}
