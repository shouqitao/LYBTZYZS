# LYBT WebAPI 远程更新脚本 (公网版)
# 使用 SCP 传输文件到公网服务器
#
# 使用方式:
#   .\deploy-remote.ps1 -Server "123.45.67.89"
#   .\deploy-remote.ps1 -Server "123.45.67.89" -Port 22 -User "root"
#
# 前置条件:
#   1. 目标服务器安装 OpenSSH Server (Windows Server 2016+ 内置)
#   2. 或安装 WinSCP (https://winscp.net/eng/download.php)
#   3. 开发机安装 OpenSSH Client (Windows 10+ 内置)

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [int]$Port = 22,
    
    [string]$User = "Administrator",
    
    [string]$Password,
    
    [int]$ApiPort = 5000,
    
    [string]$InstallPath = "C:\\Services\\LYBT-API"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LYBT WebAPI 远程更新" -ForegroundColor Cyan
Write-Host "  目标: ${User}@${Server}:${Port}" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# ── Step 1: 本地构建 ──
Write-Host "`n[1/5] 本地构建 Release..." -ForegroundColor Yellow
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -c Release -o "$env:TEMP\lybt-update" --self-contained false -v quiet
if ($LASTEXITCODE -ne 0) { throw "构建失败" }
Write-Host "  构建成功" -ForegroundColor Green

# ── Step 2: 打包 ──
Write-Host "`n[2/5] 打包部署文件..." -ForegroundColor Yellow
$zipPath = "$env:TEMP\lybt-webapi-update.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$env:TEMP\lybt-update\*" -DestinationPath $zipPath -Force
$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "  打包完成: ${zipSize} MB" -ForegroundColor Green

# ── Step 3: 上传到服务器 ──
Write-Host "`n[3/5] 上传到服务器..." -ForegroundColor Yellow
$remoteZip = "C:\\temp\\lybt-webapi-update.zip"

# 检查是否有 SCP
$scp = Get-Command scp -ErrorAction SilentlyContinue
if ($scp) {
    Write-Host "  使用 SCP 传输..." -ForegroundColor Gray
    if ($Password) {
        # 使用 sshpass 或 echo 管道 (需要安装 sshpass 或使用 plink)
        # Windows 原生方式: 使用 PSCredential + scp
        $secPassword = ConvertTo-SecureString $Password -AsPlainText -Force
        $cred = New-Object System.Management.Automation.PSCredential($User, $secPassword)
        
        # SCP 不支持直接传密码，改用 plink 或 WinSCP
        # 尝试使用 WinSCP 如果安装了
        $winscp = Get-Command WinSCP.com -ErrorAction SilentlyContinue
        if ($winscp) {
            Write-Host "  使用 WinSCP 传输..." -ForegroundColor Gray
            $winscpScript = @"
open sftp://${User}:${Password}@${Server}:${Port} -hostkey=* -nopreservetime -resumabletransfer=off
put "${zipPath}" "${remoteTemp}"
exit
"@
            $winscpScript | WinSCP.com /ini=nul /script=-
        } else {
            # 使用交互式 SCP (会提示输入密码)
            Write-Host "  使用 SCP (请在弹出的终端中输入密码)..." -ForegroundColor Gray
            scp -P $Port "$zipPath" "${User}@${Server}:${remoteZip}"
        }
    } else {
        Write-Host "  使用 SCP (请在弹出的终端中输入密码)..." -ForegroundColor Gray
        scp -P $Port "$zipPath" "${User}@${Server}:${remoteZip}"
    }
} else {
    Write-Host "  未找到 SCP，尝试 WinRM..." -ForegroundColor Gray
    # 回退到 WinRM (如果目标服务器启用了)
    $cred = if ($Password) {
        $secPwd = ConvertTo-SecureString $Password -AsPlainText -Force
        New-Object System.Management.Automation.PSCredential($User, $secPwd)
    } else {
        Get-Credential -UserName $User -Message "输入服务器登录凭据"
    }
    
    Invoke-Command -ComputerName $Server -Credential $cred -ScriptBlock {
        # 在远程服务器上创建临时目录
        New-Item -Path "C:\temp" -ItemType Directory -Force | Out-Null
    }
    
    # 使用 SMB 传输 (需要服务器共享 C$)
    $uncTemp = "\\$Server\C$\temp"
    Copy-Item $zipPath "$uncTemp\lybt-webapi-update.zip" -Force
}

Write-Host "  上传完成" -ForegroundColor Green

# ── Step 4: 远程重启服务 ──
Write-Host "`n[4/5] 重启远程服务..." -ForegroundColor Yellow

$restartScript = @"
# 停止服务
Stop-Service -Name "LYBT-API" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# 解压覆盖
Expand-Archive -Path "C:\temp\lybt-webapi-update.zip" -DestinationPath "$InstallPath" -Force

# 重启服务
Start-Service -Name "LYBT-API"
Start-Sleep -Seconds 5

# 清理
Remove-Item "C:\temp\lybt-webapi-update.zip" -Force -ErrorAction SilentlyContinue

# 返回状态
.GetService -Name "LYBT-API" | Select-Object Name,Status
"@

if ($scp) {
    # 使用 SSH 执行远程命令
    $sshCmd = "ssh -p $Port ${User}@${Server}"
    # 写入临时脚本到服务器
    $remoteScript = "C:\temp\restart.ps1"
    $restartScript | Out-File "$env:TEMP\restart.ps1" -Encoding UTF8
    scp -P $Port "$env:TEMP\restart.ps1" "${User}@${Server}:${remoteScript}"
    ssh -p $Port "${User}@${Server}" "powershell -File ${remoteScript}"
} else {
    Invoke-Command -ComputerName $Server -Credential $cred -ScriptBlock [scriptblock]::Create($restartScript)
}

Write-Host "  远程服务已重启" -ForegroundColor Green

# ── Step 5: 验证 ──
Write-Host "`n[5/5] 验证部署..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
    $health = Invoke-RestMethod -Uri "http://${Server}:${ApiPort}/health" -TimeoutSec 10
    Write-Host "  健康检查: 通过" -ForegroundColor Green
} catch {
    Write-Host "  健康检查: 失败 - $_" -ForegroundColor Red
}

# 清理
Remove-Item "$env:TEMP\lybt-update" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  更新完成!" -ForegroundColor Green
Write-Host "  服务: http://${Server}:${ApiPort}" -ForegroundColor Cyan
Write-Host "  Swagger: http://${Server}:${ApiPort}/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
