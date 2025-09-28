-- Multi-Tenant Schema Provisioning Script for SQL Server
-- This script creates tenant-specific schemas for complete data isolation

-- Parameters (to be replaced by provisioning system):
-- @TenantSlug - The district slug (e.g., 'oakland-unified')
-- @SchemaName - The database schema name (e.g., 'oakland_unified')
-- @DisplayName - Human readable district name

DECLARE @TenantSlug NVARCHAR(100) = '{TENANT_SLUG}';
DECLARE @SchemaName NVARCHAR(100) = '{SCHEMA_NAME}';  
DECLARE @DisplayName NVARCHAR(250) = '{DISPLAY_NAME}';
DECLARE @SQL NVARCHAR(MAX);

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Create the tenant schema
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @SchemaName)
    BEGIN
        SET @SQL = 'CREATE SCHEMA [' + @SchemaName + ']';
        EXEC sp_executesql @SQL;
        PRINT 'Created schema: ' + @SchemaName;
    END
    ELSE
    BEGIN
        PRINT 'Schema already exists: ' + @SchemaName;
    END

    -- 2. Create School Years table
    SET @SQL = 'CREATE TABLE [' + @SchemaName + '].[school_years] (
        [id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [year] INT NOT NULL,
        [name] NVARCHAR(50) NOT NULL,
        [start_date] DATETIME2 NOT NULL,
        [end_date] DATETIME2 NOT NULL,
        [status] INT NOT NULL DEFAULT 0,
        [is_archived] BIT NOT NULL DEFAULT 0,
        [created_date] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM'',
        CONSTRAINT [ck_' + @SchemaName + '_school_years_year_range] CHECK ([year] >= 2000 AND [year] <= 2099),
        CONSTRAINT [ck_' + @SchemaName + '_school_years_date_range] CHECK ([end_date] > [start_date])
    )';
    EXEC sp_executesql @SQL;

    -- Add indexes for school years
    SET @SQL = 'CREATE UNIQUE INDEX [ix_' + @SchemaName + '_school_years_year] ON [' + @SchemaName + '].[school_years] ([year])';
    EXEC sp_executesql @SQL;

    -- 3. Create Schools table
    SET @SQL = 'CREATE TABLE [' + @SchemaName + '].[schools] (
        [id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [code] NVARCHAR(50) NOT NULL,
        [name] NVARCHAR(200) NOT NULL,
        [type] INT NOT NULL DEFAULT 0,
        [is_active] BIT NOT NULL DEFAULT 1,
        [established_date] DATETIME2 NOT NULL,
        [address_line1] NVARCHAR(200) NULL,
        [address_line2] NVARCHAR(200) NULL,
        [city] NVARCHAR(100) NULL,
        [state] NVARCHAR(50) NULL,
        [postal_code] NVARCHAR(20) NULL,
        [phone] NVARCHAR(20) NULL,
        [email] NVARCHAR(200) NULL,
        [principal_name] NVARCHAR(200) NULL,
        [created_date] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM'',
        [last_modified] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [modified_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM''
    )';
    EXEC sp_executesql @SQL;

    -- Add indexes for schools
    SET @SQL = 'CREATE UNIQUE INDEX [ix_' + @SchemaName + '_schools_code] ON [' + @SchemaName + '].[schools] ([code])';
    EXEC sp_executesql @SQL;

    -- 4. Create Students table
    SET @SQL = 'CREATE TABLE [' + @SchemaName + '].[students] (
        [user_id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [student_number] NVARCHAR(50) NOT NULL,
        [first_name] NVARCHAR(100) NOT NULL,
        [middle_name] NVARCHAR(100) NULL,
        [last_name] NVARCHAR(100) NOT NULL,
        [date_of_birth] DATE NOT NULL,
        [gender] NVARCHAR(10) NULL,
        [status] INT NOT NULL DEFAULT 0,
        [enrollment_date] DATETIME2 NOT NULL,
        [withdrawal_date] DATETIME2 NULL,
        [withdrawal_reason] NVARCHAR(500) NULL,
        [grade_level] INT NOT NULL,
        [is_special_education] BIT NOT NULL DEFAULT 0,
        [is_gifted] BIT NOT NULL DEFAULT 0,
        [is_english_language_learner] BIT NOT NULL DEFAULT 0,
        [accommodation_tags] NVARCHAR(MAX) NULL,
        [emergency_contact_name] NVARCHAR(200) NULL,
        [emergency_contact_phone] NVARCHAR(20) NULL,
        [medical_notes] NVARCHAR(MAX) NULL,
        [created_date] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM'',
        [last_modified] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [modified_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM'',
        CONSTRAINT [ck_' + @SchemaName + '_students_grade_level] CHECK ([grade_level] >= -1 AND [grade_level] <= 12),
        CONSTRAINT [ck_' + @SchemaName + '_students_withdrawal_date] CHECK ([withdrawal_date] IS NULL OR [withdrawal_date] >= [enrollment_date])
    )';
    EXEC sp_executesql @SQL;

    -- Add indexes for students
    SET @SQL = 'CREATE UNIQUE INDEX [ix_' + @SchemaName + '_students_number] ON [' + @SchemaName + '].[students] ([student_number])';
    EXEC sp_executesql @SQL;
    
    SET @SQL = 'CREATE INDEX [ix_' + @SchemaName + '_students_status] ON [' + @SchemaName + '].[students] ([status]) INCLUDE ([grade_level], [enrollment_date])';
    EXEC sp_executesql @SQL;

    -- 5. Create Staff table
    SET @SQL = 'CREATE TABLE [' + @SchemaName + '].[staff] (
        [user_id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [employee_number] NVARCHAR(50) NOT NULL,
        [first_name] NVARCHAR(100) NOT NULL,
        [middle_name] NVARCHAR(100) NULL,
        [last_name] NVARCHAR(100) NOT NULL,
        [email] NVARCHAR(200) NOT NULL,
        [phone] NVARCHAR(20) NULL,
        [status] INT NOT NULL DEFAULT 0,
        [hire_date] DATE NOT NULL,
        [termination_date] DATE NULL,
        [job_title] NVARCHAR(200) NULL,
        [department] NVARCHAR(100) NULL,
        [supervisor_id] UNIQUEIDENTIFIER NULL,
        [created_date] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM'',
        [last_modified] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [modified_by] NVARCHAR(100) NOT NULL DEFAULT ''SYSTEM'',
        CONSTRAINT [ck_' + @SchemaName + '_staff_termination_date] CHECK ([termination_date] IS NULL OR [termination_date] >= [hire_date])
    )';
    EXEC sp_executesql @SQL;

    -- Add indexes for staff
    SET @SQL = 'CREATE UNIQUE INDEX [ix_' + @SchemaName + '_staff_employee_number] ON [' + @SchemaName + '].[staff] ([employee_number])';
    EXEC sp_executesql @SQL;
    
    SET @SQL = 'CREATE UNIQUE INDEX [ix_' + @SchemaName + '_staff_email] ON [' + @SchemaName + '].[staff] ([email]) WHERE [status] = 0'; -- Active only
    EXEC sp_executesql @SQL;

    -- Continue with remaining table creation...
    -- (Additional tables would be created here following the same pattern)

    COMMIT TRANSACTION;
    PRINT 'Successfully provisioned tenant schema: ' + @SchemaName;
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    RAISERROR('Error provisioning tenant schema: %s', 16, 1, ERROR_MESSAGE());
END CATCH