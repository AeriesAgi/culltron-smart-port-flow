# CivicOps request blocked: current checkout is Smart Port

Date: 2026-05-17

This repository checkout is **Culltron Smart Port Flow**, not **Culltron CivicOps AI**.

Evidence from the current tree:

- Project files are `src/SmartPort.*/*.csproj`.
- Web entrypoint is `src/SmartPort.Web/Program.cs`.
- Android project is `mobile/SmartPortDriverCompanion`.
- Existing submission docs are Smart Port / Gemini / Driver Companion docs.
- No CivicOps-specific routes or files were found for `/app`, `/Home/BobEvidence`, Bob evidence docs, `mobile/CivicOpsAndroid`, `/Alerts`, `/Incidents`, `/Status`, community thread routes, or report submission/status routes.

## Required CivicOps files/routes not present in this checkout

The requested final pass mentions these required assets, but they are not available in the current repository:

- `mobile/CivicOpsAndroid`
- `/app`
- `/Home/Mobile` as Citizen App / Download App / Install App
- `/Home/BobEvidence`
- `docs/bob-report.md`
- `docs/build-log.md`
- `docs/ibm-bob-session-report.md`
- `docs/ibm-bob-final-continuity-report.md`
- CivicOps `/Alerts`
- CivicOps `/Incidents`
- CivicOps `/Status`
- community thread route
- report submission/status route

## Actions intentionally not taken

To avoid corrupting Smart Port with a different product, this pass does **not** fabricate CivicOps/Bob routes or Android package names inside the Smart Port app.

## Checks attempted in this checkout

- `dotnet restore` / `dotnet build`: blocked because the container does not have the `dotnet` CLI installed.
- Android project audit: current Android project is Smart Port Driver Companion, not CivicOps Citizen Companion.
- APK workflow audit: current workflow is `.github/workflows/build-android-apk.yml` for Smart Port Driver Companion.
- Docker audit: Smart Port already has `Dockerfile` and `docker-compose.yml` with `ASPNETCORE_URLS=http://+:8080`.

## Correct next step

Run the CivicOps final submission-readiness pass in the CivicOps repository checkout. Once that repository is available, apply the requested work there:

1. Preserve Bob evidence docs/routes.
2. Make the .NET app build cleanly.
3. Complete `mobile/CivicOpsAndroid` with a real Gradle Android project.
4. Add an APK artifact workflow and web download/install page.
5. Verify required CivicOps smoke routes.
6. Add DigitalOcean deployment notes and container readiness.
