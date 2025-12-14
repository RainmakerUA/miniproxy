FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
# Install clang/zlib1g-dev dependencies for publishing to native (AoT compilation)
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RM.Web.MiniProxy/RM.Web.MiniProxy.csproj", "RM.Web.MiniProxy/"]
RUN dotnet restore "./RM.Web.MiniProxy/RM.Web.MiniProxy.csproj"
COPY . .
WORKDIR "/src/RM.Web.MiniProxy"
RUN dotnet build "./RM.Web.MiniProxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project with AoT compilation
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RM.Web.MiniProxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true /p:PublishAot=true /p:PublishTrimmed=true

# This stage is used in production (AoT compiled native executable)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS final
WORKDIR /app
EXPOSE 80 443
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_HTTPS_PORT=443
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=/app/cert-pass.txt
COPY --from=publish /app/publish .

VOLUME ~/secrets/miniproxy/appsettings.json: /app/appsettings.json:ro
VOLUME ~/secrets/miniproxy/cert.pfx: /app/cert.pfx:ro
VOLUME ~/secrets/miniproxy/cert-pass.txt: /app/cert-pass.txt:ro

ENTRYPOINT ["./RM.Web.MiniProxy"]
