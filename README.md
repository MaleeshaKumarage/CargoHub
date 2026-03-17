# CargoHub Backend

## Run API + UI together

1. Open **CargoHub.Backend.sln** in Visual Studio.
2. In Solution Explorer, **right-click CargoHub.Launcher** → **Set as Startup Project**.
3. Press **F5** (or the Start button).

This starts:

- **API** at http://localhost:5299 (Swagger: http://localhost:5299/swagger)
- **Portal (UI)** at http://localhost:3000 (a second console window will open for Next.js)

Press Enter in the Launcher console to stop both.

---

To run only the API (no UI), set **CargoHub.Api** as Startup Project instead.
