$ErrorActionPreference = 'Stop'
$backendRoot = $PSScriptRoot
$envPath = Join-Path $backendRoot '.env'
$values = @{}

if (Test-Path -LiteralPath $envPath) {
    foreach ($line in [System.IO.File]::ReadAllLines($envPath)) {
        $entry = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($entry) -or $entry.StartsWith('#')) { continue }

        $separator = $entry.IndexOf('=')
        if ($separator -le 0) { continue }

        $name = $entry.Substring(0, $separator).Trim()
        $value = $entry.Substring($separator + 1).Trim()
        if ($value.Length -ge 2 -and
            (($value.StartsWith('"') -and $value.EndsWith('"')) -or
             ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $values[$name] = $value
    }

    $mysqlNames = @('MYSQL_HOST', 'MYSQL_PORT', 'MYSQL_DATABASE', 'MYSQL_USER', 'MYSQL_PASSWORD')
    $mysqlReady = -not [string]::IsNullOrWhiteSpace($values['MYSQL_PASSWORD'])
    foreach ($name in @('MYSQL_HOST', 'MYSQL_PORT', 'MYSQL_DATABASE', 'MYSQL_USER')) {
        if ([string]::IsNullOrWhiteSpace($values[$name])) { $mysqlReady = $false }
    }

    if ($mysqlReady) {
        foreach ($name in $mysqlNames) {
            [Environment]::SetEnvironmentVariable($name, $values[$name], 'Process')
        }
        Write-Host 'Loaded local MySQL configuration.'
    }
    elseif ($mysqlNames | Where-Object { -not [string]::IsNullOrWhiteSpace($values[$_]) }) {
        Write-Host 'MYSQL_PASSWORD is blank or MySQL settings are incomplete; existing connection configuration will be used.'
    }

    $otherNames = @(
        'JWT_KEY', 'JWT_SECRET_KEY', 'JWT_ISSUER', 'JWT_AUDIENCE',
        'SMTP_HOST', 'SMTP_PORT', 'SMTP_USER', 'SMTP_PASS',
        'MAIL_FROM', 'MAIL_FROM_NAME', 'DATABASE_URL', 'MYSQL_URL'
    )
    foreach ($name in $otherNames) {
        if (-not [string]::IsNullOrWhiteSpace($values[$name])) {
            [Environment]::SetEnvironmentVariable($name, $values[$name], 'Process')
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($values['JWT_KEY']) -and
        [string]::IsNullOrWhiteSpace($values['JWT_SECRET_KEY'])) {
        [Environment]::SetEnvironmentVariable('JWT_SECRET_KEY', $values['JWT_KEY'], 'Process')
    }
    Write-Host 'Loaded non-empty local environment values; secret values are not displayed.'
}
else {
    Write-Host 'backend/.env not found; .NET User Secrets and existing environment variables remain available.'
}

Set-Location $backendRoot
dotnet run --urls=http://localhost:5000
