FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG EF_TOOLS_VERSION=10.0.9
WORKDIR /src
COPY ["nuget.config", "./nuget.config"]
COPY [".editorconfig", "./.editorconfig"]
COPY ["EducationContentService/", "EducationContentService/"]

RUN --mount=type=secret,id=gh_token \
    --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    GH_PACKAGES_TOKEN="$(cat /run/secrets/gh_token)" \
    DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=false \
    dotnet restore "EducationContentService/EducationContentService.Web/EducationContentService.Web.csproj" \
        --configfile "nuget.config"
RUN --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    dotnet tool install --tool-path /tools dotnet-ef --version $EF_TOOLS_VERSION
WORKDIR "/src/EducationContentService/EducationContentService.Web"
RUN --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    dotnet build "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    dotnet publish "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore
RUN --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    /tools/dotnet-ef migrations bundle \
        --project "../EducationContentService.Infrastructure.Postgres/EducationContentService.Infrastructure.Postgres.csproj" \
        --startup-project "./EducationContentService.Web.csproj" \
        --configuration $BUILD_CONFIGURATION \
        --no-build \
        --output /app/publish/efbundle

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EducationContentService.Web.dll"]
