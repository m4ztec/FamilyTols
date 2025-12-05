# HomeInventory Deployment Guide

This guide covers deploying HomeInventory API and Web UI to cloud providers like Railway, AWS, or similar platforms.

## Table of Contents

- [Local Development with Docker](#local-development-with-docker)
- [Railway Deployment](#railway-deployment)
- [AWS Deployment](#aws-deployment)
- [Docker Registry](#docker-registry)
- [Environment Variables](#environment-variables)

---

## Local Development with Docker

### Prerequisites

- Docker and Docker Compose installed
- Keycloak instance running (or update the environment variables to point to your auth server)

### Running Locally

1. **Clone and navigate to the project:**
   ```bash
   cd HomeInventory
   ```

2. **Start the application:**
   ```bash
   docker-compose up -d
   ```

3. **Access the application:**
   - Web UI: http://localhost
   - API: http://localhost/api/inventory
   - API Docs: http://localhost/scalar/v1

4. **View logs:**
   ```bash
   docker-compose logs -f homeInventory
   ```

5. **Stop services:**
   ```bash
   docker-compose down
   ```

6. **Remove data volumes (clean slate):**
   ```bash
   docker-compose down -v
   ```

---

## Railway Deployment

Railway makes it easy to deploy containerized applications.

### Step 1: Create a Railway Project

1. Go to [Railway Dashboard](https://railway.app/dashboard)
2. Click "New Project"
3. Select "Deploy from GitHub" and connect your repository

### Step 2: Configure the Service

1. **Railway will detect the Dockerfile automatically**
   - Select the `Dockerfile` at the root of HomeInventory folder

2. **Set environment variables:**
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `ASPNETCORE_URLS`: `http://+:8080`
   - `Keycloak__Authority`: Your Keycloak authority URL
   - `Keycloak__Audience`: Your Keycloak audience
   - `Keycloak__RequireHttpsMetadata`: `true` (for production)

3. **Port Configuration:**
   - Railway will automatically assign a public domain
   - The container exposes both port 80 (Web UI + API proxy) and 8080 (direct API)
   - Traffic will route through port 80

### Step 3: Deploy

Railway will automatically deploy when you push to your connected GitHub branch.

---

## AWS Deployment

### Option 1: AWS Elastic Container Service (ECS)

#### Prerequisites

- AWS account with appropriate permissions
- AWS CLI configured
- Docker images pushed to Amazon ECR (Elastic Container Registry)

#### Step 1: Push Image to Amazon ECR

```bash
# Create ECR repository
aws ecr create-repository --repository-name homeInventory --region us-east-1

# Get login token
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <YOUR-AWS-ACCOUNT>.dkr.ecr.us-east-1.amazonaws.com

# Build image from root directory (where Dockerfile is located)
docker build -t homeInventory HomeInventory/
docker tag homeInventory:latest <YOUR-AWS-ACCOUNT>.dkr.ecr.us-east-1.amazonaws.com/homeInventory:latest
docker push <YOUR-AWS-ACCOUNT>.dkr.ecr.us-east-1.amazonaws.com/homeInventory:latest
```

#### Step 2: Create ECS Task Definition

1. Go to AWS ECS Dashboard
2. Create task definition
3. Specify container image URI from ECR: `<YOUR-AWS-ACCOUNT>.dkr.ecr.us-east-1.amazonaws.com/homeInventory:latest`
4. Set port mappings:
   - Port 80 (Web UI + API proxy) → 80
   - Port 8080 (direct API) → 8080
5. Configure environment variables:
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `ASPNETCORE_URLS`: `http://+:8080`
   - `Keycloak__Authority`: Your Keycloak URL
   - `Keycloak__Audience`: Your audience

#### Step 3: Create ECS Service

1. Create a cluster (if you don't have one)
2. Create service with the task definition
3. Configure load balancer (ALB)
   - Target group: port 80 (recommended for most use cases)
   - Or use port 8080 for direct API access
4. Set desired task count (start with 1)

### AWS Elastic Beanstalk

```bash
# Initialize Elastic Beanstalk (from HomeInventory root directory)
eb init -p docker homeInventory --region us-east-1

# Create environment
eb create homeInventory-env

# Deploy
eb deploy
```

### AWS App Runner

1. Connect to your GitHub repository
2. Create a new App Runner service
3. Select "Source code repository"
4. Choose the branch
5. Configure:
   - Build command: (leave empty, uses Dockerfile)
   - Start command: (leave empty, uses Dockerfile entrypoint)
6. Set environment variables same as Railway
7. Click "Create and deploy"

---

## Docker Registry

### Docker Hub

```bash
# Build image from HomeInventory root directory
docker build -t yourusername/homeInventory HomeInventory/

# Push to Docker Hub
docker push yourusername/homeInventory:latest
```

### GitHub Container Registry (GHCR)

```bash
# Authenticate
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Build and push (from HomeInventory root directory)
docker build -t ghcr.io/yourusername/homeInventory HomeInventory/
docker tag ghcr.io/yourusername/homeInventory:latest ghcr.io/yourusername/homeInventory:1.0.0
docker push ghcr.io/yourusername/homeInventory:latest
```

---

## Environment Variables

### API Environment Variables

| Variable | Example | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Server binding URLs |
| `Keycloak__Authority` | `https://keycloak.example.com/auth/realms/myapp` | Keycloak authority URL |
| `Keycloak__Audience` | `homeInventory-api` | Keycloak audience |
| `Keycloak__RequireHttpsMetadata` | `true` | Require HTTPS for Keycloak metadata (true in production) |
| `ConnectionStrings__DefaultConnection` | `Data Source=/data/homeInventory.db` | SQLite connection string |

### Web UI Environment Variables

The Web UI is served via Nginx and doesn't require environment variables, but you can update appsettings at runtime if needed.

---

## Database Persistence

### SQLite (Default)

The application uses SQLite by default, which stores data in a file. For cloud deployments:

1. **Use Docker volumes** (not recommended for horizontal scaling)
2. **Use managed databases** (recommended):
   - AWS RDS (PostgreSQL, MySQL)
   - Railway PostgreSQL database
   - Managed database services

### Switching to PostgreSQL

1. Update `HomeInventory.api.csproj` to include PostgreSQL provider:
   ```xml
   <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
   ```

2. Update `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<HomeInventoryapiContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. Set connection string in environment:
   ```
   ConnectionStrings__DefaultConnection=Host=db.railway.internal;Username=user;Password=pass;Database=homeInventory
   ```

---

## Health Checks

Both containers include health checks:

- **API**: `GET http://localhost:8080/health`
- **Web UI**: `GET http://localhost/`

These are configured in the Dockerfiles and help load balancers detect unhealthy instances.

---

## Scaling Considerations

### Single Container Architecture

- **Web UI**: Served statically by Nginx
- **API**: .NET Core application running alongside Nginx
- **Both services**: Share the same container, easy to scale horizontally

### Load Balancing

- Use a load balancer (ALB, NLB, or platform-provided)
- Route all traffic to port 80 (Nginx proxy) or 8080 (direct API)
- Nginx automatically proxies `/api/*` calls to the API application

### Auto-Scaling

- Scale horizontally by running multiple container instances
- Use container orchestration (ECS, EKS) or platform auto-scaling (Railway, AWS App Runner)
- Start with 2-3 instances for high availability
- Monitor CPU/memory to adjust scaling policies

---

## Monitoring & Logging

- Use CloudWatch (AWS), Railway Logs, or similar
- Monitor:
  - CPU and memory usage
  - Error rates
  - Response times
  - Database connections

---

## Troubleshooting

### API won't start
- Check environment variables (especially Keycloak settings)
- Check logs for database connection issues
- Verify port is not in use

### Web UI returns 403/404
- Check Nginx configuration
- Ensure correct base path in Blazor app
- Verify API endpoint is configured correctly

### Database errors
- Verify connection string format
- Check database permissions
- Ensure database server is accessible

---

## Support

For issues or questions:
1. Check application logs
2. Verify environment variables
3. Check container resource limits
4. Consult platform-specific documentation (Railway, AWS, etc.)
