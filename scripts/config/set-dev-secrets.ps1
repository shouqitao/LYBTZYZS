# ============================================================================
# Configuration Governance P1 - Development Secrets Setup
# 设置开发环境敏感配置到 UserSecrets
# ============================================================================

param(
    [switch]$Force,
    [switch]$Verify
)

# 配置项目路径
$WebAPIProject = "src/Server/Services/LYBT.WebAPI"
$ProjectFile = "$WebAPIProject/LYBT.WebAPI.csproj"

Write-Host "🔐 开始配置开发环境敏感信息..." -ForegroundColor Green

# 检查项目文件是否存在
if (-not (Test-Path $ProjectFile)) {
    Write-Error "❌ 未找到项目文件: $ProjectFile"
    exit 1
}

try {
    # 切换到WebAPI项目目录
    Push-Location $WebAPIProject
    Write-Host "📁 当前目录: $(Get-Location)" -ForegroundColor Cyan

    # 初始化UserSecrets（如果尚未初始化）
    Write-Host "🔧 初始化 UserSecrets..." -ForegroundColor Yellow
    dotnet user-secrets init --force:$Force
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ UserSecrets初始化失败"
        exit 1
    }

    # 设置默认密码配置
    Write-Host "🔑 设置默认密码配置..." -ForegroundColor Yellow
    
    # 系统管理员默认密码
    dotnet user-secrets set "DefaultPasswords:SystemAdmin" "ChangeMe!DevOnly2025@Admin"
    Write-Host "  ✅ DefaultPasswords:SystemAdmin" -ForegroundColor Green
    
    # 新用户默认密码
    dotnet user-secrets set "DefaultPasswords:NewUser" "ChangeMe!DevOnly2025#User"
    Write-Host "  ✅ DefaultPasswords:NewUser" -ForegroundColor Green
    
    # 用户选项默认密码
    dotnet user-secrets set "UserOptions:DefaultUserPassword" "ChangeMe!DevOnly2025#User"
    Write-Host "  ✅ UserOptions:DefaultUserPassword" -ForegroundColor Green
    
    # 系统管理员选项默认密码
    dotnet user-secrets set "SysAdminOptions:DefaultPassword" "ChangeMe!DevOnly2025@Admin"
    Write-Host "  ✅ SysAdminOptions:DefaultPassword" -ForegroundColor Green

    # 设置JWT密钥
    Write-Host "🔐 设置JWT配置..." -ForegroundColor Yellow
    
    # 生成强JWT密钥 (至少32个字符)
    $JwtSecret = "DevOnly_JWT_Secret_Key_2025_For_LYBT_System_32Plus_Characters_Strong!"
    dotnet user-secrets set "JwtOptions:Secret" $JwtSecret
    Write-Host "  ✅ JwtOptions:Secret" -ForegroundColor Green

    # 验证设置
    if ($Verify) {
        Write-Host "🔍 验证UserSecrets配置..." -ForegroundColor Yellow
        Write-Host ""
        
        $secrets = dotnet user-secrets list
        if ($LASTEXITCODE -eq 0) {
            Write-Host "📋 当前UserSecrets配置:" -ForegroundColor Cyan
            $secrets | ForEach-Object {
                if ($_ -match ".*Secret.*") {
                    # 隐藏敏感密钥值
                    $key = ($_ -split " = ")[0]
                    Write-Host "  $key = [隐藏]" -ForegroundColor Gray
                } else {
                    Write-Host "  $_" -ForegroundColor White
                }
            }
        } else {
            Write-Warning "⚠️ 无法验证UserSecrets配置"
        }
    }

    Write-Host ""
    Write-Host "✅ 开发环境敏感配置设置完成！" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 配置项清单:" -ForegroundColor Cyan
    Write-Host "  • DefaultPasswords:SystemAdmin" -ForegroundColor White
    Write-Host "  • DefaultPasswords:NewUser" -ForegroundColor White
    Write-Host "  • UserOptions:DefaultUserPassword" -ForegroundColor White
    Write-Host "  • SysAdminOptions:DefaultPassword" -ForegroundColor White
    Write-Host "  • JwtOptions:Secret" -ForegroundColor White
    Write-Host ""
    Write-Host "🔧 使用方法:" -ForegroundColor Cyan
    Write-Host "  • 开发环境自动加载这些配置" -ForegroundColor White
    Write-Host "  • 生产环境请使用环境变量" -ForegroundColor White
    Write-Host "  • 查看配置: dotnet user-secrets list" -ForegroundColor White
    Write-Host "  • 清除配置: dotnet user-secrets clear" -ForegroundColor White

} catch {
    Write-Error "❌ 配置设置失败: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}

Write-Host "🎯 开发环境配置治理 Step ② 完成！" -ForegroundColor Green