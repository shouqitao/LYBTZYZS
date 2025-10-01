# 重置用户密码脚本
# 用法: .\ResetUserPassword.ps1 -Username "shouqitao" -NewPassword "Lybt2025@TempPass!"

param(
    [Parameter(Mandatory=$false)]
    [string]$Username = "shouqitao",

    [Parameter(Mandatory=$false)]
    [string]$NewPassword = "Lybt2025@TempPass!",

    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true"
)

Write-Host "===== 重置用户密码工具 =====" -ForegroundColor Cyan
Write-Host "目标用户: $Username" -ForegroundColor Yellow
Write-Host ""

# 加载 BCrypt.Net 以生成密码哈希
Add-Type -Path "D:\source\repos\LYBTZYZS\src\Server\Modules\LYBT.Module.Users\bin\Debug\net8.0\BCrypt.Net-Next.dll"

# 生成密码哈希
$passwordHash = [BCrypt.Net.BCrypt]::HashPassword($NewPassword)
Write-Host "密码哈希已生成" -ForegroundColor Green

try {
    # 连接数据库
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()
    Write-Host "数据库连接成功" -ForegroundColor Green

    # 查询用户信息
    $queryUser = @"
SELECT Id, Username, Email, RealName, IsActive, CreatedAt
FROM Users
WHERE Username = @Username
"@

    $cmdQuery = New-Object System.Data.SqlClient.SqlCommand($queryUser, $connection)
    $cmdQuery.Parameters.AddWithValue("@Username", $Username) | Out-Null
    $reader = $cmdQuery.ExecuteReader()

    if ($reader.Read()) {
        $userId = $reader["Id"]
        $email = $reader["Email"]
        $realName = $reader["RealName"]
        $isActive = $reader["IsActive"]

        Write-Host ""
        Write-Host "找到用户信息:" -ForegroundColor Cyan
        Write-Host "  ID: $userId"
        Write-Host "  用户名: $Username"
        Write-Host "  邮箱: $email"
        Write-Host "  真实姓名: $realName"
        Write-Host "  状态: $(if($isActive){'激活'}else{'禁用'})"
        Write-Host ""

        $reader.Close()

        # 更新密码
        $updatePassword = @"
UPDATE Users
SET PasswordHash = @PasswordHash,
    UpdatedAt = GETUTCDATE()
WHERE Id = @UserId
"@

        $cmdUpdate = New-Object System.Data.SqlClient.SqlCommand($updatePassword, $connection)
        $cmdUpdate.Parameters.AddWithValue("@PasswordHash", $passwordHash) | Out-Null
        $cmdUpdate.Parameters.AddWithValue("@UserId", $userId) | Out-Null

        $rowsAffected = $cmdUpdate.ExecuteNonQuery()

        if ($rowsAffected -gt 0) {
            Write-Host "✓ 密码重置成功!" -ForegroundColor Green
            Write-Host ""
            Write-Host "登录信息:" -ForegroundColor Cyan
            Write-Host "  用户名: $Username"
            Write-Host "  新密码: $NewPassword"
            Write-Host ""
        } else {
            Write-Host "✗ 密码重置失败" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ 未找到用户: $Username" -ForegroundColor Red
        $reader.Close()
    }

    $connection.Close()
    Write-Host "数据库连接已关闭" -ForegroundColor Green

} catch {
    Write-Host "✗ 错误: $_" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
}

Write-Host ""
Write-Host "===== 完成 =====" -ForegroundColor Cyan
