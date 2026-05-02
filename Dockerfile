# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BlogCMS.Data/BlogCMS.Data.csproj           src/BlogCMS.Data/
COPY src/BlogCMS.Business/BlogCMS.Business.csproj   src/BlogCMS.Business/
COPY src/BlogCMS.Web/BlogCMS.Web.csproj             src/BlogCMS.Web/
RUN dotnet restore src/BlogCMS.Web/BlogCMS.Web.csproj

COPY src/ src/
RUN dotnet publish src/BlogCMS.Web/BlogCMS.Web.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd -r app && useradd -r -g app app
COPY --from=build /app/publish .
RUN chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BlogCMS.Web.dll"]
