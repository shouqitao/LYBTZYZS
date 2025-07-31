# ============================================
# LYBT.WebAPI Automated Deployment Script (Local Upload Only, No Remote Execution)
# ============================================

$projectPath   = "D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI"
$publishPath   = "D:\source\repos\LYBTZYZS\auto-publish"
$zipFile       = "$publishPath\LYBT.WebAPI.zip"
$remoteShare   = "\\192.168.190.243\DeployTemp"
$userName      = "administrator"
$password      = "Shou@850528"
$logFile       = "D:\source\repos\LYBTZYZS\Deploy\deploy-log.txt"

function Write-Log {
    param([string]$msg)
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$ts $msg" | Out-File -Append -FilePath $logFile -Encoding UTF8
}

Write-Log "==== Deployment started ===="

net use $remoteShare /delete /y | Out-Null
net use $remoteShare /user:$userName $password | Out-Null

Write-Log "[1] dotnet publish ..."
$pubOut = dotnet publish $projectPath -c Release -o $publishPath 2>&1
Write-Log $pubOut

Write-Log "[2] Compress publish folder ..."
if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
$zipOut = Compress-Archive -Path "$publishPath\*" -DestinationPath $zipFile 2>&1
Write-Log $zipOut

Write-Log "[3] Upload files to server ..."
$rcOut = robocopy $publishPath "$remoteShare\publish" /MIR /NFL /NDL /NJH /NJS /NP
Write-Log $rcOut
try { Copy-Item $zipFile "$remoteShare\LYBT.WebAPI.zip" -Force -ErrorAction Stop; Write-Log "ZIP upload complete." }
catch { Write-Log "ZIP upload failed: $($_.Exception.Message)" }

try { Copy-Item ".\deploy-server.ps1" "$remoteShare\" -Force -ErrorAction Stop; Write-Log "deploy-server.ps1 upload complete." }
catch { Write-Log "deploy-server.ps1 upload failed: $($_.Exception.Message)" }

net use $remoteShare /delete /y | Out-Null
Write-Log "==== Deployment finished ===="
exit
