# Phase E1: 小诊所配置优化脚本
# 自动优化现有 appsettings.json 以适配小诊所环境

param(
    [string]$ConfigPath = "src/Server/Services/LYBT.WebAPI/appsettings.json",
    [string]$OutputPath = "src/Server/Services/LYBT.WebAPI/appsettings.ClinicOptimized.json",
    [switch]$DryRun = $false
)

Write-Host "🏥 Phase E1: 小诊所配置优化脚本" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# 检查输入文件是否存在
if (-not (Test-Path $ConfigPath)) {
    Write-Error "配置文件不存在: $ConfigPath"
    exit 1
}

# 读取现有配置
Write-Host "📖 读取现有配置: $ConfigPath" -ForegroundColor Yellow
$config = Get-Content $ConfigPath | ConvertFrom-Json

Write-Host "🔧 应用小诊所优化参数..." -ForegroundColor Yellow

# 1. 优化数据库连接池
if ($config.ConnectionStrings -and $config.ConnectionStrings.DefaultConnection) {
    $connectionString = $config.ConnectionStrings.DefaultConnection
    
    # 替换连接池参数
    $connectionString = $connectionString -replace "Max Pool Size=\d+", "Max Pool Size=10"
    $connectionString = $connectionString -replace "Min Pool Size=\d+", "Min Pool Size=1"
    $connectionString = $connectionString -replace "Connection Timeout=\d+", "Connection Timeout=10"
    $connectionString = $connectionString -replace "Command Timeout=\d+", "Command Timeout=15"
    
    $config.ConnectionStrings.DefaultConnection = $connectionString
    Write-Host "  ✅ 数据库连接池优化: Max=10, Min=1, Timeout=10/15" -ForegroundColor Green
}

# 2. 优化JWT配置
if ($config.JwtOptions) {
    $config.JwtOptions.ClockSkewSeconds = 120
    Write-Host "  ✅ JWT时钟偏移优化: 120秒" -ForegroundColor Green
}

# 3. 优化缓存配置
if ($config.CacheOptions) {
    if ($config.CacheOptions.MemoryCache) {
        $config.CacheOptions.MemoryCache.SizeLimit = 50
        $config.CacheOptions.MemoryCache.CompactionPercentage = 0.20
        $config.CacheOptions.MemoryCache.ExpirationScanFrequency = 60
        Write-Host "  ✅ 内存缓存优化: 限制50项, 清理20%, 扫描60秒" -ForegroundColor Green
    }
    
    if ($config.CacheOptions.Performance) {
        $config.CacheOptions.Performance.EnableCompression = $false
        $config.CacheOptions.Performance.CompressionThreshold = 2048
        $config.CacheOptions.Performance.AsyncTimeout = 3000
        Write-Host "  ✅ 缓存性能优化: 禁用压缩, 超时3秒" -ForegroundColor Green
    }
    
    $config.CacheOptions.DefaultExpiryMinutes = 60
}

# 4. 优化用户配置
if ($config.UserOptions) {
    $config.UserOptions.MaxBatchOperationSize = 50
    Write-Host "  ✅ 用户配置优化: 批量操作限制50" -ForegroundColor Green
}

# 5. 优化认证配置
if ($config.AuthOptions) {
    $config.AuthOptions.MaxFailedLoginAttempts = 3
    $config.AuthOptions.AccountLockoutDuration = "00:30:00"
    Write-Host "  ✅ 认证配置优化: 失败3次锁定30分钟" -ForegroundColor Green
}

# 6. 优化数据库配置
if ($config.DatabaseOptions) {
    $config.DatabaseOptions.CommandTimeout = 15
    $config.DatabaseOptions.ConnectionRetryCount = 2
    $config.DatabaseOptions.ConnectionRetryDelay = 10
    Write-Host "  ✅ 数据库配置优化: 超时15秒, 重试2次" -ForegroundColor Green
}

# 7. 优化日志配置
if ($config.Serilog) {
    # 设置更保守的日志级别
    $config.Serilog.MinimumLevel.Default = "Warning"
    $config.Serilog.MinimumLevel.Override."Microsoft.AspNetCore" = "Error"
    $config.Serilog.MinimumLevel.Override."Microsoft.EntityFrameworkCore" = "Error"
    $config.Serilog.MinimumLevel.Override."System" = "Error"
    
    # 优化文件日志配置
    foreach ($writer in $config.Serilog.WriteTo) {
        if ($writer.Name -eq "File" -and $writer.Args) {
            $writer.Args.retainedFileCountLimit = 7
            $writer.Args.fileSizeLimitBytes = 5242880  # 5MB
            $writer.Args.path = "logs/lybt-.log"
        }
    }
    
    Write-Host "  ✅ 日志配置优化: Warning级别, 保留7天, 单文件5MB" -ForegroundColor Green
}

# 8. 添加小诊所专用配置
$clinicConfig = @{
    MaxConcurrentUsers = 20
    MaxDailyVisits = 1000
    PeakHours = @{
        Start = "08:00"
        End = "18:00"
    }
    ResourceLimits = @{
        MaxMemoryMB = 512
        MaxCpuPercent = 70
        MaxDiskSpaceGB = 50
    }
    Performance = @{
        EnablePreloading = $false
        EnableWarmup = $false
        GCMode = "Conservative"
    }
}

$config | Add-Member -MemberType NoteProperty -Name "ClinicOptimizations" -Value $clinicConfig -Force
Write-Host "  ✅ 添加小诊所专用配置" -ForegroundColor Green

# 添加配置文件头部注释
$configWithComments = [ordered]@{
    "//" = "🏥 小诊所资源保守配置 - Phase E1 专用配置文件"
    "//2" = "适用于2-5名医生、<20用户、日访问量<1000次的小型诊所环境"
    "//3" = "💡 使用方法: 复制到 appsettings.Production.json 并根据实际情况调整"
}

# 合并配置
$properties = $config.PSObject.Properties | Sort-Object Name
foreach ($prop in $properties) {
    $configWithComments[$prop.Name] = $prop.Value
}

if ($DryRun) {
    Write-Host "🔍 预览模式 - 配置更改:" -ForegroundColor Cyan
    $configWithComments | ConvertTo-Json -Depth 10 | Write-Host
} else {
    # 输出优化后的配置
    Write-Host "💾 保存优化配置到: $OutputPath" -ForegroundColor Yellow
    $configWithComments | ConvertTo-Json -Depth 10 | Set-Content $OutputPath -Encoding UTF8
    
    Write-Host ""
    Write-Host "✅ Phase E1 配置优化完成!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 优化摘要:" -ForegroundColor Cyan
    Write-Host "  • 数据库连接池: 10个最大连接, 1个最小连接" -ForegroundColor White
    Write-Host "  • 缓存大小: 50个项目限制" -ForegroundColor White
    Write-Host "  • 日志保留: 7天, 单文件5MB" -ForegroundColor White
    Write-Host "  • 批量操作: 50条记录限制" -ForegroundColor White
    Write-Host "  • 认证安全: 3次失败锁定30分钟" -ForegroundColor White
    Write-Host ""
    Write-Host "📖 部署指南: docs/deployment/clinic-deployment-guide.md" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "🎯 下一步操作:" -ForegroundColor Yellow
Write-Host "  1. 复制 appsettings.ClinicOptimized.json 到生产环境" -ForegroundColor White
Write-Host "  2. 设置环境变量 (JWT密钥、数据库连接等)" -ForegroundColor White
Write-Host "  3. 参考部署指南进行配置验证" -ForegroundColor White
Write-Host "  4. 监控系统资源使用情况" -ForegroundColor White