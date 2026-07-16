# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/CargoFlow.Api/CargoFlow.Api.csproj ./CargoFlow.Api/
RUN dotnet restore ./CargoFlow.Api/

COPY backend/ ./backend/
RUN dotnet publish ./backend/CargoFlow.Api/CargoFlow.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN groupadd -r dotnetapp && useradd -r -g dotnetapp dotnetapp

COPY --from=build /app/publish .
RUN chown -R dotnetapp:dotnetapp /app
USER dotnetapp

EXPOSE 10000
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "CargoFlow.Api.dll"]
