# Transformer Mapping Guide

**Date:** January 30, 2026  
**Project:** SFAB GL07 Gateway  
**Purpose:** Explains how source folders are mapped to transformers

---

## Overview

The system processes files from multiple source systems, each potentially using a different file format. The **folder-to-transformer mapping** is configured in the database and managed via the Admin UI.

---

## How Folder → Transformer Mapping Works

### Database Configuration

The `SourceSystems` table stores the mapping between folders and transformers:

```
┌─────────────────────────────────────────────────────────────────────┐
│                     SourceSystems Table (Database)                  │
├─────────────┬───────────────┬──────────────────┬───────────────────┤
│ SystemCode  │ FolderPath    │ TransformerType  │ FilePattern       │
├─────────────┼───────────────┼──────────────────┼───────────────────┤
│ bookkeeping │ bookkeeping   │ ABWTransaction   │ *.xml             │
│ payroll     │ payroll       │ ABWTransaction   │ *.xml             │
│ bank        │ bank-imports  │ BankCSV          │ *.csv             │
└─────────────┴───────────────┴──────────────────┴───────────────────┘
                    ↓                  ↓
              Azure Blob          Code looks up
              folder path         transformer by name
```

### Worker Processing Flow

```csharp
// Worker pseudo-code
foreach (var sourceSystem in await _sourceSystemRepo.GetActiveAsync())
{
    // 1. Get transformer based on DB config
    var transformer = _transformerFactory.GetTransformer(sourceSystem.TransformerType);
    //                                                   ↑
    //                                    "ABWTransaction" or "BankCSV" from DB
    
    // 2. List files in the system's folder
    var files = await _fileService.ListFilesAsync(sourceSystem);
    //                                            ↑
    //                             Uses sourceSystem.FolderPath + "/inbox"
    //                             e.g., "bookkeeping/inbox/*.xml"
    
    // 3. Process each file with the correct transformer
    foreach (var file in files)
    {
        var content = await _fileService.DownloadAsync(sourceSystem, file);
        var transactions = transformer.Transform(content);  // ← Correct transformer!
        await _unit4Client.PostAsync(transactions);
    }
}
```

---

## Admin UI Configuration

Administrators configure source systems via the Admin UI:

```
┌──────────────────────────────────────────────────────────────────────┐
│  Source Systems                                          [+ Add New] │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ System: Bookkeeping                                    [Edit]  │  │
│  │ Folder: bookkeeping                                            │  │
│  │ Transformer: [ABWTransaction     ▼]  ← Admin selects from      │  │
│  │ File Pattern: *.xml                     available transformers │  │
│  │ Status: ✅ Active                                              │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ System: Bank Imports                                   [Edit]  │  │
│  │ Folder: bank-imports                                           │  │
│  │ Transformer: [BankCSV            ▼]  ← Different transformer   │  │
│  │ File Pattern: *.csv                                            │  │
│  │ Status: ✅ Active                                              │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

The **Transformer dropdown** shows all transformers registered in the application code.

---

## End-to-End Flow

```
1. Admin creates Source System in UI:
   - Name: "Bank Imports"
   - Folder: "bank-imports"
   - Transformer: "BankCSV" (dropdown of registered transformers)
   - Pattern: "*.csv"

2. External system drops files into Azure Blob:
   gl07-files/bank-imports/inbox/transactions-2026-01-30.csv

3. Worker runs:
   - Reads SourceSystems from database
   - For "bank-imports" folder → uses "BankCSV" transformer
   - For "bookkeeping" folder → uses "ABWTransaction" transformer

4. Files processed with correct transformer based on folder config
```

---

## What IS Automatic vs What IS NOT

### Automatic ✅

| Component | Description |
|-----------|-------------|
| **Selecting** which transformer to use | Based on `SourceSystem.TransformerType` in database |
| **Routing** files to correct transformer | Factory pattern looks up transformer by type |
| **File download/move** operations | `IFileSourceService` handles all file operations |

### NOT Automatic ❌ (Requires Developer)

| Component | Description |
|-----------|-------------|
| **Field mapping** (Source → Unit4) | Developer must write code for each format |
| **Data type conversion** | Developer handles date formats, decimals, etc. |
| **Business logic** | Developer implements validation, defaults, etc. |

---

## Adding a New Transformer

When a new source system uses a different file format, a developer must:

### 1. Create New Transformer Class

```csharp
public class BankCsvTransformer : ITransformationService
{
    public string TransformerType => "BankCSV";
    
    public IEnumerable<Unit4TransactionBatchRequest> Transform(string fileContent)
    {
        var lines = fileContent.Split('\n').Skip(1); // Skip header
        
        foreach (var line in lines)
        {
            var columns = line.Split(';');
            
            // MANUAL MAPPING - developer writes this:
            yield return new Unit4TransactionBatchRequest
            {
                BatchInformation = new BatchInformation
                {
                    Interface = "BANK",           // Hardcoded or from config
                    BatchId = columns[0]          // Column 0 = BatchId
                },
                TransactionInformation = new TransactionInformation
                {
                    CompanyId = columns[1],       // Column 1 = Company
                    Period = int.Parse(columns[2]), // Column 2 = Period
                    TransactionDate = DateTime.Parse(columns[3]), // Column 3 = Date
                    TransactionDetailInformation = new TransactionDetailInformation
                    {
                        Description = columns[4], // Column 4 = Description
                        AccountingInformation = new AccountingInformation
                        {
                            Account = columns[5], // Column 5 = Account
                        },
                        Amounts = new AmountsDto
                        {
                            Amount = decimal.Parse(columns[6]), // Column 6 = Amount
                            CurrencyCode = columns[7] // Column 7 = Currency
                        }
                    }
                }
            };
        }
    }
    
    public bool CanHandle(string fileContent)
    {
        // CSV detection logic
        return fileContent.Contains(";") && !fileContent.Contains("<");
    }
}
```

### 2. Register in Program.cs

```csharp
// Add new transformer to DI container
builder.Services.AddScoped<ITransformationService, BankCsvTransformer>();
```

### 3. Admin Selects in UI

After deployment, the new transformer appears in the dropdown and admin can assign it to a source system.

---

## Why Manual Mapping is Required

| Reason | Example |
|--------|---------|
| **Different source structures** | CSV has columns, XML has elements, JSON has properties |
| **Different field names** | CSV: `Beløp`, XML: `Amount`, JSON: `amount` |
| **Different date formats** | `2026-01-30`, `30.01.2026`, `01/30/2026` |
| **Business rules vary** | Some systems include tax, some don't |
| **Missing fields** | Must know what defaults to use |

---

## FAQ

| Question | Answer |
|----------|--------|
| **How does system know which transformer?** | `SourceSystems.TransformerType` column in database |
| **Who configures it?** | Admin, via Source Systems UI page |
| **Can same folder use different transformers?** | No, one folder = one transformer |
| **Can same transformer be used by multiple folders?** | Yes (bookkeeping + payroll both use ABWTransaction) |
| **What if new transformer is added?** | Developer registers it, then admin can select it in dropdown |
| **What if folder doesn't exist in Azure?** | Worker skips it (or creates it on first run) |
| **What if transformer doesn't exist?** | Worker throws error for that source system, continues with others |

---

## Azure Blob Folder Structure

```
gl07-files/                          ← Container
├── bookkeeping/                     ← Source System 1 (ABWTransaction)
│   ├── inbox/
│   │   └── voucher-2026-01-30.xml
│   ├── done/
│   │   └── 2026-01-29_voucher-2026-01-29.xml
│   └── error/
│       └── 2026-01-28_bad-file.xml
│
├── payroll/                         ← Source System 2 (ABWTransaction)
│   ├── inbox/
│   ├── done/
│   └── error/
│
└── bank-imports/                    ← Source System 3 (BankCSV)
    ├── inbox/
    │   └── transactions-2026-01-30.csv
    ├── done/
    └── error/
```

Each source system has its own isolated folder structure with inbox/done/error subfolders.
