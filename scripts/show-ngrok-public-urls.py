#!/usr/bin/env python3
"""Parse ngrok /api/tunnels JSON from stdin; print public URLs and append GitHub Actions step summary."""
import json
import os
import sys


def main():
    raw = sys.stdin.read()
    try:
        data = json.loads(raw)
    except json.JSONDecodeError:
        print("Could not parse ngrok response.")
        sys.exit(0)

    tunnels = data.get("tunnels") or []
    rows = []
    for t in tunnels:
        name = (t.get("name") or "").strip()
        url = (t.get("public_url") or "").strip()
        if not url:
            continue
        if name == "public":
            label = "Public app (nginx :8888 — portal + /api)"
        elif name == "portal":
            label = "Portal (Next.js UI)"
        elif name == "api":
            label = "API (backend)"
        else:
            label = name or "tunnel"
        print(f"  {label}")
        print(f"  {url}")
        print("")
        rows.append((label, url))
        safe = url.replace("%", "%25").replace("\n", " ")
        print(f"::notice title=Public URL — {label}::{safe}", file=sys.stderr)

    if not rows:
        print("  (no tunnels in response)")
        print("")
        print("  Troubleshooting:")
        print("  - Image must include ngrok (Dockerfile.all-in-one) — pull latest :latest")
        print("  - NGROK_AUTHTOKEN must be passed into the container (compose env / GitHub secret)")
        print("  - ngrok listens on :4040 inside the container; compose maps host :14040 -> :4040")
        print("  - Check: docker logs cargohub 2>&1 | tail -80")
        print("  - Check: docker exec cargohub tail -50 /tmp/ngrok.log")

    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path and rows:
        lines = [f"| **{lbl}** | `{u}` |" for lbl, u in rows]
        table = "\n".join(lines)
        first_url = rows[0][1] if rows else ""
        if first_url:
            tip = (
                f"**Tip:** Open **`{first_url}/en/login`** (or **`/en/dashboard`**). "
                f"**`/dashboard` alone returns 404.** Set **`CORS__PORTAL_ORIGIN`** to `{first_url}` "
                "if API calls fail (CORS).\n\n"
            )
        else:
            tip = (
                "**Tip:** Use locale paths (`/en/login`, `/en/dashboard`). "
                "Set **`CORS__PORTAL_ORIGIN`** to the HTTPS origin if API calls fail.\n\n"
            )
        body = (
            "## Public URLs (remote access)\n\n"
            "Open these from **any browser** (internet). On the Mac: **http://localhost:14040** "
            "for the ngrok dashboard (mapped from container :4040).\n\n"
            f"{tip}"
            "| Service | URL |\n"
            "|---------|-----|\n"
            f"{table}\n\n"
            "<details><summary>Raw ngrok JSON</summary>\n\n"
            f"```json\n{raw[:12000]}\n```\n\n"
            "</details>\n"
        )
        with open(summary_path, "a", encoding="utf-8") as f:
            f.write(body)


if __name__ == "__main__":
    main()
