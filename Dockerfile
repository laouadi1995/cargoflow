# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY backend/CargoFlow.Api/CargoFlow.Api.csproj ./
RUN dotnet restore

COPY backend/CargoFlow.Api/ ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

CMD ["dotnet", "CargoFlow.Api.dll"]
