# Production 配置验证脚本
# Issue: Production 配置问题（Phase 1）
# Date: 2025-09-30

param(
    [switch]$Verbose,
    [switch]$ExitOnError
)

Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "  LYBT Production 配置验证" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan

# 配置项定义
$requiredConfigs = @(
    @{
        EnvVar = "ConnectionStrings__DefaultConnection"
        Name = "数据库连接字符串"
        Severity = "Critical"
        Pattern = "Server=.+;Database=.+;"
        Example = "Server=localhost;Database=LYBTDB;User Id=sa;Password=***;"
    },
    @{
        EnvVar = "Lybt__Authentication__Jwt__SecretKey"
        Name = "JWT 签名密钥"
        Severity = "Critical"
        MinLength = 32
        Example = "[自动生成的 Base64 字符串，至少 32 字符]"
    },
    @{
        EnvVar = "Lybt__Authentication__DefaultPasswords__SysAdminPassword"
        Name = "管理员默认密码"
        Severity = "Important"
        Example = "Admin@123456"
    },
    @{
        EnvVar = "Lybt__Authentication__DefaultPasswords__NewUserPassword"
        Name = "新用户默认密码"
        Severity = "Important"
        Example = "User@123456"
    },
    @{
        EnvVar = "Lybt__Business__SystemAdmin__Username"
        Name = "管理员用户名"
        Severity = "Important"
        Example = "admin"
    },
    @{
        EnvVar = "Lybt__Business__SystemAdmin__Email"
        Name = "管理员邮箱"
        Severity = "Important"
        Pattern = "^[^@]+@[^@]+\.[^@]+$"
        Example = "admin@example.com"
    },
    @{
        EnvVar = "AllowedHosts"
        Name = "允许的主机名"
        Severity = "Optional"
        Example = "example.com;*.example.com"
    }
)

$errorItems = @()
$warnings = @()
$passed = @()

Write-Host "`n[1/2] 检查环境变量配置..." -ForegroundColor Yellow
Write-Host ""

foreach ($config in $requiredConfigs) {
    $value = [Environment]::GetEnvironmentVariable($config.EnvVar, "Machine")
    
    # 检查 1: 是否存在
    if ([string]::IsNullOrWhiteSpace($value)) {
        if ($config.Severity -eq "Optional") {
            if ($Verbose) {
                Write-Host "  💡 $($config.Name) (未设置，可选)" -ForegroundColor Gray
            }
            continue
        }
        
        $errorItemMsg = "❌ $($config.Name) 未设置"
        $errorItems += @{
            Name = $config.Name
            EnvVar = $config.EnvVar
            Message = $errorItemMsg
            Severity = $config.Severity
            Example = $config.Example
        }
        Write-Host "  $errorItemMsg" -ForegroundColor Red
        if ($Verbose) {
            Write-Host "     环境变量: $($config.EnvVar)" -ForegroundColor Gray
        }
        continue
    }
    
    # 检查 2: 是否仍是占位符
    if ($value -match "#\{.+\}#") {
        $errorItemMsg = "❌ $($config.Name) 包含占位符"
        $errorItems += @{
            Name = $config.Name
            EnvVar = $config.EnvVar
            Message = $errorItemMsg
            Severity = $config.Severity
            Value = $value
            Example = $config.Example
        }
        Write-Host "  $errorItemMsg" -ForegroundColor Red
        if ($Verbose) {
            Write-Host "     当前值: $value" -ForegroundColor Gray
        }
        continue
    }
    
    $hasWarning = $false
    
    # 检查 3: 长度验证
    if ($config.MinLength -and $value.Length -lt $config.MinLength) {
        $warnMsg = "⚠️ $($config.Name) 长度不足 (当前: $($value.Length), 需要: $($config.MinLength))"
        $warnings += @{
            Name = $config.Name
            EnvVar = $config.EnvVar
            Message = $warnMsg
        }
        Write-Host "  $warnMsg" -ForegroundColor Yellow
        $hasWarning = $true
    }
    
    # 检查 4: 格式验证
    if ($config.Pattern -and $value -notmatch $config.Pattern) {
        $warnMsg = "⚠️ $($config.Name) 格式可能不正确"
        $warnings += @{
            Name = $config.Name
            EnvVar = $config.EnvVar
            Message = $warnMsg
        }
        Write-Host "  $warnMsg" -ForegroundColor Yellow
        $hasWarning = $true
    }
    
    if (-not $hasWarning) {
        $passed += $config.Name
        Write-Host "  ✅ $($config.Name)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "[2/2] 生成验证报告..." -ForegroundColor Yellow
Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  验证结果" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  通过: $($passed.Count)" -ForegroundColor Green
Write-Host "  警告: $($warnings.Count)" -ForegroundColor $(if($warnings.Count -gt 0){"Yellow"}else{"Green"})
Write-Host "  错误: $($errorItems.Count)" -ForegroundColor $(if($errorItems.Count -gt 0){"Red"}else{"Green"})
Write-Host "=============================================" -ForegroundColor Cyan

if ($errorItems.Count -gt 0) {
    Write-Host "`n❌ 发现 $($errorItems.Count) 个配置错误：`n" -ForegroundColor Red
    
    $criticalErrors = $errorItems | Where-Object { $_.Severity -eq "Critical" }
    $importantErrors = $errorItems | Where-Object { $_.Severity -eq "Important" }
    
    if ($criticalErrors.Count -gt 0) {
        Write-Host "⚠️ CRITICAL 错误（必须修复）:" -ForegroundColor Red
        Write-Host ""
        foreach ($errorItem in $criticalErrors) {
            Write-Host "  [$($errorItems.IndexOf($errorItem) + 1)] $($errorItem.Name)" -ForegroundColor White
            Write-Host "      环境变量: $($errorItem.EnvVar)" -ForegroundColor Gray
            Write-Host "      问题: $($errorItem.Message)" -ForegroundColor Gray
            if ($errorItem.Value) {
                Write-Host "      当前值: $($errorItem.Value)" -ForegroundColor Gray
            }
            if ($errorItem.Example) {
                Write-Host "      示例: $($errorItem.Example)" -ForegroundColor Gray
            }
            Write-Host "      修复命令: setx $($errorItem.EnvVar) `"<your-value>`" /M" -ForegroundColor Yellow
            Write-Host ""
        }
    }
    
    if ($importantErrors.Count -gt 0) {
        Write-Host "⚠️ IMPORTANT 错误（建议修复）:" -ForegroundColor Yellow
        Write-Host ""
        foreach ($errorItem in $importantErrors) {
            Write-Host "  [$($errorItems.IndexOf($errorItem) + 1)] $($errorItem.Name)" -ForegroundColor White
            Write-Host "      环境变量: $($errorItem.EnvVar)" -ForegroundColor Gray
            Write-Host "      问题: $($errorItem.Message)" -ForegroundColor Gray
            if ($errorItem.Example) {
                Write-Host "      示例: $($errorItem.Example)" -ForegroundColor Gray
            }
            Write-Host "      修复命令: setx $($errorItem.EnvVar) `"<your-value>`" /M" -ForegroundColor Yellow
            Write-Host ""
        }
    }
    
    Write-Host "─────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "📖 详细配置指南: docs\deployment\production-setup.md" -ForegroundColor Cyan
    Write-Host "📖 配置项参考: docs\deployment\environment-variables.md" -ForegroundColor Cyan
    Write-Host ""
}

if ($warnings.Count -gt 0) {
    Write-Host "`n⚠️ 发现 $($warnings.Count) 个警告：`n" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  • $($warning.Message)" -ForegroundColor Yellow
        if ($Verbose) {
            Write-Host "    环境变量: $($warning.EnvVar)" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

if ($errorItems.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "`n✅ 所有配置验证通过！" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步:" -ForegroundColor Cyan
    Write-Host "  1. 设置环境变量: `$env:ASPNETCORE_ENVIRONMENT=`"Production`"" -ForegroundColor White
    Write-Host "  2. 启动应用: dotnet run --project src\Server\Services\LYBT.WebAPI" -ForegroundColor White
    Write-Host ""
}

if ($ExitOnError -and $errorItems.Count -gt 0) {
    exit 1
}

exit 0