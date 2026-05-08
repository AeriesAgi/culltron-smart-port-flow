# ── Build Stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SmartPort.Web/SmartPort.Web.csproj",                   "src/SmartPort.Web/"]
COPY ["src/SmartPort.Application/SmartPort.Application.csproj",   "src/SmartPort.Application/"]
COPY ["src/SmartPort.Domain/SmartPort.Domain.csproj",             "src/SmartPort.Domain/"]
COPY ["src/SmartPort.Infrastructure/SmartPort.Infrastructure.csproj", "src/SmartPort.Infrastructure/"]
COPY ["src/SmartPort.Shared/SmartPort.Shared.csproj",             "src/SmartPort.Shared/"]

RUN dotnet restore "src/SmartPort.Web/SmartPort.Web.csproj"

COPY . .

WORKDIR "/src/src/SmartPort.Web"
RUN dotnet publish "SmartPort.Web.csproj" -c Release -o /app/publish --no-restore

# ── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080


COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080


ENTRYPOINT ["dotnet", "SmartPort.Web.dll"]
