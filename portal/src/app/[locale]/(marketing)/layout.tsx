import { MarketingTourHeader } from "@/components/marketing/MarketingTourHeader";
import { TechGridBackground } from "@/components/marketing/TechGridBackground";

export default function MarketingLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="marketing-tour-root min-h-screen bg-zinc-950 text-zinc-100">
      <TechGridBackground />
      <MarketingTourHeader />
      {children}
    </div>
  );
}
