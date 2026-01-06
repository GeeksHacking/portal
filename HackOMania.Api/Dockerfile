FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV PORT=8080
ENV ASPNETCORE_URLS=https://+:${PORT}
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HackOMania.Api/HackOMania.Api.csproj", "HackOMania.Api/"]
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
