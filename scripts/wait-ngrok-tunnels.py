#!/usr/bin/env python3
"""Poll ngrok /api/tunnels until at least one tunnel exists or timeout. Prints final JSON to stdout."""
import json
import os
import sys
import time
import urllib.error
import urllib.request

# Host port must match docker-compose.one.yml (14040 -> container 4040).
_DEFAULT_TUNNELS = "http://127.0.0.1:14040/api/tunnels"
URL = os.environ.get("NGROK_LOCAL_API_URL", _DEFAULT_TUNNELS)
# Total wait up to ~2 minutes (60 * 2s)
ATTEMPTS = 60
INTERVAL = 2.0


def main():
    last_body = "{}"
    for attempt in range(ATTEMPTS):
        try:
            with urllib.request.urlopen(URL, timeout=5) as resp:
                last_body = resp.read().decode("utf-8", errors="replace")
            data = json.loads(last_body)
            tunnels = data.get("tunnels") or []
            if len(tunnels) > 0:
                print(last_body)
                return
        except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError, TimeoutError):
            pass
        except Exception:
            pass
        if attempt < ATTEMPTS - 1:
            time.sleep(INTERVAL)

    print(last_body)


if __name__ == "__main__":
    main()
