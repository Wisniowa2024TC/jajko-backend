# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until the .csproj changes
COPY JajkoBackend.csproj ./
RUN dotnet restore JajkoBackend.csproj

COPY . .
RUN dotnet publish JajkoBackend.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# SQLite database lives here; mount a volume so it survives redeploys
RUN mkdir -p /data && chown app:app /data
VOLUME /data

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__DefaultConnection="Data Source=/data/jajko.db"

EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "JajkoBackend.dll"]
