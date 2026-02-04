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
-- GL07 Report Setups - defines Unit4 report configurations
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Gl07ReportSetups')
BEGIN
    CREATE TABLE Gl07ReportSetups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SetupCode NVARCHAR(50) NOT NULL,            -- Unique identifier e.g., 'gl07-standard'
        SetupName NVARCHAR(100) NOT NULL,           -- Display name
        Description NVARCHAR(500) NULL,
        ReportId NVARCHAR(100) NOT NULL,            -- Unit4 report ID
        ReportName NVARCHAR(100) NOT NULL,          -- Unit4 report name
        Variant INT NULL,                           -- Unit4 variant number
        UserId NVARCHAR(100) NOT NULL,              -- Unit4 user ID
        CompanyId NVARCHAR(100) NOT NULL,           -- Unit4 company ID
        Priority INT NOT NULL DEFAULT 999999999,    -- General parameter: priority
        EmailConfirmation BIT NOT NULL DEFAULT 0,   -- General parameter: emailConfirmation
        Status CHAR(1) NOT NULL DEFAULT 'P',        -- General parameter: status
        OutputType INT NOT NULL DEFAULT 0,          -- General parameter: outputType
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Gl07ReportSetups_SetupCode UNIQUE (SetupCode)
    );

    CREATE INDEX IX_Gl07ReportSetups_IsActive ON Gl07ReportSetups(IsActive);
    CREATE INDEX IX_Gl07ReportSetups_SetupCode ON Gl07ReportSetups(SetupCode);
END
GO

-- =====================================================
-- GL07 Report Setup Parameters - user-defined parameters
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Gl07ReportSetupParameters')
BEGIN
    CREATE TABLE Gl07ReportSetupParameters (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Gl07ReportSetupId INT NOT NULL,
        ParameterId NVARCHAR(100) NOT NULL,         -- Parameter ID for Unit4 API
        ParameterValue NVARCHAR(500) NULL,          -- Parameter value
        CONSTRAINT FK_Gl07ReportSetupParameters_Setup 
            FOREIGN KEY (Gl07ReportSetupId) REFERENCES Gl07ReportSetups(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Gl07ReportSetupParameters_SetupId ON Gl07ReportSetupParameters(Gl07ReportSetupId);
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
        Provider NVARCHAR(20) NOT NULL              -- 'Local' or 'AzureBlob'
            DEFAULT 'Local',
        FolderPath NVARCHAR(255) NOT NULL,          -- e.g., 'bookkeeping' (relative path)
        TransformerType NVARCHAR(100) NOT NULL      -- e.g., 'ABWTransaction', 'CustomCSV'
            DEFAULT 'ABWTransaction',
        FilePattern NVARCHAR(100) NOT NULL          -- e.g., '*.xml', '*.csv'
            DEFAULT '*.xml',
        IsActive BIT NOT NULL DEFAULT 1,
        Description NVARCHAR(500),
        -- GL07 Report Setup configuration (mandatory)
        Gl07ReportSetupId INT NOT NULL,             -- FK to Gl07ReportSetups
        Interface NVARCHAR(50) NULL,                -- Override for batchInformation.interface (if null, use from XML)
        TransactionType NVARCHAR(50) NULL,          -- Override for transactionType (if null, use from source)
        BatchId NVARCHAR(100) NULL,                 -- Override for batchId (if null, use from XML)
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_SourceSystems_SystemCode UNIQUE (SystemCode),
        CONSTRAINT FK_SourceSystems_Gl07ReportSetup 
            FOREIGN KEY (Gl07ReportSetupId) REFERENCES Gl07ReportSetups(Id)
    );

    CREATE INDEX IX_SourceSystems_IsActive ON SourceSystems(IsActive);
    CREATE INDEX IX_SourceSystems_SystemCode ON SourceSystems(SystemCode);
    CREATE INDEX IX_SourceSystems_Gl07ReportSetupId ON SourceSystems(Gl07ReportSetupId);
END
GO

-- Add Provider column if it doesn't exist (migration for existing tables)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'Provider')
BEGIN
    ALTER TABLE SourceSystems ADD Provider NVARCHAR(20) NOT NULL DEFAULT 'Local';
END
GO

-- Add Description column if it doesn't exist (migration for existing tables)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'Description')
BEGIN
    ALTER TABLE SourceSystems ADD Description NVARCHAR(500);
END
GO

-- Add GL07 Report Setup columns if they don't exist (migration for existing tables)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'Gl07ReportSetupId')
BEGIN
    -- Note: For existing tables, you must create at least one Gl07ReportSetup first and update existing records
    ALTER TABLE SourceSystems ADD Gl07ReportSetupId INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'Interface')
BEGIN
    ALTER TABLE SourceSystems ADD Interface NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'TransactionType')
BEGIN
    ALTER TABLE SourceSystems ADD TransactionType NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'BatchId')
BEGIN
    ALTER TABLE SourceSystems ADD BatchId NVARCHAR(100) NULL;
END
GO

-- Drop old columns if they exist (migration from old schema)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'KeepOriginalBatchId')
BEGIN
    ALTER TABLE SourceSystems DROP COLUMN KeepOriginalBatchId;
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_SourceSystems_BatchIdPrefix')
BEGIN
    DROP INDEX UX_SourceSystems_BatchIdPrefix ON SourceSystems;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'BatchIdPrefix')
BEGIN
    ALTER TABLE SourceSystems DROP COLUMN BatchIdPrefix;
END
GO

-- Add FK constraint if not exists (after migration columns are added)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SourceSystems_Gl07ReportSetup')
BEGIN
    -- Only add FK if column exists and Gl07ReportSetups table exists
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SourceSystems') AND name = 'Gl07ReportSetupId')
       AND EXISTS (SELECT * FROM sys.tables WHERE name = 'Gl07ReportSetups')
    BEGIN
        ALTER TABLE SourceSystems ADD CONSTRAINT FK_SourceSystems_Gl07ReportSetup 
            FOREIGN KEY (Gl07ReportSetupId) REFERENCES Gl07ReportSetups(Id);
    END
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
        TaskExecutionId UNIQUEIDENTIFIER NULL,      -- Nexus task execution ID for log retrieval
        CONSTRAINT FK_ProcessingLog_SourceSystem 
            FOREIGN KEY (SourceSystemId) REFERENCES SourceSystems(Id)
    );

    CREATE INDEX IX_ProcessingLog_Status ON ProcessingLog(Status);
    CREATE INDEX IX_ProcessingLog_ProcessedAt ON ProcessingLog(ProcessedAt);
    CREATE INDEX IX_ProcessingLog_SourceSystemId ON ProcessingLog(SourceSystemId);
    CREATE INDEX IX_ProcessingLog_TaskExecutionId ON ProcessingLog(TaskExecutionId);
END
GO

-- Add TaskExecutionId column if it doesn't exist (migration for existing tables)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProcessingLog') AND name = 'TaskExecutionId')
BEGIN
    ALTER TABLE ProcessingLog ADD TaskExecutionId UNIQUEIDENTIFIER NULL;
    CREATE INDEX IX_ProcessingLog_TaskExecutionId ON ProcessingLog(TaskExecutionId);
END
GO

-- =====================================================
-- Seed default settings
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'Unit4:BaseUrl')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('Unit4:BaseUrl', 'https://no01-npe.erpx-api.unit4cloud.com', 0, 'Unit4', 'Unit4 API base URL'),
    ('Unit4:TokenUrl', 'https://s-no-ids1-preview.unit4cloud.com/identity/connect/token', 0, 'Unit4', 'OAuth2 token endpoint'),
    ('Unit4:ClientId', '', 0, 'Unit4', 'OAuth2 client ID'),
    ('Unit4:ClientSecret', '', 1, 'Unit4', 'OAuth2 client secret (encrypted)'),
    ('Unit4:Scope', 'api', 0, 'Unit4', 'OAuth2 scope'),
    ('Unit4:GrantType', 'client_credentials', 0, 'Unit4', 'OAuth2 grant type'),
    ('Unit4:BatchEndpoint', '/v1/financial-transaction-batch', 0, 'Unit4', 'API endpoint path for financial transaction batches'),
    ('FileSource:LocalBasePath', 'C:/dev/gl07-files', 0, 'FileSource', 'Base path for local file system'),
    ('AzureStorage:ConnectionString', '', 1, 'AzureStorage', 'Azure Blob connection string (encrypted)'),
    ('AzureStorage:ContainerName', 'gl07-files', 0, 'AzureStorage', 'Blob container name');
END
GO

-- Add Unit4:BatchEndpoint if it doesn't exist (migration for existing tables)
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'Unit4:BatchEndpoint')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('Unit4:BatchEndpoint', '/v1/financial-transaction-batch', 0, 'Unit4', 'API endpoint path for financial transaction batches');
END
GO

-- Add GL07 default settings if they don't exist (migration for existing tables)
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'GL07:DefaultReportId')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('GL07:DefaultReportId', 'BI202', 0, 'GL07', 'Default Report ID for GL07 report setups');
END
GO

IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'GL07:DefaultReportName')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('GL07:DefaultReportName', 'GL07', 0, 'GL07', 'Default Report Name for GL07 report setups (fixed)');
END
GO

IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'GL07:DefaultUserId')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('GL07:DefaultUserId', '', 0, 'GL07', 'Default User ID for GL07 report setups');
END
GO

IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'GL07:DefaultCompanyId')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('GL07:DefaultCompanyId', '', 0, 'GL07', 'Default Company ID for GL07 report setups');
END
GO

-- Add log retention setting if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE ParamName = 'LogRetention:Days')
BEGIN
    INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description) VALUES
    ('LogRetention:Days', '90', 0, 'General', 'Number of days to retain processing logs (minimum 7)');
END
GO

-- =====================================================
-- Seed example source systems (commented out - requires Gl07ReportSetup to be created first)
-- =====================================================
-- Note: Source systems now require a Gl07ReportSetup. Create report setup first via UI,
-- then create source systems linked to it.
-- Example:
-- INSERT INTO Gl07ReportSetups (SetupCode, SetupName, ReportId, ReportName, UserId, CompanyId) VALUES
-- ('standard', 'Standard GL07 Report', 'GL07', 'GL07 Financial Report', 'api-user', 'COMPANY1');
-- 
-- INSERT INTO SourceSystems (SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, Description, 
--     Gl07ReportSetupId, Interface, KeepOriginalBatchId, BatchIdPrefix) VALUES
-- ('bookkeeping', 'Bookkeeping System', 'Local', 'bookkeeping', 'ABWTransaction', '*.xml', 
--     'Main bookkeeping system - ABWTransaction XML format', 1, 'GL07', 0, 'BOOK');
GO
