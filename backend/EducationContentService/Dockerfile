FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
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
WORKDIR "/src/EducationContentService/EducationContentService.Web"
RUN --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    dotnet build "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=education-content-nuget,target=/root/.nuget/packages \
    dotnet publish "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EducationContentService.Web.dll"]
