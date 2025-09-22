# JWT密钥旋转脚本
# 用途：手动触发JWT密钥旋转，用于紧急安全事件响应

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Production",

    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "http://localhost:5001",

    [Parameter(Mandatory=$false)]
    [string]$AdminToken = ""
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "JWT密钥旋转脚本" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# 检查管理员权限
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "错误：此脚本需要管理员权限运行" -ForegroundColor Red
    exit 1
}

# 确认操作
Write-Host "警告：此操作将旋转JWT密钥，所有现有令牌将在短时间内失效！" -ForegroundColor Yellow
Write-Host "环境: $Environment" -ForegroundColor Yellow
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host ""

$confirmation = Read-Host "确定要继续吗？(yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "操作已取消" -ForegroundColor Green
    exit 0
}

try {
    # 如果没有提供管理员令牌，提示输入
    if ([string]::IsNullOrEmpty($AdminToken)) {
        Write-Host "请输入管理员身份验证令牌：" -ForegroundColor Yellow
        $AdminToken = Read-Host -AsSecureString
        $AdminToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminToken))
    }

    # 构建请求
    $headers = @{
        "Authorization" = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }

    $body = @{
        "action" = "rotate"
        "keyType" = "JWT_SECRET"
        "reason" = "Manual rotation requested by administrator"
        "timestamp" = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    } | ConvertTo-Json

    # 发送旋转请求
    Write-Host "正在发送密钥旋转请求..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri "$ApiUrl/api/v1/admin/security/rotate-key" -Method Post -Headers $headers -Body $body

    if ($response.success) {
        Write-Host "✓ JWT密钥旋转成功！" -ForegroundColor Green
        Write-Host "  新密钥ID: $($response.newKeyId)" -ForegroundColor Green
        Write-Host "  生效时间: $($response.effectiveTime)" -ForegroundColor Green
        Write-Host "  旧密钥过期时间: $($response.oldKeyExpiryTime)" -ForegroundColor Yellow

        # 记录到审计日志
        $logEntry = @{
            "timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            "action" = "JWT_KEY_ROTATION"
            "environment" = $Environment
            "newKeyId" = $response.newKeyId
            "operator" = $env:USERNAME
            "machine" = $env:COMPUTERNAME
        }

        $logPath = ".\logs\key-rotation-$(Get-Date -Format 'yyyyMMdd').log"
        $logEntry | ConvertTo-Json -Compress | Out-File -FilePath $logPath -Append

        Write-Host ""
        Write-Host "重要提示：" -ForegroundColor Yellow
        Write-Host "1. 新令牌将使用新密钥签名" -ForegroundColor Yellow
        Write-Host "2. 旧令牌将在过渡期内继续有效" -ForegroundColor Yellow
        Write-Host "3. 请通知所有集成系统更新其缓存" -ForegroundColor Yellow
        Write-Host "4. 监控错误日志以确保平滑过渡" -ForegroundColor Yellow
    }
    else {
        Write-Host "✗ 密钥旋转失败：$($response.error)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "✗ 发生错误：$($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "操作完成" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan