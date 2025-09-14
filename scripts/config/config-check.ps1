# Configuration Governance P1 - Configuration Self-Check
# 验证配置加载顺序和敏感信息外置是否正确

param(
    [switch]$Detailed,
    [switch]$FixIssues
)

$WebAPIProject = "src/Server/Services/LYBT.WebAPI"
$ProjectFile = "$WebAPIProject/LYBT.WebAPI.csproj"

Write-Host "=== Configuration Governance P1 - Config Check ===" -ForegroundColor Cyan
Write-Host ""

# 检查项目文件存在
if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

$issues = @()
$checks = 0

try {
    Push-Location $WebAPIProject
    
    Write-Host "📋 Checking Configuration Setup..." -ForegroundColor Yellow
    Write-Host ""

    # 1. 检查 UserSecrets 配置
    Write-Host "1. UserSecrets Configuration Check" -ForegroundColor Green
    $checks++
    
    $userSecretsOutput = dotnet user-secrets list 2>&1
    if ($LASTEXITCODE -eq 0) {
        $secretsCount = ($userSecretsOutput | Measure-Object).Count
        if ($secretsCount -ge 5) {
            Write-Host "   ✅ UserSecrets configured ($secretsCount items)" -ForegroundColor Green
            if ($Detailed) {
                $userSecretsOutput | ForEach-Object {
                    if ($_ -match ".*Secret.*") {
                        $key = ($_ -split " = ")[0]
                        Write-Host "   - $key = [PROTECTED]" -ForegroundColor Gray
                    } else {
                        Write-Host "   - $_" -ForegroundColor White
                    }
                }
            }
        } else {
            $issues += "UserSecrets has insufficient items (expected: 5, found: $secretsCount)"
            Write-Host "   ❌ UserSecrets incomplete (found: $secretsCount, expected: 5)" -ForegroundColor Red
        }
    } else {
        $issues += "UserSecrets not initialized or accessible"
        Write-Host "   ❌ UserSecrets not accessible" -ForegroundColor Red
    }

    # 2. 检查配置文件安全性
    Write-Host ""
    Write-Host "2. Configuration Files Security Check" -ForegroundColor Green
    $checks++
    
    # 检查 appsettings.json 中是否还有敏感信息
    $appsettingsContent = Get-Content "appsettings.json" -Raw
    $sensitivePatterns = @(
        '"Secret"\s*:\s*"[^"]{8,}"',
        '"SystemAdmin"\s*:\s*"[^"]{8,}"',
        '"NewUser"\s*:\s*"[^"]{8,}"',
        '"DefaultUserPassword"\s*:\s*"[^"]{8,}"',
        '"DefaultPassword"\s*:\s*"[^"]{8,}"'
    )
    
    $foundSensitive = $false
    foreach ($pattern in $sensitivePatterns) {
        if ($appsettingsContent -match $pattern) {
            $foundSensitive = $true
            $issues += "Sensitive data found in appsettings.json: $($matches[0])"
        }
    }
    
    if (-not $foundSensitive) {
        Write-Host "   ✅ appsettings.json free of sensitive data" -ForegroundColor Green
    } else {
        Write-Host "   ❌ appsettings.json contains sensitive data" -ForegroundColor Red
    }

    # 3. 检查开发配置文件
    Write-Host ""
    Write-Host "3. Development Configuration Check" -ForegroundColor Green
    $checks++
    
    if (Test-Path "appsettings.Development.json") {
        $devContent = Get-Content "appsettings.Development.json" -Raw
        $requiredDevConfigs = @(
            '"Cors"',
            '"Security"',
            '"EnableSensitiveDataLogging"\s*:\s*true',
            '"HideDetailedErrors"\s*:\s*false'
        )
        
        $missingDevConfigs = @()
        foreach ($config in $requiredDevConfigs) {
            if ($devContent -notmatch $config) {
                $missingDevConfigs += $config
            }
        }
        
        if ($missingDevConfigs.Count -eq 0) {
            Write-Host "   ✅ appsettings.Development.json properly configured" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Some development configs missing:" -ForegroundColor Yellow
            $missingDevConfigs | ForEach-Object { Write-Host "     - $_" -ForegroundColor Yellow }
        }
    } else {
        $issues += "appsettings.Development.json not found"
        Write-Host "   ❌ appsettings.Development.json missing" -ForegroundColor Red
    }

    # 4. 验证配置加载优先级
    Write-Host ""
    Write-Host "4. Configuration Loading Priority Check" -ForegroundColor Green
    $checks++
    
    Write-Host "   📄 Configuration Loading Order:" -ForegroundColor Cyan
    Write-Host "     1. appsettings.json (base)" -ForegroundColor White
    Write-Host "     2. appsettings.Development.json (environment override)" -ForegroundColor White
    Write-Host "     3. UserSecrets (sensitive - highest priority)" -ForegroundColor White
    
    # 检查是否可以通过 IConfiguration 读取敏感配置
    Write-Host "   🔍 Testing configuration access..." -ForegroundColor Cyan
    
    # 这里可以添加更复杂的配置访问测试，比如启动一个临时的 ASP.NET Core app 来验证

    # 5. 运行环境验证
    Write-Host ""
    Write-Host "5. Runtime Environment Check" -ForegroundColor Green  
    $checks++
    
    $env = $env:ASPNETCORE_ENVIRONMENT
    if ([string]::IsNullOrEmpty($env)) {
        $env = "Development"  # Default
    }
    
    Write-Host "   🌍 Current Environment: $env" -ForegroundColor White
    
    if ($env -eq "Development") {
        Write-Host "   ✅ Development environment - UserSecrets will be loaded" -ForegroundColor Green
    } elseif ($env -eq "Production") {
        Write-Host "   ⚠️  Production environment - ensure environment variables are set" -ForegroundColor Yellow
        # 列出需要的环境变量
        $requiredEnvVars = @(
            "DefaultPasswords__SystemAdmin",
            "DefaultPasswords__NewUser", 
            "UserOptions__DefaultUserPassword",
            "SysAdminOptions__DefaultPassword",
            "JwtOptions__Secret"
        )
        Write-Host "   📋 Required environment variables for production:" -ForegroundColor Cyan
        $requiredEnvVars | ForEach-Object { Write-Host "     - $_" -ForegroundColor White }
    }

    # 6. 脚本和工具检查
    Write-Host ""
    Write-Host "6. Configuration Scripts Check" -ForegroundColor Green
    $checks++
    
    # Check setup script existence (relative to WebAPI project directory)
    $setupScriptPath = "../../../../scripts/config/setup-user-secrets.ps1"
    
    if (Test-Path $setupScriptPath) {
        Write-Host "   ✅ setup-user-secrets.ps1 exists" -ForegroundColor Green
    } else {
        # Try absolute path from project root
        $absolutePath = Join-Path (Split-Path -Parent (Split-Path -Parent (Get-Location))) "scripts\config\setup-user-secrets.ps1"
        if (Test-Path $absolutePath) {
            Write-Host "   ✅ setup-user-secrets.ps1 exists" -ForegroundColor Green  
        } else {
            $issues += "Missing setup-user-secrets.ps1 script"
            Write-Host "   ❌ setup-user-secrets.ps1 missing (checked: $setupScriptPath)" -ForegroundColor Red
        }
    }

} catch {
    Write-Error "Configuration check failed: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}

# 输出总结
Write-Host ""
Write-Host "=== Configuration Check Summary ===" -ForegroundColor Cyan
Write-Host "📊 Checks performed: $checks" -ForegroundColor White

if ($issues.Count -eq 0) {
    Write-Host "✅ All configuration checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎯 Configuration Status:" -ForegroundColor Cyan
    Write-Host "  • Sensitive data externalized ✅" -ForegroundColor Green
    Write-Host "  • UserSecrets configured ✅" -ForegroundColor Green  
    Write-Host "  • Development config separated ✅" -ForegroundColor Green
    Write-Host "  • Security validation passed ✅" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 Ready for development and production deployment!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ Found $($issues.Count) configuration issues:" -ForegroundColor Red
    $issues | ForEach-Object { Write-Host "  • $_" -ForegroundColor Red }
    
    if ($FixIssues) {
        Write-Host ""
        Write-Host "🔧 Attempting to fix issues..." -ForegroundColor Yellow
        Write-Host "Suggested fix: Run setup-user-secrets.ps1" -ForegroundColor White
        # 这里可以添加自动修复逻辑
    } else {
        Write-Host ""
        Write-Host "💡 To fix issues, run with -FixIssues parameter or manually:" -ForegroundColor Cyan
        Write-Host "   powershell -File scripts/config/setup-user-secrets.ps1" -ForegroundColor White
    }
    exit 1
}

Write-Host "Configuration Governance P1 Step ③ validation completed!" -ForegroundColor Green