import { RevealOnScroll } from "@/components/marketing/RevealOnScroll";
import {
  TourAdminPreview,
  TourBookingPreview,
  TourCarriersPreview,
  TourDashboardPreview,
  TourWorkflowPreview,
} from "@/components/marketing/TourRealPreviews";
import { Link } from "@/i18n/navigation";
import { getTranslations } from "next-intl/server";
import { Button } from "@/components/ui/button";
import type { Metadata } from "next";

type Props = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "tour" });
  return {
    title: t("metaTitle"),
    description: t("metaDescription"),
  };
}

export default async function TourPage({ params }: Props) {
  const { locale } = await params;
  const tCta = await getTranslations({ locale, namespace: "tour.cta" });
  const tHero = await getTranslations({ locale, namespace: "tour.hero" });
  const tPlat = await getTranslations({ locale, namespace: "tour.platform" });
  const tBook = await getTranslations({ locale, namespace: "tour.bookings" });
  const tDash = await getTranslations({ locale, namespace: "tour.dashboard" });
  const tFlow = await getTranslations({ locale, namespace: "tour.workflow" });
  const tInt = await getTranslations({ locale, namespace: "tour.integrations" });
  const tCar = await getTranslations({ locale, namespace: "tour.carriers" });
  const tAdm = await getTranslations({ locale, namespace: "tour.admin" });
  const tSub = await getTranslations({ locale, namespace: "tour.subscriptions" });
  const tStart = await getTranslations({ locale, namespace: "tour.start" });
  const tFoot = await getTranslations({ locale, namespace: "tour.footer" });

  return (
    <main className="relative w-full max-w-full overflow-x-hidden">
      <section
        id="top"
        className="mx-auto max-w-6xl px-4 pb-20 pt-12 text-center sm:px-6 sm:pt-16 sm:text-left lg:px-8 lg:pt-20"
      >
        <RevealOnScroll>
          <p className="mb-3 font-mono text-xs tracking-[0.2em] text-cyan-400/90">
            {tHero("kicker")}
          </p>
          <h1 className="mx-auto max-w-3xl text-4xl font-bold tracking-tight text-zinc-50 sm:mx-0 sm:text-5xl lg:text-6xl">
            {tHero("title")}
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg text-zinc-400 sm:mx-0">{tHero("subtitle")}</p>
          <div className="mt-10 flex flex-wrap items-center justify-center gap-3 sm:justify-start">
            <Button
              asChild
              size="lg"
              className="border border-cyan-400/40 bg-cyan-500/20 font-mono text-sm text-cyan-50 hover:bg-cyan-500/30"
            >
              <Link href="/register">{tCta("register")}</Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="ghost"
              className="font-mono text-sm text-zinc-300 hover:bg-cyan-500/10 hover:text-cyan-200"
            >
              <Link href="/login">{tCta("signIn")}</Link>
            </Button>
            <a
              href="#platform"
              className="ml-0 font-mono text-xs text-zinc-500 underline-offset-4 hover:text-cyan-300 hover:underline sm:ml-2"
            >
              {tCta("explore")}
            </a>
          </div>
          <ul className="mt-14 flex flex-wrap justify-center gap-6 border-t border-cyan-500/15 pt-8 font-mono text-xs text-zinc-500 sm:justify-start">
            <li className="flex items-center gap-2">
              <span className="h-1.5 w-1.5 rounded-full bg-cyan-400" />
              {tHero("strip1")}
            </li>
            <li className="flex items-center gap-2">
              <span className="h-1.5 w-1.5 rounded-full bg-cyan-400" />
              {tHero("strip2")}
            </li>
            <li className="flex items-center gap-2">
              <span className="h-1.5 w-1.5 rounded-full bg-cyan-400" />
              {tHero("strip3")}
            </li>
          </ul>
        </RevealOnScroll>
      </section>

      <section
        id="platform"
        className="border-t border-cyan-500/10 bg-zinc-950/80 py-20 sm:py-24"
      >
        <div className="mx-auto max-w-6xl px-4 text-center sm:px-6 sm:text-left lg:px-8">
          <RevealOnScroll>
            <div className="flex flex-wrap items-baseline justify-center gap-4 sm:justify-start">
              <span className="font-mono text-sm text-cyan-400/80">{tPlat("index")}</span>
              <span className="font-mono text-xs text-zinc-500">{tPlat("kicker")}</span>
            </div>
            <h2 className="mx-auto mt-2 max-w-2xl text-3xl font-semibold tracking-tight text-zinc-50 sm:mx-0 sm:text-4xl">
              {tPlat("title")}
            </h2>
            <p className="mx-auto mt-4 max-w-2xl text-zinc-400 sm:mx-0">{tPlat("intro")}</p>
            <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {[
                { title: tPlat("card1Title"), body: tPlat("card1Body") },
                { title: tPlat("card2Title"), body: tPlat("card2Body") },
                { title: tPlat("card3Title"), body: tPlat("card3Body") },
                { title: tPlat("card4Title"), body: tPlat("card4Body") },
              ].map((card) => (
                <div
                  key={card.title}
                  className="rounded-xl border border-cyan-500/15 bg-zinc-900/40 p-5 text-left backdrop-blur-sm transition-colors hover:border-cyan-500/30"
                >
                  <h3 className="font-semibold text-zinc-100">{card.title}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-zinc-400">{card.body}</p>
                </div>
              ))}
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <section id="bookings" className="py-20 sm:py-24">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid items-center gap-12 lg:grid-cols-2">
            <RevealOnScroll>
              <span className="font-mono text-sm text-cyan-400/80">{tBook("index")}</span>
              <p className="mt-2 font-mono text-xs text-zinc-500">{tBook("kicker")}</p>
              <h2 className="mt-4 text-3xl font-semibold tracking-tight text-zinc-50 sm:text-4xl">
                {tBook("title")}
              </h2>
              <p className="mt-4 text-zinc-400">{tBook("body")}</p>
              <ul className="mx-auto mt-6 w-fit space-y-2 text-left font-mono text-sm text-cyan-200/90 sm:mx-0 sm:w-full">
                <li>— {tBook("b1")}</li>
                <li>— {tBook("b2")}</li>
                <li>— {tBook("b3")}</li>
                <li>— {tBook("b4")}</li>
              </ul>
            </RevealOnScroll>
            <RevealOnScroll className="lg:justify-self-end">
              <TourBookingPreview />
            </RevealOnScroll>
          </div>
          <RevealOnScroll className="mt-14 w-full border-t border-cyan-500/15 pt-10 lg:mt-16 lg:pt-12">
            <div className="w-full rounded-xl border border-cyan-500/20 bg-zinc-950/70 p-6 text-left shadow-[inset_0_1px_0_0_rgba(34,211,238,0.06)] backdrop-blur-sm sm:p-8 lg:grid lg:grid-cols-[minmax(0,15rem)_1fr] lg:items-start lg:gap-x-10 lg:gap-y-1">
              <h3 className="font-semibold text-zinc-100 sm:text-lg">{tBook("bulkTitle")}</h3>
              <p className="mt-3 text-sm leading-relaxed text-zinc-400 lg:mt-0">{tBook("bulkBody")}</p>
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <section
        id="dashboard"
        className="border-t border-cyan-500/10 bg-zinc-950/80 py-20 sm:py-24"
      >
        <div className="mx-auto grid max-w-6xl items-center justify-items-center gap-12 px-4 text-center sm:justify-items-stretch sm:px-6 sm:text-left lg:grid-cols-2 lg:px-8">
          <RevealOnScroll className="order-2 w-full max-w-xl lg:order-1 lg:max-w-none">
            <TourDashboardPreview />
          </RevealOnScroll>
          <RevealOnScroll className="order-1 w-full max-w-xl lg:order-2 lg:max-w-none">
            <span className="font-mono text-sm text-cyan-400/80">{tDash("index")}</span>
            <p className="mt-2 font-mono text-xs text-zinc-500">{tDash("kicker")}</p>
            <h2 className="mt-4 text-3xl font-semibold tracking-tight text-zinc-50 sm:text-4xl">
              {tDash("title")}
            </h2>
            <p className="mt-4 text-zinc-400">{tDash("body")}</p>
            <ul className="mx-auto mt-6 w-fit space-y-2 text-left font-mono text-sm text-cyan-200/90 sm:mx-0 sm:w-full">
              <li>— {tDash("b1")}</li>
              <li>— {tDash("b2")}</li>
              <li>— {tDash("b3")}</li>
            </ul>
          </RevealOnScroll>
        </div>
      </section>

      <section id="workflow" className="py-20 sm:py-24">
        <div className="mx-auto grid max-w-6xl items-center gap-12 px-4 sm:px-6 lg:grid-cols-2 lg:px-8">
          <RevealOnScroll>
            <span className="font-mono text-sm text-cyan-400/80">{tFlow("index")}</span>
            <p className="mt-2 font-mono text-xs text-zinc-500">{tFlow("kicker")}</p>
            <h2 className="mt-4 text-3xl font-semibold tracking-tight text-zinc-50 sm:text-4xl">
              {tFlow("title")}
            </h2>
            <p className="mt-4 text-zinc-400">{tFlow("body")}</p>
            <ul className="mx-auto mt-6 w-fit space-y-2 text-left font-mono text-sm text-cyan-200/90 sm:mx-0 sm:w-full">
              <li>— {tFlow("b1")}</li>
              <li>— {tFlow("b2")}</li>
              <li>— {tFlow("b3")}</li>
            </ul>
          </RevealOnScroll>
          <RevealOnScroll>
            <TourWorkflowPreview />
          </RevealOnScroll>
        </div>
      </section>

      <section
        id="integrations"
        className="border-t border-cyan-500/10 bg-zinc-950/80 py-16 sm:py-20"
      >
        <div className="mx-auto max-w-6xl px-4 text-center sm:px-6 sm:text-left lg:px-8">
          <RevealOnScroll>
            <span className="font-mono text-sm text-cyan-400/80">{tInt("index")}</span>
            <p className="mt-2 font-mono text-xs text-zinc-500">{tInt("kicker")}</p>
            <h2 className="mx-auto mt-4 max-w-2xl text-2xl font-semibold tracking-tight text-zinc-50 sm:mx-0 sm:text-3xl">
              {tInt("title")}
            </h2>
            <p className="mx-auto mt-4 max-w-3xl text-zinc-400 sm:mx-0">{tInt("body")}</p>
            <div className="mt-10 max-w-2xl">
              <div className="rounded-xl border border-cyan-500/15 bg-zinc-900/40 p-6 text-left backdrop-blur-sm">
                <h3 className="font-semibold text-zinc-100">{tInt("apiTitle")}</h3>
                <p className="mt-3 text-sm leading-relaxed text-zinc-400">{tInt("apiBody")}</p>
              </div>
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <section
        id="carriers"
        className="border-t border-cyan-500/10 py-16 sm:py-20"
      >
        <div className="mx-auto max-w-6xl px-4 text-center sm:px-6 sm:text-left lg:px-8">
          <RevealOnScroll className="grid justify-items-center gap-10 sm:justify-items-stretch lg:grid-cols-3 lg:items-center">
            <div className="w-full max-w-xl lg:col-span-2 lg:max-w-none">
              <span className="font-mono text-sm text-cyan-400/80">{tCar("index")}</span>
              <p className="mt-2 font-mono text-xs text-zinc-500">{tCar("kicker")}</p>
              <h2 className="mt-4 text-2xl font-semibold tracking-tight text-zinc-50 sm:text-3xl">
                {tCar("title")}
              </h2>
              <p className="mx-auto mt-4 max-w-xl text-zinc-400 lg:mx-0">{tCar("body")}</p>
            </div>
            <div className="w-full max-w-xl lg:max-w-none">
              <TourCarriersPreview />
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <section id="admin" className="py-16 sm:py-20">
        <div className="mx-auto max-w-6xl px-4 text-center sm:px-6 sm:text-left lg:px-8">
          <RevealOnScroll className="grid justify-items-center gap-10 sm:justify-items-stretch lg:grid-cols-2 lg:items-center">
            <div className="w-full max-w-xl lg:max-w-none">
              <span className="font-mono text-sm text-cyan-400/80">{tAdm("index")}</span>
              <p className="mt-2 font-mono text-xs text-zinc-500">{tAdm("kicker")}</p>
              <h2 className="mt-4 text-2xl font-semibold tracking-tight text-zinc-50 sm:text-3xl">
                {tAdm("title")}
              </h2>
              <p className="mt-4 text-zinc-400">{tAdm("body")}</p>
            </div>
            <div className="w-full max-w-xl lg:max-w-none">
              <TourAdminPreview />
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <section
        id="subscriptions"
        className="border-t border-cyan-500/10 bg-zinc-950/80 py-16 sm:py-20"
      >
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <RevealOnScroll>
            <span className="font-mono text-sm text-cyan-400/80">{tSub("index")}</span>
            <p className="mt-2 font-mono text-xs text-zinc-500">{tSub("kicker")}</p>
            <h2 className="mt-4 max-w-2xl text-2xl font-semibold tracking-tight text-zinc-50 sm:text-3xl">
              {tSub("title")}
            </h2>
            <p className="mt-4 max-w-3xl text-zinc-400">{tSub("intro")}</p>
            <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              <div className="rounded-xl border border-cyan-500/15 bg-zinc-900/40 p-6 backdrop-blur-sm">
                <h3 className="font-semibold text-zinc-100">{tSub("tiersTitle")}</h3>
                <p className="mt-3 text-sm leading-relaxed text-zinc-400">{tSub("tiersBody")}</p>
              </div>
              <div className="rounded-xl border border-cyan-500/15 bg-zinc-900/40 p-6 text-left backdrop-blur-sm">
                <h3 className="font-semibold text-zinc-100">{tSub("customTitle")}</h3>
                <p className="mt-3 text-sm leading-relaxed text-zinc-400">{tSub("customBody")}</p>
              </div>
              <div className="rounded-xl border border-cyan-500/15 bg-zinc-900/40 p-6 backdrop-blur-sm sm:col-span-2 lg:col-span-1">
                <h3 className="font-semibold text-zinc-100">{tSub("invoiceTitle")}</h3>
                <p className="mt-3 text-sm leading-relaxed text-zinc-400">{tSub("invoiceBody")}</p>
              </div>
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <section id="start" className="border-t border-cyan-500/10 py-20 sm:py-24">
        <div className="mx-auto max-w-6xl px-4 text-center sm:px-6 sm:text-left lg:px-8">
          <RevealOnScroll>
            <span className="font-mono text-sm text-cyan-400/80">{tStart("index")}</span>
            <p className="mt-2 font-mono text-xs text-zinc-500">{tStart("kicker")}</p>
            <h2 className="mx-auto mt-4 max-w-2xl text-3xl font-semibold tracking-tight text-zinc-50 sm:mx-0 sm:max-w-none sm:text-4xl">
              {tStart("title")}
            </h2>
            <ol className="mx-auto mt-8 w-fit max-w-xl space-y-4 text-left font-mono text-sm text-zinc-300 sm:mx-0 sm:w-full sm:max-w-none">
              <li className="flex gap-3">
                <span className="text-cyan-400">01</span>
                <span>{tStart("step1")}</span>
              </li>
              <li className="flex gap-3">
                <span className="text-cyan-400">02</span>
                <span>{tStart("step2")}</span>
              </li>
              <li className="flex gap-3">
                <span className="text-cyan-400">03</span>
                <span>{tStart("step3")}</span>
              </li>
            </ol>
            <p className="mx-auto mt-8 max-w-xl border-cyan-500/40 pl-0 text-sm text-zinc-500 sm:mx-0 sm:border-l-2 sm:pl-4 sm:text-left">
              {tStart("faq")}
            </p>
            <div className="mt-10 flex flex-wrap justify-center gap-3 sm:justify-start">
              <Button
                asChild
                className="border border-cyan-400/40 bg-cyan-500/20 font-mono text-sm text-cyan-50 hover:bg-cyan-500/30"
              >
                <Link href="/register">{tCta("register")}</Link>
              </Button>
              <Button asChild variant="ghost" className="font-mono text-sm text-zinc-300">
                <Link href="/login">{tCta("signIn")}</Link>
              </Button>
            </div>
          </RevealOnScroll>
        </div>
      </section>

      <footer className="border-t border-cyan-500/10 py-10">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 px-4 sm:flex-row sm:px-6 lg:px-8">
          <p className="font-mono text-xs text-zinc-500">
            © {new Date().getFullYear()} CargoHub. {tFoot("rights")}
          </p>
          <div className="flex flex-wrap items-center justify-center gap-4">
            <a
              href="#top"
              className="font-mono text-xs text-cyan-400/80 underline-offset-4 hover:underline"
            >
              {tFoot("backToTour")}
            </a>
            <Link
              href="/login"
              className="font-mono text-xs text-zinc-400 hover:text-cyan-300"
            >
              {tCta("signIn")}
            </Link>
            <Link
              href="/register"
              className="font-mono text-xs text-zinc-400 hover:text-cyan-300"
            >
              {tCta("register")}
            </Link>
          </div>
        </div>
      </footer>
    </main>
  );
}
