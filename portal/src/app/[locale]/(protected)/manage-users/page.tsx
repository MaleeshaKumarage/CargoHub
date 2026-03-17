"use client";

import { useRouter } from "@/i18n/navigation";
import { useEffect } from "react";

export default function ManageUsersRedirectPage() {
  const router = useRouter();
  useEffect(() => {
    router.replace("/manage/users");
  }, [router]);
  return (
    <div className="flex min-h-[20vh] items-center justify-center">
      <p className="text-muted-foreground">Redirecting…</p>
    </div>
  );
}
