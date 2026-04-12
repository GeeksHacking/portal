FROM mcr.microsoft.com/dotnet/nightly/aspnet:11.0-preview AS base
WORKDIR /app
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview AS subset
WORKDIR /src
COPY . .
RUN dotnet tool install --tool-path /tools dotnet-subset \
    && /tools/dotnet-subset restore "HackOMania.Api/HackOMania.Api.csproj" \
        --root-directory /src --output /src/restore_subset/

FROM mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY --from=subset /src/restore_subset/ .
RUN dotnet restore "HackOMania.Api/HackOMania.Api.csproj"
COPY . .
WORKDIR "/src/HackOMania.Api"
RUN dotnet build "./HackOMania.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HackOMania.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "HackOMania.Api.dll"]
