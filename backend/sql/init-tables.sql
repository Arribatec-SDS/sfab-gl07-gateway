-- GL07 Gateway Database Schema
-- Run this script to initialize the database tables

-- =====================================================
-- Settings table - stores application configuration
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppSettings')
BEGIN
    CREATE TABLE AppSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ParamName NVARCHAR(100) NOT NULL,
        ParamValue NVARCHAR(MAX),
        Sensitive BIT NOT NULL DEFAULT 0,
        Category NVARCHAR(50) NOT NULL DEFAULT 'General',
        Description NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_AppSettings_ParamName UNIQUE (ParamName)
    );

    CREATE INDEX IX_AppSettings_ParamName ON AppSettings(ParamName);
    CREATE INDEX IX_AppSettings_Category ON AppSettings(Category);
END
GO

-- =====================================================
-- Source systems table - one folder per source system
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SourceSystems')
BEGIN
    CREATE TABLE SourceSystems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SystemCode NVARCHAR(50) NOT NULL,           -- e.g., 'bookkeeping', 'payroll'
        SystemName NVARCHAR(100) NOT NULL,          -- e.g., 'Bookkeeping System'
        FolderPath NVARCHAR(255) NOT NULL,          -- e.g., 'bookkeeping' (relative path)
        TransformerType NVARCHAR(100) NOT NULL      -- e.g., 'ABWTransaction', 'CustomCSV'
            DEFAULT 'ABWTransaction',
        FilePattern NVARCHAR(100) NOT NULL          -- e.g., '*.xml', '*.csv'
            DEFAULT '*.xml',
        IsActive BIT NOT NULL DEFAULT 1,
        Description NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_SourceSystems_SystemCode UNIQUE (SystemCode)
    );

    CREATE INDEX IX_SourceSystems_IsActive ON SourceSystems(IsActive);
    CREATE INDEX IX_SourceSystems_SystemCode ON SourceSystems(SystemCode);
END
GO

-- Add Description column if it doesn't exist (migration for existing tables)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'Description')
BEGIN
    ALTER TABLE SourceSystems ADD Description NVARCHAR(500);
END
GO

-- =====================================================
-- Processing log - tracks file processing history
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessingLog')
BEGIN
    CREATE TABLE ProcessingLog (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SourceSystemId INT NOT NULL,
        FileName NVARCHAR(500) NOT NULL,
        Status NVARCHAR(20) NOT NULL,               -- 'Success', 'Error', 'Processing'
        VoucherCount INT,
        TransactionCount INT,
        ErrorMessage NVARCHAR(MAX),
        ProcessedAt DATETIME2 DEFAULT GETUTCDATE(),
        DurationMs INT,
        CONSTRAINT FK_ProcessingLog_SourceSystem 
            FOREIGN KEY (SourceSystemId) REFERENCES SourceSystems(Id)
    );

    CREATE INDEX IX_ProcessingLog_Status ON ProcessingLog(Status);
    CREATE INDEX IX_ProcessingLog_ProcessedAt ON ProcessingLog(ProcessedAt);
    CREATE INDEX IX_ProcessingLog_SourceSystemId ON ProcessingLog(SourceSystemId);
END
GO

-- =====================================================
-- Seed default settings
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'Unit4:BaseUrl')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('Unit4:BaseUrl', 'https://no01-npe.erpx-api.unit4cloud.com', 0, 'Unit4', 'Unit4 API base URL'),
    ('Unit4:TokenEndpoint', 'https://auth.unit4cloud.com/oauth/token', 0, 'Unit4', 'OAuth2 token endpoint'),
    ('Unit4:ClientId', '', 0, 'Unit4', 'OAuth2 client ID'),
    ('Unit4:ClientSecret', '', 1, 'Unit4', 'OAuth2 client secret (encrypted)'),
    ('Unit4:Scope', 'api', 0, 'Unit4', 'OAuth2 scope'),
    ('Unit4:TenantId', '', 0, 'Unit4', 'Unit4 tenant GUID'),
    ('AzureStorage:ConnectionString', '', 1, 'AzureStorage', 'Azure Blob connection string (encrypted)'),
    ('AzureStorage:ContainerName', 'gl07-files', 0, 'AzureStorage', 'Blob container name'),
    ('FileSource:Provider', 'Local', 0, 'FileSource', 'File source provider: Local or AzureBlob'),
    ('FileSource:LocalBasePath', 'C:/dev/gl07-files', 0, 'FileSource', 'Base path for local file system (development)');
END
GO

-- =====================================================
-- Seed example source systems
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM SourceSystems WHERE SystemCode = 'bookkeeping')
BEGIN
    INSERT INTO SourceSystems (SystemCode, SystemName, FolderPath, TransformerType, FilePattern, Description) VALUES
    ('bookkeeping', 'Bookkeeping System', 'bookkeeping', 'ABWTransaction', '*.xml', 'Main bookkeeping system - ABWTransaction XML format'),
    ('payroll', 'Payroll System', 'payroll', 'ABWTransaction', '*.xml', 'Payroll transactions - ABWTransaction XML format');
END
GO
