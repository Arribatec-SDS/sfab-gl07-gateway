# Sample Multi-Tenant Application

This is a complete Stena Fastigheter – GL07 Gateway demonstrating the Nexus Platform multi-tenant architecture.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Traefik (HTTPS)                        │
│              https://*.localtest.me/sfab-gl07-gateway                  │
└─────────────────────────────────────────────────────────────┘
                          │
                          ├──► nginx (port 80)
                          │     ├──> React SPA (/)
                          │     ├──> Backend API (/api/)
                          │     └──> Master API (/api/master/)
                          │
┌─────────────────────────────────────────────────────────────┐
│  Sample Container (sfab-gl07-gateway-app)                              │
│  ┌────────────────────┐  ┌─────────────────────────────┐   │
│  │   React Frontend   │  │   .NET 8 Backend API        │   │
│  │   (Static Files)   │  │   (Port 5000)               │   │
│  └────────────────────┘  └─────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
           │                           │
           │                           ▼
           │              ┌─────────────────────────┐
           │              │  Nexus Platform         │
           │              │  (Master API Client)    │
           │              └─────────────────────────┘
           ▼
┌─────────────────────┐
│  Keycloak           │
│  (Authentication)   │
└─────────────────────┘
```

## Project Structure

```
sfab-gl07-gateway/
├── frontend/                    # React frontend
│   ├── src/
│   │   ├── App.tsx             # Auth provider setup
│   │   ├── main.tsx            # Application entry
│   │   ├── index.css           # Global styles
│   │   ├── utils/
│   │   │   └── api.ts          # API path utilities
│   │   └── components/
│   │       ├── HomePage.tsx    # Protected home page
│   │       └── LoginPage.tsx   # Login page
│   ├── package.json            # Dependencies
│   ├── vite.config.ts          # Vite configuration (base path)
│   ├── tsconfig.json           # TypeScript config
│   └── index.html              # HTML template
├── backend/                     # .NET 8 API
│   ├── Program.cs              # API configuration
│   ├── SfabGl07GatewayController.cs     # Sample endpoints
│   ├── SfabGl07Gateway.Api.csproj       # Project file
│   ├── appsettings.json        # Configuration
│   └── nuget.config            # NuGet sources
├── Dockerfile                   # Multi-stage build
├── docker-compose.yml           # Service definition
├── nginx.conf                   # Reverse proxy config
├── .dockerignore               # Docker ignore rules
└── README.md                    # This file
```

## Dynamic API Path Construction

The sample demonstrates how to build API paths dynamically based on the application's base path, avoiding hardcoded paths.

### The Problem

When an app runs at `https://demo.localtest.me/sfab-gl07-gateway`, API calls need to go to `https://demo.localtest.me/sfab-gl07-gateway/api/*`. Hardcoding `/sfab-gl07-gateway/api/` makes the code less portable.

### The Solution

Use the `api.ts` utility that automatically builds paths from Vite's `base` configuration:

```typescript
// frontend/src/utils/api.ts
import { buildApiPath, getApiBasePath, createApiClient } from '@/utils/api';

// Option 1: Build individual API paths
const userEndpoint = buildApiPath('/user');
// Returns: "/sfab-gl07-gateway/api/user" (base path from vite.config.ts)

const response = await axios.get(userEndpoint);

// Option 2: Use pre-configured axios instance (recommended)
const apiClient = createApiClient();
// apiClient has baseURL set to "/sfab-gl07-gateway/api"

// Add auth token
const token = await getToken();
apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

// Make calls - base path is automatic
await apiClient.get('/user');          // -> /sfab-gl07-gateway/api/user
await apiClient.post('/items', data);  // -> /sfab-gl07-gateway/api/items
```

### How It Works

1. **Vite Configuration** (`vite.config.ts`):
   ```typescript
   export default defineConfig({
     base: '/sfab-gl07-gateway/',  // This sets the base path
     // ...
   });
   ```

2. **Environment Variable**:
   - Vite exposes this as `import.meta.env.BASE_URL`
   - Available at runtime without hardcoding

3. **API Utility**:
   - `getBasePath()` - Gets the base path (e.g., "/sfab-gl07-gateway")
   - `buildApiPath(endpoint)` - Builds full path (e.g., "/sfab-gl07-gateway/api/user")
   - `getApiBasePath()` - Gets API base (e.g., "/sfab-gl07-gateway/api")
   - `createApiClient()` - Returns configured axios instance

4. **Proxy Configuration** (`vite.config.ts`):
   ```typescript
   proxy: {
     '/sfab-gl07-gateway/api': {
       target: 'http://localhost:7458',
       rewrite: (path) => path.replace(/^\/sfab-gl07-gateway\/api/, '/api'),
     }
   }
   ```

### Benefits

✅ **No hardcoded paths** - Change base in one place (vite.config.ts)  
✅ **Portable code** - Works for any app name  
✅ **Type-safe** - Full TypeScript support  
✅ **Consistent** - All API calls use the same pattern  
✅ **Easy testing** - Clear separation of concerns  

### When to Use Each Method

**Use `buildApiPath()`** when:
- Making one-off API calls
- Need full control over the request
- Using custom axios configurations

**Use `createApiClient()`** when:
- Making multiple API calls
- Want consistent headers/interceptors
- Need centralized error handling
- Building a larger application (recommended)

### Example in Practice

```typescript
// frontend/src/components/HomePage.tsx
import { buildApiPath } from '@/utils/api';

const fetchData = async () => {
  const token = await getToken();
  
  // API path is built dynamically
  const response = await axios.get(buildApiPath('/user'), {
    headers: { Authorization: `Bearer ${token}` }
  });
  
  // Works regardless of base path!
};
```

## Prerequisites

- Docker and Docker Compose
- Node.js 18+ (for local frontend development)
- .NET 8 SDK (for local backend development)
- **GitHub Personal Access Token** with `read:packages` scope
- Running infrastructure:
  - Traefik on `nexus-net` network
  - Keycloak at `keycloak.localtest.me`
  - Nexus Console as `nexus-console`

## GitHub Packages Authentication

The sample uses the `Nexus.MasterApi.Client` NuGet package from GitHub Packages, which requires authentication.

### Create a GitHub Personal Access Token

1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token (classic)"
3. Select scopes:
   - `read:packages` (required)
4. Generate and copy the token

### Set Environment Variables

**PowerShell:**
```powershell
$env:GITHUB_USERNAME = "your-github-username"
$env:GITHUB_TOKEN = "ghp_your_token_here"
```

**Bash:**
```bash
export GITHUB_USERNAME="your-github-username"
export GITHUB_TOKEN="ghp_your_token_here"
```

**Create .env file (recommended):**
```env
GITHUB_USERNAME=your-github-username
GITHUB_TOKEN=ghp_your_token_here
```

Docker Compose will automatically load variables from `.env` file in the same directory.

## Quick Start

### Option 1: Docker (Recommended)

Build and run everything in Docker:

```bash
cd sample

# Make sure GitHub credentials are set (see above)
docker-compose build
docker-compose up -d

# Check logs
docker logs sfab-gl07-gateway-app -f
```

Access the app:
- Via Traefik: `https://demo.localtest.me/sfab-gl07-gateway/` or `https://admin.localtest.me/sfab-gl07-gateway/`
- Direct: `http://localhost:8280/sfab-gl07-gateway/`

### Option 2: Local Development

**Frontend:**
```bash
cd frontend
npm install
npm run dev
# Open http://localhost:5173
```

**Backend:**
```bash
cd backend
dotnet restore
dotnet run
# API runs on http://localhost:5000
```

Note: For local development, you need to set GITHUB_TOKEN for NuGet restore:
```powershell
$env:GITHUB_TOKEN = "your_token"
dotnet restore
```

## Configuration

### Frontend

- **Vite Config** (`vite.config.ts`):
  - Base path: `/sfab-gl07-gateway/`
  - React plugin with deduplication
  - Dev server on port 5173

- **Auth Config** (`App.tsx`):
  - Keycloak URL: `https://keycloak.localtest.me`
  - Realms: `admin`, `demo`
  - Silent SSO check enabled

### Backend

- **ASP.NET Core** (`Program.cs`):
  - Path base: `/sfab-gl07-gateway` (from environment)
  - CORS enabled for local development
  - Master API client configured

- **API Endpoints**:
  - `GET /api/sfab-gl07-gateway/user` - Current user info
  - `GET /health` - Health check

### Docker

- **Build Args**:
  - `GITHUB_USERNAME` - Your GitHub username
  - `GITHUB_TOKEN` - Your GitHub PAT

- **Environment Variables**:
  - `TZ=Europe/Oslo` - Timezone
  - `MasterApiUrl=http://nexus-console:7833` - Master API endpoint
  - `ApplicationName=Sample` - App name for telemetry

- **Ports**:
  - `8280` - Direct access port

### Traefik Integration

The service is configured with:
- StripPrefix middleware to remove `/sfab-gl07-gateway` before forwarding
- TLS enabled
- Load balancer on port 80
- Health check at `/health`

## Testing

### Manual Testing

1. **Access via Traefik**:
   ```
   https://demo.localtest.me/sfab-gl07-gateway/
   https://admin.localtest.me/sfab-gl07-gateway/
   ```

2. **Login Flow**:
   - Should redirect to Keycloak login
   - Login with admin realm or demo realm user
   - After successful login, redirected back to app

3. **Test API**:
   ```bash
   # Get current user (requires auth)
   curl -H "Authorization: Bearer $TOKEN" http://localhost:8280/api/sfab-gl07-gateway/user
   ```

4. **Check Health**:
   ```bash
   curl http://localhost:8280/health
   ```

### Check Logs

```bash
# Container logs
docker logs sfab-gl07-gateway-app -f

# Specific component
docker exec sfab-gl07-gateway-app cat /var/log/nginx/access.log
docker exec sfab-gl07-gateway-app cat /var/log/nginx/error.log
```

## Development Workflow

### Frontend Changes

1. Edit files in `frontend/src/`
2. Rebuild: `cd frontend && npm run build`
3. Rebuild Docker: `docker-compose build`
4. Restart: `docker-compose up -d`

### Backend Changes

1. Edit files in `backend/`
2. Rebuild Docker: `docker-compose build`
3. Restart: `docker-compose up -d`

### Configuration Changes

- `nginx.conf` - Update and rebuild Docker
- `docker-compose.yml` - Restart service
- `Dockerfile` - Rebuild Docker image

## Troubleshooting

### 404 Not Found

- **Symptom**: Page not found at `/sfab-gl07-gateway/`
- **Check**:
  ```bash
  # Verify container is running
  docker ps | grep sfab-gl07-gateway-app
  
  # Check Traefik routing
  docker logs traefik-shared | grep sample
  
  # Check nginx logs
  docker exec sfab-gl07-gateway-app cat /var/log/nginx/error.log
  ```

### Login Redirect Issues

- **Symptom**: Login fails or infinite redirect
- **Solutions**:
  1. Check Keycloak is running: `docker ps | grep keycloak`
  2. Verify Keycloak redirect URIs include your domain
  3. Check Nexus Console is running: `docker ps | grep nexus-console`
  3. Check browser console for errors
  4. Verify `KC_SPI_SECURITY_FRAME_OPTIONS` is set in Keycloak

### API Calls Fail

- **Symptom**: 401 Unauthorized or 403 Forbidden
- **Solutions**:
  1. Check token in browser DevTools (Application → Storage)
  2. Verify Master API is running: `docker ps | grep common-services`
  3. Check backend logs: `docker logs sfab-gl07-gateway-app | grep -i error`
  4. Verify CORS headers in nginx.conf

### Docker Build Fails

- **Symptom**: Build fails with authentication error
- **Solutions**:
  1. Verify GitHub token is set: `echo $env:GITHUB_TOKEN`
  2. Check token has `read:packages` scope
  3. Verify token is not expired
  4. Try `docker-compose build --no-cache`

### Health Check Failing

- **Symptom**: Container unhealthy
- **Check**:
  ```bash
  # Manual health check
  docker exec sfab-gl07-gateway-app curl -f http://localhost/health
  
  # Check if nginx is running
  docker exec sfab-gl07-gateway-app ps aux | grep nginx
  
  # Check if .NET app is running
  docker exec sfab-gl07-gateway-app ps aux | grep dotnet
  ```

## Next Steps

- Add more pages and components
- Implement additional API endpoints
- Add tenant-specific functionality
- Configure logging to Loki
- Add metrics for Prometheus
- Implement proper error handling
- Add unit and integration tests

## Database Setup

Your application needs a database to store its data. The Nexus Platform provides a shared SQL Server instance and manages database connections through the Nexus Console.

### Step 1: Create Your Database

The shared SQL Server runs inside Docker. Connect to it and create your app's database:

**From your workstation (using SSMS, Azure Data Studio, or sqlcmd):**
```
Server: localhost,14330
Username: sa
Password: Arribatec123Password
```

**From inside Docker containers:**
```
Server: sqlserver,1433
Username: sa
Password: Arribatec123Password
```

**Create the database using sqlcmd:**

```powershell
# Windows
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Arribatec123Password" -C -Q "CREATE DATABASE SampleDB"
```

```bash
# macOS/Linux
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Arribatec123Password" -C -Q "CREATE DATABASE SampleDB"
```

> **Recommendation:** Create a dedicated database for your app (e.g., `SampleDB`, `MyAppDB`) rather than using the shared `DemoDB`. This keeps your data isolated and makes backups/migrations easier.

### Step 2: Create Database Tables

After creating the database, add your tables:

```powershell
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Arribatec123Password" -C -d SampleDB -Q "
CREATE TABLE Items (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
"
```

Or use a SQL file:
```powershell
docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Arribatec123Password" -C -d SampleDB -i /path/to/init.sql
```

### Step 3: Register Database Connection in Nexus Console

The Nexus Platform manages database connections centrally. You must register your database connection through the Nexus Console:

1. **Open Nexus Console**: `https://admin.localtest.me/nexus-console`

2. **Navigate to Products** and find your app (or create a new product entry)

3. **Add Database Connection**:
   - **Connection Name**: `SampleDB` (or your app name)
   - **Server**: `sqlserver,1433` (Docker internal hostname)
   - **Database**: `SampleDB`
   - **Username**: `sa`
   - **Password**: `Arribatec123Password`
   - **Connection Type**: Product (recommended for app-specific databases)

4. **Link to your Product/App**: Associate the database connection with your registered product

> **Important:** Choose **Product** connection type for app-specific databases. **Tenant** connection type is for advanced multi-tenant scenarios where each tenant has a separate database.

### Step 4: Access Database in Your Code

**Always use `IContextAwareDatabaseService.CreateProductConnectionAsync()`** to get database connections. This service is automatically registered when you call `builder.AddArribatecNexus()`.

**In your repository or service class:**

```csharp
using System.Data;
using Dapper;
using Arribatec.Nexus.Client.Services;

public class ItemRepository
{
    private readonly IContextAwareDatabaseService _databaseService;
    private readonly ILogger<ItemRepository> _logger;

    public ItemRepository(
        IContextAwareDatabaseService databaseService,
        ILogger<ItemRepository> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<IEnumerable<Item>> GetAllItemsAsync()
    {
        // Get connection from Nexus Platform - connection string is managed centrally
        using var connection = await _databaseService.CreateProductConnectionAsync();
        
        const string sql = "SELECT * FROM Items ORDER BY CreatedAt DESC";
        return await connection.QueryAsync<Item>(sql);
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        using var connection = await _databaseService.CreateProductConnectionAsync();
        
        const string sql = "SELECT * FROM Items WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Item>(sql, new { Id = id });
    }

    public async Task<int> CreateItemAsync(Item item)
    {
        using var connection = await _databaseService.CreateProductConnectionAsync();
        
        const string sql = @"
            INSERT INTO Items (Name, Description, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, GETUTCDATE(), GETUTCDATE())";
        
        return await connection.QuerySingleAsync<int>(sql, item);
    }

    public async Task UpdateItemAsync(Item item)
    {
        using var connection = await _databaseService.CreateProductConnectionAsync();
        
        const string sql = @"
            UPDATE Items 
            SET Name = @Name, Description = @Description, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";
        
        await connection.ExecuteAsync(sql, item);
    }

    public async Task DeleteItemAsync(int id)
    {
        using var connection = await _databaseService.CreateProductConnectionAsync();
        
        const string sql = "DELETE FROM Items WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
```

**In your controller:**

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IContextAwareDatabaseService _databaseService;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(
        IContextAwareDatabaseService databaseService,
        ILogger<ItemsController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems()
    {
        using var connection = await _databaseService.CreateProductConnectionAsync();
        
        var items = await connection.QueryAsync<Item>("SELECT * FROM Items");
        return Ok(items);
    }
}
```

**Key points:**
- ✅ Always use `CreateProductConnectionAsync()` - never hardcode connection strings
- ✅ Always wrap connections in `using` statements for proper disposal
- ✅ The connection is automatically configured based on your product registration in Nexus Console
- ✅ Works seamlessly in both local development and Docker environments

### Database Connection Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  Your App calls CreateProductConnectionAsync()                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  IContextAwareDatabaseService                                   │
│  - Identifies current product from AddArribatecNexus() config   │
│  - Fetches connection details from Nexus Master API             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Nexus Console (Master API)                                     │
│  - Looks up database connection for your product                │
│  - Returns connection string                                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  SQL Server (sqlserver:1433)                                    │
│  - Your app's database (e.g., SampleDB)                         │
└─────────────────────────────────────────────────────────────────┘
```

### Troubleshooting Database Connections

**"Product database connection not found"**
- Verify your product is registered in Nexus Console
- Check that a database connection is linked to your product
- Ensure `productShortName` in `AddArribatecNexus()` matches the registered product name

**"Cannot connect to SQL Server"**
- Check SQL Server is running: `docker ps | grep sqlserver`
- Test connectivity: `docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Arribatec123Password" -C -Q "SELECT 1"`
- Verify the server name in Nexus Console is `sqlserver,1433` (not localhost)

**"Login failed for user"**
- Verify credentials in Nexus Console database connection settings
- Check the user has access to the specified database

### Using Adminer for Database Management

Adminer provides a web-based interface for managing your databases:

1. Open: `https://adminer.localtest.me`
2. Select: **MS SQL** as the system
3. Enter:
   - Server: `sqlserver`
   - Username: `sa`
   - Password: `Arribatec123Password`
   - Database: `SampleDB` (or leave empty to see all)

## CI/CD with GitHub Actions

This app includes a GitHub Actions workflow that automatically builds and publishes Docker images to GitHub Container Registry (ghcr.io).

### Setup

1. Create a Personal Access Token with `read:packages` and `write:packages` scopes
2. Add it as a repository secret named `GH_PACKAGES_TOKEN`:
   - Go to **Settings → Secrets and variables → Actions**
   - Click **New repository secret**
   - Name: `GH_PACKAGES_TOKEN`
   - Value: Your token

### What It Does

- **On push to main/master**: Builds and publishes the image with version tag
- **On pull requests**: Validates the build without publishing
- **Manual trigger**: Can be run on-demand from GitHub Actions UI

The workflow:
1. Builds the frontend with npm
2. Builds the Docker image
3. Tags with date-based version (`YYYYMMDD.N`) and `latest`
4. Pushes to `ghcr.io/<owner>/<repo>`

For more details, see [.github/workflows/README.md](.github/workflows/README.md)

## Related Documentation

- `../docs/MULTI_TENANT_APP_DEVELOPMENT_GUIDE.md` - Complete development guide
- `../docs/DATABASE_GUIDE.md` - Advanced database setup and management
- `../docs/MIGRATION_GUIDE.md` - Migrating existing apps to the platform
- `../docs/PRIVATE_REGISTRY_AUTH.md` - GitHub Packages authentication
- `../docs/TROUBLESHOOTING.md` - Common issues and solutions
- `.github/workflows/README.md` - CI/CD setup and usage
