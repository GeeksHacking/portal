FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY global.json ./
COPY */*.csproj ./
RUN for file in *.csproj; do mkdir -p "${file%.*}/" && mv "$file" "${file%.*}/"; done
RUN dotnet restore "GeeksHackingPortal.Api/GeeksHackingPortal.Api.csproj"
COPY . .
WORKDIR "/src/GeeksHackingPortal.Api"
RUN dotnet build "./GeeksHackingPortal.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GeeksHackingPortal.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish \
    /p:PublishReadyToRun=true \
    /p:PublishReadyToRunShowWarnings=true \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "GeeksHackingPortal.Api.dll"]
