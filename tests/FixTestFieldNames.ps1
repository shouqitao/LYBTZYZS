# 批量修复测试文件中的字段名称以匹配实体定义
# 基于实体定义的字段映射表

Write-Host "开始批量修复测试文件中的字段名称..." -ForegroundColor Green

# 定义要修复的文件路径模式
$testPaths = @(
    "tests/UnitTests/**/*.cs"
)

# 获取所有测试文件
$testFiles = Get-ChildItem -Path $testPaths -Recurse -File | Where-Object { $_.Extension -eq ".cs" }

Write-Host "找到 $($testFiles.Count) 个测试文件需要检查" -ForegroundColor Yellow

# User实体字段修正映射
$userFieldMappings = @{
    # 修正字段名称
    'UserName = '          = 'Username = '
    'UserName ='           = 'Username ='
    '"UserName"'           = '"Username"'
    '\.UserName'           = '.Username'
    'x\.UserName'          = 'x.Username'
    'u\.UserName'          = 'u.Username'
    'user\.UserName'       = 'user.Username'
    'Name = '              = 'RealName = '
    'Name ='               = 'RealName ='
    '"Name"'               = '"RealName"'
    '\.Name'               = '.RealName'
    'x\.Name'              = 'x.RealName'
    'u\.Name'              = 'u.RealName'
    'user\.Name'           = 'user.RealName'
    'Password = '          = 'PasswordHash = '
    'Password ='           = 'PasswordHash ='
    '\.Password'           = '.PasswordHash'
    'x\.Password'          = 'x.PasswordHash'
    'u\.Password'          = 'u.PasswordHash'
    'user\.Password'       = 'user.PasswordHash'

    # 修正时间戳字段
    'CreatedTime'          = 'CreatedAt'
    'UpdatedTime'          = 'UpdateTime'
    'LastUpdateTime'       = 'UpdateTime'
    'ModifiedTime'         = 'UpdateTime'

    # UserDto和UserCreateDto特殊处理
    'UserCreateDto.*Name =' = 'UserCreateDto { RealName ='
    'UserDto.*Name ='      = 'UserDto { RealName ='
}

# Patient实体字段修正映射
$patientFieldMappings = @{
    # 修正时间戳字段
    'CreatedTime'          = 'CreatedAt'
    'UpdatedTime'          = 'UpdateTime'
    'UpdatedAt'            = 'UpdateTime'
    'LastUpdateTime'       = 'UpdateTime'
    'ModifiedTime'         = 'UpdateTime'
    'LastModifiedTime'     = 'UpdateTime'

    # 保持Name字段不变(Patient实体确实用Name)
    'PatientName'          = 'Name'
}

# 通用的时间戳字段修正
$commonFieldMappings = @{
    'CreatedTime'          = 'CreatedAt'
    'UpdatedTime'          = 'UpdateTime'
    'UpdatedAt'            = 'UpdateTime'
    'LastUpdateTime'       = 'UpdateTime'
    'ModifiedTime'         = 'UpdateTime'
    'LastModifiedTime'     = 'UpdateTime'
    'ModifiedAt'           = 'UpdateTime'
}

$totalFixed = 0

foreach ($file in $testFiles) {
    Write-Host "  处理文件: $($file.Name)" -ForegroundColor Gray

    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileFixed = 0

    # 根据文件类型应用不同的修正规则
    if ($file.FullName -like "*User*Test*.cs" -or $file.FullName -like "*Auth*Test*.cs") {
        # User和Auth相关测试
        foreach ($pattern in $userFieldMappings.Keys) {
            $replacement = $userFieldMappings[$pattern]
            $matches = [regex]::Matches($content, $pattern)
            if ($matches.Count -gt 0) {
                $content = $content -replace [regex]::Escape($pattern), $replacement
                $fileFixed += $matches.Count
            }
        }
    }
    elseif ($file.FullName -like "*Patient*Test*.cs") {
        # Patient相关测试
        foreach ($pattern in $patientFieldMappings.Keys) {
            $replacement = $patientFieldMappings[$pattern]
            $matches = [regex]::Matches($content, $pattern)
            if ($matches.Count -gt 0) {
                $content = $content -replace [regex]::Escape($pattern), $replacement
                $fileFixed += $matches.Count
            }
        }
    }

    # 应用通用时间戳修正
    foreach ($pattern in $commonFieldMappings.Keys) {
        $replacement = $commonFieldMappings[$pattern]
        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            $content = $content -replace [regex]::Escape($pattern), $replacement
            $fileFixed += $matches.Count
        }
    }

    # 特殊处理：修复DTO创建中的字段名
    if ($content -match "new UserDto" -or $content -match "new UserCreateDto") {
        # UserDto专用修正
        $content = $content -replace '(new UserDto[^{]*{[^}]*?)Name\s*=', '$1RealName ='
        $content = $content -replace '(new UserDto[^{]*{[^}]*?)UserName\s*=', '$1Username ='
        $content = $content -replace '(new UserCreateDto[^{]*{[^}]*?)Name\s*=', '$1RealName ='
        $content = $content -replace '(new UserCreateDto[^{]*{[^}]*?)UserName\s*=', '$1Username ='
    }

    # 修复Setup和Verify调用中的字段名
    $content = $content -replace '(Setup.*?x\s*=>\s*x\.)UserName', '$1Username'
    $content = $content -replace '(Verify.*?x\s*=>\s*x\.)UserName', '$1Username'
    $content = $content -replace '(Setup.*?u\s*=>\s*u\.)UserName', '$1Username'
    $content = $content -replace '(Verify.*?u\s*=>\s*u\.)UserName', '$1Username'

    # 修复It.IsAny中的参数
    $content = $content -replace 'It\.Is<User>\(.*?u\.UserName', 'It.Is<User>(u => u.Username'
    $content = $content -replace 'It\.Is<User>\(.*?u\.Name', 'It.Is<User>(u => u.RealName'

    # 修复断言中的字段名
    $content = $content -replace '(Should\(\)\..*?)\.UserName', '$1.Username'
    $content = $content -replace '(Should\(\)\..*?)\.Name([^a-zA-Z])', '$1.RealName$2'

    # 保存修正后的文件
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "    ✓ 修复了 $fileFixed 处字段名称" -ForegroundColor Green
        $totalFixed += $fileFixed
    }
}

Write-Host "`n修复完成! 共修正了 $totalFixed 处字段名称问题" -ForegroundColor Green

# 编译测试以验证修复
Write-Host "`n开始编译测试项目以验证修复..." -ForegroundColor Yellow
dotnet build LYBT.All.sln --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ 编译成功!" -ForegroundColor Green
} else {
    Write-Host "✗ 编译失败，请检查错误信息" -ForegroundColor Red
}