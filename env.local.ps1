$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $repoRoot '.env'
$supabaseConfigPath = Join-Path $repoRoot 'supabase\config.toml'

if (-not (Test-Path $envFile)) {
    throw "Missing local environment file at '$envFile'."
}

$localProjectId = $null
if (Test-Path $supabaseConfigPath) {
    $projectIdLine = Select-String -Path $supabaseConfigPath -Pattern '^\s*project_id\s*=\s*"([^"]+)"' | Select-Object -First 1
    if ($projectIdLine -and $projectIdLine.Matches.Count -gt 0) {
        $localProjectId = $projectIdLine.Matches[0].Groups[1].Value
    }
}

$forbiddenRemoteKeys = @(
    'SUPABASE_ACCESS_TOKEN',
    'SUPABASE_DB_PASSWORD'
)

$localUrlPrefixes = @(
    'http://127.0.0.1',
    'https://127.0.0.1',
    'http://localhost',
    'https://localhost'
)

$localOnlyRedirectVariables = @(
    'SUPABASE_AUTH_EXTERNAL_GOOGLE_REDIRECT_URI'
)

Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*#' -or $_ -match '^\s*$') { return }

    $name, $value = $_ -split '=', 2
    $name = $name.Trim()
    $value = $value.Trim()

    if ([string]::IsNullOrWhiteSpace($name)) {
        return
    }

    if ($forbiddenRemoteKeys -contains $name) {
        throw "env.local.ps1 refuses to load '$name' from .env. Keep remote deploy credentials in GitHub secrets only."
    }

    if ($name -eq 'SUPABASE_PROJECT_ID' -and -not [string]::IsNullOrWhiteSpace($value)) {
        if ([string]::IsNullOrWhiteSpace($localProjectId)) {
            throw "env.local.ps1 cannot validate SUPABASE_PROJECT_ID because supabase/config.toml does not declare a local project_id."
        }

        if ($value -ne $localProjectId) {
            throw "env.local.ps1 refuses hosted SUPABASE_PROJECT_ID '$value'. Expected local project_id '$localProjectId' from supabase/config.toml."
        }
    }

    if ($name -eq 'SUPABASE_URL' -and -not [string]::IsNullOrWhiteSpace($value)) {
        $isLocalUrl = $false
        foreach ($prefix in $localUrlPrefixes) {
            if ($value.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $isLocalUrl = $true
                break
            }
        }

        if (-not $isLocalUrl) {
            throw "env.local.ps1 refuses hosted SUPABASE_URL '$value'. Local shells may only target localhost or 127.0.0.1."
        }
    }

    if ($localOnlyRedirectVariables -contains $name -and -not [string]::IsNullOrWhiteSpace($value)) {
        $isLocalRedirect = $false
        foreach ($prefix in $localUrlPrefixes) {
            if ($value.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $isLocalRedirect = $true
                break
            }
        }

        if (-not $isLocalRedirect) {
            throw "env.local.ps1 refuses non-local redirect URI '$value' for '$name'."
        }
    }

    [System.Environment]::SetEnvironmentVariable($name, $value, 'Process')
}
