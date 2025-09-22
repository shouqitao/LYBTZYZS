# Start-TestSqlServer.ps1
# 配置本地 SQL Server 测试环境

param(
    [string]$ServerName = "localhost",
    [string]$DatabaseName = "LYBT_Test"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "LYBT SQL Server Test Environment Setup" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

Write-Host "`n使用本地 SQL Server 实例..." -ForegroundColor Yellow
Write-Host "服务器: $ServerName" -ForegroundColor White
Write-Host "数据库: $DatabaseName" -ForegroundColor White

# 连接字符串（使用集成身份验证）
$connectionString = "Server=$ServerName;Database=master;Integrated Security=True;TrustServerCertificate=True"

try {
    Write-Host "`n连接到 SQL Server..." -ForegroundColor Yellow
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()

    # 检查数据库是否存在，如果不存在则创建
    Write-Host "检查测试数据库..." -ForegroundColor Yellow
    $checkDbCmd = $connection.CreateCommand()
    $checkDbCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = '$DatabaseName'"
    $dbExists = $checkDbCmd.ExecuteScalar()

    if ($dbExists -eq 0) {
        Write-Host "创建测试数据库 $DatabaseName..." -ForegroundColor Yellow
        $createDbCmd = $connection.CreateCommand()
        $createDbCmd.CommandText = "CREATE DATABASE [$DatabaseName]"
        $createDbCmd.ExecuteNonQuery() | Out-Null
        Write-Host "数据库创建成功！" -ForegroundColor Green
    }
    else {
        Write-Host "数据库已存在" -ForegroundColor Green
    }

    $connection.Close()

    Write-Host "`nSQL Server 测试环境准备就绪！" -ForegroundColor Green
    Write-Host "`n连接字符串：" -ForegroundColor Cyan
    Write-Host "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" -ForegroundColor White
}
catch {
    Write-Host "配置失败：$_" -ForegroundColor Red
    Write-Host "`n可能的原因：" -ForegroundColor Yellow
    Write-Host "1. SQL Server 服务未运行" -ForegroundColor White
    Write-Host "2. 当前用户没有创建数据库的权限" -ForegroundColor White
    Write-Host "3. 服务器名称不正确" -ForegroundColor White
    exit 1
}

Write-Host "`n================================================" -ForegroundColor Green
Write-Host "测试环境配置完成！" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green