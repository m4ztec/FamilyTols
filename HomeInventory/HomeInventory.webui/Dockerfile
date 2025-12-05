# Build stage for Blazor WebAssembly
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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

# Build the project
RUN dotnet build "HomeInventory.webui/HomeInventory.webui.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "HomeInventory.webui/HomeInventory.webui.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use nginx to serve the static Blazor WebAssembly app
FROM nginx:alpine AS runtime

# Install curl for health checks
RUN apk add --no-cache curl

# Copy nginx configuration
COPY HomeInventory.webui/nginx.conf /etc/nginx/nginx.conf

# Copy published Blazor files
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/index.html || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
