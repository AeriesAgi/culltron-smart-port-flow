# Deployment Smoke Test

```bash
dotnet restore SmartPort.sln
dotnet build SmartPort.sln
dotnet publish src/SmartPort.Web/SmartPort.Web.csproj -c Release
docker compose up -d --build
docker compose logs -f web
```

Runtime checks:

```bash
BASE_URL=http://localhost:8080 SMARTPORT_DRIVER_DEMO_CODE=culltron-driver-2026 ./scripts/smoke-test.sh
```

Android:

```bash
cd mobile/SmartPortDriverCompanion
gradle assembleDebug
ls -lah app/build/outputs/apk/debug/
```
