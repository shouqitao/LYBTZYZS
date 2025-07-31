# LYBT WebAPI 服务器端部署触发脚本
# 设置控制台编码为UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

param(
    [string]$ServerIP,
    [string]$ServerUser
)

try {
    Write-Host "正在触发服务器端部署..." -ForegroundColor Yellow
    
    # 使用 PowerShell Remoting 执行服务器端脚本
    if (Test-WSMan -ComputerName $ServerIP -ErrorAction SilentlyContinue) {
        Write-Host "连接到服务器 $ServerIP..." -ForegroundColor Green
        
        $session = New-PSSession -ComputerName $ServerIP
        
        # 执行服务器端部署脚本
        Invoke-Command -Session $session -ScriptBlock {
            & "C:\LYBT\Scripts\server-deploy.bat"
        }
        
        Remove-PSSession $session
        Write-Host "✅ 服务器端部署完成！" -ForegroundColor Green
    }
    # 备用方案: 使用 PsExec
    elseif (Get-Command "PsExec.exe" -ErrorAction SilentlyContinue) {
        Write-Host "使用 PsExec 执行远程部署..." -ForegroundColor Green
        & PsExec.exe \\$ServerIP -u $ServerUser -i -d "C:\LYBT\Scripts\server-deploy.bat"
    }
    # 备用方案: 使用计划任务
    else {
        Write-Host "使用计划任务触发部署..." -ForegroundColor Green
        
        # 创建触发文件
        $triggerFile = "\\$ServerIP\C$\temp\deploy-trigger.txt"
        "$(Get-Date): Deploy requested" | Out-File -FilePath $triggerFile -Force
        
        Write-Host "✅ 部署信号已发送！" -ForegroundColor Green
    }
}
catch {
    Write-Host "❌ 触发部署失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}