@echo off
setlocal

REM === Set working directory ===
cd /d D:\source\repos\LYBTZYZS\Deploy

REM === Log batch start time ===
echo [%date% %time%] ==== Deployment batch started ==== >> deploy-batch-log.txt

REM === Run PowerShell deployment script silently, append output to log ===
powershell -ExecutionPolicy Bypass -File ".\deploy-client.ps1" >> deploy-batch-log.txt 2>&1

REM === Log batch finish time ===
echo [%date% %time%] ==== Deployment batch finished ==== >> deploy-batch-log.txt

REM === Optional: Pause for manual review (remove in production) ===
REM pause

endlocal
