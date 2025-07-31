# =============================================
# LYBT.WebAPI Server-side Deployment Script
# Manual trigger, English log, UTF-8 encoding
# =============================================

$zipFile      = "D:\DeployTemp\LYBT.WebAPI.zip"
$deployPath   = "D:\LYBT\WebAPI"
$backupRoot   = "D:\LYBT\Backups"
$serviceName  = "LYBTWebAPI"
$logFile      = "D:\LYBT\deploy.log"

function Write-Log {
    param([string]$msg)
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$ts $msg" | Out-File -Append -FilePath $logFile -Encoding UTF8
}

Write-Log "==== Server-side Deployment started ===="

# Only deploy if zip exists
if (-Not (Test-Path $zipFile)) {
    Write-Log "No zip file found, skip deployment."
    exit
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupDir = "$backupRoot\backup-$timestamp"
Write-Log "Backup to $backupDir"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
Copy-Item "$deployPath\*" $backupDir -Recurse -Force

Write-Log "Stopping service $serviceName"
Stop-Service -Name $serviceName -Force

Write-Log "Cleaning old files"
Get-ChildItem $deployPath | Where-Object { $_.Name -ne "deploy.log" } | Remove-Item -Recurse -Force

Write-Log "Extracting new version"
Expand-Archive -Path $zipFile -DestinationPath $deployPath -Force

Write-Log "Starting service $serviceName"
Start-Service -Name $serviceName

Write-Log "==== Server-side Deployment finished ===="
exit
