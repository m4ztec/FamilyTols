# Build stage for Blazor WebAssembly
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-webui
WORKDIR /src

# Copy solution and project files
COPY ["HomeInventory.sln", "."]
COPY ["HomeInventory.webui/HomeInventory.webui.csproj", "HomeInventory.webui/"]
COPY ["HomeInventory.shared/HomeInventory.shared.csproj", "HomeInventory.shared/"]

# Restore dependencies
RUN dotnet restore "HomeInventory.sln"

# Copy source code
COPY ["HomeInventory.webui/", "HomeInventory.webui/"]
COPY ["HomeInventory.shared/", "HomeInventory.shared/"]

# Build and publish the Blazor WASM app
RUN dotnet publish "HomeInventory.webui/HomeInventory.webui.csproj" -c Release -o /app/webui /p:UseAppHost=false

# Build stage for API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-api
WORKDIR /src

# Copy solution and project files
COPY ["HomeInventory.sln", "."]
COPY ["HomeInventory.api/HomeInventory.api.csproj", "HomeInventory.api/"]
COPY ["HomeInventory.shared/HomeInventory.shared.csproj", "HomeInventory.shared/"]

# Restore dependencies
RUN dotnet restore "HomeInventory.sln"

# Copy source code
COPY ["HomeInventory.api/", "HomeInventory.api/"]
COPY ["HomeInventory.shared/", "HomeInventory.shared/"]

# Build the API project
RUN dotnet build "HomeInventory.api/HomeInventory.api.csproj" -c Release -o /app/api/build

# Publish stage for API
FROM build-api AS publish-api
RUN dotnet publish "HomeInventory.api/HomeInventory.api.csproj" -c Release -o /app/api/publish /p:UseAppHost=false

# Runtime stage - Combined API + Static Files
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Install Nginx
RUN apt-get update && \
    apt-get install -y nginx curl && \
    rm -rf /var/lib/apt/lists/*

# Copy Nginx configuration
COPY nginx-combined.conf /etc/nginx/nginx.conf

# Copy published API files
COPY --from=publish-api /app/api/publish ./api

# Copy published Blazor WebUI static files to Nginx
COPY --from=build-webui /app/webui/wwwroot /var/www/html

# Create directories for runtime data
RUN mkdir -p /data && chmod 777 /data

# Expose ports
EXPOSE 8080 80

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Copy startup script
COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

# Run both services
ENTRYPOINT ["/app/docker-entrypoint.sh"]
