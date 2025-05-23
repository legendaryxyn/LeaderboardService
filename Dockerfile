FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LeaderboardService.csproj", "./"]
RUN dotnet restore "LeaderboardService.csproj"
COPY . .
RUN dotnet build "LeaderboardService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LeaderboardService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaderboardService.dll"]