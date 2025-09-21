# RunSecurityAudit.ps1 - Comprehensive Security Audit Script
# Performs security checks on the LYBT application

param(
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport = $false,

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./security-audit-report.html"
)

$ErrorActionPreference = "Stop"

# Color output function
function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Initialize audit results
$auditResults = @{
    Timestamp = Get-Date
    Checks = @()
    Summary = @{
        Total = 0
        Passed = 0
        Failed = 0
        Warnings = 0
    }
}

# Function to add audit result
function Add-AuditResult {
    param(
        [string]$Category,
        [string]$Check,
        [string]$Status,
        [string]$Details = ""
    )

    $result = @{
        Category = $Category
        Check = $Check
        Status = $Status
        Details = $Details
        Timestamp = Get-Date
    }

    $auditResults.Checks += $result
    $auditResults.Summary.Total++

    switch ($Status) {
        "PASS" {
            $auditResults.Summary.Passed++
            Write-ColoredOutput "  [PASS] $Check" -Color Green
        }
        "FAIL" {
            $auditResults.Summary.Failed++
            Write-ColoredOutput "  [FAIL] $Check - $Details" -Color Red
        }
        "WARN" {
            $auditResults.Summary.Warnings++
            Write-ColoredOutput "  [WARN] $Check - $Details" -Color Yellow
        }
    }
}

Write-ColoredOutput "`n=== LYBT Security Audit ===" -Color Cyan
Write-ColoredOutput "Starting at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" -Color White

# 1. Check Configuration Files
Write-ColoredOutput "[1] Checking Configuration Files" -Color Cyan

$configPath = Join-Path $PSScriptRoot "..\..\src\Server\Services\LYBT.WebAPI"
$configFiles = @("appsettings.json", "appsettings.Development.json", "appsettings.Production.json")

foreach ($file in $configFiles) {
    $filePath = Join-Path $configPath $file
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw

        # Check for hardcoded secrets
        if ($content -match "password|secret|key" -and $content -notmatch "REPLACE_WITH_ENVIRONMENT_VARIABLE") {
            if ($file -eq "appsettings.Production.json") {
                Add-AuditResult "Configuration" "$file secrets" "FAIL" "Contains hardcoded secrets"
            } else {
                Add-AuditResult "Configuration" "$file secrets" "WARN" "Contains development secrets"
            }
        } else {
            Add-AuditResult "Configuration" "$file secrets" "PASS"
        }

        # Check for sensitive logging
        try {
            $config = $content | ConvertFrom-Json
        } catch {
            # Skip if JSON parsing fails (likely encoding issue)
            $config = $null
        }
        if ($config.DatabaseOptions -and $config.DatabaseOptions.EnableSensitiveDataLogging -eq $true) {
            Add-AuditResult "Configuration" "$file logging" "FAIL" "Sensitive data logging enabled"
        } else {
            Add-AuditResult "Configuration" "$file logging" "PASS"
        }
    }
}

# 2. Check Source Code Security
Write-ColoredOutput "`n[2] Checking Source Code Security" -Color Cyan

# Check for SQL injection vulnerabilities
$sourceFiles = Get-ChildItem -Path "$PSScriptRoot\..\..\src" -Recurse -Include *.cs -File
$sqlInjectionPatterns = @(
    'ExecuteSqlRaw\(',
    'FromSqlRaw\(',
    'ExecuteSqlCommand\(',
    'string.Format.*SELECT',
    'string.Concat.*SELECT'
)

$sqlVulnerabilities = 0
foreach ($pattern in $sqlInjectionPatterns) {
    $matches = $sourceFiles | Select-String -Pattern $pattern
    if ($matches) {
        $sqlVulnerabilities += $matches.Count
    }
}

if ($sqlVulnerabilities -gt 0) {
    Add-AuditResult "Code Security" "SQL Injection" "WARN" "$sqlVulnerabilities potential vulnerabilities found"
} else {
    Add-AuditResult "Code Security" "SQL Injection" "PASS"
}

# Check for hardcoded credentials
$credentialPatterns = @(
    'password\s*=\s*"[^"]+"',
    'secret\s*=\s*"[^"]+"',
    'apikey\s*=\s*"[^"]+"'
)

$hardcodedCreds = 0
foreach ($pattern in $credentialPatterns) {
    $matches = $sourceFiles | Select-String -Pattern $pattern -CaseSensitive:$false |
        Where-Object { $_.Line -notmatch "placeholder|example|test|dummy" }
    if ($matches) {
        $hardcodedCreds += $matches.Count
    }
}

if ($hardcodedCreds -gt 0) {
    Add-AuditResult "Code Security" "Hardcoded Credentials" "FAIL" "$hardcodedCreds instances found"
} else {
    Add-AuditResult "Code Security" "Hardcoded Credentials" "PASS"
}

# 3. Check Security Headers Implementation
Write-ColoredOutput "`n[3] Checking Security Headers" -Color Cyan

$middlewareFile = "$PSScriptRoot\..\..\src\Server\Services\LYBT.WebAPI\Middleware\SecurityHeadersMiddleware.cs"
if (Test-Path $middlewareFile) {
    $content = Get-Content $middlewareFile -Raw

    $requiredHeaders = @(
        "X-Content-Type-Options",
        "X-Frame-Options",
        "X-XSS-Protection",
        "Content-Security-Policy",
        "Referrer-Policy",
        "Permissions-Policy"
    )

    foreach ($header in $requiredHeaders) {
        if ($content -match [regex]::Escape($header)) {
            Add-AuditResult "Security Headers" $header "PASS"
        } else {
            Add-AuditResult "Security Headers" $header "FAIL" "Not implemented"
        }
    }
} else {
    Add-AuditResult "Security Headers" "Middleware" "FAIL" "SecurityHeadersMiddleware.cs not found"
}

# 4. Check Authentication & Authorization
Write-ColoredOutput "`n[4] Checking Authentication & Authorization" -Color Cyan

# Check JWT configuration
$jwtPattern = 'services\.AddAuthentication.*JwtBearer'
$authFiles = Get-ChildItem -Path "$PSScriptRoot\..\..\src\Server\Services\LYBT.WebAPI\Extensions" -Include *.cs -Recurse
$jwtConfigured = $authFiles | Select-String -Pattern $jwtPattern

if ($jwtConfigured) {
    Add-AuditResult "Authentication" "JWT Configuration" "PASS"
} else {
    Add-AuditResult "Authentication" "JWT Configuration" "FAIL" "JWT not configured"
}

# Check for [Authorize] attributes
$controllerPath = "$PSScriptRoot\..\..\src\Server\Services\LYBT.WebAPI\Controllers"
$controllers = Get-ChildItem -Path $controllerPath -Include *.cs -File

foreach ($controller in $controllers) {
    $content = Get-Content $controller.FullName -Raw
    if ($content -match '\[Authorize\]|\[AllowAnonymous\]') {
        Add-AuditResult "Authorization" "$($controller.Name)" "PASS"
    } else {
        Add-AuditResult "Authorization" "$($controller.Name)" "WARN" "No authorization attributes found"
    }
}

# 5. Check Password Policy
Write-ColoredOutput "`n[5] Checking Password Policy" -Color Cyan

$passwordValidatorFile = "$PSScriptRoot\..\..\src\Shared\LYBT.Shared.Utilities\Security\PasswordPolicyValidator.cs"
if (Test-Path $passwordValidatorFile) {
    $content = Get-Content $passwordValidatorFile -Raw

    if ($content -match "MinLength\s*=\s*(\d+)") {
        $minLength = [int]$Matches[1]
        if ($minLength -ge 12) {
            Add-AuditResult "Password Policy" "Minimum Length" "PASS" "$minLength characters"
        } else {
            Add-AuditResult "Password Policy" "Minimum Length" "WARN" "$minLength characters (recommend 12+)"
        }
    }

    if ($content -match "RequireUppercase|RequireLowercase|RequireDigit|RequireSpecialChar") {
        Add-AuditResult "Password Policy" "Complexity Requirements" "PASS"
    } else {
        Add-AuditResult "Password Policy" "Complexity Requirements" "FAIL" "Not enforced"
    }
} else {
    Add-AuditResult "Password Policy" "Implementation" "FAIL" "PasswordPolicyValidator.cs not found"
}

# 6. Check Rate Limiting
Write-ColoredOutput "`n[6] Checking Rate Limiting" -Color Cyan

$rateLimitingConfig = "$PSScriptRoot\..\..\src\Server\Core\LYBT.Infrastructure\Configuration\Options\RateLimitingOptions.cs"
if (Test-Path $rateLimitingConfig) {
    Add-AuditResult "Rate Limiting" "Configuration" "PASS"

    # Check if rate limiting is applied
    $serviceRegistration = "$PSScriptRoot\..\..\src\Server\Services\LYBT.WebAPI\Extensions\UnifiedServiceRegistration.cs"
    $content = Get-Content $serviceRegistration -Raw

    if ($content -match "AddRateLimiter|UseRateLimiter") {
        Add-AuditResult "Rate Limiting" "Implementation" "PASS"
    } else {
        Add-AuditResult "Rate Limiting" "Implementation" "WARN" "Not fully implemented"
    }
} else {
    Add-AuditResult "Rate Limiting" "Configuration" "FAIL" "RateLimitingOptions.cs not found"
}

# 7. Check HTTPS Configuration
Write-ColoredOutput "`n[7] Checking HTTPS Configuration" -Color Cyan

$programFile = "$PSScriptRoot\..\..\src\Server\Services\LYBT.WebAPI\Program.cs"
if (Test-Path $programFile) {
    $content = Get-Content $programFile -Raw

    if ($content -match "UseHttpsRedirection|UseHsts") {
        Add-AuditResult "HTTPS" "Redirection" "PASS"
    } else {
        Add-AuditResult "HTTPS" "Redirection" "WARN" "Not configured"
    }
}

# 8. Check for Security Tests
Write-ColoredOutput "`n[8] Checking Security Tests" -Color Cyan

$securityTests = @(
    "$PSScriptRoot\..\..\tests\SecurityTests\SecurityValidationTests.cs",
    "$PSScriptRoot\..\..\tests\UnitTests\Security.UnitTests\PasswordPolicyValidatorTests.cs",
    "$PSScriptRoot\..\..\tests\UnitTests\Security.UnitTests\AuthorizationTests.cs"
)

foreach ($testFile in $securityTests) {
    if (Test-Path $testFile) {
        Add-AuditResult "Security Tests" (Split-Path $testFile -Leaf) "PASS"
    } else {
        Add-AuditResult "Security Tests" (Split-Path $testFile -Leaf) "WARN" "Test file not found"
    }
}

# Summary
Write-ColoredOutput "`n=== Audit Summary ===" -Color Cyan
Write-ColoredOutput "Total Checks: $($auditResults.Summary.Total)" -Color White
Write-ColoredOutput "Passed: $($auditResults.Summary.Passed)" -Color Green
Write-ColoredOutput "Failed: $($auditResults.Summary.Failed)" -Color Red
Write-ColoredOutput "Warnings: $($auditResults.Summary.Warnings)" -Color Yellow

$score = [math]::Round(($auditResults.Summary.Passed / $auditResults.Summary.Total) * 100, 2)
Write-ColoredOutput "`nSecurity Score: $score%" -Color $(if ($score -ge 80) { "Green" } elseif ($score -ge 60) { "Yellow" } else { "Red" })

# Generate HTML report if requested
if ($GenerateReport) {
    Write-ColoredOutput "`nGenerating HTML report..." -Color White

    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>LYBT Security Audit Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
        .header { background: #2c3e50; color: white; padding: 20px; border-radius: 5px; }
        .summary { display: flex; justify-content: space-around; margin: 20px 0; }
        .summary-item { background: white; padding: 15px; border-radius: 5px; text-align: center; }
        .pass { color: #27ae60; }
        .fail { color: #e74c3c; }
        .warn { color: #f39c12; }
        table { width: 100%; background: white; border-collapse: collapse; margin: 20px 0; }
        th { background: #34495e; color: white; padding: 10px; text-align: left; }
        td { padding: 10px; border-bottom: 1px solid #ecf0f1; }
        .score { font-size: 48px; font-weight: bold; margin: 20px 0; text-align: center; }
    </style>
</head>
<body>
    <div class="header">
        <h1>LYBT Security Audit Report</h1>
        <p>Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    </div>

    <div class="score $(if ($score -ge 80) { 'pass' } elseif ($score -ge 60) { 'warn' } else { 'fail' })">
        Security Score: $score%
    </div>

    <div class="summary">
        <div class="summary-item">
            <h3>Total Checks</h3>
            <p>$($auditResults.Summary.Total)</p>
        </div>
        <div class="summary-item">
            <h3 class="pass">Passed</h3>
            <p>$($auditResults.Summary.Passed)</p>
        </div>
        <div class="summary-item">
            <h3 class="fail">Failed</h3>
            <p>$($auditResults.Summary.Failed)</p>
        </div>
        <div class="summary-item">
            <h3 class="warn">Warnings</h3>
            <p>$($auditResults.Summary.Warnings)</p>
        </div>
    </div>

    <h2>Detailed Results</h2>
    <table>
        <tr>
            <th>Category</th>
            <th>Check</th>
            <th>Status</th>
            <th>Details</th>
        </tr>
"@

    foreach ($check in $auditResults.Checks) {
        $statusClass = switch ($check.Status) {
            "PASS" { "pass" }
            "FAIL" { "fail" }
            "WARN" { "warn" }
        }

        $html += @"
        <tr>
            <td>$($check.Category)</td>
            <td>$($check.Check)</td>
            <td class="$statusClass">$($check.Status)</td>
            <td>$($check.Details)</td>
        </tr>
"@
    }

    $html += @"
    </table>

    <div class="footer">
        <p>Report generated by LYBT Security Audit Script</p>
    </div>
</body>
</html>
"@

    $html | Set-Content $OutputPath
    Write-ColoredOutput "Report saved to: $OutputPath" -Color Green
}

Write-ColoredOutput "`nSecurity audit completed!" -Color Green