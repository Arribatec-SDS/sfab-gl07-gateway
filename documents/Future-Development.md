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
**Status:** Not Started  
**Added:** 2026-02-03

**Description:**  
Reduce redundant Master API calls by caching database connection strings within the worker execution scope.

**Current Behavior:**
- Every repository method calls `CreateProductConnectionAsync()`
- Each call makes 2 Master API requests:
  1. `GET /api/nexus/product/{shortName}/connections`
  2. `GET /api/nexus/database-connections/{id}/secure`
- Processing 1 file can result in 10+ database connection lookups

**Impact:**
- Adds ~50ms overhead per operation
- Noisy logs during task execution
- Unnecessary network traffic

**Potential Solutions:**
1. Cache connection string at worker level for duration of task execution
2. Pass shared `IDbConnection` through method parameters
3. Implement scoped connection service with lazy initialization

**Note:** This is by design in the Nexus client library to ensure fresh credentials. Caching should respect token expiration.

---

## Completed Features

_No items yet._

---

## Notes

- Features should be discussed with the team before implementation
- Check Nexus Console/Master API documentation for available endpoints
- Consider impact on multi-tenant scenarios when implementing caching
