# P4-Server 环境变量验证脚本
# 用于生产部署前验证必需的环境变量

param(
    [string]$Environment = "Production"
)

Write-Host "P4-Server 环境变量验证 - $Environment 环境" -ForegroundColor Green

$requiredVars = @(
    @{Name="ENCRYPTION_KEY"; MinLength=32; Description="数据加密密钥"},
    @{Name="JWT_SECRET"; MinLength=32; Description="JWT令牌密钥"},
    @{Name="ADMIN_DEFAULT_PASSWORD"; MinLength=8; Description="系统管理员默认密码"},
    @{Name="USER_DEFAULT_PASSWORD"; MinLength=8; Description="用户默认密码"}
)

$errors = @()
$warnings = @()

foreach ($var in $requiredVars) {
    $value = [Environment]::GetEnvironmentVariable($var.Name)
    
    if ([string]::IsNullOrEmpty($value)) {
        $errors += "❌ 缺失环境变量: $($var.Name) - $($var.Description)"
    }
    elseif ($value.Contains('${')) {
        $errors += "❌ 环境变量未替换: $($var.Name) = $value"
    }
    elseif ($value.Length -lt $var.MinLength) {
        $warnings += "⚠️ 环境变量长度不足: $($var.Name) (当前:$($value.Length), 最小:$($var.MinLength))"
    }
    else {
        Write-Host "✅ $($var.Name): 已配置且符合要求" -ForegroundColor Green
    }
}

# JWT密钥特殊检查
$jwtSecret = [Environment]::GetEnvironmentVariable("JWT_SECRET")
if (-not [string]::IsNullOrEmpty($jwtSecret) -and $jwtSecret.Contains("Development")) {
    $warnings += "⚠️ JWT_SECRET 包含 'Development' 字样，确认是否适合生产环境"
}

# 输出结果
if ($errors.Count -gt 0) {
    Write-Host "`n❌ 环境变量验证失败:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host "`n⚠️ 环境变量警告:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

Write-Host "`n✅ 环境变量验证通过!" -ForegroundColor Green
Write-Host "生产环境配置就绪，可以部署 LYBT.WebAPI" -ForegroundColor Green