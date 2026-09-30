# delete-tables.ps1
# Drops only the Stack86 project-related tables (Identity + app tables + EF migration history)
# from one or more SQL Server instances. Leaves the database itself intact.

param(
    [string[]]$Servers = @("(localdb)\MSSQLLocalDB", "localhost"),
    [string]$Database = "Stack86",
    [switch]$Force,
    [switch]$Quiet,
    # Bypass the __EFMigrationsHistory fingerprint check. Only use this if you
    # know what you are doing (e.g. the migration table was wiped manually).
    [switch]$SkipSafetyCheck
)

# Fingerprint: known Stack86 EF Core migration IDs. The script will refuse to
# drop anything unless ALL of these are present in __EFMigrationsHistory,
# proving the database really belongs to Stack86 and not another app that
# happens to share an "AspNetUsers"-style schema.
$stack86MigrationIds = @(
    "20260425132328_InitialIdentitySchema",
    "20260506125024_AddTwoFactorAndTrustedDevices"
)

# Tables created by Stack86 (EF Core + Identity). Order is irrelevant: the SQL
# below drops all FKs first, then drops tables.
$tables = @(
    "AspNetRoleClaims",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserRoles",
    "AspNetUserTokens",
    "AspNetRoles",
    "AspNetUsers",
    "UserRefreshTokens",
    "TrustedDevices",
    "__EFMigrationsHistory"
)

function Write-Status {
    param([string]$Message, [string]$Color = "White")
    if (-not $Quiet) {
        Write-Host $Message -ForegroundColor $Color
    }
}

if (-not $Quiet) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Delete Stack86 Tables" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Servers:  $($Servers -join ', ')" -ForegroundColor Gray
    Write-Host "Database: $Database" -ForegroundColor Gray
    Write-Host "Tables:   $($tables -join ', ')" -ForegroundColor Gray
    Write-Host ""
}

if (-not $Force) {
    $confirmation = Read-Host "Drop these tables from '$Database' on the listed servers? (y/N)"
    if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
        Write-Status "Operation cancelled." "Yellow"
        exit 0
    }
}

# Build a quoted SQL list of table names: N'AspNetRoleClaims', N'AspNetUserClaims', ...
$tableList = ($tables | ForEach-Object { "N'$_'" }) -join ", "

# Drop all foreign keys referencing the target tables, then drop the tables themselves.
# Wrapped in a check so missing databases / tables don't error out.
$sql = @"
IF DB_ID(N'$Database') IS NULL
BEGIN
    PRINT 'Database [$Database] does not exist on this server. Skipping.';
    RETURN;
END

USE [$Database];

DECLARE @sql NVARCHAR(MAX) = N'';

-- Drop all foreign keys on the target tables (in either direction).
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
                   + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
INNER JOIN sys.tables t  ON fk.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name IN ($tableList)
   OR fk.referenced_object_id IN (SELECT object_id FROM sys.tables WHERE name IN ($tableList));

IF LEN(@sql) > 0
BEGIN
    PRINT 'Dropping foreign keys...';
    EXEC sp_executesql @sql;
END

DECLARE @drop NVARCHAR(MAX) = N'';
SELECT @drop = @drop + N'DROP TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(10)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name IN ($tableList);

IF LEN(@drop) > 0
BEGIN
    PRINT 'Dropping tables...';
    EXEC sp_executesql @drop;
    PRINT 'Tables dropped from [$Database].';
END
ELSE
BEGIN
    PRINT 'No matching Stack86 tables found in [$Database].';
END
"@

$overallExit = 0

foreach ($server in $Servers) {
    Write-Status ""
    Write-Status "--- $server ---" "Cyan"

    # ----------------- Safety check -----------------
    # Verify this is actually a Stack86 database before dropping anything.
    if (-not $SkipSafetyCheck) {
        $checkSql = @"
SET NOCOUNT ON;
IF DB_ID(N'$Database') IS NULL
BEGIN
    PRINT 'MISSING_DB';
    RETURN;
END
USE [$Database];
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    PRINT 'NO_MIGRATIONS_TABLE';
    RETURN;
END
DECLARE @found INT = (
    SELECT COUNT(*) FROM dbo.__EFMigrationsHistory
    WHERE MigrationId IN ($((($stack86MigrationIds | ForEach-Object { "N'$_'" }) -join ', ')))
);
IF @found = $($stack86MigrationIds.Count)
    PRINT 'STACK86_OK';
ELSE
    PRINT 'NOT_STACK86';
"@

        $checkOutput = & sqlcmd -S $server -E -h -1 -W -Q $checkSql 2>&1
        $checkExit = $LASTEXITCODE

        if ($checkExit -ne 0) {
            Write-Host "Could not connect to ${server}: $checkOutput" -ForegroundColor Red
            $overallExit = $checkExit
            continue
        }

        $checkText = ($checkOutput -join "`n")
        if ($checkText -match 'MISSING_DB') {
            Write-Status "Database [$Database] does not exist on $server. Skipping." "Yellow"
            continue
        }
        if ($checkText -match 'NO_MIGRATIONS_TABLE') {
            Write-Host "Database [$Database] on $server has no __EFMigrationsHistory table." -ForegroundColor Yellow
            Write-Host "Refusing to drop tables — this does not look like a Stack86 database." -ForegroundColor Yellow
            Write-Host "Use -SkipSafetyCheck to override." -ForegroundColor Yellow
            $overallExit = 2
            continue
        }
        if ($checkText -notmatch 'STACK86_OK') {
            Write-Host "Database [$Database] on $server is missing expected Stack86 migrations." -ForegroundColor Red
            Write-Host "Refusing to drop tables — another application may be using this database." -ForegroundColor Red
            Write-Host "Use -SkipSafetyCheck to override." -ForegroundColor Red
            $overallExit = 2
            continue
        }

        Write-Status "Stack86 fingerprint verified on $server." "DarkGray"
    }

    # ----------------- Drop -----------------
    try {
        $output = & sqlcmd -S $server -E -b -Q $sql 2>&1
        $exitCode = $LASTEXITCODE

        if ($exitCode -eq 0) {
            Write-Status "Done on $server." "Green"
            if (-not $Quiet) {
                $output | ForEach-Object { Write-Host $_ }
            }
        }
        else {
            Write-Host "Failed on ${server} (exit $exitCode):" -ForegroundColor Red
            $output | ForEach-Object { Write-Host $_ -ForegroundColor Red }
            $overallExit = $exitCode
        }
    }
    catch {
        Write-Host "Error on ${server}: $_" -ForegroundColor Red
        $overallExit = 1
    }
}

exit $overallExit
