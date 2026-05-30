# Stage 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create uploads folder
RUN mkdir -p wwwroot/uploads

COPY --from=build /out .

EXPOSE 5009

ENTRYPOINT ["dotnet", "CargoFlow.Api.dll"]
