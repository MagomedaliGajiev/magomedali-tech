FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EducationContentService/EducationContentService.Web/EducationContentService.Web.csproj", "EducationContentService/EducationContentService.Web/"]
COPY ["EducationContentService/EducationContentService.Contracts/EducationContentService.Contracts.csproj", "EducationContentService/EducationContentService.Contracts/"]
COPY ["EducationContentService/EducationContentService.Domain/EducationContentService.Domain.csproj", "EducationContentService/EducationContentService.Domain/"]
COPY ["EducationContentService/EducationContentService.Infrastructure.Postgres/EducationContentService.Infrastructure.Postgres.csproj", "EducationContentService/EducationContentService.Infrastructure.Postgres/"]
COPY ["EducationContentService/EducationContentService.Core/EducationContentService.Core.csproj", "EducationContentService/EducationContentService.Core/"]
COPY ["FileService/FileService.Contracts/FileService.Contracts.csproj", "FileService/FileService.Contracts/"]

COPY ["EducationContentService/Directory.Build.props", "EducationContentService/"]
COPY ["EducationContentService/Directory.Packages.props", "EducationContentService/"]
COPY ["EducationContentService/nuget.config", "EducationContentService/"]
COPY ["FileService/Directory.Build.props", "FileService/"]
COPY ["FileService/Directory.Packages.props", "FileService/"]

RUN --mount=type=secret,id=gh_token \
    GH_PACKAGES_TOKEN="$(cat /run/secrets/gh_token)" \
    dotnet restore "EducationContentService/EducationContentService.Web/EducationContentService.Web.csproj" \
        --configfile "EducationContentService/nuget.config"
COPY . .
WORKDIR "/src/EducationContentService/EducationContentService.Web"
RUN dotnet build "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EducationContentService.Web.dll"]
