# Future Development

This document tracks planned features and improvements for the GL07 Gateway application.

---

## Planned Features

### 1. Task Execution Log Download

**Priority:** Medium  
**Status:** Not Started  
**Added:** 2026-02-03

**Description:**  
Add the ability to download task execution logs directly from the application instead of requiring users to access Nexus Console.

**Current State:**
- Task execution logs are visible in Nexus Console under task executions
- Logs include detailed information: file processing, Unit4 API calls, errors, timing
- Logs are enriched with `task_execution_id`, `tenant_id`, `correlation_id` for traceability

**Implementation Plan:**

1. **Backend** - Add endpoint in `WorkerController.cs`:
   ```csharp
   [HttpGet("logs/{taskExecutionId}")]
   [Authorize]
   public async Task<IActionResult> DownloadTaskLogs(Guid taskExecutionId)
   {
       // Call Master API: GET /api/task-executions/{id}/logs
       // Return as downloadable text file
   }
   ```

2. **Frontend** - Add to `RunWorkerPage.tsx`:
   - "Download Logs" button in the Status Panel
   - Show button when execution is complete (success or failed)
   - Trigger browser file download with filename: `gl07-execution-{taskExecutionId}.log`

3. **Master API Verification:**
   - Confirm endpoint exists: `GET /api/task-executions/{taskExecutionId}/logs`
   - Alternative: Query Loki directly by `task_execution_id` label

**UI Mockup:**
```
┌─────────────────────────────────────────────────────┐
│ Status Panel                                        │
├─────────────────────────────────────────────────────┤
│ Task ID: f948a235-8c80-4189-bd17-0207febe5d02      │
│ Status: ✓ Completed                                 │
│ Duration: 2845ms                                    │
│                                                     │
│ [Download Logs]  [Run Again]                        │
└─────────────────────────────────────────────────────┘
```

---

### 2. Database Connection Caching

**Priority:** Low  
**Status:** ✅ Completed  
**Added:** 2026-02-03  
**Completed:** 2026-02-03

**Description:**  
Reduce redundant Master API calls by caching database connection strings within the worker execution scope.

**Previous Behavior:**
- Every repository method called `CreateProductConnectionAsync()`
- Each call made 2 Master API requests:
  1. `GET /api/nexus/product/{shortName}/connections`
  2. `GET /api/nexus/database-connections/{id}/secure`
- Processing 1 file resulted in 10+ database connection lookups

**Solution Implemented:**
Created `IScopedDbConnectionProvider` - a scoped service that caches the database connection for the lifetime of the DI scope (HTTP request or task execution).

**Files Changed:**
- `Services/ScopedDbConnectionProvider.cs` - New service with thread-safe lazy initialization
- `Program.cs` - Registered `IScopedDbConnectionProvider` as scoped
- `Repositories/AppSettingsRepository.cs` - Uses cached connection
- `Repositories/SourceSystemRepository.cs` - Uses cached connection
- `Repositories/ProcessingLogRepository.cs` - Uses cached connection
- `Repositories/Gl07ReportSetupRepository.cs` - Uses cached connection
- `Services/DatabaseInitializer.cs` - Uses cached connection

**Impact:**
- Master API calls reduced from N per request/task to **1 per request/task**
- Connection automatically disposed at end of scope via `IAsyncDisposable`
- Thread-safe via `SemaphoreSlim` double-check pattern

---

## Completed Features

### 1. Database Connection Caching (2026-02-03)

Implemented `IScopedDbConnectionProvider` to cache database connections within request/task scope, reducing Master API traffic by ~90%.

---

## Notes

- Features should be discussed with the team before implementation
- Check Nexus Console/Master API documentation for available endpoints
- Consider impact on multi-tenant scenarios when implementing caching
