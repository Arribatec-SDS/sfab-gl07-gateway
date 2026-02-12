# GL07 Gateway Implementation Plan

**Date:** January 30, 2026  
**Project:** SFAB GL07 Gateway  
**Purpose:** XML-to-Unit4 REST API transformation with background worker

---

## Overview

Create a complete solution with:
1. Database-stored settings with Data Protection API encryption
2. Admin-only frontend settings page in Admin section
3. Background worker for XML→Unit4 transformation
4. File management with date-prefixed naming
5. **Multi-source system support** - fetch files from different folders (one per source system)
6. **Pluggable transformation architecture** - support different file formats with extensible transformers

Role-based access restricts settings to admin users only.

### Multi-Source System Design

The system must support fetching files from **multiple source folders**, where each folder represents a different external system:

```
Azure Blob Container: gl07-files/
├── system-a/              ← Source System A (ABWTransaction XML)
│   ├── inbox/
│   ├── done/
│   └── error/
├── system-b/              ← Source System B (ABWTransaction XML)
│   ├── inbox/
│   ├── done/
│   └── error/
├── system-c/              ← Source System C (Future: different format)
│   ├── inbox/
│   ├── done/
│   └── error/
```

**Key Requirements:**
- Each source system has its own inbox/done/error folder structure
- Most systems use the same ABWTransaction XML format
- Future systems may use different file formats requiring different transformations
- Transformation logic must be pluggable (Strategy Pattern)
- Source system configuration stored in database

---

## Architecture

### Data Flow

```
Azure Blob Storage                    Backend Worker                      Unit4 API
                                  ┌─────────────────────┐     
┌─────────────────┐               │   GL07Processing    │           ┌───────────────┐
│  system-a/      │               │       Worker        │           │  /v1/         │
│   └─ inbox/*.xml│ ──┐           │                     │    ┌─────►│  financial-   │
├─────────────────┤   │           │  1. Get Sources     │    │      │  transaction- │
│  system-b/      │   ├──fetch───►│  2. For each source:│────┘      │  batch        │
│   └─ inbox/*.xml│   │           │     a. List files   │           └───────────────┘
├─────────────────┤   │           │     b. Select       │                  │
│  system-c/      │ ──┘           │        transformer  │            OAuth2 Token
│   └─ inbox/*.xml│               │     c. Parse        │                  │
└─────────────────┘               │     d. Transform    │          ┌───────▼───────┐
                                  │     e. POST to API  │          │ Unit4 Auth    │
┌─────────────────┐               │  3. Move files      │          │ Server        │
│  system-a/done/ │ ◄─success────│                     │          └───────────────┘
│  system-b/done/ │               └─────────────────────┘
│  system-c/done/ │                        │
├─────────────────┤                        │
│  system-a/error/│ ◄─failure──────────────┘
│  system-b/error/│
│  system-c/error/│
└─────────────────┘

                           Transformer Selection (Strategy Pattern)
                           ┌─────────────────────────────────────────┐
                           │  ITransformationService                 │
                           │  ├─ ABWTransactionTransformer (default) │
                           │  ├─ CustomFormatATransformer (future)   │
                           │  └─ CustomFormatBTransformer (future)   │
                           └─────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 18 + TypeScript + MUI v7 |
| Backend | .NET 8 Web API |
| Database | SQL Server (via Nexus) |
| Storage (Production) | Azure Blob Storage |
| Storage (Development) | Local File System |
| Auth | Keycloak (OAuth2/OIDC) |
| Encryption | ASP.NET Core Data Protection API |

---

## Implementation Steps

### Step 1: Create Folder Structure

**Backend:**
- `Models/Xml/` - XML deserialization models
- `Models/Unit4/` - Unit4 API DTOs
- `Models/Settings/` - Settings entity
- `Services/` - Business logic services
- `Repositories/` - Data access layer
- `sql/` - Database scripts

**Frontend:**
- `src/pages/admin/` - Admin section pages
- `src/components/settings/` - Settings UI components
- `src/components/layout/` - Admin layout

### Step 2: Create Database Schema

**File:** `backend/sql/init-tables.sql`

```sql
-- Settings table
CREATE TABLE AppSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ParamName NVARCHAR(100) NOT NULL UNIQUE,
    ParamValue NVARCHAR(MAX),
    Sensitive BIT NOT NULL DEFAULT 0,
    Category NVARCHAR(50) NOT NULL DEFAULT 'General',
    Description NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Index for fast lookups
CREATE INDEX IX_AppSettings_ParamName ON AppSettings(ParamName);
CREATE INDEX IX_AppSettings_Category ON AppSettings(Category);

-- Source systems table (one folder per system)
CREATE TABLE SourceSystems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SystemCode NVARCHAR(50) NOT NULL UNIQUE,      -- e.g., 'system-a', 'bookkeeping'
    SystemName NVARCHAR(100) NOT NULL,            -- e.g., 'System A', 'Bookkeeping System'
    FolderPath NVARCHAR(255) NOT NULL,            -- e.g., 'system-a' (relative to container)
    TransformerType NVARCHAR(100) NOT NULL        -- e.g., 'ABWTransaction', 'CustomFormatA'
        DEFAULT 'ABWTransaction',
    FilePattern NVARCHAR(100) NOT NULL            -- e.g., '*.xml', '*.csv'
        DEFAULT '*.xml',
    IsActive BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Index for active systems lookup
CREATE INDEX IX_SourceSystems_IsActive ON SourceSystems(IsActive);
CREATE INDEX IX_SourceSystems_SystemCode ON SourceSystems(SystemCode);

-- Processing log for reports (includes source system)
CREATE TABLE ProcessingLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SourceSystemId INT NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    Status NVARCHAR(20) NOT NULL, -- 'Success', 'Error', 'Processing'
    VoucherCount INT,
    TransactionCount INT,
    ErrorMessage NVARCHAR(MAX),
    ProcessedAt DATETIME2 DEFAULT GETUTCDATE(),
    DurationMs INT,
    CONSTRAINT FK_ProcessingLog_SourceSystem 
        FOREIGN KEY (SourceSystemId) REFERENCES SourceSystems(Id)
);

-- Index for reporting queries
CREATE INDEX IX_ProcessingLog_Status ON ProcessingLog(Status);
CREATE INDEX IX_ProcessingLog_ProcessedAt ON ProcessingLog(ProcessedAt);
CREATE INDEX IX_ProcessingLog_SourceSystemId ON ProcessingLog(SourceSystemId);

-- Seed settings data
INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
('Unit4:BaseUrl', 'https://no01-npe.erpx-api.unit4cloud.com', 0, 'Unit4', 'Unit4 API base URL'),
('Unit4:TokenUrl', 'https://s-no-ids1-preview.unit4cloud.com/identity/connect/token', 0, 'Unit4', 'OAuth2 token endpoint'),
('Unit4:ClientId', '', 0, 'Unit4', 'OAuth2 client ID'),
('Unit4:ClientSecret', '', 1, 'Unit4', 'OAuth2 client secret (encrypted)'),
('Unit4:Scope', 'api', 0, 'Unit4', 'OAuth2 scope'),
('Unit4:GrantType', 'client_credentials', 0, 'Unit4', 'OAuth2 grant type'),
('AzureStorage:ConnectionString', '', 1, 'AzureStorage', 'Azure Blob connection string (encrypted)'),
('AzureStorage:ContainerName', 'gl07-files', 0, 'AzureStorage', 'Blob container name');
-- Note: InboxFolder, DoneFolder, ErrorFolder removed - now per-system in SourceSystems table

-- Seed source systems (examples)
INSERT INTO SourceSystems (SystemCode, SystemName, FolderPath, TransformerType, FilePattern, Description) VALUES
('bookkeeping', 'Bookkeeping System', 'bookkeeping', 'ABWTransaction', '*.xml', 'Main bookkeeping system - ABWTransaction XML format'),
('payroll', 'Payroll System', 'payroll', 'ABWTransaction', '*.xml', 'Payroll transactions - ABWTransaction XML format');
-- Add more systems as needed, with different TransformerType for different formats
```

### Step 3: Create Settings Backend

**Models/Settings/AppSetting.cs**
```csharp
public class AppSetting
{
    public int Id { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public string? ParamValue { get; set; }
    public bool Sensitive { get; set; }
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Repositories/IAppSettingsRepository.cs**
```csharp
public interface IAppSettingsRepository
{
    Task<IEnumerable<AppSetting>> GetAllAsync();
    Task<AppSetting?> GetByNameAsync(string paramName);
    Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category);
    Task UpdateAsync(string paramName, string? paramValue);
    Task UpsertAsync(AppSetting setting);
}
```

**Services/IAppSettingsService.cs**
```csharp
public interface IAppSettingsService
{
    Task<IEnumerable<AppSettingDto>> GetAllAsync(bool maskSensitive = true);
    Task<string?> GetValueAsync(string paramName, bool decrypt = true);
    Task SetValueAsync(string paramName, string? value);
    Task<T> GetSettingsGroupAsync<T>(string category) where T : new();
}
```

### Step 4: Create Settings API Controller

**Controllers/SettingsController.cs**

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/settings` | GET | List all settings (sensitive masked) |
| `/api/settings/{name}` | GET | Get single setting |
| `/api/settings/{name}` | PUT | Update setting value |

All endpoints require `[Authorize(Roles = "admin")]`

### Step 5: Create Admin Layout Component

**frontend/src/components/layout/AdminLayout.tsx**

Sidebar navigation with:
- ⚙️ Settings
- 📋 Logs (future)
- 📊 Reports (future)

### Step 6: Create Settings Frontend Page

**frontend/src/pages/admin/SettingsPage.tsx**

Features:
- Accordion groups by category (Unit4, AzureStorage)
- Inline editing with MUI TextField
- Password field for sensitive values
- Save button per setting
- Success/error notifications

### Step 7: Add Admin Route Guard

**frontend/src/components/auth/AdminRoute.tsx**

```typescript
// Checks user.roles.includes('admin')
// Redirects to home if not admin
// Shows loading state while checking
```

**App.tsx routes:**
```typescript
<Route path="/admin" element={<AdminRoute><AdminLayout /></AdminRoute>}>
  <Route path="settings" element={<SettingsPage />} />
</Route>
```

### Step 8: Define XML Models

Based on ABWTransaction.xsd and ABWSchemaLib.xsd:

| File | Description |
|------|-------------|
| `ABWTransaction.cs` | Root element with Interface, BatchId, Vouchers |
| `Voucher.cs` | VoucherNo, VoucherType, CompanyCode, Period, VoucherDate, Transactions |
| `Transaction.cs` | TransType, Description, Status, TransDate, Amounts, GLAnalysis, ApArInfo |
| `Amounts.cs` | DcFlag, Amount, CurrAmount, Number1, Value1-3 |
| `GLAnalysis.cs` | Account, Dim1-7, Currency, TaxCode, TaxSystem |
| `ApArInfo.cs` | ApArType, ApArNo, InvoiceNo, DueDate, PayMethod, SundryInfo |
| `TaxTransInfo.cs` | Account2, BaseAmount, BaseCurr |

XML Namespace: `http://services.agresso.com/schema/ABWTransaction/2011/11/14`

### Step 9: Define Unit4 DTOs

Based on Unit4 REST API JSON structure:

| File | Description |
|------|-------------|
| `Unit4TransactionBatchRequest.cs` | Array wrapper for batch POST |
| `BatchInformation.cs` | interface, batchId |
| `TransactionInformation.cs` | companyId, period, transactionDate, transactionType, invoice, transactionDetailInformation |
| `Invoice.cs` | customerOrSupplierId, ledgerType, invoiceNumber, dueDate, paymentMethod |
| `TransactionDetailInformation.cs` | sequenceNumber, lineType, description, accountingInformation, amounts |
| `AccountingInformation.cs` | account, accountingDimension1-7 |
| `AmountsDto.cs` | debitCreditFlag, amount, currencyAmount, currencyCode |
| `TaxInformation.cs` | taxCode, taxSystem, taxDetails |
| `AdditionalInformation.cs` | distributionKey, periodNumber, sequenceReference |

### Step 10: Create File Source Service

**Services/IFileSourceService.cs** (Interface)
```csharp
public interface IFileSourceService
{
    /// <summary>
    /// List files in a source system's inbox folder
    /// </summary>
    Task<IEnumerable<string>> ListFilesAsync(SourceSystem sourceSystem);
    
    /// <summary>
    /// Download file content as string
    /// </summary>
    Task<string> DownloadAsStringAsync(SourceSystem sourceSystem, string fileName);
    
    /// <summary>
    /// Move file to the source system's done folder with date prefix
    /// </summary>
    Task MoveToProcessedAsync(SourceSystem sourceSystem, string fileName);
    
    /// <summary>
    /// Move file to the source system's error folder with date prefix
    /// </summary>
    Task MoveToErrorAsync(SourceSystem sourceSystem, string fileName);
}
```

**Two Implementations:**

| Implementation | Environment | Description |
|----------------|-------------|-------------|
| `LocalFileSourceService` | Development | Reads from local file system |
| `AzureBlobFileSourceService` | Production | Reads from Azure Blob Storage |

**Services/LocalFileSourceService.cs** (Development)
```csharp
public class LocalFileSourceService : IFileSourceService
{
    private readonly string _basePath;
    
    public LocalFileSourceService(IAppSettingsService settings)
    {
        _basePath = settings.GetValueAsync("FileSource:LocalBasePath").Result 
            ?? "C:/dev/gl07-files";
    }
    
    public async Task<IEnumerable<string>> ListFilesAsync(SourceSystem sourceSystem)
    {
        var inboxPath = Path.Combine(_basePath, sourceSystem.FolderPath, "inbox");
        if (!Directory.Exists(inboxPath))
            return Enumerable.Empty<string>();
            
        return Directory.GetFiles(inboxPath, sourceSystem.FilePattern)
            .Select(Path.GetFileName)!;
    }
    
    public async Task<string> DownloadAsStringAsync(SourceSystem sourceSystem, string fileName)
    {
        var filePath = Path.Combine(_basePath, sourceSystem.FolderPath, "inbox", fileName);
        return await File.ReadAllTextAsync(filePath);
    }
    
    public async Task MoveToProcessedAsync(SourceSystem sourceSystem, string fileName)
    {
        var source = Path.Combine(_basePath, sourceSystem.FolderPath, "inbox", fileName);
        var destFolder = Path.Combine(_basePath, sourceSystem.FolderPath, "done");
        Directory.CreateDirectory(destFolder);
        var dest = Path.Combine(destFolder, $"{DateTime.UtcNow:yyyy-MM-dd}_{fileName}");
        File.Move(source, dest);
    }
    
    public async Task MoveToErrorAsync(SourceSystem sourceSystem, string fileName)
    {
        var source = Path.Combine(_basePath, sourceSystem.FolderPath, "inbox", fileName);
        var destFolder = Path.Combine(_basePath, sourceSystem.FolderPath, "error");
        Directory.CreateDirectory(destFolder);
        var dest = Path.Combine(destFolder, $"{DateTime.UtcNow:yyyy-MM-dd}_{fileName}");
        File.Move(source, dest);
    }
}
```

**Provider Selection in Program.cs:**
```csharp
// File source provider (based on configuration)
var fileSourceProvider = builder.Configuration["FileSource:Provider"] ?? "Local";
if (fileSourceProvider == "AzureBlob")
{
    builder.Services.AddScoped<IFileSourceService, AzureBlobFileSourceService>();
}
else
{
    builder.Services.AddScoped<IFileSourceService, LocalFileSourceService>();
}
```

**Folder Structure (same for both providers):**
```
{basePath}/
└── {sourceSystem.FolderPath}/
    ├── inbox/     ← Files to process
    ├── done/      ← Successfully processed
    └── error/     ← Failed processing
```

**File Naming:**
- Success: `bookkeeping/inbox/invoice.xml` → `bookkeeping/done/2026-01-30_invoice.xml`
- Error: `bookkeeping/inbox/invoice.xml` → `bookkeeping/error/2026-01-30_invoice.xml`

### Step 11: Create XML Parser Service

**Services/IXmlParserService.cs**
```csharp
public interface IXmlParserService
{
    ABWTransaction Parse(string xmlContent);
    ABWTransaction ParseFile(Stream fileStream);
}
```

Uses `XmlSerializer` with namespace handling.

### Step 12: Create Transformation Service (Strategy Pattern)

Use **Strategy Pattern** to support different file formats with pluggable transformers.

**Services/ITransformationService.cs** (Base Interface)
```csharp
public interface ITransformationService
{
    /// <summary>
    /// Transformer type identifier (matches SourceSystem.TransformerType)
    /// </summary>
    string TransformerType { get; }
    
    /// <summary>
    /// Transform file content to Unit4 transaction batch
    /// </summary>
    IEnumerable<Unit4TransactionBatchRequest> Transform(string fileContent);
    
    /// <summary>
    /// Validate if file content is valid for this transformer
    /// </summary>
    bool CanHandle(string fileContent);
}
```

**Services/ABWTransactionTransformer.cs** (Default Implementation)
```csharp
public class ABWTransactionTransformer : ITransformationService
{
    public string TransformerType => "ABWTransaction";
    
    private readonly IXmlParserService _xmlParser;
    
    public ABWTransactionTransformer(IXmlParserService xmlParser)
    {
        _xmlParser = xmlParser;
    }
    
    public IEnumerable<Unit4TransactionBatchRequest> Transform(string fileContent)
    {
        var abwTransaction = _xmlParser.Parse(fileContent);
        // ... mapping logic ...
    }
    
    public bool CanHandle(string fileContent)
    {
        return fileContent.Contains("ABWTransaction") && 
               fileContent.Contains("http://services.agresso.com/schema/ABWTransaction");
    }
}
```

**Services/ITransformationServiceFactory.cs** (Factory for selecting transformer)
```csharp
public interface ITransformationServiceFactory
{
    /// <summary>
    /// Get transformer by type identifier
    /// </summary>
    ITransformationService GetTransformer(string transformerType);
    
    /// <summary>
    /// Get all registered transformer types
    /// </summary>
    IEnumerable<string> GetAvailableTransformerTypes();
}
```

**Services/TransformationServiceFactory.cs**
```csharp
public class TransformationServiceFactory : ITransformationServiceFactory
{
    private readonly IEnumerable<ITransformationService> _transformers;
    
    public TransformationServiceFactory(IEnumerable<ITransformationService> transformers)
    {
        _transformers = transformers;
    }
    
    public ITransformationService GetTransformer(string transformerType)
    {
        return _transformers.FirstOrDefault(t => t.TransformerType == transformerType)
            ?? throw new NotSupportedException($"Transformer '{transformerType}' not found");
    }
    
    public IEnumerable<string> GetAvailableTransformerTypes()
    {
        return _transformers.Select(t => t.TransformerType);
    }
}
```

**Adding New Transformers (Future):**
```csharp
// Example: Custom CSV transformer
public class CustomCsvTransformer : ITransformationService
{
    public string TransformerType => "CustomCSV";
    
    public IEnumerable<Unit4TransactionBatchRequest> Transform(string fileContent)
    {
        // Parse CSV and map to Unit4 format
    }
    
    public bool CanHandle(string fileContent) => /* CSV detection logic */;
}

// Register in Program.cs:
builder.Services.AddScoped<ITransformationService, CustomCsvTransformer>();
```

**Key Field Mappings:**

| XML (ABWTransaction) | Unit4 JSON |
|---------------------|------------|
| `Interface` | `batchInformation.interface` |
| `BatchId` | `batchInformation.batchId` |
| `Voucher.CompanyCode` | `transactionInformation.companyId` |
| `Voucher.Period` | `transactionInformation.period` |
| `Voucher.VoucherDate` | `transactionInformation.transactionDate` |
| `Transaction.TransType` | `transactionInformation.transactionType` |
| `Transaction.TransDate` | `transactionDetailInformation.valueDate` |
| `Transaction.Description` | `transactionDetailInformation.description` |
| `GLAnalysis.Account` | `accountingInformation.account` |
| `GLAnalysis.Dim1` | `accountingInformation.accountingDimension1` |
| `GLAnalysis.Dim2` | `accountingInformation.accountingDimension2` |
| `GLAnalysis.Dim3-7` | `accountingInformation.accountingDimension3-7` |
| `GLAnalysis.Currency` | `amounts.currencyCode` |
| `GLAnalysis.TaxCode` | `taxInformation.taxCode` |
| `Amounts.DcFlag` | `amounts.debitCreditFlag` |
| `Amounts.Amount` | `amounts.amount` |
| `Amounts.CurrAmount` | `amounts.currencyAmount` |
| `ApArInfo.ApArNo` | `invoice.customerOrSupplierId` |
| `ApArInfo.ApArType` | `invoice.ledgerType` |
| `ApArInfo.InvoiceNo` | `invoice.invoiceNumber` |
| `ApArInfo.DueDate` | `invoice.dueDate` |
| `ApArInfo.PayMethod` | `invoice.paymentMethod` |

### Step 13: Create Unit4 API Client

**Services/IUnit4ApiClient.cs**
```csharp
public interface IUnit4ApiClient
{
    Task<Unit4Response> PostTransactionBatchAsync(
        IEnumerable<Unit4TransactionBatchRequest> transactions);
}
```

**OAuth2 Flow:**
1. Read credentials from `IAppSettingsService`
2. Request token from TokenEndpoint (client_credentials grant)
3. Cache token until expiry
4. POST to `/v1/financial-transaction-batch` with Bearer token
5. Include `tenant` query parameter

### Step 14: Create Background Worker

**Workers/GL07ProcessingWorker.cs**

```csharp
[TaskHandler("gl07-process",
    Name = "GL07 Transaction Processing",
    Description = "Processes files from all active source systems and posts to Unit4")]
public class GL07ProcessingWorker : ITaskHandler<GL07ProcessingParameters>
```

**Worker Flow (Multi-Source):**
```
1. Get settings from IAppSettingsService
2. Get all active source systems from ISourceSystemRepository
3. For each active source system:
   a. Get transformer for system's TransformerType via ITransformationServiceFactory
   b. List files in {system.FolderPath}/inbox/ matching FilePattern
   c. For each file:
      i.   Download file content
      ii.  Transform via selected transformer
      iii. POST to Unit4 via IUnit4ApiClient
      iv.  Log result to ProcessingLog (with SourceSystemId)
      v.   On success: Move to {system.FolderPath}/done/YYYY-MM-DD_filename.xml
      vi.  On failure: Move to {system.FolderPath}/error/YYYY-MM-DD_filename.xml, STOP this system
   d. Continue to next source system (isolation between systems)
4. Log overall summary
```

**Error Handling Strategy:**
- If a file fails in System A, stop processing System A but continue with System B
- Each source system processes independently
- Failed files don't block other systems

### Step 15: Register Services and Packages

**NuGet Packages:**
```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.0" />
```

**Program.cs Registrations:**
```csharp
// Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("keys"));

// Repositories
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<ISourceSystemRepository, SourceSystemRepository>();
builder.Services.AddScoped<IProcessingLogRepository, ProcessingLogRepository>();

// Services
builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
builder.Services.AddScoped<IFileSourceService, AzureBlobFileSourceService>();
builder.Services.AddScoped<IXmlParserService, XmlParserService>();
builder.Services.AddScoped<IUnit4ApiClient, Unit4ApiClient>();

// Transformation services (Strategy Pattern)
builder.Services.AddScoped<ITransformationService, ABWTransactionTransformer>();
// Add more transformers as needed:
// builder.Services.AddScoped<ITransformationService, CustomCsvTransformer>();
builder.Services.AddScoped<ITransformationServiceFactory, TransformationServiceFactory>();

// HTTP Client for Unit4
builder.Services.AddHttpClient<IUnit4ApiClient, Unit4ApiClient>();
```

---

## Project Structure (Complete)

```
backend/
├── Controllers/
│   ├── A1arErpSfabGl07GatewayController.cs (existing)
│   ├── SettingsController.cs
│   └── SourceSystemsController.cs      # NEW: CRUD for source systems
├── Models/
│   ├── Settings/
│   │   ├── AppSetting.cs
│   │   └── SourceSystem.cs               # NEW: Source system entity
│   ├── Xml/
│   │   ├── ABWTransaction.cs
│   │   ├── Voucher.cs
│   │   ├── Transaction.cs
│   │   ├── Amounts.cs
│   │   ├── GLAnalysis.cs
│   │   ├── ApArInfo.cs
│   │   ├── TaxTransInfo.cs
│   │   └── SundryInfo.cs
│   └── Unit4/
│       ├── Unit4TransactionBatchRequest.cs
│       ├── BatchInformation.cs
│       ├── TransactionInformation.cs
│       ├── Invoice.cs
│       ├── TransactionDetailInformation.cs
│       ├── AccountingInformation.cs
│       ├── AmountsDto.cs
│       ├── TaxInformation.cs
│       ├── TaxDetails.cs
│       ├── StatisticalInformation.cs
│       └── AdditionalInformation.cs
├── Repositories/
│   ├── IAppSettingsRepository.cs
│   ├── AppSettingsRepository.cs
│   ├── ISourceSystemRepository.cs       # NEW
│   ├── SourceSystemRepository.cs        # NEW
│   ├── IProcessingLogRepository.cs      # NEW
│   └── ProcessingLogRepository.cs       # NEW
├── Services/
│   ├── IAppSettingsService.cs
│   ├── AppSettingsService.cs
│   ├── IFileSourceService.cs
│   ├── LocalFileSourceService.cs           # Development
│   ├── AzureBlobFileSourceService.cs       # Production
│   ├── IXmlParserService.cs
│   ├── XmlParserService.cs
│   ├── ITransformationService.cs        # NEW: Base interface
│   ├── ITransformationServiceFactory.cs # NEW: Factory interface
│   ├── TransformationServiceFactory.cs  # NEW: Factory implementation
│   ├── Transformers/                    # NEW: Folder for transformers
│   │   ├── ABWTransactionTransformer.cs # Default XML transformer
│   │   └── (future transformers here)
│   ├── IUnit4ApiClient.cs
│   └── Unit4ApiClient.cs
├── Workers/
│   ├── A1arErpSfabGl07GatewayWorker.cs (existing)
│   └── GL07ProcessingWorker.cs
├── sql/
│   └── init-tables.sql
├── Program.cs
└── appsettings.json

frontend/src/
├── components/
│   ├── layout/
│   │   └── AdminLayout.tsx
│   ├── settings/
│   │   ├── SettingsTable.tsx
│   │   └── SettingEditDialog.tsx
│   ├── sourceSystems/                   # NEW: Source system components
│   │   ├── SourceSystemsTable.tsx
│   │   └── SourceSystemEditDialog.tsx
│   └── auth/
│       └── AdminRoute.tsx
├── pages/
│   └── admin/
│       ├── SettingsPage.tsx
│       └── SourceSystemsPage.tsx        # NEW: Manage source systems
├── App.tsx (updated)
└── main.tsx
```

---

## Authorization Matrix

| Resource | Endpoint/Route | Required Role |
|----------|----------------|---------------|
| Settings API | `GET /api/settings` | admin |
| Settings API | `PUT /api/settings/{name}` | admin |
| Source Systems API | `GET /api/sourcesystems` | admin |
| Source Systems API | `POST /api/sourcesystems` | admin |
| Source Systems API | `PUT /api/sourcesystems/{id}` | admin |
| Source Systems API | `DELETE /api/sourcesystems/{id}` | admin |
| Settings Page | `/admin/settings` | admin |
| Source Systems Page | `/admin/source-systems` | admin |
| Background Worker | Task execution | System (no user) |
| Home Page | `/` | authenticated |

---

## Admin Navigation

```
┌────────────────────┐
│  ADMIN             │
├────────────────────┤
│  ⚙️ Settings         │  ← Unit4 & Azure Storage config
│  📡 Source Systems   │  ← NEW: Manage source folders/transformers
│  📋 Logs (future)    │  ← Processing logs per system
│  📊 Reports (future) │  ← Statistics per system
└────────────────────┘
```

---

## Future Enhancements

1. **Logs Page** (`/admin/logs`) - View ProcessingLog table with filtering by source system
2. **Reports Page** (`/admin/reports`) - Processing statistics, success rates per source system
3. **Manual Trigger** - API endpoint to trigger worker for specific source system
4. **Retry Failed** - Reprocess files from error folder per source system
5. **File Preview** - View file content before processing
6. **New Transformers** - Add CSV, JSON, or custom format transformers as needed
7. **Source System Health** - Dashboard showing file counts in inbox/done/error per system

---

## Development & Testing Setup

### File Source Provider Configuration

The application supports two file source providers, configured via `appsettings.json`:

**appsettings.Development.json** (Local File System)
```json
{
  "FileSource": {
    "Provider": "Local",
    "LocalBasePath": "C:/dev/gl07-files"
  }
}
```

**appsettings.Production.json** (Azure Blob Storage)
```json
{
  "FileSource": {
    "Provider": "AzureBlob"
  },
  "AzureStorage": {
    "ConnectionString": "<from-database-settings>",
    "ContainerName": "gl07-files"
  }
}
```

### Local Development Folder Setup

Create the following folder structure for local development:

```
C:/dev/gl07-files/                    ← LocalBasePath
├── bookkeeping/                      ← Source System 1
│   ├── inbox/                        ← Drop test XML files here
│   │   └── test-voucher.xml
│   ├── done/                         ← Processed files moved here
│   └── error/                        ← Failed files moved here
│
├── payroll/                          ← Source System 2
│   ├── inbox/
│   ├── done/
│   └── error/
│
└── bank-imports/                     ← Source System 3 (future CSV)
    ├── inbox/
    ├── done/
    └── error/
```

### Quick Start for Development

1. **Create test folder:**
   ```powershell
   mkdir C:/dev/gl07-files/bookkeeping/inbox
   mkdir C:/dev/gl07-files/bookkeeping/done
   mkdir C:/dev/gl07-files/bookkeeping/error
   ```

2. **Copy test XML file to inbox:**
   ```powershell
   copy sample-voucher.xml C:/dev/gl07-files/bookkeeping/inbox/
   ```

3. **Configure appsettings.Development.json:**
   ```json
   {
     "FileSource": {
       "Provider": "Local",
       "LocalBasePath": "C:/dev/gl07-files"
     }
   }
   ```

4. **Add source system to database** (or via Admin UI):
   ```sql
   INSERT INTO SourceSystems (SystemCode, SystemName, FolderPath, TransformerType, FilePattern)
   VALUES ('bookkeeping', 'Bookkeeping Test', 'bookkeeping', 'ABWTransaction', '*.xml');
   ```

5. **Run the worker** and check that:
   - File is processed from `/inbox`
   - File is moved to `/done` (success) or `/error` (failure)

### Testing Without Unit4 API

For testing transformations without calling Unit4 API, you can:

1. **Mock the Unit4 client** in development
2. **Add a "dry run" mode** that logs the transformed payload instead of POSTing
3. **Use Unit4 sandbox/test environment** with test credentials

### Database Settings for Development

Add these settings via Admin UI or directly in database:

| ParamName | Value (Development) | Description |
|-----------|---------------------|-------------|
| `FileSource:Provider` | `Local` | Use local file system |
| `FileSource:LocalBasePath` | `C:/dev/gl07-files` | Base path for local files |
| `Unit4:BaseUrl` | `https://no01-npe.erpx-api.unit4cloud.com` | Unit4 sandbox URL |

---

## Debugging

### VS Code Debugging Setup

The project includes VS Code configurations for debugging both backend and frontend.

#### Configuration Files

| File | Purpose |
|------|---------|
| `.vscode/launch.json` | Debug configurations |
| `.vscode/tasks.json` | Build and run tasks |
| `.vscode/settings.json` | Editor settings |
| `.vscode/extensions.json` | Recommended extensions |

#### Available Debug Configurations

| Configuration | Description |
|---------------|-------------|
| **Backend: .NET API** | Debug the .NET 8 backend with breakpoints |
| **Frontend: Vite Dev Server** | Debug React app in Chrome with breakpoints |
| **Frontend: Debug in Edge** | Debug React app in Microsoft Edge |
| **Backend: Attach to Process** | Attach debugger to running backend process |
| **Full Stack: Backend + Frontend** | Launch both backend and frontend simultaneously |

#### How to Debug

**Backend (.NET API):**
1. Open VS Code in the project root
2. Press `F5` or go to **Run > Start Debugging**
3. Select **"Backend: .NET API"**
4. Set breakpoints in any `.cs` file
5. The debugger will break at your breakpoints

**Frontend (React):**
1. Ensure backend is running (or use Full Stack config)
2. Press `F5` and select **"Frontend: Vite Dev Server"**
3. Set breakpoints in any `.tsx` or `.ts` file
4. Chrome will open and stop at breakpoints

**Full Stack (Both):**
1. Press `F5` and select **"Full Stack: Backend + Frontend"**
2. Both backend and frontend will start with debugging enabled
3. Set breakpoints in both `.cs` and `.tsx` files

#### VS Code Tasks

Run tasks via **Terminal > Run Task**:

| Task | Command | Description |
|------|---------|-------------|
| `build-backend` | `dotnet build` | Build the .NET backend |
| `watch-backend` | `dotnet watch run` | Run backend with hot reload |
| `start-frontend` | `npm run dev` | Start Vite dev server |
| `build-frontend` | `npm run build` | Build frontend for production |
| `install-frontend` | `npm install` | Install frontend dependencies |
| `restore-backend` | `dotnet restore` | Restore NuGet packages |
| `Full Stack: Start All` | Both | Run backend and frontend in parallel |

#### Recommended Extensions

Install these VS Code extensions for the best development experience:

| Extension | ID | Purpose |
|-----------|-----|---------|
| C# | `ms-dotnettools.csharp` | C# language support |
| C# Dev Kit | `ms-dotnettools.csdevkit` | Enhanced C# tooling |
| ESLint | `dbaeumer.vscode-eslint` | TypeScript/JS linting |
| Prettier | `esbenp.prettier-vscode` | Code formatting |
| Code Spell Checker | `streetsidesoftware.code-spell-checker` | Spell checking |

#### Debugging Tips

**Backend:**
- Use `ILogger<T>` for logging - logs appear in Debug Console
- Set `ASPNETCORE_ENVIRONMENT=Development` for detailed errors
- Use Swagger at `http://localhost:7458/swagger` for API testing
- Check **Debug Console** for request/response details

**Frontend:**
- Source maps are enabled by default (`sourcemap: true` in vite.config.ts)
- Use browser DevTools (F12) alongside VS Code debugging
- React DevTools extension helps inspect component state
- Network tab shows API calls to `/a1ar-erp-sfab-gl07-gateway/api/*`

**Common Issues:**

| Issue | Solution |
|-------|----------|
| Breakpoints not hitting (Backend) | Ensure "Build" task ran successfully |
| Breakpoints not hitting (Frontend) | Check sourcemaps enabled, clear browser cache |
| Port already in use | Kill existing process or use different port |
| Chrome doesn't open | Install Chrome or use Edge configuration |
| API calls fail | Ensure backend is running on port 7458 |

#### Environment Variables for Debugging

Backend debug environment (configured in launch.json):

```json
{
  "ASPNETCORE_ENVIRONMENT": "Development",
  "ASPNETCORE_URLS": "http://localhost:7458",
  "NEXUS_API_URL": "http://localhost:7833",
  "SECURITY__INTERNALAPITOKEN": "dev-internal-token-change-in-production"
}
```

---

## References

- **Unit4 API:** https://no01-npe.erpx-api.unit4cloud.com/swagger/
- **Endpoint:** `POST /v1/financial-transaction-batch`
- **XSD Schema:** ABWTransaction.xsd, ABWSchemaLib.xsd
- **XML Namespace:** `http://services.agresso.com/schema/ABWTransaction/2011/11/14`
