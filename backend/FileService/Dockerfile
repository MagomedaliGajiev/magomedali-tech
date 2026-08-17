FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "./nuget.config"]
COPY [".editorconfig", "./.editorconfig"]
COPY ["FileService/", "FileService/"]

RUN --mount=type=secret,id=gh_token \
    --mount=type=cache,id=file-service-nuget,target=/root/.nuget/packages \
    GH_PACKAGES_TOKEN="$(cat /run/secrets/gh_token)" \
    DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=false \
    dotnet restore "FileService/FileService.Web/FileService.Web.csproj" \
        --configfile "nuget.config"
WORKDIR "/src/FileService/FileService.Web"
RUN --mount=type=cache,id=file-service-nuget,target=/root/.nuget/packages \
    dotnet build "./FileService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=file-service-nuget,target=/root/.nuget/packages \
    dotnet publish "./FileService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileService.Web.dll"]
