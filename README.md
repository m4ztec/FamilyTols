# FamilyTools

A comprehensive suite of family management applications built with .NET, including tools for managing household inventories and other family-related utilities.

## 📋 Table of Contents

- [Projects Overview](#projects-overview)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Development](#development)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

---

## 🎯 Projects Overview

### FamilyTools
A command-line utility project built with .NET 10.0 for family management tasks. This is the foundational project for other family-related tools.

**Tech Stack:**
- .NET 10.0
- C#

### HomeInventory
A full-stack web application for managing household inventories collaboratively with family members.

**Components:**
- **HomeInventory.api** - RESTful API backend
- **HomeInventory.shared** - Shared data models and DTOs
- **HomeInventory.webui** - Blazor WebAssembly frontend

**Tech Stack:**
- .NET (ASP.NET Core + Blazor)
- Entity Framework Core
- SQL Server
- Keycloak (Authentication/Authorization)
- Docker & Docker Compose
- Scalar for API documentation

---

## 🏗️ Architecture

### HomeInventory System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser / Client                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ↓
┌─────────────────────────────────────────────────────────────┐
│        HomeInventory.webui (Blazor WebAssembly)              │
│  - OIDC Authentication (Keycloak)                            │
│  - Interactive UI Components                                 │
│  - Real-time Data Binding                                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ↓ (HTTP Requests)
┌─────────────────────────────────────────────────────────────┐
│         HomeInventory.api (ASP.NET Core REST API)            │
│  - JWT Bearer Token Validation                               │
│  - Inventory Endpoints (/api/inventory)                      │
│  - User Endpoints (/api/users)                               │
│  - API Documentation (/scalar/v1)                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ↓
┌─────────────────────────────────────────────────────────────┐
│            HomeInventory.shared (Data Models)                │
│  - Inventory Model                                           │
│  - InventoryProducts Model                                   │
│  - InventoryMembers Model                                    │
│  - PackageUnits Model                                        │
│  - Product Model                                             │
│  - DTOs and Interfaces                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ↓
┌─────────────────────────────────────────────────────────────┐
│         Entity Framework Core + SQL Server                   │
│  - Database Context                                          │
│  - Migrations                                                │
│  - Data Persistence                                          │
└─────────────────────────────────────────────────────────────┘
```

### Authentication Flow

1. User logs in via Keycloak OIDC integration
2. Frontend receives ID token and access token
3. Access token sent with API requests as Bearer token
4. API validates token with Keycloak authority
5. Claims are extracted and used for authorization

---

## 📋 Prerequisites

### For Development

- **.NET SDK**: 10.0 or 9.0
- **Node.js**: 18+ (for build tools)
- **Docker & Docker Compose**: For containerized development
- **SQL Server** (or compatible database)
- **Keycloak**: For authentication (can run locally via Docker)
- **Git**: For version control

### For Production

- Docker & Docker Compose
- Cloud hosting platform (Railway, AWS, Azure, etc.)
- Keycloak instance or alternative authentication provider
- SQL Server database

---

## 🚀 Getting Started

### Quick Start with Docker (Recommended)

1. **Navigate to HomeInventory folder:**
   ```bash
   cd HomeInventory
   ```

2. **Start the application stack:**
   ```bash
   docker-compose up -d
   ```

3. **Access the application:**
   - Web UI: http://localhost:8080
   - API: http://localhost:8080/api
   - API Docs: http://localhost:8080/scalar/v1

4. **View logs:**
   ```bash
   docker-compose logs -f
   ```

5. **Stop the services:**
   ```bash
   docker-compose down
   ```

### Local Development Setup

#### 1. Clone the Repository
```bash
git clone https://github.com/m4ztec/FamilyTools.git
cd FamilyTools
```

#### 2. Setup Keycloak (Local)
```bash
# Run Keycloak in Docker
docker run -d \
  -p 8888:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest \
  start-dev
```

#### 3. Configure Environment Variables

Create `appsettings.Development.json` for each project:

**HomeInventory.api:**
```json
{
  "Keycloak": {
    "Authority": "http://localhost:8888/realms/your-realm",
    "Audience": "your-api-audience",
    "RequireHttpsMetadata": false
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HomeInventory;Trusted_Connection=true;"
  }
}
```

**HomeInventory.webui:**
```json
{
  "Keycloak": {
    "Authority": "http://localhost:8888/realms/your-realm",
    "ClientId": "your-client-id",
    "Scope": "openid profile email"
  },
  "ApiAddress": "http://localhost:5196"
}
```

#### 4. Restore NuGet Packages
```bash
cd HomeInventory
dotnet restore
```

#### 5. Run Database Migrations
```bash
cd HomeInventory.api
dotnet ef database update
```

#### 6. Run Individual Projects

**Terminal 1 - API Server:**
```bash
cd HomeInventory/HomeInventory.api
dotnet run
```

**Terminal 2 - Web UI:**
```bash
cd HomeInventory/HomeInventory.webui
dotnet run
```

The applications will be available at:
- API: https://localhost:7003 (HTTPS) or http://localhost:5196 (HTTP)
- WebUI: http://localhost:5197 (or as shown in console)

---

## 🛠️ Development

### Key Technologies

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10.0 / 9.0 |
| Backend | ASP.NET Core | Latest |
| Frontend | Blazor WebAssembly | Latest |
| Database | Entity Framework Core | Latest |
| Auth | Keycloak / OIDC | Latest |
| Containerization | Docker | Latest |
| Documentation | Scalar | Latest |

### Common Development Tasks

#### Building the Solution
```bash
cd HomeInventory
dotnet build
```

#### Running Tests
```bash
dotnet test
```

#### Creating Database Migrations
```bash
cd HomeInventory.api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

#### Publishing a Release Build
```bash
dotnet publish -c Release -o ./publish
```

#### Debugging in Visual Studio
1. Set startup project to `HomeInventory.api` or `HomeInventory.webui`
2. Press F5 to start debugging
3. Set breakpoints as needed

---

## 🐳 Deployment

### Docker Deployment

The project includes a `Dockerfile` and `docker-compose.yml` for easy containerization.

#### Build Docker Image
```bash
cd HomeInventory
docker build -t homeinventory:latest .
```

#### Push to Registry
```bash
docker tag homeinventory:latest <registry>/homeinventory:latest
docker push <registry>/homeinventory:latest
```

### Cloud Deployment

See [DEPLOYMENT.md](HomeInventory/DEPLOYMENT.md) for detailed instructions on deploying to:
- **Railway**
- **AWS ECS (Elastic Container Service)**
- **Other cloud providers**

#### Environment Variables for Production

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Keycloak__Authority=<your-keycloak-url>
Keycloak__Audience=<your-api-audience>
Keycloak__RequireHttpsMetadata=true
ConnectionStrings__DefaultConnection=<your-db-connection-string>
```

---

## 🤝 Contributing

1. Create a feature branch (`git checkout -b feature/amazing-feature`)
2. Commit your changes (`git commit -m 'Add amazing feature'`)
3. Push to the branch (`git push origin feature/amazing-feature`)
4. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.