# Shared SAS Token for Azure File Share Connections

**Date:** February 12, 2026  
**Status:** Planned  
**Requested by:** Customer

---

## Background

Currently, each Azure File Share connection in the GL07 Gateway requires its own SAS token configured as a separate setting:

```
AzureFileShare:{connectionName}:Url    → File share URL
AzureFileShare:{connectionName}:Token  → Per-connection SAS token
```

**Customer Request:** The SAS token is at the storage account level, meaning all connections share the same token. Configuring duplicate tokens per connection is unnecessary.

---

## Current Implementation

### Settings Pattern (Per-Connection)

```
AzureFileShare:pigello-prod:Url    = https://mystorageaccount.file.core.windows.net/share1
AzureFileShare:pigello-prod:Token  = sv=2021-06-08&ss=f&srt=sco&sp=rwdlc...

AzureFileShare:system-b:Url        = https://mystorageaccount.file.core.windows.net/share2  
AzureFileShare:system-b:Token      = sv=2021-06-08&ss=f&srt=sco&sp=rwdlc...  ← DUPLICATE!
```

### Current Code Flow

```
SourceSystem.AzureFileShareConnectionName → GetConnectionAsync(name) → URL + Token
                                                     ↓
                                          BuildSasUrlAsync() → {URL}?{Token}
```

**Key File:** `backend/Services/AzureFileShareFileSourceService.cs`

---

## Proposed Implementation

### New Settings Pattern (Shared Token)

```
AzureFileShare:SharedSasToken          = sv=2021-06-08&ss=f&srt=sco&sp=rwdlc...  ← ONE TOKEN

AzureFileShare:pigello-prod:Url        = https://mystorageaccount.file.core.windows.net/share1
AzureFileShare:system-b:Url            = https://mystorageaccount.file.core.windows.net/share2
```

### New Code Flow

```
SourceSystem.AzureFileShareConnectionName → GetConnectionUrlAsync(name) → URL
                                                     ↓
                                          GetSharedSasTokenAsync() → Token
                                                     ↓
                                          BuildSasUrlAsync() → {URL}?{Token}
```

---

## Implementation Steps

### Step 1: Update `GetConnectionAsync` Method

**File:** `backend/Services/AzureFileShareFileSourceService.cs`

**Before:**
```csharp
private async Task<(string Url, string Token)> GetConnectionAsync(string connectionName)
{
    var urlKey = $"AzureFileShare:{connectionName}:Url";
    var tokenKey = $"AzureFileShare:{connectionName}:Token";

    var url = await _settingsService.GetValueAsync(urlKey);
    var token = await _settingsService.GetValueAsync(tokenKey);
    
    // ... validation ...
    
    return (url, token);
}
```

**After:**
```csharp
private const string SharedSasTokenKey = "AzureFileShare:SharedSasToken";

private async Task<string> GetConnectionUrlAsync(string connectionName)
{
    var urlKey = $"AzureFileShare:{connectionName}:Url";
    var url = await _settingsService.GetValueAsync(urlKey);

    if (string.IsNullOrEmpty(url))
    {
        throw new InvalidOperationException(
            $"Azure File Share connection '{connectionName}' URL not found in settings. " +
            $"Please add setting '{urlKey}'.");
    }

    return url;
}

private async Task<string> GetSharedSasTokenAsync()
{
    var token = await _settingsService.GetValueAsync(SharedSasTokenKey);
    
    if (string.IsNullOrEmpty(token))
    {
        throw new InvalidOperationException(
            $"Azure File Share shared SAS token not found in settings. " +
            $"Please add setting '{SharedSasTokenKey}'.");
    }

    return token;
}
```

### Step 2: Update `BuildSasUrlAsync` Method

**Before:**
```csharp
private async Task<string> BuildSasUrlAsync(SourceSystem sourceSystem)
{
    var (url, token) = await GetConnectionAsync(sourceSystem.AzureFileShareConnectionName);
    var separator = url.Contains('?') ? "&" : "?";
    return $"{url}{separator}{token}";
}
```

**After:**
```csharp
private async Task<string> BuildSasUrlAsync(SourceSystem sourceSystem)
{
    if (string.IsNullOrEmpty(sourceSystem.AzureFileShareConnectionName))
    {
        throw new InvalidOperationException(
            $"Source system '{sourceSystem.SystemCode}' is configured to use AzureFileShare " +
            "but has no connection name configured");
    }

    var url = await GetConnectionUrlAsync(sourceSystem.AzureFileShareConnectionName);
    var token = await GetSharedSasTokenAsync();
    
    var separator = url.Contains('?') ? "&" : "?";
    return $"{url}{separator}{token}";
}
```

### Step 3: Update Documentation

**File:** `backend/Models/Settings/SourceSystem.cs`

Update the XML comment for `AzureFileShareConnectionName`:

```csharp
/// <summary>
/// Name of the Azure File Share connection in AppSettings. 
/// References "AzureFileShare:{connectionName}:Url" for the file share URL.
/// The SAS token is shared across all connections via "AzureFileShare:SharedSasToken".
/// </summary>
public string? AzureFileShareConnectionName { get; set; }
```

### Step 4: Update Settings Service Auto-Encryption Pattern

**File:** `backend/Services/AppSettingsService.cs`

Ensure `SharedSasToken` is treated as sensitive (auto-encrypted):

```csharp
var isSensitive = setting?.Sensitive ??
    paramName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
    paramName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
    paramName.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) ||
    paramName.Contains("SasToken", StringComparison.OrdinalIgnoreCase) ||  // ← Add this
    paramName.Contains(":Token:", StringComparison.OrdinalIgnoreCase);
```

---

## Migration Guide

### For Existing Deployments

1. **Add the shared SAS token setting:**
   ```
   ParamName:  AzureFileShare:SharedSasToken
   ParamValue: sv=2021-06-08&ss=f&srt=sco&sp=rwdlc&se=...
   Category:   AzureFileShare
   Sensitive:  true
   ```

2. **Remove per-connection token settings (optional cleanup):**
   ```sql
   DELETE FROM AppSettings 
   WHERE ParamName LIKE 'AzureFileShare:%:Token';
   ```

3. **Keep existing URL settings** - they remain unchanged.

---

## Settings Required After Implementation

| Setting | Example Value | Required |
|---------|---------------|----------|
| `AzureFileShare:SharedSasToken` | `sv=2021-06-08&ss=f&srt=sco...` | **Yes** (one for all) |
| `AzureFileShare:{name}:Url` | `https://storage.file.core.windows.net/share` | Yes (per connection) |
| `AzureFileShare:{name}:Token` | ~~Not used~~ | **Removed** |

---

## Verification

1. **Build backend:**
   ```bash
   cd backend && dotnet build
   ```

2. **Test with existing source system:**
   - Ensure files can be listed from Azure File Share
   - Ensure files can be downloaded, moved to archive/error

3. **Verify token encryption:**
   - Check that `AzureFileShare:SharedSasToken` is stored encrypted in the database

---

## Benefits

- ✅ **Simpler configuration** - One token for all connections
- ✅ **Easier token rotation** - Update one setting instead of many
- ✅ **Less duplication** - No repeated tokens in AppSettings
- ✅ **Matches customer's Azure setup** - Storage account-level SAS tokens

---

## Files to Modify

| File | Changes |
|------|---------|
| `backend/Services/AzureFileShareFileSourceService.cs` | Split `GetConnectionAsync` into `GetConnectionUrlAsync` + `GetSharedSasTokenAsync` |
| `backend/Models/Settings/SourceSystem.cs` | Update XML documentation comment |
| `backend/Services/AppSettingsService.cs` | Add `SasToken` to auto-encryption patterns |

---

## Timeline

- **Implementation:** ~30 minutes
- **Testing:** ~15 minutes
- **Documentation update:** Done (this document)
