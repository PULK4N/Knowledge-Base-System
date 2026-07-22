FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY API/EventSourcing/Shared/Shared.csproj API/EventSourcing/Shared/
COPY API/EventSourcing/Persistence.Interfaces/Persistence.Interfaces.csproj API/EventSourcing/Persistence.Interfaces/
COPY API/EventSourcing/Core/Core.csproj API/EventSourcing/Core/
COPY API/EventSourcing/Persistence/Persistence.csproj API/EventSourcing/Persistence/
COPY API/MemoryMcp/MemoryMcp.csproj API/MemoryMcp/
RUN dotnet restore API/MemoryMcp/MemoryMcp.csproj

COPY API/EventSourcing API/EventSourcing
COPY API/MemoryMcp API/MemoryMcp
RUN dotnet publish API/MemoryMcp/MemoryMcp.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "MemoryMcp.dll"]
