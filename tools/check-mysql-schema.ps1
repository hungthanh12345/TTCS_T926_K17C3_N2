param(
    [Parameter(Mandatory = $true)]
    [string]$Schema,

    [Parameter(Mandatory = $true)]
    [string]$ConfirmTestSchema,

    [switch]$ShowMatched,

    [switch]$ExportCheckDetails
)

$ErrorActionPreference = 'Stop'
$allowedSchemas = @('internship_restore_check', 'ttcs_sprint4_test', 'internship_sprint5_dev', 'internship_sprint5_v2_verify_20261017')

if ($Schema -cnotin $allowedSchemas) {
    throw 'Schema is not in the checker Test allowlist.'
}
if ($Schema -imatch '^internship_management') {
    throw 'Production schema is never allowed.'
}
if ($ConfirmTestSchema -cne $Schema) {
    throw 'Type the exact Test schema name in -ConfirmTestSchema.'
}

$projectPath = Join-Path $PSScriptRoot 'DatabaseSchemaChecker/DatabaseSchemaChecker.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw 'Database checker project was not found.'
}

$hadOriginalConnection = Test-Path Env:TTCS_SCHEMA_CHECK_CONNECTION_STRING
$originalConnection = $env:TTCS_SCHEMA_CHECK_CONNECTION_STRING
$secureStringPointer = [IntPtr]::Zero
$secureConnection = $null

try {
    # Never reuse inherited credentials; this invocation always obtains them locally.
    Remove-Item Env:TTCS_SCHEMA_CHECK_CONNECTION_STRING -ErrorAction SilentlyContinue
    $secureConnection = Read-Host 'Enter the loopback, SELECT-only Test connection string' -AsSecureString
    if ($secureConnection.Length -eq 0) {
        throw 'A Test connection string is required.'
    }
    $secureStringPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureConnection)
    $env:TTCS_SCHEMA_CHECK_CONNECTION_STRING = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($secureStringPointer)

    $arguments = @(
        'run',
        '--project', $projectPath,
        '--',
        '--schema', $Schema,
        '--confirm-test-schema', $ConfirmTestSchema
    )
    if ($ShowMatched) {
        $arguments += '--show-matched'
    }
    if ($ExportCheckDetails) {
        $arguments += '--export-check-details'
    }

    & dotnet @arguments
    exit $LASTEXITCODE
}
finally {
    if ($secureStringPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secureStringPointer)
    }
    if ($null -ne $secureConnection) {
        $secureConnection.Dispose()
    }
    if ($hadOriginalConnection) {
        $env:TTCS_SCHEMA_CHECK_CONNECTION_STRING = $originalConnection
    }
    else {
        Remove-Item Env:TTCS_SCHEMA_CHECK_CONNECTION_STRING -ErrorAction SilentlyContinue
    }
}
