"use client";

import { Suspense, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { useRouter } from "@/i18n/navigation";
import { Card, CardContent } from "@/components/ui/card";

/** Email links use this path; forwards to `accept-invite` with `type=rider`. */
function RedirectInner() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token");

  useEffect(() => {
    const q = new URLSearchParams();
    if (token) q.set("token", token);
    q.set("type", "rider");
    router.replace(`/accept-invite?${q.toString()}`);
  }, [token, router]);

  return (
    <Card className="w-full max-w-sm">
      <CardContent className="pt-6">
        <p className="text-muted-foreground text-sm">Redirecting…</p>
      </CardContent>
    </Card>
  );
}

export default function AcceptRiderInviteRedirectPage() {
  return (
    <Suspense
      fallback={
        <Card className="w-full max-w-sm">
          <CardContent className="pt-6">
            <p className="text-muted-foreground text-sm">Loading…</p>
          </CardContent>
        </Card>
      }
    >
      <RedirectInner />
    </Suspense>
  );
}
