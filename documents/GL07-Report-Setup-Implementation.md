# GL07 Report Setup Implementation Plan

## Overview

Add GL07 report configuration to connect source systems with Unit4 WebAPI posting. Fix Unit4 transaction models to match correct API structure. Each transformer implements its own logic for extracting/mapping batchId and transactionType, with SourceSystem config providing overrides.

## Key Features

- **GL07 Report Setups**: Configurable report definitions with parameters for Unit4 posting
- **Source System Integration**: Mandatory linkage to GL07 report setup
- **BatchId Logic**: Per source system - either keep original from source file or generate with prefix + timestamp
- **Interface/TransactionType**: Configurable per source system with transformer-specific extraction logic
- **Uniqueness Validation**: BatchIdPrefix validated in both DB (partial unique index) and UI

## Database Schema Changes

### New Table: `Gl07ReportSetups`

```sql
CREATE TABLE Gl07ReportSetups (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SetupCode NVARCHAR(50) NOT NULL UNIQUE,
    SetupName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    ReportId NVARCHAR(100) NOT NULL,
    ReportName NVARCHAR(100) NOT NULL,
    Variant INT NULL,
    UserId NVARCHAR(100) NOT NULL,
    CompanyId NVARCHAR(100) NOT NULL,
    Priority INT NOT NULL DEFAULT 999999999,
    EmailConfirmation BIT NOT NULL DEFAULT 0,
    Status CHAR(1) NOT NULL DEFAULT 'P',
    OutputType INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### New Table: `Gl07ReportSetupParameters`

```sql
CREATE TABLE Gl07ReportSetupParameters (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Gl07ReportSetupId INT NOT NULL,
    ParameterId NVARCHAR(100) NOT NULL,
    ParameterValue NVARCHAR(500) NULL,
    CONSTRAINT FK_Gl07ReportSetupParameters_Setup 
        FOREIGN KEY (Gl07ReportSetupId) REFERENCES Gl07ReportSetups(Id) ON DELETE CASCADE
);
```

### Modified Table: `SourceSystems`

New columns:
- `Gl07ReportSetupId INT NOT NULL` - FK to Gl07ReportSetups (mandatory)
- `Interface NVARCHAR(50) NOT NULL` - Maps to batchInformation.interface
- `TransactionType NVARCHAR(50) NULL` - Override for transactionType (if null, use value from source)
- `KeepOriginalBatchId BIT NOT NULL DEFAULT 0` - If true, keep batchId from source file
- `BatchIdPrefix NVARCHAR(10) NULL` - Required if KeepOriginalBatchId=false, must be unique

```sql
CREATE UNIQUE INDEX UX_SourceSystems_BatchIdPrefix 
    ON SourceSystems(BatchIdPrefix) WHERE BatchIdPrefix IS NOT NULL;
```

### New Setting: `Unit4:BatchEndpoint`

Added to AppSettings table for configurable batch endpoint path.

## Unit4 API Payload Structure

### Transaction Batch Request (Corrected)

```json
{
  "notificationMessages": null,
  "batchInformation": {
    "notificationMessages": null,
    "interface": "string",
    "batchId": "string"
  },
  "transactionInformation": {
    "notificationMessages": null,
    "companyId": "string",
    "period": 0,
    "transactionDate": "2026-02-03T08:29:08.830Z",
    "transactionType": "string",
    "registeredDate": "2026-02-03T08:29:08.830Z",
    "registeredTransactionNumber": 0,
    "transactionNumber": 0,
    "invoice": { ... },
    "transactionDetailInformation": {
      "accountingInformation": { ... },
      "amounts": { ... },
      "taxInformation": { ... },
      "statisticalInformation": { ... }
    },
    "additionalInformation": { ... }
  }
}
```

### GL07 Report Request

```json
{
  "notificationMessages": null,
  "reportId": "string",
  "reportName": "string",
  "variant": 2147483647,
  "userId": "string",
  "companyId": "string",
  "parameters": [
    {
      "notificationMessages": null,
      "id": "string",
      "value": "string"
    }
  ],
  "generalParameters": {
    "notificationMessages": null,
    "priority": 999999999,
    "emailConfirmation": true,
    "status": "P",
    "start": "2026-02-03T08:03:13.585Z",
    "outputType": 0
  }
}
```

## BatchId Generation Logic

Per `TransformerType`, each transformer extracts batchId from source in its own way:

1. **If `KeepOriginalBatchId = true`**: Use batchId from source file (transformer-specific extraction)
2. **If `KeepOriginalBatchId = false`**: Generate `{BatchIdPrefix}-{yyMMddHHmmssff}` (max 25 chars total)

Example: `SFAB-26020308031599` (4 char prefix + dash + 14 char timestamp = 19 chars)

## TransactionType Logic

Per `TransformerType`:

1. **If `SourceSystem.TransactionType` is set**: Use that value
2. **If null/empty**: Extract from source file (e.g., ABW uses `voucherType` from XML)

## Implementation Steps

### Step 1: Database Schema (init-tables.sql)
- [x] Create `Gl07ReportSetups` table
- [x] Create `Gl07ReportSetupParameters` table
- [x] Add columns to `SourceSystems` table
- [x] Add partial unique index for `BatchIdPrefix`
- [x] Add `Unit4:BatchEndpoint` setting

### Step 2: Backend Models
- [x] Replace/update Unit4 transaction models to match correct JSON
- [x] Create `Gl07ReportSetup.cs` model
- [x] Create `Gl07ReportSetupParameter.cs` model
- [x] Update `SourceSystem.cs` model with new fields

### Step 3: Backend Repositories
- [x] Create `IGl07ReportSetupRepository.cs`
- [x] Create `Gl07ReportSetupRepository.cs`
- [x] Update `ISourceSystemRepository.cs` with `IsBatchIdPrefixUniqueAsync`
- [x] Update `SourceSystemRepository.cs` with new fields and methods

### Step 4: Backend Services
- [x] Update `Unit4Settings` class in `AppSettingsService.cs`
- [x] Refactor `ITransformationService` interface signature
- [x] Update `ABWTransactionTransformer.cs` mapping logic
- [x] Update `Unit4ApiClient.cs` to use BatchEndpoint setting
- [x] Register `IGl07ReportSetupRepository` in `Program.cs`

### Step 5: Backend Controllers
- [x] Create `Gl07ReportSetupsController.cs`
- [x] Update `SourceSystemsController.cs` with validation
- [x] Add prefix uniqueness check endpoint

### Step 6: Update Worker
- [x] Update `GL07ProcessingWorker.cs` to pass SourceSystem to transformer

### Step 7: Frontend
- [x] Add `Unit4:BatchEndpoint` field to `SettingsPage.tsx`
- [x] Create `Gl07ReportSetupsPage.tsx`
- [x] Update `SourceSystemsPage.tsx` with new fields
- [x] Add navigation in sidebar (`AdminLayout.tsx`)
- [x] Add route in `App.tsx`

## Validation Rules

### Source System

| Field | Rule |
|-------|------|
| `Interface` | Required, free text |
| `Gl07ReportSetupId` | Required, must exist |
| `TransactionType` | Optional |
| `KeepOriginalBatchId` | Boolean |
| `BatchIdPrefix` | Required if `KeepOriginalBatchId=false`, max 10 chars, unique |

### GL07 Report Setup

| Field | Rule |
|-------|------|
| `SetupCode` | Required, unique |
| `SetupName` | Required |
| `ReportId` | Required |
| `ReportName` | Required |
| `UserId` | Required |
| `CompanyId` | Required |
| `Status` | Single char, default 'P' |

## API Endpoints

### GL07 Report Setups

- `GET /api/gl07reportsetups` - List all setups
- `GET /api/gl07reportsetups/{id}` - Get setup with parameters
- `POST /api/gl07reportsetups` - Create setup
- `PUT /api/gl07reportsetups/{id}` - Update setup
- `DELETE /api/gl07reportsetups/{id}` - Delete setup

### Source Systems (updated)

- `GET /api/sourcesystems/check-prefix?prefix={value}&excludeId={id}` - Check prefix uniqueness

## Notes

- Unit4 credentials remain global (from AppSettings)
- Each `TransformerType` defines its own source-specific extraction logic for batchId/transactionType
- No seed data for `Gl07ReportSetups` - manual configuration required
