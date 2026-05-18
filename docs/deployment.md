# DigitalOcean deployment notes

## Container

The app is container-ready through `Dockerfile` and `docker-compose.yml`. The runtime listens on `ASPNETCORE_URLS=http://+:8080`.

## App Platform / Droplet commands

```bash
docker compose build web
docker compose up -d db web
```

For a single DigitalOcean App Platform web service, set the HTTP port to `8080` and provide environment variables in the DO dashboard.

## Required environment

- `ConnectionStrings__DefaultConnection`
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- Demo codes if using judge/demo access.
- `GEMINI_API_KEY` only if Gemini should be active.
- WhatsApp/Meta credentials only for approved LiveTest/Live connector tests.

Do not hardcode Gemini, WhatsApp, database or API secrets in source.

## Final redeploy checklist

```bash
dotnet restore SmartPort.sln
dotnet publish src/SmartPort.Web/SmartPort.Web.csproj -c Release -o ./publish
```

Example Droplet package copy and service restart (confirm the service name on the host):

```bash
tar -czf smartport-web.tar.gz -C publish .
scp smartport-web.tar.gz root@YOUR_DROPLET:/tmp/smartport-web.tar.gz
ssh root@YOUR_DROPLET 'mkdir -p /var/www/smartport && tar -xzf /tmp/smartport-web.tar.gz -C /var/www/smartport && systemctl restart smartport && systemctl status smartport --no-pager'
```

Post-deploy smoke checks:

```bash
curl -I https://smartport.culltron.app/
curl -I https://smartport.culltron.app/demo-access
curl -I https://smartport.culltron.app/driver-app
curl -I https://smartport.culltron.app/fleet/download-app
curl -I https://smartport.culltron.app/downloads/SmartPortDriverCompanion-debug.apk
```

If nginx fronts Kestrel, confirm proxy headers and HTTPS forwarding. Do not hardcode `GEMINI_API_KEY`, WhatsApp credentials, database strings, or demo production secrets in source; configure them as environment variables or platform secrets.
