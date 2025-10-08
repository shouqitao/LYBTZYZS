# ============================================
# 更新 AdminSecrets 表密码为 BCrypt 格式
# ============================================

$ErrorActionPreference = "Stop"

Write-Host "==================== 更新管理员密码 ====================" -ForegroundColor Green
Write-Host ""

# 加载 BCrypt.Net DLL
$dllPath = Get-ChildItem -Path "src/Server" -Filter "BCrypt.Net-Next.dll" -Recurse | Select-Object -First 1 -ExpandProperty FullName

if (-not $dllPath) {
    Write-Host "❌ 未找到 BCrypt.Net-Next.dll，尝试从 NuGet 包加载..." -ForegroundColor Yellow
    $dllPath = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\bcrypt.net-next" -Filter "BCrypt.Net-Next.dll" -Recurse | Select-Object -First 1 -ExpandProperty FullName
}

if ($dllPath) {
    Write-Host "✅ 找到 BCrypt.Net DLL: $dllPath" -ForegroundColor Green
    Add-Type -Path $dllPath
} else {
    Write-Host "❌ 无法找到 BCrypt.Net-Next.dll" -ForegroundColor Red
    Write-Host "   请运行: dotnet build src/Server/LYBT.Server.sln" -ForegroundColor Yellow
    exit 1
}

# 生成 BCrypt 哈希
$password = "Dev@Admin2025!"
$hash = [BCrypt.Net.BCrypt]::HashPassword($password)

Write-Host "✅ BCrypt 哈希生成成功" -ForegroundColor Green
Write-Host "   密码: $password" -ForegroundColor Cyan
Write-Host "   哈希: $hash" -ForegroundColor Cyan
Write-Host ""

# 数据库连接字符串
$connectionString = "Server=.;Database=LYBT_DB;Trusted_Connection=True;TrustServerCertificate=True;"

Write-Host "[步骤1] 连接数据库..." -ForegroundColor Cyan

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "✅ 数据库连接成功" -ForegroundColor Green

    # 更新密码哈希
    $sql = "UPDATE AdminSecrets SET PasswordHash = @Hash WHERE Id = '00000000-0000-0000-0000-000000000001'"
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.Parameters.AddWithValue("@Hash", $hash) | Out-Null

    Write-Host ""
    Write-Host "[步骤2] 更新密码哈希..." -ForegroundColor Cyan
    $rowsAffected = $command.ExecuteNonQuery()

    if ($rowsAffected -gt 0) {
        Write-Host "✅ 密码哈希更新成功，影响 $rowsAffected 行" -ForegroundColor Green
    } else {
        Write-Host "⚠️  未找到匹配的记录" -ForegroundColor Yellow
    }

    # 验证更新
    $verifySql = "SELECT TOP 1 LEFT(PasswordHash, 10) AS HashPrefix FROM AdminSecrets WHERE Id = '00000000-0000-0000-0000-000000000001'"
    $verifyCommand = $connection.CreateCommand()
    $verifyCommand.CommandText = $verifySql
    $reader = $verifyCommand.ExecuteReader()

    if ($reader.Read()) {
        $prefix = $reader["HashPrefix"]
        Write-Host ""
        Write-Host "[验证] 当前密码哈希前缀: $prefix..." -ForegroundColor Cyan
    }
    $reader.Close()

    $connection.Close()

    Write-Host ""
    Write-Host "==================== 更新完成 ====================" -ForegroundColor Green
    Write-Host "现在可以使用以下凭据登录:" -ForegroundColor Yellow
    Write-Host "  用户名: sysadmin" -ForegroundColor White
    Write-Host "  密码: Dev@Admin2025!" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host "❌ 数据库操作失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
