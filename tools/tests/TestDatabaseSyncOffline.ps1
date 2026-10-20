$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$schema = 'internship_sprint5_v2_verify_20261017'
$setupPath = Join-Path $root 'backend\database\setup\20261017_new_database_install_v2_internship_sprint5_v2_verify_20261017.sql'
$preflightPath = Join-Path $root 'backend\database\setup\preflight_internship_sprint5_v2_verify_20261017.sql'
$reviewPath = Join-Path $root 'backend\database\review\20261017_internship_restore_check_us33_upgrade_review_only.sql'
$checksumPath = "$setupPath.sha256"
$expectedHash = 'CD5128BC260B833439E527402FFDFDB26399B2DB4C0884C22F813780D60CA96C'
function Assert-ReleaseCheck([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAIL $Name" }
    Write-Output "PASS $Name"
}
$sql = [System.IO.File]::ReadAllText($setupPath, [System.Text.Encoding]::UTF8)
$actualHash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
Assert-ReleaseCheck ($actualHash -eq $expectedHash) 'Owner-verified installer SHA-256'
$sidecar = [System.IO.File]::ReadAllText($checksumPath, [System.Text.Encoding]::ASCII).Trim()
Assert-ReleaseCheck ($sidecar -eq "$expectedHash  $(Split-Path -Leaf $setupPath)") 'Checksum sidecar matches installer'
Assert-ReleaseCheck ($sql -match [regex]::Escape($schema)) 'Installer names dedicated schema'
Assert-ReleaseCheck ($sql -notmatch '(?i)internship_management|internship_restore_check|internship_sprint5_dev') 'Installer does not reference protected or prior schemas'
$useMatches = [regex]::Matches($sql, '(?im)^\s*USE\s+\x60(?<schema>[A-Za-z0-9_]+)\x60\s*;')
Assert-ReleaseCheck ($useMatches.Count -eq 1 -and $useMatches[0].Groups['schema'].Value -ceq $schema) 'Exactly one USE targets the dedicated schema'
$qualified = [regex]::Matches($sql, '\x60(?<schema>[A-Za-z0-9_]+)\x60\s*\.\s*\x60[^\x60]+\x60')
Assert-ReleaseCheck ($qualified.Count -gt 0 -and @($qualified | Where-Object {$_.Groups['schema'].Value -cne $schema}).Count -eq 0) 'All qualified object references use the dedicated schema'
Assert-ReleaseCheck ($sql -notmatch '(?im)^\s*(?:CREATE|DROP)\s+DATABASE\b|^\s*(?:DROP\s+TABLE|TRUNCATE\s+TABLE|DELETE\s+FROM)\b') 'Installer does not create/drop database or delete/truncate data'
$preflight = [System.IO.File]::ReadAllText($preflightPath, [System.Text.Encoding]::UTF8)
Assert-ReleaseCheck ($preflight.TrimStart().StartsWith('SELECT', [System.StringComparison]::OrdinalIgnoreCase) -and $preflight.Contains($schema) -and $preflight -notmatch '(?im)^\s*(?:INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|TRUNCATE|GRANT|REVOKE)\b') 'Preflight is SELECT-only and pinned to the dedicated schema'
$review = [System.IO.File]::ReadAllText($reviewPath, [System.Text.Encoding]::UTF8)
$executableReview = [regex]::Replace($review, '(?s)/\*.*?\*/', '')
$executableReview = [regex]::Replace($executableReview, '(?m)^\s*--.*$', '')
$reviewStatements = @($executableReview -split ';' | ForEach-Object {$_.Trim()} | Where-Object {$_})
Assert-ReleaseCheck ($reviewStatements.Count -gt 0 -and @($reviewStatements | Where-Object {$_ -notmatch '(?is)^SELECT\b'}).Count -eq 0) 'Existing-database candidate is review-only with SELECT executable statements'
$gaps = Get-Content -Raw -Encoding utf8 (Join-Path $root 'tools\DatabaseSchemaChecker\known-source-gaps.json') | ConvertFrom-Json
Assert-ReleaseCheck (@($gaps | Where-Object {$_.migrationId -eq '20261008065402_AddRegistrationReceivedEmailTemplate'}).Count -eq 1) 'SOURCE_MISSING remains explicitly recorded'
$runner = [System.IO.File]::ReadAllText((Join-Path $root 'tools\check-mysql-schema.ps1'), [System.Text.Encoding]::UTF8)
Assert-ReleaseCheck ($runner.Contains("'$schema'") -and $runner.Contains("internship_management")) 'Checker allows verified Test target and blocks production'
$guide = [System.IO.File]::ReadAllText((Join-Path $root 'backend\database\README.md'), [System.Text.Encoding]::UTF8)
$guideLower = $guide.ToLowerInvariant()
Assert-ReleaseCheck ($guideLower.Contains('source_missing') -and $guideLower.Contains('không chạy file cài mới') -and $guideLower.Contains('chưa được kiểm thử runtime')) 'Member guide distinguishes fresh install from blocked existing-database upgrade'
$project = Join-Path $root 'tools\DatabaseSchemaChecker\DatabaseSchemaChecker.csproj'
& dotnet run --project $project -- --self-test
if ($LASTEXITCODE -ne 0) { throw "Checker offline tests failed with exit code $LASTEXITCODE" }