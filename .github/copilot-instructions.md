# GitHub Copilot Instructions

This is a **multi-tenant web application** built with React (frontend) and .NET 8 (backend), designed to work with the **Nexus Platform**.

## Architecture Overview

- **Frontend**: React 18 + TypeScript + Vite + Material-UI (MUI) v7.3.4
- **Backend**: .NET 8 Web API with Arribatec.Nexus.Client v6.1.0
- **Authentication**: Keycloak (OAuth2/OIDC) with JWT Bearer tokens (dynamic multi-realm support)
- **Database**: Context-aware connections via Master API (supports SQL Server, PostgreSQL, MySQL, Oracle)
- **Reverse Proxy**: Traefik with automatic SSL
- **Master API**: Nexus Console for tenant/user/database management
- **Logging**: Serilog with Console, File, and Loki (Grafana) sinks

### Backend Architecture Setup

The backend uses **one-line setup** via `Arribatec.Nexus.Client` v6.1.0:

```csharp
using Arribatec.Nexus.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ONE method call configures everything:
// - Serilog logging (Console, File, Loki)
// - Master API client
// - Database connection factory
// - Dynamic JWT Bearer authentication (Keycloak)
// - Authorization
// - CORS
// - Request context middleware
builder.AddArribatecNexus(productShortName: "SampleApp");

builder.Services.AddControllers();

var app = builder.Build();

// CRITICAL: Correct middleware order for v2.3.0
app.UseRequestContext();        // 1. Extract tenant from headers/URL/subdomain
app.UseCors();                  // 2. CORS
app.UseAuthentication();        // 3. Validate JWT token (now dynamic per tenant!)
app.UseNexusContext();          // 4. Enrich context from Master API
app.UseAuthorization();         // 5. Check roles/policies
app.MapControllers();

app.Run();
```

This automatically provides:

- ✅ Multi-tenant authentication via Keycloak
- ✅ **Dynamic JWT validation with multi-realm support** (v2.3.0)
- ✅ JWT token validation (fetches realm config from Master API per tenant)
- ✅ Master API integration for tenant/user operations
- ✅ Automatic context enrichment from Master API
- ✅ Context-aware database connection management
- ✅ CORS configured for localtest.me domains
- ✅ Role-based authorization support
- ✅ Structured logging with Serilog
- ✅ Centralized log aggregation with Loki (optional)

## App-Specific Instructions

> **Important:** This file (`copilot-instructions.md`) contains **Nexus Platform guidelines** and should not be modified for app-specific customizations.

For app-specific rules, conventions, and notes, use:

```
.github/instructions/app.instructions.md
```

**When to add to `app.instructions.md`:**

- App-specific coding conventions
- Architectural decisions
- Business rules specific to this application
- TODOs and notes for future work
- Corrections to Copilot's understanding of your app

## Key Concepts

### Multi-Tenancy

- Users belong to tenants
- Tenants are isolated but share the same database
- Use `TenantId` for data isolation in all database queries
- Never expose data across tenant boundaries

### Path-Based Routing

- App is accessed via `/sfab-gl07-gateway` path (configurable base path)
- Traefik routes based on path prefix
- Frontend uses `base: '/sfab-gl07-gateway/'` in Vite config
- Backend uses `app.UsePathBase("/sfab-gl07-gateway")` in Program.cs
- **API calls**: Frontend MUST call APIs at `https://<realm>.<domain>/<basepath>/api`
  - Example: If app loads from `https://admin.localtest.me/sfab-gl07-gateway/`, call APIs at `https://admin.localtest.me/sfab-gl07-gateway/api`
  - Use relative URLs (`/sfab-gl07-gateway/api/...`) to automatically match the frontend's origin
  - In development: Vite dev server proxies these calls to backend
  - In production: nginx routes these calls to backend container

### Authentication Flow

1. User navigates to app URL
2. Keycloak auth provider redirects to login
3. User logs in (realm: admin or demo)
4. Token issued and stored in browser
5. Frontend includes token in API requests
6. **Backend dynamically validates token** (v2.3.0):
   - Extracts tenant from subdomain/headers
   - Calls Master API to get tenant-specific Keycloak configuration
   - Fetches OIDC configuration from the correct realm
   - Validates JWT signature against tenant's realm keys
   - Supports multiple Keycloak realms per tenant

### Dynamic JWT Validation (v2.3.0)

The backend uses **dynamic JWT validation** that fetches tenant-specific Keycloak configuration from Master API:

**How it works:**

1. Request arrives with JWT token
2. `DynamicJwtBearerHandler` extracts tenant from subdomain (e.g., `admin.localtest.me` → tenant = "admin")
3. Calls Master API: `GET /api/master/keycloak-config?url=admin.localtest.me:7200/api/users`
4. Master API returns tenant's Keycloak authority: `https://keycloak.localtest.me/realms/admin`
5. Fetches OIDC config from: `https://keycloak.localtest.me/realms/admin/.well-known/openid-configuration`
6. Validates JWT token against the realm's signing keys
7. Authentication successful - request continues

**Benefits:**

- ✅ Each tenant can use a different Keycloak realm
- ✅ No static `Keycloak:Authority` configuration needed
- ✅ Automatic tenant detection from subdomain/headers
- ✅ Supports multi-realm multi-tenant scenarios

**Note:** This adds one Master API call per authentication request for dynamic configuration lookup.

## Project Structure

```
sfab-gl07-gateway/
├── frontend/              # React application
│   ├── src/
│   │   ├── App.tsx       # Auth provider & routing
│   │   ├── main.tsx      # Application entry point
│   │   ├── utils/
│   │   │   └── api.ts    # Dynamic API path utilities (buildApiPath, createApiClient)
│   │   ├── components/   # React components
│   │   └── vite-env.d.ts # Vite environment type definitions
│   ├── vite.config.ts    # Vite configuration (sets base path)
│   └── package.json      # Frontend dependencies
├── backend/              # .NET 8 API
│   ├── Program.cs        # API configuration & DI
│   ├── Controllers/      # API endpoints
│   ├── appsettings.json  # Configuration
│   └── *.csproj          # Project file
├── docker-compose.yml    # Docker configuration
├── Dockerfile           # Multi-stage build
└── nginx.conf           # Reverse proxy config
```

## Important Files

### Frontend

- **`App.tsx`**: Keycloak auth provider setup, must wrap all routes
- **`vite.config.ts`**: Base path configuration (exposes as `import.meta.env.BASE_URL`), HTTPS certs, API proxy
- **`src/utils/api.ts`**: Dynamic API path utilities - use `buildApiPath()` or `createApiClient()` instead of hardcoding paths
- **`src/vite-env.d.ts`**: TypeScript definitions for Vite environment variables (includes `BASE_URL`)
- **`package.json`**: Dependencies including `@arribatec-sds/keycloak-auth-react`

### Backend

- **`Program.cs`**:
  - Path base configuration (`UsePathBase`)
  - CORS setup
  - Master API client registration
  - Health checks
- **`appsettings.json`**:
  - Database connection strings
  - Keycloak configuration
  - Master API URL

## Development Guidelines

### Build Verification (IMPORTANT for Copilot)

**After making code changes, always verify the build succeeds:**

1. **Backend (.NET)**: Run `dotnet build` in the `backend/` directory
2. **Frontend (React)**: Run `npm run build` in the `frontend/` directory

If build errors occur, **fix them immediately** before proceeding. Common issues:
- TypeScript type errors in frontend
- Missing imports or unused variables
- C# compilation errors (missing using statements, type mismatches)
- Null reference warnings

```bash
# Backend build verification
cd backend && dotnet build

# Frontend build verification  
cd frontend && npm run build
```

> **Note for Copilot**: When assisting with code changes, proactively run these build commands to catch errors early. Fix any errors before considering the task complete.

### Frontend Best Practices

1. **Always use the auth hook**: `const { user, isAuthenticated, token } = useAuth()`
2. **API calls - Use Dynamic Path Construction**:
   - **RECOMMENDED**: Use `buildApiPath()` or `createApiClient()` from `@/utils/api.ts` to dynamically construct API paths
   - Automatically adapts to any base path (no hardcoding `/sfab-gl07-gateway/api/...`)
   - Works in development (Vite proxy) and production (nginx routing)
   - **ALWAYS include Bearer token**: Every API call MUST include `Authorization: Bearer ${token}` header

   ```typescript
   // BEST: Use pre-configured axios instance (recommended)
   import { createApiClient } from "@/utils/api";
   import { useAuth } from "@arribatec-sds/keycloak-auth-react";

   const { token } = useAuth();
   const apiClient = createApiClient();
   apiClient.defaults.headers.common["Authorization"] = `Bearer ${token}`;

   // All calls automatically use correct base path
   await apiClient.get("/user"); // -> /sfab-gl07-gateway/api/user
   await apiClient.post("/items", data); // -> /sfab-gl07-gateway/api/items

   // GOOD: Build individual paths
   import { buildApiPath } from "@/utils/api";
   await axios.get(buildApiPath("/user"), {
     headers: { Authorization: `Bearer ${token}` },
   });

   // ACCEPTABLE: Legacy pattern (works but less portable)
   await axios.get("/sfab-gl07-gateway/api/user", {
     headers: { Authorization: `Bearer ${token}` },
   });

   // WRONG: Never hardcode backend URLs
   await axios.get("http://localhost:7458/api/user"); // ❌ BAD

   // WRONG: Never call APIs without authentication
   await axios.get("/sfab-gl07-gateway/api/user"); // ❌ Missing Bearer token!
   ```

3. **Routing**: All routes must be under base path (`/sfab-gl07-gateway/`)
4. **Protected routes**: Wrap with auth check or use route guards
5. **TypeScript**: Maintain type safety, define interfaces for API responses
6. **UI Components**: Use Material-UI (MUI) v7.3.4 for all UI components - it's our preferred component library
7. **MUI Grid System**: Use the Grid component from `@mui/material/Grid` - the responsive layout system based on CSS Grid
   - Import: `import Grid from '@mui/material/Grid';`
   - **Note**: In MUI v7, `GridLegacy` is deprecated - always use `Grid`
   - Uses CSS variables and CSS Grid for better performance and flexibility
   - Key features:
     - Use `container` prop to create a grid container
     - Use `size` prop for responsive column sizing: `size={6}` or `size={{ xs: 12, md: 6 }}`
     - Use `size="grow"` for auto-growing columns (replaces legacy `xs` without value)
     - Use `offset` prop for grid offsets: `offset={2}` or `offset={{ md: 2 }}`
     - Use `columns` prop to define grid columns (default: 12)
     - Use `spacing` prop for gaps between grid items
     - No `item` prop needed - all grids are items by default

### Backend Best Practices

1. **Controllers**: Use `[Route("api/[controller]")]` attribute
2. **Route naming**: Use lowercase for all API routes (e.g., `/api/myendpoint`, not `/api/MyEndpoint`)
3. **Authentication**: Use `[Authorize]` attribute on ALL endpoints - every API call requires authentication
4. **Tenant isolation**: Always filter by `TenantId` in database queries
5. **Database access**: Use `IContextAwareDatabaseService` for automatic tenant-aware connections - it handles tenant/product context automatically
6. **LLM access**: Use `IContextAwareLlmService` for automatic context-aware LLM connections - never hardcode API keys or endpoints
7. **Master API**: Inject `IMasterApiClient` for tenant/user operations
8. **Health checks**: Add to `/health` endpoint for monitoring
9. **CORS**: Configure for development (localhost + localtest.me domains)
10. **Logging**: Use `ILogger<T>` - Serilog is configured automatically with Console, File, and Loki sinks

### Common Patterns

#### Database Connections

**🚀 Quick Start - Use the add-database Script**

To add database support to your app, run the `add-database` script from the developer kit:

```bash
# From your app directory (macOS/Linux)
..\..\arribatec-nexus-developer-kit/add-database.sh

# From your app directory (Windows PowerShell)
..\..\arribatec-nexus-developer-kit\add-database.ps1
```

This script automatically:

1. Creates a database and user in SQL Server
2. Registers the database connection in Nexus Console
3. Links the connection to your application
4. Generates a repository class with Dapper
5. Registers the repository in Program.cs
6. Creates a sample `sql/init-tables.sql` script

**📋 Connection Methods**

Use `IContextAwareDatabaseService` for database access:

| Method                           | Use Case                                      |
| -------------------------------- | --------------------------------------------- |
| `CreateProductConnectionAsync()` | **Most common** - Gets the product's database |
| `CreateTenantConnectionAsync()`  | Gets the current tenant's database            |
| `CreateConnectionAsync()`        | Auto-detects from context (tenant or product) |

**🏗️ Using the Generated Repository**

The script generates a repository class (e.g., `MyAppRepository.cs`) that you can inject into controllers:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IMyAppRepository _repository;

    public ItemsController(IMyAppRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repository.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? Ok(item) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
    {
        var id = await _repository.CreateAsync(request.Name, request.Description);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
```

**📝 Custom Database Operations**

For custom queries, inject `IContextAwareDatabaseService` directly:

```csharp
using Arribatec.Nexus.Client.Services;
using Dapper;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IContextAwareDatabaseService _dbService;

    public ReportsController(IContextAwareDatabaseService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        // Use CreateProductConnectionAsync() for product-specific database
        using var connection = await _dbService.CreateProductConnectionAsync();

        var summary = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT COUNT(*) as Total, MAX(CreatedAt) as LastCreated FROM Items"
        );

        return Ok(summary);
    }
}
```

**⚠️ Important Rules**

- **Never hardcode connection strings** - Always use `IContextAwareDatabaseService`
- **Use `CreateProductConnectionAsync()`** for app-specific databases
- **Use `CreateTenantConnectionAsync()`** for tenant-specific databases
- **Always dispose connections** with `using` statements
- **Use parameterized queries** (Dapper handles this automatically)

#### LLM Connections

> **⚠️ CRITICAL: Always Use Nexus LLM Connections**
>
> **NEVER make direct LLM API calls outside of Nexus.** All LLM connections must go through Nexus to ensure:
>
> - Centralized API key management and rotation
> - Tenant/customer access control via Nexus Dashboard
> - Usage tracking and cost allocation per tenant
> - Consistent configuration across environments
>
> The **Arribatec Cognitive API** (`https://cognitiveservices.arribatec.net`) is the **primary and recommended** LLM provider. Other providers (OpenAI, Azure, Anthropic, etc.) are available as alternatives for specific use cases.

**🚀 Quick Start - Use the add-llm-connection Script**

To add LLM (Large Language Model) API support to your app, run the `add-llm-connection` script from the developer kit:

```bash
# From your app directory (macOS/Linux)
..\..\arribatec-nexus-developer-kit/add-llm-connection.sh

# From your app directory (Windows PowerShell)
..\..\arribatec-nexus-developer-kit\add-llm-connection.ps1
```

This script automatically:

1. Registers an LLM connection in Nexus Console
2. Links the connection to your application
3. Updates existing connections if the name already exists (idempotent)

> **💡 If the script fails:** Add the LLM connection manually via the **Nexus Dashboard** (https://admin.localtest.me/nexus-console or your production Nexus Console). This ensures correct configuration and is the recommended approach for production environments.

**Supported Providers:**

| Provider      | Base URL                                  | Notes                                                           |
| ------------- | ----------------------------------------- | --------------------------------------------------------------- |
| **Arribatec** | `https://cognitiveservices.arribatec.net` | **✅ RECOMMENDED** - Arribatec Cognitive API (primary provider) |
| OpenAI        | `https://api.openai.com`                  | Alternative - OpenAI API                                        |
| AzureOpenAI   | (custom per deployment)                   | Alternative - Azure OpenAI Service                              |
| Anthropic     | `https://api.anthropic.com`               | Alternative - Anthropic Claude API                              |
| Ollama        | `http://localhost:11434`                  | Alternative - Self-hosted Ollama (local dev only)               |
| Custom        | (custom)                                  | Alternative - Any OpenAI-compatible API                         |

**🔍 Discovering Available Models**

Available models vary by provider and may change over time. **Do not hardcode model names** - instead:

1. **Use the Nexus Dashboard**: Navigate to your LLM connection settings and use the "Discover Models" feature to see available models
2. **Use the connection's DefaultModel**: When creating a connection, set a `DefaultModel` that will be returned via `connection.DefaultModel`

The Nexus Dashboard validates model availability when you create or edit LLM connections.

**📋 Connection Methods**

Use `IContextAwareLlmService` for LLM access (mirrors `IContextAwareDatabaseService` pattern):

| Method                                             | Use Case                                                         |
| -------------------------------------------------- | ---------------------------------------------------------------- |
| `GetProductLlmConnectionAsync()`                   | **Most common** - Gets the product's first active LLM connection |
| `GetProductLlmConnectionAsync("openai")`           | Gets first active connection from a specific provider            |
| `GetProductLlmConnectionAsync(LlmProvider.OpenAI)` | Gets first active connection from a specific provider (by enum)  |
| `GetTenantLlmConnectionAsync()`                    | Gets the current tenant's first active LLM connection            |
| `GetProductLlmConnectionsAsync()`                  | Gets all LLM connections for the product (for manual filtering)  |
| `GetTenantLlmConnectionsAsync()`                   | Gets all LLM connections for the tenant                          |
| `GetLlmConnectionByIdAsync(id)`                    | Gets a specific connection by ID (includes API key)              |

**🤖 Using the LLM API (Recommended Pattern)**

Inject `IContextAwareLlmService` for automatic context-aware LLM access:

```csharp
using Arribatec.Nexus.Client.Services;
using System.Net.Http.Json;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IContextAwareLlmService _llmService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IContextAwareLlmService llmService,
        IHttpClientFactory httpClientFactory,
        ILogger<AiController> logger)
    {
        _llmService = llmService;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        // Get LLM connection from Nexus (automatically uses product context)
        var connection = await _llmService.GetProductLlmConnectionAsync();
        if (connection == null)
        {
            _logger.LogWarning("No LLM connection configured for product");
            return BadRequest("No LLM connection available");
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {connection.ApiKey}");

        var payload = new
        {
            model = connection.DefaultModel ?? "gpt-5.1-test",
            messages = new[]
            {
                new { role = "user", content = request.Message }
            },
            max_tokens = 1000
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{connection.BaseUrl}/v1/chat/completions",
            payload
        );

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }
}
```

**🖼️ Vision / Image Processing**

Many LLM providers support image analysis through the same chat completions endpoint. Pass images as base64 or URLs:

```csharp
[HttpPost("analyze-image")]
public async Task<IActionResult> AnalyzeImage([FromBody] ImageAnalysisRequest request)
{
    var connection = await _llmService.GetProductLlmConnectionAsync();
    if (connection == null)
        return BadRequest("No LLM connection available");

    _httpClient.DefaultRequestHeaders.Clear();
    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {connection.ApiKey}");

    var payload = new
    {
        model = connection.DefaultModel ?? "gpt-5.1-test",
        messages = new[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = request.Prompt ?? "What's in this image?" },
                    new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = request.ImageUrl // or "data:image/jpeg;base64,{base64data}"
                        }
                    }
                }
            }
        },
        max_tokens = 1000
    };

    var response = await _httpClient.PostAsJsonAsync(
        $"{connection.BaseUrl}/v1/chat/completions",
        payload
    );

    return Ok(await response.Content.ReadFromJsonAsync<JsonElement>());
}
```

**📝 Provider-Specific Headers**

| Provider     | Auth Header                   | Additional Headers              |
| ------------ | ----------------------------- | ------------------------------- |
| OpenAI       | `Authorization: Bearer {key}` | -                               |
| Anthropic    | `X-Api-Key: {key}`            | `anthropic-version: 2023-06-01` |
| Azure OpenAI | `api-key: {key}`              | -                               |
| Ollama       | (none required)               | -                               |
| Arribatec    | `Authorization: Bearer {key}` | -                               |

**⚠️ Important Rules**

- **Never hardcode API keys or base URLs** - Always use `IContextAwareLlmService`
- **Use `GetProductLlmConnectionAsync()`** for app-specific LLM connections
- **Use `GetTenantLlmConnectionAsync()`** for tenant-specific LLM connections
- **Handle null connections** - Check if connection exists before using
- **Use `/v1/` endpoints** - All providers support OpenAI-compatible endpoints
- **Handle rate limits** - Implement retry logic with exponential backoff
- **Log usage** - Track token usage for cost monitoring per tenant

**Why Nexus LLM Connections?**

- Tenant administrators can control which LLM connections are available
- API keys can be rotated without code changes
- Usage is tracked and billed per tenant
- Consistent security and compliance across all applications

**Extracting Tenant and User Information:**

For detailed information on extracting tenant and user context from JWT claims, see the [Tenant and User Extraction Guide](..\..\arribatec-nexus-developer-kit/docs/TENANT_USER_EXTRACTION_GUIDE.md).

Quick reference for extracting from JWT claims:

```csharp
// Extract tenant ID
var tenantId = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst("tenant")?.Value
            ?? User.FindFirst("realm")?.Value
            ?? throw new UnauthorizedAccessException("Tenant ID not found in token");

// Extract user ID
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
          ?? User.FindFirst("sub")?.Value
          ?? User.FindFirst("user_id")?.Value
          ?? throw new UnauthorizedAccessException("User ID not found in token");
```

#### Protected API Endpoint

```csharp
using Arribatec.Nexus.Client.Services;
using Dapper;

[Authorize]
[HttpGet("data")]  // Results in /api/mycontroller/data (lowercase)
public async Task<IActionResult> GetData()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var tenantId = User.FindFirst("tenant_id")?.Value
        ?? throw new UnauthorizedAccessException("Tenant ID not found");

    try
    {
        // Use context-aware database service
        using var connection = await _dbService.CreateConnectionAsync();

        var data = await connection.QueryAsync<MyData>(
            "SELECT * FROM Items WHERE TenantId = @TenantId AND UserId = @UserId",
            new { TenantId = tenantId, UserId = userId }
        );

        return Ok(data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to fetch data for tenant {TenantId}", tenantId);
        return StatusCode(500, "An error occurred while fetching data");
    }
}
```

Note: Use lowercase route names. ASP.NET Core route matching is case-insensitive by default, but lowercase URLs are cleaner and follow REST conventions.

#### Frontend API Call (Dynamic Path Construction)

**Recommended Approach - Use API Utility:**

```typescript
import { createApiClient, buildApiPath } from "@/utils/api";
import { useAuth } from "@arribatec-sds/keycloak-auth-react";

const { token } = useAuth();

// BEST: Use pre-configured axios instance
const apiClient = createApiClient(); // baseURL is automatically set to /sfab-gl07-gateway/api
apiClient.defaults.headers.common["Authorization"] = `Bearer ${token}`;

// All calls work regardless of base path
const response = await apiClient.get("/myendpoint"); // -> /sfab-gl07-gateway/api/myendpoint
const data = await apiClient.post("/items", { name: "New Item" });

// GOOD: Build individual paths dynamically
import axios from "axios";
const response = await axios.get(buildApiPath("/myendpoint"), {
  headers: { Authorization: `Bearer ${token}` },
});
// buildApiPath('/myendpoint') automatically returns '/sfab-gl07-gateway/api/myendpoint'

// ACCEPTABLE: Legacy hardcoded pattern (works but less portable)
const response = await axios.get("/sfab-gl07-gateway/api/myendpoint", {
  headers: { Authorization: `Bearer ${token}` },
});

// WRONG: Never hardcode backend URLs
// const response = await axios.get('http://localhost:7458/api/myendpoint', ...)  // ❌

// WRONG: Never call APIs without authentication
// const response = await axios.get('/sfab-gl07-gateway/api/myendpoint'); // Missing Authorization header!
```

#### Protected Route

```typescript
import { Navigate } from "react-router-dom";
import { useAuth } from "@arribatec-sds/keycloak-auth-react";
import { Box, CircularProgress } from "@mui/material";

function ProtectedPage() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading)
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
      >
        <CircularProgress />
      </Box>
    );
  if (!isAuthenticated) return <Navigate to="/login" />;

  return <div>Protected content</div>;
}
```

#### MUI Grid Layout

```typescript
import Grid from "@mui/material/Grid";
import { Card, CardContent, Typography } from "@mui/material";

function DashboardPage() {
  return (
    <Grid container spacing={3}>
      {/* Full width header */}
      <Grid size={12}>
        <Typography variant="h4">Dashboard</Typography>
      </Grid>

      {/* Responsive cards - 12 cols on xs, 6 on sm, 4 on md+ */}
      <Grid size={{ xs: 12, sm: 6, md: 4 }}>
        <Card>
          <CardContent>
            <Typography variant="h6">Total Users</Typography>
            <Typography variant="h3">1,234</Typography>
          </CardContent>
        </Card>
      </Grid>

      <Grid size={{ xs: 12, sm: 6, md: 4 }}>
        <Card>
          <CardContent>
            <Typography variant="h6">Active Sessions</Typography>
            <Typography variant="h3">456</Typography>
          </CardContent>
        </Card>
      </Grid>

      <Grid size={{ xs: 12, sm: 6, md: 4 }}>
        <Card>
          <CardContent>
            <Typography variant="h6">Revenue</Typography>
            <Typography variant="h3">$12,345</Typography>
          </CardContent>
        </Card>
      </Grid>

      {/* Full width table with offset */}
      <Grid size={10} offset={1}>
        <Card>
          <CardContent>
            <Typography variant="h6">Recent Activity</Typography>
            {/* Table content here */}
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
}

// MUI Grid key features (v7):
// ✅ Import from '@mui/material/Grid' (GridLegacy is deprecated)
// ✅ Use `container` prop for grid containers
// ✅ Use `size` prop: size={6} or size={{ xs: 12, md: 6 }}
// ✅ Use `size="grow"` for auto-growing columns
// ✅ Use `offset` prop for positioning: offset={2} or offset={{ md: 2 }}
// ✅ Use `columns` prop to change grid columns (default: 12)
// ✅ Use `spacing` prop for gaps between items
// ✅ No `item` prop needed - all grids are items by default
// ✅ CSS Grid + CSS variables for better performance
```

## Environment Variables

### Development

- `GITHUB_USERNAME`: For accessing private NuGet packages
- `GITHUB_TOKEN`: GitHub Personal Access Token with `read:packages` scope

### Runtime (Docker)

- `MASTER_API_URL` or `MasterApiUrl`: **REQUIRED** - Nexus Console Master API endpoint (default: http://nexus-console:7833)
- `SECURITY__INTERNALAPITOKEN`: **REQUIRED** - Internal token for service-to-service communication (required for database connection factory)
- ~~`Keycloak__Authority`~~: **NO LONGER NEEDED** (v2.3.0) - Fetched dynamically from Master API per tenant
- ~~`Keycloak__RequireHttpsMetadata`~~: **NO LONGER NEEDED** (v2.3.0) - Determined by Master API
- `ApplicationName`: App name for telemetry/logging
- `LOKI_URL`: Loki server URL for centralized logging (optional, e.g., http://loki:3100)
- `Serilog__MinimumLevel`: Logging level (default: Information)
- `Serilog__EnableFileSink`: Enable file logging (default: true)
- `Serilog__FilePath`: Log file path (default: logs/app-.log)

### Required Configuration (appsettings.json or Environment Variables)

**v2.3.0 - Dynamic JWT Validation (Simplified Configuration):**

```json
{
  "MasterApiUrl": "http://nexus-console:7833",
  "Security": {
    "InternalApiToken": "your-secure-internal-token"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "EnableFileSink": true,
    "FilePath": "logs/app-.log",
    "Loki": {
      "Url": "http://loki:3100",
      "Labels": {
        "tenant": "demo",
        "service": "sample-api"
      }
    }
  }
}
```

## URLs & Endpoints

### Development

- Frontend dev server: `https://admin.localtest.me:7358/sfab-gl07-gateway/`
- Backend API: `http://localhost:7458/api/`
- Swagger: `http://localhost:7458/swagger`

### Production (Docker via Traefik)

- Admin realm: `https://admin.localtest.me/sfab-gl07-gateway/`
- Demo realm: `https://demo.localtest.me/sfab-gl07-gateway/`
- API: Routes through frontend proxy

## API Path Utilities

The app uses **dynamic API path construction** to avoid hardcoded base paths, making code portable across different deployments.

### Available Functions (`src/utils/api.ts`)

```typescript
/**
 * Get the base path of the application from Vite config.
 * Example: '/sfab-gl07-gateway' (derived from vite.config.ts base: '/sfab-gl07-gateway/')
 */
getBasePath(): string

/**
 * Build a complete API path from a relative endpoint.
 * Example: buildApiPath('/user') -> '/sfab-gl07-gateway/api/user'
 */
buildApiPath(endpoint: string): string

/**
 * Get the base URL for API calls.
 * Example: '/sfab-gl07-gateway/api'
 */
getApiBasePath(): string

/**
 * Create an axios instance pre-configured with the correct baseURL.
 * Recommended for applications making multiple API calls.
 */
createApiClient(): AxiosInstance
```

### Usage Examples

**Method 1: Pre-configured Axios Instance (Recommended)**

```typescript
import { createApiClient } from "@/utils/api";
import { useAuth } from "@arribatec-sds/keycloak-auth-react";

function MyComponent() {
  const { token } = useAuth();

  const fetchData = async () => {
    const apiClient = createApiClient();
    apiClient.defaults.headers.common["Authorization"] = `Bearer ${token}`;

    // All calls automatically use correct base path
    const users = await apiClient.get("/user"); // -> /sfab-gl07-gateway/api/user
    const items = await apiClient.get("/items"); // -> /sfab-gl07-gateway/api/items
    const item = await apiClient.post("/items", data); // -> /sfab-gl07-gateway/api/items
  };
}
```

**Method 2: Individual Path Building**

```typescript
import axios from "axios";
import { buildApiPath } from "@/utils/api";
import { useAuth } from "@arribatec-sds/keycloak-auth-react";

function MyComponent() {
  const { token } = useAuth();

  const fetchData = async () => {
    const response = await axios.get(buildApiPath("/user"), {
      headers: { Authorization: `Bearer ${token}` },
    });
    // buildApiPath('/user') -> '/sfab-gl07-gateway/api/user'
  };
}
```

### How It Works

1. **Vite Config** (`vite.config.ts`) sets the base path:

   ```typescript
   export default defineConfig({
     base: "/sfab-gl07-gateway/", // Exposed as import.meta.env.BASE_URL
   });
   ```

2. **API Utility** reads `import.meta.env.BASE_URL` at runtime
3. **Dynamic Construction**: All API paths are built from this base
4. **Portability**: Change base path in one place, works everywhere

### Benefits

✅ **No hardcoded paths** - Change base in `vite.config.ts` only  
✅ **Portable code** - Works for any app name (`/myapp`, `/demo`, etc.)  
✅ **Type-safe** - Full TypeScript support via `vite-env.d.ts`  
✅ **Consistent** - Same pattern across the entire app  
✅ **Easy testing** - Clear separation of concerns

### When to Use Each Method

- **Use `createApiClient()`** when:
  - Making multiple API calls in a component
  - Need consistent headers/interceptors
  - Want centralized error handling
  - Building a larger application (recommended)

- **Use `buildApiPath()`** when:
  - Making one-off API calls
  - Need full control over the request
  - Using custom axios configurations

## CI/CD with GitHub Actions

This app includes a GitHub Actions workflow for automated Docker image builds and publishing to GitHub Container Registry (ghcr.io).

### Setup Requirements

1. **Create a Personal Access Token**:
   - Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
   - Generate new token with scopes:
     - `read:packages` - read npm packages from GitHub Packages
     - `write:packages` - push Docker images to Container Registry
     - `repo` (if repository is private)

2. **Add Repository Secret**:
   - Go to repository Settings → Secrets and variables → Actions
   - Create secret named `GH_PACKAGES_TOKEN` with the token value

### Workflow Behavior

- **Push to main/master**: Builds and publishes with version tag + `latest`
- **Pull requests**: Validates build only (no publish)
- **Manual trigger**: Can be run on-demand from GitHub Actions UI

### Version Tags

- Date-based: `YYYYMMDD.N` (e.g., `20240127.1`)
- Increments `N` for each build on the same day
- Also tagged as `latest` on main/master branch

### Using Published Images

```bash
# Pull from GitHub Container Registry
docker pull ghcr.io/<owner>/<repo>:latest

# Use in docker-compose.yml
services:
  app:
    image: ghcr.io/<owner>/<repo>:latest
```

For detailed setup and troubleshooting, see [.github/workflows/README.md](.github/workflows/README.md)

## Common Tasks

### Add a New API Endpoint

1. Create controller method in `backend/Controllers/`
2. Add `[Authorize]` attribute (required for all endpoints)
3. Use lowercase route names
4. Inject `IContextAwareDatabaseService` for database access (automatic tenant context)
5. Inject `IMasterApiClient` for tenant/user operations from Master API
6. Use `ILogger<T>` for structured logging
7. Handle exceptions and return appropriate status codes
8. Test with Swagger or curl (include Bearer token)

Example:

```csharp
[Authorize]
[HttpGet("items")]
public async Task<IActionResult> GetItems()
{
    try
    {
        using var connection = await _dbService.CreateConnectionAsync();
        var items = await connection.QueryAsync<Item>(
            "SELECT * FROM Items WHERE TenantId = @TenantId",
            new { TenantId = GetTenantIdFromClaims() }
        );
        return Ok(items);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to fetch items");
        return StatusCode(500, "An error occurred");
    }
}
```

### Add a New Frontend Page

1. Create component in `frontend/src/components/`
2. Add route in `App.tsx`
3. Use auth hook for protected pages
4. Make API calls using `buildApiPath()` or `createApiClient()` from `@/utils/api` (recommended)
   - Alternatively, use relative URLs like `/sfab-gl07-gateway/api/...` (legacy pattern)
   - Always include Bearer token in Authorization header

Example:

```typescript
import { createApiClient } from "@/utils/api";
import { useAuth } from "@arribatec-sds/keycloak-auth-react";
import { useEffect, useState } from "react";

function MyNewPage() {
  const { token, isAuthenticated } = useAuth();
  const [data, setData] = useState(null);

  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchData = async () => {
      const apiClient = createApiClient();
      apiClient.defaults.headers.common["Authorization"] = `Bearer ${token}`;

      const response = await apiClient.get("/mydata");
      setData(response.data);
    };

    fetchData();
  }, [isAuthenticated, token]);

  return <div>{/* Render data */}</div>;
}
```

### Database Changes

1. Update SQL schema directly (no migrations)
2. Use tenant isolation (`TenantId` in WHERE clauses)
3. Consider impact on other tenants
4. Test with multiple tenants
5. Use `IContextAwareDatabaseService` for automatic context-aware connections
6. Always use Dapper for parameterized queries to prevent SQL injection

### Testing Database Connectivity

```csharp
[Authorize]
[HttpGet("test-connection")]
public async Task<IActionResult> TestConnection()
{
    try
    {
        using var connection = await _dbService.CreateConnectionAsync();
        await connection.OpenAsync();

        return Ok(new
        {
            Status = "Connected",
            Database = connection.Database,
            ServerVersion = connection.ServerVersion
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Database connection test failed");
        return StatusCode(500, new { Status = "Failed", Error = ex.Message });
    }
}
```

## Background Workers (Task Handlers)

The Nexus Platform supports background workers (task handlers) for scheduled or on-demand processing.

### Worker Types

| Type              | Interface                    | Use Case                                                                     |
| ----------------- | ---------------------------- | ---------------------------------------------------------------------------- |
| **Single-Tenant** | `ITaskHandler<T>`            | Execute for ONE tenant at a time (user sync, reports, cleanup)               |
| **Multi-Tenant**  | `IMultiTenantTaskHandler<T>` | Execute for ALL activated tenants in one run (batch processing, aggregation) |

### Creating a Single-Tenant Worker

#### 1. Create Parameter Class

```csharp
namespace YourApp.Api.Handlers;

/// <summary>
/// Parameters for the sync-users task
/// </summary>
public record SyncUsersParameters
{
    /// <summary>
    /// If true, performs a full sync of all users.
    /// </summary>
    public bool FullSync { get; init; } = false;

    /// <summary>
    /// Optional: limit sync to specific department
    /// </summary>
    public string? Department { get; init; }
}
```

#### 2. Implement Handler

```csharp
using Arribatec.Nexus.Client.TaskExecution;

namespace YourApp.Api.Handlers;

/// <summary>
/// Handler for syncing users from external identity provider.
/// </summary>
[TaskHandler("sync-users",
    Name = "Sync Users",
    Description = "Synchronizes users from external identity provider")]
public class SyncUsersHandler : ITaskHandler<SyncUsersParameters>
{
    private readonly ILogger<SyncUsersHandler> _logger;
    private readonly ITaskContext _context;
    private readonly IYourService _yourService;

    public SyncUsersHandler(
        ILogger<SyncUsersHandler> logger,
        ITaskContext context,
        IYourService yourService)
    {
        _logger = logger;
        _context = context;
        _yourService = yourService;
    }

    public async Task ExecuteAsync(
        SyncUsersParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Logs automatically include tenant_id, task_execution_id, task_code, correlation_id
        _logger.LogInformation(
            "Starting user sync for tenant {TenantShortName} (Mode: {Mode})",
            _context.TenantShortName,
            parameters.FullSync ? "Full" : "Incremental");

        // Access tenant information
        var tenantId = _context.TenantId;
        var tenantShortName = _context.TenantShortName;

        // Get tenant-specific database connection
        var connectionString = await _context.GetDatabaseConnectionAsync(
            DatabaseConnectionType.SINGLE);

        // Perform your business logic
        if (parameters.FullSync)
        {
            await _yourService.PerformFullSyncAsync(tenantId, connectionString, cancellationToken);
        }
        else
        {
            await _yourService.PerformIncrementalSyncAsync(tenantId, connectionString, cancellationToken);
        }

        _logger.LogInformation("User sync completed for tenant {TenantShortName}", _context.TenantShortName);
    }
}
```

#### 3. Available Context Properties (`ITaskContext`)

```csharp
// Tenant Information
Guid TenantId { get; }                        // Tenant GUID
string TenantShortName { get; }               // e.g., "admin", "tenant1"
ActivatedTenantInfo Tenant { get; }           // Full tenant details

// Task Metadata
Guid TaskExecutionId { get; }                 // Unique execution ID
string TaskCode { get; }                      // e.g., "sync-users"
string CorrelationId { get; }                 // For distributed tracing
Guid ApplicationId { get; }                   // Application/Product GUID
Guid ApplicationTaskId { get; }               // Task definition GUID

// Database Connection Helper
Task<string> GetDatabaseConnectionAsync(
    DatabaseConnectionType type = DatabaseConnectionType.SINGLE,
    CancellationToken cancellationToken = default);
```

### Creating a Multi-Tenant Worker

#### 1. Create Parameter Class

```csharp
namespace YourApp.Api.Handlers;

public record SyncAllTenantsParameters
{
    public bool FullSync { get; init; } = false;
    public int MaxConcurrency { get; init; } = 5;
}
```

#### 2. Implement Handler

```csharp
using Arribatec.Nexus.Client.TaskExecution;

namespace YourApp.Api.Handlers;

[TaskHandler("sync-all-tenants",
    Name = "Sync All Tenants",
    Description = "Synchronizes data across all activated tenants")]
public class SyncAllTenantsHandler : IMultiTenantTaskHandler<SyncAllTenantsParameters>
{
    private readonly ILogger<SyncAllTenantsHandler> _logger;
    private readonly IMultiTenantTaskContext _context;
    private readonly IYourService _yourService;

    public SyncAllTenantsHandler(
        ILogger<SyncAllTenantsHandler> logger,
        IMultiTenantTaskContext context,
        IYourService yourService)
    {
        _logger = logger;
        _context = context;
        _yourService = yourService;
    }

    public async Task ExecuteAsync(
        SyncAllTenantsParameters parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting multi-tenant sync (TenantCount: {TenantCount})",
            _context.Tenants.Count);

        if (!_context.Tenants.Any())
        {
            _logger.LogWarning("No tenants to process");
            return;
        }

        var successCount = 0;
        var errorCount = 0;

        foreach (var tenant in _context.Tenants)
        {
            try
            {
                _logger.LogInformation("Processing tenant: {TenantName}", tenant.Name);

                var connectionString = await _context.GetDatabaseConnectionAsync(
                    tenant.TenantId,
                    DatabaseConnectionType.SINGLE,
                    cancellationToken);

                await _yourService.SyncTenantDataAsync(
                    tenant.TenantId, connectionString, parameters.FullSync, cancellationToken);

                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to process tenant: {TenantName}", tenant.Name);
                // Continue processing other tenants
            }
        }

        _logger.LogInformation(
            "Multi-tenant sync completed: {SuccessCount} succeeded, {ErrorCount} failed",
            successCount, errorCount);
    }
}
```

#### 3. Available Context Properties (`IMultiTenantTaskContext`)

```csharp
// Tenant List
IReadOnlyList<ActivatedTenantInfo> Tenants { get; }  // All activated tenants

// Task Metadata
Guid TaskExecutionId { get; }
string TaskCode { get; }
string CorrelationId { get; }
Guid ApplicationId { get; }
Guid ApplicationTaskId { get; }

// Database Connection Helper (per tenant)
Task<string> GetDatabaseConnectionAsync(
    Guid tenantId,
    DatabaseConnectionType type = DatabaseConnectionType.SINGLE,
    CancellationToken cancellationToken = default);
```

### Worker Best Practices

| Practice                | ✅ Good                                               | ❌ Bad                       |
| ----------------------- | ----------------------------------------------------- | ---------------------------- |
| **Task codes**          | `"sync-users"`, `"generate-report"`                   | `"task1"`, `"handler"`       |
| **Parameters**          | Strongly-typed records with defaults                  | `object? Data`               |
| **Logging**             | `_logger.LogInformation("Processing {Count}", count)` | `$"Processing {count}"`      |
| **Cancellation**        | `cancellationToken.ThrowIfCancellationRequested()`    | Ignoring cancellation        |
| **Multi-tenant errors** | Continue processing other tenants                     | Fail entire batch            |
| **Database**            | `_context.GetDatabaseConnectionAsync()`               | Hardcoded connection strings |

### Common Worker Patterns

#### Batch Processing

```csharp
public async Task ExecuteAsync(BatchParameters parameters, CancellationToken cancellationToken)
{
    const int batchSize = 100;
    var offset = 0;

    while (true)
    {
        var batch = await _repository.GetBatchAsync(_context.TenantId, offset, batchSize, cancellationToken);
        if (!batch.Any()) break;

        foreach (var item in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessItemAsync(item, cancellationToken);
        }

        offset += batchSize;
        _logger.LogInformation("Processed {Offset} items", offset);
    }
}
```

#### Progress Reporting

```csharp
var total = items.Count;
var processed = 0;

foreach (var item in items)
{
    await ProcessItemAsync(item, cancellationToken);
    processed++;

    if (processed % 100 == 0)
    {
        _logger.LogInformation("Progress: {Processed}/{Total} ({Percentage:F1}%)",
            processed, total, (processed / (double)total) * 100);
    }
}
```

### Worker Creation Checklist

- [ ] Create strongly-typed parameter class with defaults
- [ ] Add `[TaskHandler("task-code")]` attribute with descriptive code
- [ ] Implement `ITaskHandler<T>` (single) or `IMultiTenantTaskHandler<T>` (multi)
- [ ] Inject `ITaskContext` or `IMultiTenantTaskContext` in constructor
- [ ] Use structured logging with `{Parameters}` syntax
- [ ] Handle cancellation tokens properly
- [ ] Use `try-catch` for error handling (especially in multi-tenant loops)
- [ ] Get database connections via `_context.GetDatabaseConnectionAsync()`
- [ ] Add XML documentation comments

### Troubleshooting Workers

| Problem                  | Solution                                                                                  |
| ------------------------ | ----------------------------------------------------------------------------------------- |
| **Handler not found**    | Verify `[TaskHandler]` attribute, check TaskCode matches DB, ensure class is not abstract |
| **Context is null**      | Verify `app.UseArribatecNexus()` is called, check tenant headers/subdomain                |
| **DB connection failed** | Verify tenant has DB registered, check `DatabaseConnectionType`, ensure app is activated  |

## Security Considerations

- **Never trust client input**: Always validate on backend
- **Tenant isolation**: Every database query MUST filter by TenantId
- **Token validation**: Backend validates all tokens with Keycloak
- **CORS**: Restrict in production, allow in development
- **Secrets**: Never commit tokens or passwords
- **SQL injection**: Use parameterized queries (Dapper does this automatically)

## Testing

### Frontend

```bash
cd frontend
npm run dev  # Start dev server
npm run build  # Production build
```

### Backend

```bash
cd backend
dotnet run  # Start API
dotnet test  # Run tests (if any)
```

### Docker

```bash
docker-compose build
docker-compose up -d
docker logs sfab-gl07-gateway-app -f  # View logs
```

## Troubleshooting

### 401 Unauthorized

- Check token is included in request headers (`Authorization: Bearer ${token}`)
- Verify token hasn't expired
- Confirm Keycloak configuration matches
- Ensure `[Authorize]` attribute is on the controller/action

### 404 Not Found

- Verify base path is correct (`/sfab-gl07-gateway`)
- Check Traefik routing rules
- Ensure nginx.conf has correct proxy settings

### CORS Errors

- Add origin to backend CORS policy
- Check browser DevTools Network tab
- Verify API proxy in vite.config.ts

### Database Connection Issues

- Database connections are now managed by `IContextAwareDatabaseService` or `IDatabaseConnectionFactory`
- No need to configure connection strings in appsettings.json (fetched from Master API)
- Verify `Security.InternalApiToken` is configured for Master API communication
- Check Master API is accessible at `MasterApiUrl`
- Verify tenant/product context is available in request (headers, JWT claims, or URL)
- Use `GetConnectionsForTenantAsync()` to list available databases for debugging

## Useful Commands

```bash
# Frontend
npm install          # Install dependencies
npm run dev         # Start dev server
npm run build       # Build for production
npm run setup:certs # Generate SSL certificates

# Backend
dotnet restore      # Restore NuGet packages
dotnet run         # Run the API
dotnet build       # Compile the project

# Docker
docker-compose up -d              # Start in background
docker-compose down              # Stop and remove
docker-compose logs -f sfab-gl07-gateway-app # Follow logs
docker exec -it sfab-gl07-gateway-app bash  # Shell into container
```

## Nexus Developer Kit

This app was generated from the **Nexus Developer Kit**, which contains platform documentation, shared scripts, and tools.

**Path to developer kit:**

- Relative: `..\..\arribatec-nexus-developer-kit`
- Absolute (at creation): `C:\Users\bku0097\source\repos\Nexus\arribatec-nexus-developer-kit`

> **Note:** The relative path was set when this app was created. If the developer kit has been moved, update the path above.

### ⚠️ Important: Follow Developer Kit Documentation

When implementing features or troubleshooting issues related to the Nexus Platform, **always consult the developer kit documentation first**. The docs contain platform-specific patterns, conventions, and solutions that may differ from general best practices.

**Before writing code:**

1. Check if there's a relevant guide in `..\..\arribatec-nexus-developer-kit/docs/`
2. Check if there's a script that automates the task in `..\..\arribatec-nexus-developer-kit/scripts/`
3. Follow the patterns and conventions documented there

### Nexus Dashboard (GUI Alternative)

Many platform operations can also be performed through the **Nexus Dashboard** web interface at `https://admin.localtest.me/nexus-console/`:

**Dashboard Features:**

- **Getting Started**: Create Tenant, Provision Tenant DB, Provision App DB
- **User Management**: Manage users and their permissions across the platform
- **Tenant Management**: Manage organizational tenants and their configurations
- **App Management**: Manage applications and their configurations
- **Database Connections**: Manage database connections for apps and tenants
- **LLM Connections**: Manage LLM/AI API connections (Arribatec Cognitive API is the primary provider; alternatives include OpenAI, Anthropic, Azure, Ollama)
- **Task Management**: View and manage scheduled background tasks
- **Role Management**: Manage user assignments and roles across tenants
- **Admin Features**: System Settings, User Reports

| Operation            | Script (bash / PowerShell)                                  | Dashboard Location                    |
| -------------------- | ----------------------------------------------------------- | ------------------------------------- |
| Create tenant        | N/A                                                         | Getting Started → Create Tenant       |
| Provision tenant DB  | N/A                                                         | Getting Started → Provision Tenant DB |
| Provision app DB     | `..\..\arribatec-nexus-developer-kit/add-database.sh` / `.ps1`         | Getting Started → Provision App DB    |
| Add LLM connection   | `..\..\arribatec-nexus-developer-kit/add-llm-connection.sh` / `.ps1`   | LLM Connections → Add Connection      |
| Register app         | `..\..\arribatec-nexus-developer-kit/scripts/register-app.sh` / `.ps1` | Apps → App Management → View          |
| Manage tenants       | N/A                                                         | Tenants → Tenant Management → View    |
| View DB connections  | N/A                                                         | Database Connections → View           |
| View LLM connections | N/A                                                         | LLM Connections → View                |
| Manage users         | N/A                                                         | Users → User Management → View        |
| Manage roles         | N/A                                                         | Role Management → Manage              |
| View tasks           | N/A                                                         | Tasks → Task Management → View        |
| System settings      | N/A                                                         | Admin Tools → System Settings         |

**When to use scripts vs. Dashboard:**

- **Scripts**: Automation, CI/CD pipelines, reproducible setup, bulk operations
- **Dashboard**: One-off tasks, visual exploration, debugging, managing existing resources

### User-Facing Scripts (Call Directly)

These scripts are designed to be run directly by developers:

| Script (bash / PowerShell)                                | Purpose                                 | When to Use                      |
| --------------------------------------------------------- | --------------------------------------- | -------------------------------- |
| `..\..\arribatec-nexus-developer-kit/run-nexus.sh` / `.ps1`          | Start/stop the Nexus platform           | Beginning of development session |
| `..\..\arribatec-nexus-developer-kit/add-database.sh` / `.ps1`       | Add database support to your app        | When app needs database access   |
| `..\..\arribatec-nexus-developer-kit/add-llm-connection.sh` / `.ps1` | Add LLM/AI API connection to your app   | When app needs AI capabilities   |
| `..\..\arribatec-nexus-developer-kit/create-nexus-app.sh` / `.ps1`   | Create a new app from template          | Starting a new project           |
| `..\..\arribatec-nexus-developer-kit/migrate-app.sh` / `.ps1`        | Migrate existing app to latest template | After template updates           |

### Internal Scripts (Don't Call Directly)

These scripts are called by other scripts or used internally:

| Script (bash / PowerShell)                                    | Purpose                    | Called By                                      |
| ------------------------------------------------------------- | -------------------------- | ---------------------------------------------- |
| `..\..\arribatec-nexus-developer-kit/scripts/register-app.sh` / `.ps1`   | Register app with platform | `create-nexus-app.sh` / `create-nexus-app.ps1` |
| `..\..\arribatec-nexus-developer-kit/scripts/app-registry.sh` / `.ps1`   | Manage global app registry | `create-nexus-app.sh` / `create-nexus-app.ps1` |
| `..\..\arribatec-nexus-developer-kit/scripts/keycloak-setup.sh` / `.ps1` | Configure Keycloak         | `register-app.sh` / `register-app.ps1`         |
| `..\..\arribatec-nexus-developer-kit/scripts/sqlserver.sh` / `.ps1`      | SQL Server utilities       | `add-database.sh` / `add-database.ps1`         |

### Key Documentation

**Always read these docs before implementing related features:**

| Topic                  | Document                                                              | Read Before                     |
| ---------------------- | --------------------------------------------------------------------- | ------------------------------- |
| Database access        | `..\..\arribatec-nexus-developer-kit/docs/DATABASE_GUIDE.md`                     | Adding any database code        |
| Multi-tenancy          | `..\..\arribatec-nexus-developer-kit/docs/MULTI_TENANT_APP_DEVELOPMENT_GUIDE.md` | Any tenant-related code         |
| User/tenant extraction | `..\..\arribatec-nexus-developer-kit/docs/TENANT_USER_EXTRACTION_GUIDE.md`       | Accessing user/tenant info      |
| SQL queries            | `..\..\arribatec-nexus-developer-kit/docs/SQL_SERVER_GUIDE.md`                   | Writing Dapper queries          |
| Troubleshooting        | `..\..\arribatec-nexus-developer-kit/docs/TROUBLESHOOTING.md`                    | Debugging any issue             |
| Integration            | `..\..\arribatec-nexus-developer-kit/docs/INTEGRATION_GUIDE.md`                  | Connecting to platform services |
| CI/CD setup            | `.github/workflows/README.md`                                         | Setting up GitHub Actions       |
| Keycloak Auth React    | https://github.com/Arribatec-SDS/keycloak-auth-react                  | Frontend authentication setup   |

### Platform Configuration

- `..\..\arribatec-nexus-developer-kit/docker/` - Docker Compose files for platform services
- `..\..\arribatec-nexus-developer-kit/docker/docker-compose.nexus.yml` - Main platform services

### Adding Developer Kit to Copilot Context

If GitHub Copilot cannot access the developer kit documentation (typically located one folder up from your app), you need to add it to the context:

**VS Code:**

- Click the **Add context** button above the chat input to attach the developer kit folder
- Or use **File → Add Folder to Workspace** to load the developer kit folder into your workspace
- Copilot may also prompt you for permission to access additional folders when needed - approve these requests to enable full context access

**Copilot CLI:**

- Use `/add-dir ..\..\arribatec-nexus-developer-kit` to add the developer kit to context

> **Tip:** Adding the developer kit ensures Copilot has access to platform-specific patterns, scripts, and documentation for accurate assistance.

---

## Saving App-Specific Instructions

**For Copilot:** When assisting developers, proactively offer to save important information to `.github/instructions/app.instructions.md` in these situations:

| Trigger                                   | Example                                             | Action                                                                     |
| ----------------------------------------- | --------------------------------------------------- | -------------------------------------------------------------------------- |
| **User explicitly asks to save/remember** | "Save this", "Remember this", "Note this for later" | Always save to `app.instructions.md`                                       |
| **Architectural decision made**           | Choosing a specific library, pattern, or approach   | Offer: "Would you like me to save this decision to your app instructions?" |
| **App-specific convention established**   | Naming conventions, file organization, coding style | Offer to document it                                                       |
| **Correction to Copilot's understanding** | "No, in this app we do X instead of Y"              | Offer: "Should I add this to your app instructions so I remember?"         |
| **Production TODO identified**            | "We'll need to change this before going live"       | Offer to add as a TODO note                                                |
| **Business rule explained**               | Domain-specific logic that affects code             | Offer to document for future reference                                     |

**Format for saving:**

```markdown
## [Category]

- [Concise instruction or note]
```

---

**Remember**: This app is part of a larger multi-tenant platform. Always consider tenant isolation, security, and the shared infrastructure when making changes.
