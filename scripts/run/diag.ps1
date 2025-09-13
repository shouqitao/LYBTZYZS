# Environment Diagnostics Script
# Collects system environment information for WebAPI troubleshooting
param(
    [Parameter(Mandatory=$false)]
    [int]$PreferredPort = 8080,
    
    [Parameter(Mandatory=$false)]
    [int]$FallbackPort = 5080
)

$OutputFile = "_reports/2025-09/webapi/run-fix/diag.md"

Write-Host "🔍 Collecting environment diagnostics..." -ForegroundColor Green
Write-Host "📝 Output: $OutputFile" -ForegroundColor Yellow

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path (Split-Path $OutputFile -Parent) | Out-Null

# Start building diagnostic report
$report = @"
# WebAPI Environment Diagnostics

Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## System Information

### .NET Version
``````
"@

# Get .NET version
try {
    $dotnetVersion = dotnet --version 2>&1
    $report += "`n$dotnetVersion`n"
} catch {
    $report += "`nFailed to get .NET version: $($_.Exception.Message)`n"
}

$report += @"
``````

### Environment Variables
``````
"@

# Get relevant environment variables
$envVars = @(
    "ASPNETCORE_ENVIRONMENT", 
    "ASPNETCORE_URLS", 
    "DOTNET_ENVIRONMENT",
    "ConnectionStrings__DefaultConnection",
    "JwtOptions__Secret",
    "LYBT_DB_CONNECTION"
)

foreach ($envVar in $envVars) {
    $value = [Environment]::GetEnvironmentVariable($envVar)
    if ($value) {
        # Mask sensitive data
        if ($envVar -like "*Secret*" -or $envVar -like "*Password*" -or $envVar -like "*Connection*") {
            $maskedValue = if ($value.Length -gt 10) { $value.Substring(0, 5) + "***" + $value.Substring($value.Length - 3) } else { "***" }
            $report += "`n$envVar = $maskedValue`n"
        } else {
            $report += "`n$envVar = $value`n"
        }
    } else {
        $report += "`n$envVar = (not set)`n"
    }
}

$report += @"
``````

### Port Availability Check

"@

# Check port availability
function Test-Port {
    param([int]$Port)
    try {
        $listener = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
        $inUse = $listener.GetActiveTcpListeners() | Where-Object { $_.Port -eq $Port }
        return $inUse -eq $null
    } catch {
        return $false
    }
}

$port8080Available = Test-Port -Port $PreferredPort
$port5080Available = Test-Port -Port $FallbackPort

if ($port8080Available) {
    $report += "- Port ${PreferredPort}: Available`n"
    $selectedPort = $PreferredPort
} else {
    $report += "- Port ${PreferredPort}: In use`n"
    if ($port5080Available) {
        $report += "- Port ${FallbackPort}: Available (will use as fallback)`n"
        $selectedPort = $FallbackPort
    } else {
        $report += "- Port ${FallbackPort}: In use`n"
        $selectedPort = $null
    }
}

$report += "`n**Selected Port: $selectedPort**`n`n"

# Check dev certificates
$report += @"
### Development Certificates
``````
"@

try {
    $certInfo = dotnet dev-certs https --check 2>&1
    $report += "`n$certInfo`n"
} catch {
    $report += "`nFailed to check dev certificates: $($_.Exception.Message)`n"
}

$report += @"
``````

### Active TCP Listeners
``````
"@

try {
    $listeners = netstat -an | Select-String ":808" | Select-String "LISTENING"
    if ($listeners) {
        $report += "`n$($listeners -join "`n")`n"
    } else {
        $report += "`nNo listeners on 808x ports found`n"
    }
} catch {
    $report += "`nFailed to get port information: $($_.Exception.Message)`n"
}

$report += @"
``````

## Configuration Files

### appsettings.json
``````json
"@

# Read appsettings.json
$appsettingsPath = "src/Server/Services/LYBT.WebAPI/appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $appsettingsContent = Get-Content $appsettingsPath -Raw
        # Remove sensitive data
        $appsettingsContent = $appsettingsContent -replace '"DefaultConnection":\s*"[^"]*"', '"DefaultConnection": "***MASKED***"'
        $appsettingsContent = $appsettingsContent -replace '"Secret":\s*"[^"]*"', '"Secret": "***MASKED***"'
        $report += "`n$appsettingsContent`n"
    } catch {
        $report += "`nFailed to read appsettings.json: $($_.Exception.Message)`n"
    }
} else {
    $report += "`nappsettings.json not found at: $appsettingsPath`n"
}

$report += @"
``````

### appsettings.Development.json
``````json
"@

# Read appsettings.Development.json
$appsettingsDevPath = "src/Server/Services/LYBT.WebAPI/appsettings.Development.json"
if (Test-Path $appsettingsDevPath) {
    try {
        $appsettingsDevContent = Get-Content $appsettingsDevPath -Raw
        # Remove sensitive data
        $appsettingsDevContent = $appsettingsDevContent -replace '"DefaultConnection":\s*"[^"]*"', '"DefaultConnection": "***MASKED***"'
        $appsettingsDevContent = $appsettingsDevContent -replace '"Secret":\s*"[^"]*"', '"Secret": "***MASKED***"'
        $report += "`n$appsettingsDevContent`n"
    } catch {
        $report += "`nFailed to read appsettings.Development.json: $($_.Exception.Message)`n"
    }
} else {
    $report += "`nappsettings.Development.json not found at: $appsettingsDevPath`n"
}

$report += @"
``````

## Recommendations

"@

if (-not $port8080Available) {
    $report += "- WARNING: Port ${PreferredPort} is occupied. Use scripts with -Port ${FallbackPort} parameter`n"
}

if ($selectedPort) {
    $report += "- OK: Use port $selectedPort for WebAPI startup`n"
} else {
    $report += "- ERROR: Both preferred ports are occupied. Consider using different ports`n"
}

$report += @"
- Set ASPNETCORE_URLS=http://localhost:$selectedPort before running
- Check connection strings and JWT secrets are properly configured
- After startup, use health.ps1 to verify API availability

---
*Generated by diag.ps1*
"@

# Write report to file
$report | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "✅ Diagnostics complete! Check $OutputFile" -ForegroundColor Green

if ($selectedPort) {
    Write-Host "🎯 Recommended port: $selectedPort" -ForegroundColor Cyan
} else {
    Write-Host "⚠️ No available ports found in preferred range" -ForegroundColor Yellow
}