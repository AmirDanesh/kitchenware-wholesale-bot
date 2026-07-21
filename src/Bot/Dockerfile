FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["NuGet.config", "./"]
COPY ["src/Domain/KitchenwareBot.Domain.csproj", "src/Domain/"]
COPY ["src/Application/KitchenwareBot.Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/KitchenwareBot.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Bot/KitchenwareBot.Bot.csproj", "src/Bot/"]
RUN dotnet restore "src/Bot/KitchenwareBot.Bot.csproj" --source "https://nexus.etadbir.com/repository/nuget-group/index.json"

COPY . .
RUN dotnet publish "src/Bot/KitchenwareBot.Bot.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "KitchenwareBot.Bot.dll"]
