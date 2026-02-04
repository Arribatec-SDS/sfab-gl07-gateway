# Future Development

This document tracks planned features and improvements for the GL07 Gateway application.

---

## Planned Features

### 1. Task Execution Log Download

**Priority:** Medium  
**Status:** ✅ Completed  
**Added:** 2026-02-03  
**Completed:** 2026-02-04

**Description:**  
Add the ability to download task execution logs directly from the Processing Logs page.

**Implementation:**

1. **Backend** - Added endpoint in `WorkerController.cs`:
   - `GET /api/worker/logs/{taskExecutionId}` - Downloads execution logs as plain text
   - Calls Master API: `GET /api/task-executions/{id}/logs`
   - Parses JSON response and formats as human-readable text with timestamps

2. **Model Changes** - `ProcessingLog.cs`:
   - Added `TaskExecutionId` (Guid?) to link processing logs to Nexus task executions
   - Updated DTO and repository queries

3. **Database** - `init-tables.sql`:
   - Added `TaskExecutionId UNIQUEIDENTIFIER NULL` column with index
   - Migration script for existing tables

4. **Frontend** - `ProcessingLogsPage.tsx`:
   - Download button (📥) next to each log entry with TaskExecutionId
   - Downloads as `log_YYYY-MM-DD_HH-MM-SS.log`
   - Tooltip shows "Download execution log"

5. **Worker Enhancement** - `GL07ProcessingWorker.cs`:
   - Sets `TaskExecutionId = _context.TaskExecutionId` on log entries
   - Creates log entry even when no files to process
   - Logs GL07 configuration details (Report Setup, Report ID, Company, etc.)

6. **Log Cleanup** - New `LogCleanupWorker.cs`:
   - Task handler with code `log-cleanup`
   - Reads `LogRetention:Days` from settings (default 90, minimum 7)
   - Deletes logs older than retention period

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
cd- `Repositories/SourceSystemRepository.cs` - Uses cached connection
- `Repositories/ProcessingLogRepository.cs` - Uses cached connection
- `Repositories/Gl07ReportSetupRepository.cs` - Uses cached connection
- `Services/DatabaseInitializer.cs` - Uses cached connection

**Impact:**
- Master API calls reduced from N per request/task to **1 per request/task**
- Connection automatically disposed at end of scope via `IAsyncDisposable`
- Thread-safe via `SemaphoreSlim` double-check pattern

---

## Completed Features

### 1. Task Execution Log Download (2026-02-04)

Implemented log download from Processing Logs page with TaskExecutionId linking, text-formatted logs, and LogCleanupWorker for retention management.

### 2. Database Connection Caching (2026-02-03)

Implemented `IScopedDbConnectionProvider` to cache database connections within request/task scope, reducing Master API traffic by ~90%.

---

## Notes

- Features should be discussed with the team before implementation
- Check Nexus Console/Master API documentation for available endpoints
- Consider impact on multi-tenant scenarios when implementing caching
