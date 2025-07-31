# LYBT WebAPI 文件上传脚本
# 设置控制台编码为UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

param(
    [string]$ServerIP,
    [string]$ServerUser,
    [string]$ZipFile
)

try {
    Write-Host "正在上传文件到服务器 $ServerIP..." -ForegroundColor Yellow
    
    # 方法1: 使用 WinSCP (如果安装了)
    if (Get-Command "WinSCP.exe" -ErrorAction SilentlyContinue) {
        Write-Host "使用 WinSCP 上传..." -ForegroundColor Green
        & WinSCP.exe /command `
            "open sftp://$ServerUser@$ServerIP" `
            "put `"$ZipFile`" /temp/" `
            "exit"
    }
    # 方法2: 使用 PowerShell Remoting (推荐)
    elseif (Test-WSMan -ComputerName $ServerIP -ErrorAction SilentlyContinue) {
        Write-Host "使用 PowerShell Remoting 上传..." -ForegroundColor Green
        
        $session = New-PSSession -ComputerName $ServerIP
        Copy-Item -Path $ZipFile -Destination "C:\temp\WebAPI-Deploy.zip" -ToSession $session
        Remove-PSSession $session
        
        Write-Host "✅ 文件上传成功！" -ForegroundColor Green
    }
    # 方法3: 使用共享文件夹
    else {
        Write-Host "使用网络共享上传..." -ForegroundColor Green
        $sharePath = "\\$ServerIP\C$\temp\"
        
        if (Test-Path $sharePath) {
            Copy-Item -Path $ZipFile -Destination "$sharePath\WebAPI-Deploy.zip" -Force
            Write-Host "✅ 文件上传成功！" -ForegroundColor Green
        } else {
            throw "无法访问共享路径: $sharePath"
        }
    }
}
catch {
    Write-Host "❌ 上传失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}