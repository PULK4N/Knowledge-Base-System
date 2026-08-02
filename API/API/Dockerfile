FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .

RUN dotnet restore API/API/API.csproj
RUN dotnet publish API/API/API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]
