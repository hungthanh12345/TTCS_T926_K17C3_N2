[CmdletBinding()]
param(
    [ValidateSet(5002)]
    [int]$Port = 5002
)

$ErrorActionPreference = 'Stop'

function ConvertFrom-SecurePrompt {
    param([Parameter(Mandatory = $true)][string]$Prompt)

    $secureValue = Read-Host -Prompt $Prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Get-ConnectionValue {
    param(
        [Parameter(Mandatory = $true)][System.Data.Common.DbConnectionStringBuilder]$Builder,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    foreach ($actualName in $Builder.Keys) {
        foreach ($name in $Names) {
            if ([string]$actualName -ieq $name) {
                return [string]$Builder[$actualName]
            }
        }
    }

    return $null
}

$backendRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $backendRoot '..'))
$expectedRoot = [IO.Path]::GetFullPath('D:\TTCS_T926_K17C3_N2')
$projectPath = Join-Path $backendRoot 'InternshipManagementApi.csproj'
$testSettingsPath = Join-Path $backendRoot 'appsettings.Test.json'
$programPath = Join-Path $backendRoot 'Program.cs'

if (-not [string]::Equals($repositoryRoot, $expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to run: this script must be launched from D:\TTCS_T926_K17C3_N2\backend.'
}
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw 'Refusing to run: backend project file was not found.'
}
if (-not (Test-Path -LiteralPath $testSettingsPath -PathType Leaf)) {
    throw 'Refusing to run: appsettings.Test.json was not found.'
}
if (-not (Test-Path -LiteralPath $programPath -PathType Leaf)) {
    throw 'Refusing to run: Program.cs was not found.'
}

$testSettings = Get-Content -LiteralPath $testSettingsPath -Raw | ConvertFrom-Json
if ($null -eq $testSettings.EmailWorker -or $testSettings.EmailWorker.Enabled -ne $false) {
    throw 'Refusing to run: appsettings.Test.json must explicitly set EmailWorker.Enabled to false.'
}

$sourceFiles = Get-ChildItem -LiteralPath $backendRoot -Filter '*.cs' -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$automaticSchemaCalls = $sourceFiles | Select-String -Pattern 'Database\.(MigrateAsync?|EnsureCreatedAsync?)\s*\('
if ($automaticSchemaCalls) {
    throw 'Refusing to run: automatic database schema creation or migration calls were found in backend source.'
}

$tcpQueryVerified = $false
$tcpQueryErrors = @()
$tcpListeners = @()
try {
    $tcpListeners = @(Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue -ErrorVariable +tcpQueryErrors)
    $unexpectedTcpQueryErrors = @($tcpQueryErrors | Where-Object {
        $_.Exception.Message -notmatch 'No MSFT_NetTCPConnection objects found'
    })
    if ($unexpectedTcpQueryErrors.Count -eq 0) {
        $tcpQueryVerified = $true
    }
}
catch {
    $tcpQueryVerified = $false
}
if ($tcpListeners.Count -gt 0) {
    throw "Refusing to run: port $Port already has a listener. Stop or identify it outside this script; this script never stops processes."
}

$netstatVerified = $false
$netstatListeners = @()
$netstatCommand = Get-Command -Name 'netstat.exe' -CommandType Application -ErrorAction SilentlyContinue
if ($netstatCommand) {
    try {
        $netstatOutput = @(& $netstatCommand.Source -ano -p tcp 2>$null)
        if ($LASTEXITCODE -eq 0) {
            $netstatVerified = $true
            $listenerPattern = '^\s*TCP\s+(?:\[[^\]]+\]|[^\s:]+):' + $Port + '\s+.*\s+LISTENING\s+\d+\s*$'
            $netstatListeners = @($netstatOutput | Where-Object { $_ -match $listenerPattern })
        }
    }
    catch {
        $netstatVerified = $false
    }
}
if ($netstatListeners.Count -gt 0) {
    throw "Refusing to run: port $Port already has a listener. Stop or identify it outside this script; this script never stops processes."
}

$portProbe = $null
$bindVerified = $false
try {
    $portProbe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    $portProbe.Start()
    $bindVerified = $true
}
catch [System.Net.Sockets.SocketException] {
    if ($_.Exception.SocketErrorCode -eq [System.Net.Sockets.SocketError]::AddressAlreadyInUse -or $_.Exception.NativeErrorCode -eq 10048) {
        throw "Refusing to run: port $Port already has a listener. Stop or identify it outside this script; this script never stops processes."
    }
}
catch {
    $bindVerified = $false
}
finally {
    if ($null -ne $portProbe) {
        try { $portProbe.Stop() } catch { }
    }
}

if (-not $bindVerified -or (-not $tcpQueryVerified -and -not $netstatVerified)) {
    throw 'Refusing to run: could not verify whether the requested backend port is free.'
}
if (-not $tcpQueryVerified) {
    Write-Warning 'Get-NetTCPConnection was unavailable; netstat and a local bind probe verified the port.'
}

$connectionString = $env:ConnectionStrings__TestConnection
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    $connectionString = ConvertFrom-SecurePrompt -Prompt 'Enter the Test-only MySQL connection string (input is hidden)'
}

$connectionBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
try {
    $connectionBuilder.ConnectionString = $connectionString
}
catch {
    throw 'Refusing to run: the Test connection string could not be parsed.'
}

$database = Get-ConnectionValue -Builder $connectionBuilder -Names @('Database', 'Initial Catalog')
$server = Get-ConnectionValue -Builder $connectionBuilder -Names @('Server', 'Host', 'Data Source', 'Address', 'Network Address')
$user = Get-ConnectionValue -Builder $connectionBuilder -Names @('User ID', 'UID', 'User', 'Username')
$password = Get-ConnectionValue -Builder $connectionBuilder -Names @('Password', 'Pwd')

if ($database -cne 'ttcs_sprint4_test') {
    throw 'Refusing to run: Test connection must target database ttcs_sprint4_test.'
}
if ($server -notin @('localhost', '127.0.0.1', '::1')) {
    throw 'Refusing to run: Test connection must use a local MySQL server.'
}
if ($user -cne 'ttcs_s4_run_bb7be801') {
    throw 'Refusing to run: Test connection must use the ttcs_s4_run_bb7be801 runtime account.'
}
if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'Refusing to run: Test runtime account password is missing.'
}

$jwtKey = $env:TTCS_TEST_JWT_KEY
if ([string]::IsNullOrWhiteSpace($jwtKey)) {
    $jwtKey = ConvertFrom-SecurePrompt -Prompt 'Enter the Test-only JWT signing key (input is hidden)'
}
if ($jwtKey.Length -lt 32) {
    throw 'Refusing to run: Test JWT signing key must contain at least 32 characters.'
}

$confirmation = Read-Host 'Only after independently verifying ttcs_sprint4_test exists, SELECT DATABASE() and CURRENT_USER() identify ttcs_sprint4_test/ttcs_s4_run_bb7be801, and SHOW GRANTS plus a denied access check confirm no internship_management access, type TEST-DB-ISOLATION-VERIFIED'
if ($confirmation -cne 'TEST-DB-ISOLATION-VERIFIED') {
    throw 'Refusing to run: database privilege isolation was not confirmed.'
}

$managedNames = @(
    'ASPNETCORE_ENVIRONMENT',
    'DOTNET_ENVIRONMENT',
    'ASPNETCORE_URLS',
    'ConnectionStrings__TestConnection',
    'ConnectionStrings__DefaultConnection',
    'TTCS_TEST_JWT_KEY',
    'EmailWorker__Enabled',
    'MYSQL_HOST',
    'MYSQL_PORT',
    'MYSQL_DATABASE',
    'MYSQL_USER',
    'MYSQL_PASSWORD',
    'MYSQL_URL',
    'DATABASE_URL',
    'JWT_KEY',
    'JWT_SECRET_KEY',
    'SMTP_HOST',
    'SMTP_PORT',
    'SMTP_USER',
    'SMTP_PASS',
    'MAIL_FROM',
    'MAIL_FROM_NAME'
)
$previousEnvironment = @{}
foreach ($name in $managedNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    foreach ($name in @(
        'ConnectionStrings__DefaultConnection',
        'MYSQL_HOST', 'MYSQL_PORT', 'MYSQL_DATABASE', 'MYSQL_USER', 'MYSQL_PASSWORD',
        'MYSQL_URL', 'DATABASE_URL', 'JWT_KEY', 'JWT_SECRET_KEY',
        'SMTP_HOST', 'SMTP_PORT', 'SMTP_USER', 'SMTP_PASS', 'MAIL_FROM', 'MAIL_FROM_NAME'
    )) {
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }

    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Test', 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Test', 'Process')
    [Environment]::SetEnvironmentVariable('ASPNETCORE_URLS', "http://127.0.0.1:$Port", 'Process')
    [Environment]::SetEnvironmentVariable('ConnectionStrings__TestConnection', $connectionString, 'Process')
    [Environment]::SetEnvironmentVariable('TTCS_TEST_JWT_KEY', $jwtKey, 'Process')
    [Environment]::SetEnvironmentVariable('EmailWorker__Enabled', 'false', 'Process')

    Write-Host "Starting D-drive backend in Test mode on http://127.0.0.1:$Port."
    Write-Host 'Database is restricted by configuration to ttcs_sprint4_test; Email Worker is disabled.'
    Push-Location $backendRoot
    try {
        & dotnet run --project $projectPath --no-launch-profile --no-restore
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
}
finally {
    foreach ($name in $managedNames) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }
}

if ($null -ne $exitCode) {
    exit $exitCode
}
