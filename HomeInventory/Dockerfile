# Build stage for Blazor WebAssembly
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-webui
WORKDIR /src

# Copy all files
COPY . .

# Restore dependencies for webui
RUN dotnet restore "HomeInventory.webui/HomeInventory.webui.csproj"

# Build and publish the Blazor WASM app
RUN dotnet publish "HomeInventory.webui/HomeInventory.webui.csproj" -c Release -o /app/webui /p:UseAppHost=false

# Build stage for API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-api
WORKDIR /src

# Copy all files
COPY . .

# Restore dependencies for API
RUN dotnet restore "HomeInventory.api/HomeInventory.api.csproj"

# Build the API project
RUN dotnet build "HomeInventory.api/HomeInventory.api.csproj" -c Release -o /app/api/build

# Publish stage for API
FROM build-api AS publish-api
RUN dotnet publish "HomeInventory.api/HomeInventory.api.csproj" -c Release -o /app/api/publish /p:UseAppHost=false

# Runtime stage - Kestrel only
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy published API files
COPY --from=publish-api /app/api/publish .

# Copy published Blazor WebUI static files to wwwroot
COPY --from=build-webui /app/webui/wwwroot ./wwwroot

# Verify wwwroot exists and has files
RUN ls -la /app && ls -la /app/wwwroot

# Create directories for runtime data
RUN mkdir -p /data && chmod 777 /data

# Expose port (Railway will assign dynamically)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "HomeInventory.api.dll"]
