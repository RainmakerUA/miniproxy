# =========================
# Build & Publish (Native AoT)
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build

# Install AoT dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file first for cache optimization
COPY src/RM.Web.MiniProxy/RM.Web.MiniProxy.csproj src/RM.Web.MiniProxy/
RUN dotnet restore src/RM.Web.MiniProxy/RM.Web.MiniProxy.csproj

# Copy the remaining sources
COPY . .

WORKDIR /src/src/RM.Web.MiniProxy

# Publish native AoT build
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
EXPOSE 8080 8443

# Serving ports
ENV HTTP_PORT=8080
ENV HTTPS_PORT=8443

# TLS cert paths (bind-mounted at runtime)
ENV HTTPS_CERT_PATH=/app/cert.pfx
ENV HTTPS_CERT_PWD_FILE=/app/cert-pass.txt

# Copy native publish output
COPY --from=build /app/publish ./

# Non-root
USER 65532

ENTRYPOINT ["./RM.Web.MiniProxy"]
