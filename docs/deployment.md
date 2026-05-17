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
