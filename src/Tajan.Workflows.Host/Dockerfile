# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/Tajan.Workflows.Host/Tajan.Workflows.Host.csproj src/Tajan.Workflows.Host/
COPY src/Tajan.Workflows.Extensions/Tajan.Workflows.Extensions.csproj src/Tajan.Workflows.Extensions/
RUN dotnet restore src/Tajan.Workflows.Host/Tajan.Workflows.Host.csproj

COPY . .
RUN dotnet publish src/Tajan.Workflows.Host/Tajan.Workflows.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tajan.Workflows.Host.dll"]
