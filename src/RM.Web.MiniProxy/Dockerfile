# =========================
# Build & Publish (Native AoT)
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj first for better caching
COPY src/RM.Web.MiniProxy/RM.Web.MiniProxy.csproj src/RM.Web.MiniProxy/
RUN dotnet restore src/RM.Web.MiniProxy/RM.Web.MiniProxy.csproj

# Copy remaining sources
COPY . .

WORKDIR /src/src/RM.Web.MiniProxy

RUN dotnet publish RM.Web.MiniProxy.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:PublishAot=true \
    /p:PublishTrimmed=true \
    /p:StripSymbols=true \
    /p:InvariantGlobalization=true \
    /p:OptimizationPreference=Speed \
    /p:IlcGenerateCompleteTypeMetadata=false \
    /p:EventSourceSupport=false \
    /p:UseAppHost=true

# =========================
# Runtime (minimal)
# =========================
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS final

WORKDIR /app
EXPOSE 80 443

# URLs only — no code tuning
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_HTTPS_PORT=443

# TLS cert paths (mounted at runtime)
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__PasswordFile=/app/cert-pass.txt

COPY --from=build /app/publish ./

# Non-root
USER 65532

ENTRYPOINT ["./RM.Web.MiniProxy"]
