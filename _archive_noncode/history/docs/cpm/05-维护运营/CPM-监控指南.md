# CCPM 监控指南

## 概述

本指南介绍CCPM (Code-Claude Project Manager) 系统的监控配置、告警设置和性能监控策略。基于LYBTZYZS项目实际运营需求，提供完整的监控解决方案，确保系统稳定性和高可用性。

## 监控架构

### 监控层次结构

```
应用层监控
├── API性能监控 (响应时间、吞吐量、错误率)
├── 业务指标监控 (用户活动、功能使用)
└── 前端性能监控 (页面加载、用户体验)

系统层监控
├── 服务器资源监控 (CPU、内存、磁盘、网络)
├── 数据库监控 (连接数、查询性能、锁等待)
└── 进程监控 (应用进程状态、资源占用)

基础设施监控
├── 网络连接监控 (延迟、丢包率、带宽)
├── 存储监控 (磁盘IO、空间使用)
└── 外部依赖监控 (第三方服务可用性)
```

### 监控工具选择

#### 内置监控（推荐用于小型部署）
- **Windows性能计数器** - 系统资源监控
- **IIS日志分析** - Web服务器监控  
- **SQL Server监控** - 数据库性能监控
- **自定义PowerShell脚本** - 业务指标监控

#### 第三方监控（可选）
- **Prometheus + Grafana** - 开源监控解决方案
- **Application Insights** - Azure云监控服务
- **New Relic** - 商业APM解决方案

## 核心监控指标

### 应用性能指标

#### 1. API响应时间监控

```csharp
// PerformanceMonitoringMiddleware.cs
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly IMemoryCache _cache;
    
    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;
            
            // 记录性能指标
            LogPerformanceMetric(path, responseTime, context.Response.StatusCode);
            
            // 更新缓存中的性能统计
            UpdatePerformanceCache(path, responseTime);
            
            // 如果响应时间过长，记录警告
            if (responseTime > 2000) // 2秒阈值
            {
                _logger.LogWarning("Slow response detected: {Path} took {ResponseTime}ms", path, responseTime);
            }
        }
    }
    
    private void LogPerformanceMetric(string path, long responseTime, int statusCode)
    {
        _logger.LogInformation("API_PERFORMANCE|Path:{Path}|ResponseTime:{ResponseTime}ms|StatusCode:{StatusCode}", 
            path, responseTime, statusCode);
    }
    
    private void UpdatePerformanceCache(string path, long responseTime)
    {
        var key = $"perf_stats_{path}";
        var stats = _cache.GetOrCreate(key, factory =>
        {
            factory.SetSlidingExpiration(TimeSpan.FromMinutes(15));
            return new PerformanceStats();
        });
        
        stats.AddMeasurement(responseTime);
        _cache.Set(key, stats);
    }
}

public class PerformanceStats
{
    private readonly Queue<long> _measurements = new();
    private readonly object _lock = new();
    
    public void AddMeasurement(long responseTime)
    {
        lock (_lock)
        {
            _measurements.Enqueue(responseTime);
            if (_measurements.Count > 100) // 保持最近100次测量
            {
                _measurements.Dequeue();
            }
        }
    }
    
    public double GetAverageResponseTime()
    {
        lock (_lock)
        {
            return _measurements.Count > 0 ? _measurements.Average() : 0;
        }
    }
    
    public long GetMaxResponseTime()
    {
        lock (_lock)
        {
            return _measurements.Count > 0 ? _measurements.Max() : 0;
        }
    }
}
```

#### 2. 错误率监控

```csharp
// ErrorTrackingMiddleware.cs
public class ErrorTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorTrackingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ErrorCounter> _errorCounters = new();
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            
            // 记录成功请求
            if (context.Response.StatusCode < 400)
            {
                RecordRequest(context.Request.Path, success: true);
            }
        }
        catch (Exception ex)
        {
            // 记录异常
            RecordRequest(context.Request.Path, success: false);
            _logger.LogError(ex, "Unhandled exception in request {Path}", context.Request.Path);
            throw;
        }
        
        // 记录客户端错误和服务器错误
        if (context.Response.StatusCode >= 400)
        {
            RecordRequest(context.Request.Path, success: false);
            
            if (context.Response.StatusCode >= 500)
            {
                _logger.LogError("Server error {StatusCode} for {Path}", 
                    context.Response.StatusCode, context.Request.Path);
            }
        }
    }
    
    private void RecordRequest(string path, bool success)
    {
        var counter = _errorCounters.GetOrAdd(path, _ => new ErrorCounter());
        counter.RecordRequest(success);
    }
    
    public static Dictionary<string, double> GetErrorRates()
    {
        return _errorCounters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetErrorRate());
    }
}

public class ErrorCounter
{
    private long _totalRequests;
    private long _errorRequests;
    
    public void RecordRequest(bool success)
    {
        Interlocked.Increment(ref _totalRequests);
        if (!success)
        {
            Interlocked.Increment(ref _errorRequests);
        }
    }
    
    public double GetErrorRate()
    {
        var total = _totalRequests;
        var errors = _errorRequests;
        return total > 0 ? (double)errors / total * 100 : 0;
    }
}
```

### 系统资源监控

#### 1. 系统资源监控脚本

```powershell
# system-resource-monitor.ps1
param(
    [int]$IntervalSeconds = 60,
    [string]$LogPath = "logs\system-metrics.log",
    [switch]$SendAlerts
)

Write-Host "=== 系统资源监控启动 ===" -ForegroundColor Green
Write-Host "监控间隔: ${IntervalSeconds}秒" -ForegroundColor Cyan
Write-Host "日志路径: $LogPath" -ForegroundColor Cyan

# 确保日志目录存在
$logDir = Split-Path $LogPath -Parent
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force
}

# 告警阈值配置
$thresholds = @{
    CPUWarning = 70      # CPU使用率警告阈值
    CPUCritical = 85     # CPU使用率严重阈值
    MemoryWarning = 75   # 内存使用率警告阈值
    MemoryCritical = 90  # 内存使用率严重阈值
    DiskWarning = 80     # 磁盘使用率警告阈值
    DiskCritical = 90    # 磁盘使用率严重阈值
}

function Get-SystemMetrics {
    # CPU使用率
    $cpu = Get-WmiObject -Class Win32_Processor | Measure-Object -Property LoadPercentage -Average
    $cpuUsage = [math]::Round($cpu.Average, 2)
    
    # 内存使用率
    $memory = Get-WmiObject -Class Win32_OperatingSystem
    $memoryUsage = [math]::Round((($memory.TotalVisibleMemorySize - $memory.FreePhysicalMemory) / $memory.TotalVisibleMemorySize) * 100, 2)
    
    # 磁盘使用率
    $disks = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
    $diskMetrics = @()
    
    foreach ($disk in $disks) {
        $diskUsage = [math]::Round(((1 - ($disk.FreeSpace / $disk.Size)) * 100), 2)
        $diskMetrics += [PSCustomObject]@{
            Drive = $disk.DeviceID
            Usage = $diskUsage
            FreeSpaceGB = [math]::Round($disk.FreeSpace / 1GB, 2)
            TotalSpaceGB = [math]::Round($disk.Size / 1GB, 2)
        }
    }
    
    # 网络使用率（简化）
    $networkAdapters = Get-WmiObject -Class Win32_NetworkAdapter | Where-Object {$_.NetEnabled -eq $true -and $_.AdapterType -like "*Ethernet*"}
    $networkUsage = 0 # 简化处理，实际中需要计算网络吞吐量
    
    # 应用程序进程监控
    $appProcesses = Get-Process | Where-Object {$_.ProcessName -like "*LYBT*" -or $_.ProcessName -eq "dotnet"}
    $processMetrics = @()
    
    foreach ($proc in $appProcesses) {
        $processMetrics += [PSCustomObject]@{
            ProcessName = $proc.ProcessName
            PID = $proc.Id
            MemoryMB = [math]::Round($proc.WorkingSet64 / 1MB, 2)
            CPUTime = [math]::Round($proc.CPU, 2)
        }
    }
    
    return [PSCustomObject]@{
        Timestamp = Get-Date
        CPU = $cpuUsage
        Memory = $memoryUsage
        Disks = $diskMetrics
        Network = $networkUsage
        Processes = $processMetrics
    }
}

function Send-Alert($severity, $message) {
    if (-not $SendAlerts) { return }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $alertMessage = "[$timestamp] [$severity] $message"
    
    # 记录到告警日志
    $alertLogPath = "logs\alerts.log"
    Add-Content -Path $alertLogPath -Value $alertMessage -Encoding UTF8
    
    # 控制台输出
    switch ($severity) {
        "WARNING" { Write-Host "⚠️  $alertMessage" -ForegroundColor Yellow }
        "CRITICAL" { Write-Host "🚨 $alertMessage" -ForegroundColor Red }
        default { Write-Host "ℹ️  $alertMessage" -ForegroundColor Cyan }
    }
    
    # 这里可以添加邮件、短信、钉钉等通知方式
    # Send-Email -Subject "CCPM系统告警" -Body $alertMessage
    # Send-DingTalkMessage -Message $alertMessage
}

function Check-Thresholds($metrics) {
    # CPU检查
    if ($metrics.CPU -ge $thresholds.CPUCritical) {
        Send-Alert "CRITICAL" "CPU使用率严重过高: $($metrics.CPU)%"
    } elseif ($metrics.CPU -ge $thresholds.CPUWarning) {
        Send-Alert "WARNING" "CPU使用率偏高: $($metrics.CPU)%"
    }
    
    # 内存检查
    if ($metrics.Memory -ge $thresholds.MemoryCritical) {
        Send-Alert "CRITICAL" "内存使用率严重过高: $($metrics.Memory)%"
    } elseif ($metrics.Memory -ge $thresholds.MemoryWarning) {
        Send-Alert "WARNING" "内存使用率偏高: $($metrics.Memory)%"
    }
    
    # 磁盘检查
    foreach ($disk in $metrics.Disks) {
        if ($disk.Usage -ge $thresholds.DiskCritical) {
            Send-Alert "CRITICAL" "磁盘 $($disk.Drive) 使用率严重过高: $($disk.Usage)%"
        } elseif ($disk.Usage -ge $thresholds.DiskWarning) {
            Send-Alert "WARNING" "磁盘 $($disk.Drive) 使用率偏高: $($disk.Usage)%"
        }
    }
    
    # 进程检查
    foreach ($proc in $metrics.Processes) {
        if ($proc.MemoryMB -gt 1024) { # 进程内存超过1GB
            Send-Alert "WARNING" "进程 $($proc.ProcessName) (PID: $($proc.PID)) 内存使用过高: $($proc.MemoryMB)MB"
        }
    }
}

# 主监控循环
try {
    while ($true) {
        $metrics = Get-SystemMetrics
        
        # 生成JSON格式的指标数据
        $metricsJson = $metrics | ConvertTo-Json -Depth 3
        
        # 记录到日志文件
        Add-Content -Path $LogPath -Value $metricsJson -Encoding UTF8
        
        # 检查告警阈值
        Check-Thresholds $metrics
        
        # 控制台输出当前状态
        Write-Host "$(Get-Date -Format 'HH:mm:ss') - CPU: $($metrics.CPU)%, 内存: $($metrics.Memory)%, 进程数: $($metrics.Processes.Count)" -ForegroundColor Green
        
        # 等待下一个监控周期
        Start-Sleep -Seconds $IntervalSeconds
    }
} catch {
    Write-Host "监控异常: $($_.Exception.Message)" -ForegroundColor Red
    Send-Alert "CRITICAL" "系统监控脚本异常: $($_.Exception.Message)"
}
```

### 数据库监控

#### 1. SQL Server性能监控

```sql
-- database-performance-monitor.sql

-- 创建监控视图
CREATE OR ALTER VIEW vw_DatabasePerformanceMetrics
AS
SELECT 
    GETDATE() AS Timestamp,
    -- 连接数统计
    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1) AS UserConnections,
    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE database_id = DB_ID()) AS DatabaseConnections,
    
    -- 缓存命中率
    (SELECT 
        CAST(100.0 * (a.cntr_value - b.cntr_value) / 
        CASE WHEN a.cntr_value = 0 THEN 1 ELSE a.cntr_value END AS DECIMAL(5,2))
     FROM sys.dm_os_performance_counters a
     JOIN sys.dm_os_performance_counters b ON a.counter_name = b.counter_name
     WHERE a.counter_name = 'Buffer cache hit ratio'
     AND a.instance_name = ''
     AND b.counter_name = 'Buffer cache hit ratio base'
     AND b.instance_name = ''
    ) AS BufferCacheHitRatio,
    
    -- 锁等待统计
    (SELECT COUNT(*) FROM sys.dm_os_waiting_tasks WHERE wait_type LIKE 'LCK%') AS LockWaits,
    
    -- 磁盘IO统计
    (SELECT 
        SUM(num_of_reads + num_of_writes) 
     FROM sys.dm_io_virtual_file_stats(DB_ID(), NULL)
    ) AS TotalIOOperations,
    
    -- 数据库大小
    (SELECT 
        CAST(SUM(size) * 8.0 / 1024 / 1024 AS DECIMAL(10,2))
     FROM sys.database_files
    ) AS DatabaseSizeGB;
```

```powershell
# database-monitor.ps1
param(
    [int]$IntervalMinutes = 5,
    [string]$LogPath = "logs\database-metrics.log"
)

Write-Host "=== 数据库性能监控启动 ===" -ForegroundColor Green

$connectionString = "Server=localhost;Database=LYBTDB;Integrated Security=true;Connection Timeout=30;"

function Get-DatabaseMetrics {
    try {
        $query = "SELECT * FROM vw_DatabasePerformanceMetrics"
        $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query
        
        return [PSCustomObject]@{
            Timestamp = Get-Date
            UserConnections = $result.UserConnections
            DatabaseConnections = $result.DatabaseConnections
            BufferCacheHitRatio = $result.BufferCacheHitRatio
            LockWaits = $result.LockWaits
            TotalIOOperations = $result.TotalIOOperations
            DatabaseSizeGB = $result.DatabaseSizeGB
        }
    } catch {
        Write-Host "获取数据库指标失败: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Check-DatabaseThresholds($metrics) {
    if ($metrics -eq $null) { return }
    
    # 连接数检查
    if ($metrics.DatabaseConnections -gt 50) {
        Write-Host "⚠️  数据库连接数过多: $($metrics.DatabaseConnections)" -ForegroundColor Yellow
    }
    
    # 缓存命中率检查
    if ($metrics.BufferCacheHitRatio -lt 90) {
        Write-Host "⚠️  缓存命中率过低: $($metrics.BufferCacheHitRatio)%" -ForegroundColor Yellow
    }
    
    # 锁等待检查
    if ($metrics.LockWaits -gt 10) {
        Write-Host "⚠️  锁等待过多: $($metrics.LockWaits)" -ForegroundColor Yellow
    }
    
    # 数据库大小检查
    if ($metrics.DatabaseSizeGB -gt 10) {
        Write-Host "ℹ️  数据库大小: $($metrics.DatabaseSizeGB)GB" -ForegroundColor Cyan
    }
}

# 监控主循环
while ($true) {
    $metrics = Get-DatabaseMetrics
    
    if ($metrics) {
        # 记录到日志
        $metricsJson = $metrics | ConvertTo-Json
        Add-Content -Path $LogPath -Value $metricsJson -Encoding UTF8
        
        # 检查阈值
        Check-DatabaseThresholds $metrics
        
        # 控制台输出
        Write-Host "$(Get-Date -Format 'HH:mm:ss') - 连接: $($metrics.DatabaseConnections), 缓存命中率: $($metrics.BufferCacheHitRatio)%" -ForegroundColor Green
    }
    
    Start-Sleep -Seconds ($IntervalMinutes * 60)
}
```

### 业务指标监控

#### 1. 业务活动监控

```csharp
// BusinessMetricsService.cs
public class BusinessMetricsService : IBusinessMetricsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BusinessMetricsService> _logger;
    private readonly IMemoryCache _cache;
    
    public BusinessMetricsService(AppDbContext context, ILogger<BusinessMetricsService> logger, IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<BusinessMetrics> GetBusinessMetricsAsync()
    {
        const string cacheKey = "business_metrics";
        
        return await _cache.GetOrCreateAsync(cacheKey, async factory =>
        {
            factory.SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // 5分钟缓存
            
            var now = DateTime.Now;
            var today = now.Date;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            
            var metrics = new BusinessMetrics
            {
                Timestamp = now,
                
                // 今日统计
                TodayUsers = await _context.Users.CountAsync(u => u.CreateTime.Date == today),
                TodayPatients = await _context.Patients.CountAsync(p => p.CreateTime.Date == today),
                TodayConsultations = await _context.Consultations.CountAsync(c => c.CreateTime.Date == today),
                TodayPrescriptions = await _context.Prescriptions.CountAsync(p => p.CreateTime.Date == today),
                
                // 本月统计
                MonthlyUsers = await _context.Users.CountAsync(u => u.CreateTime >= thisMonth),
                MonthlyPatients = await _context.Patients.CountAsync(p => p.CreateTime >= thisMonth),
                MonthlyConsultations = await _context.Consultations.CountAsync(c => c.CreateTime >= thisMonth),
                MonthlyPrescriptions = await _context.Prescriptions.CountAsync(p => p.CreateTime >= thisMonth),
                
                // 总计统计
                TotalUsers = await _context.Users.CountAsync(),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalConsultations = await _context.Consultations.CountAsync(),
                TotalPrescriptions = await _context.Prescriptions.CountAsync(),
                
                // 活跃用户（最近7天有操作）
                ActiveUsers = await _context.Users
                    .Where(u => u.LastLoginTime >= now.AddDays(-7))
                    .CountAsync(),
                
                // 系统健康指标
                DatabaseSize = await GetDatabaseSizeAsync(),
                ActiveConnections = await GetActiveConnectionsAsync()
            };
            
            return metrics;
        });
    }
    
    private async Task<decimal> GetDatabaseSizeAsync()
    {
        try
        {
            var query = "SELECT CAST(SUM(size) * 8.0 / 1024 / 1024 AS DECIMAL(10,2)) FROM sys.database_files";
            var result = await _context.Database.SqlQueryRaw<decimal>(query).FirstOrDefaultAsync();
            return result;
        }
        catch
        {
            return 0;
        }
    }
    
    private async Task<int> GetActiveConnectionsAsync()
    {
        try
        {
            var query = "SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE database_id = DB_ID()";
            var result = await _context.Database.SqlQueryRaw<int>(query).FirstOrDefaultAsync();
            return result;
        }
        catch
        {
            return 0;
        }
    }
    
    public async Task LogBusinessEventAsync(string eventType, string description, object data = null)
    {
        var logEntry = new BusinessEventLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Description = description,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            Timestamp = DateTime.Now
        };
        
        _context.BusinessEventLogs.Add(logEntry);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("BUSINESS_EVENT|Type:{EventType}|Description:{Description}", 
            eventType, description);
    }
}

public class BusinessMetrics
{
    public DateTime Timestamp { get; set; }
    
    // 今日统计
    public int TodayUsers { get; set; }
    public int TodayPatients { get; set; }
    public int TodayConsultations { get; set; }
    public int TodayPrescriptions { get; set; }
    
    // 本月统计
    public int MonthlyUsers { get; set; }
    public int MonthlyPatients { get; set; }
    public int MonthlyConsultations { get; set; }
    public int MonthlyPrescriptions { get; set; }
    
    // 总计统计
    public int TotalUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalConsultations { get; set; }
    public int TotalPrescriptions { get; set; }
    
    // 系统指标
    public int ActiveUsers { get; set; }
    public decimal DatabaseSize { get; set; }
    public int ActiveConnections { get; set; }
}
```

## 告警配置

### 告警规则配置

```json
// alert-rules.json
{
  "alertRules": {
    "system": {
      "cpu": {
        "warningThreshold": 70,
        "criticalThreshold": 85,
        "checkInterval": 60,
        "recipients": ["admin@company.com"]
      },
      "memory": {
        "warningThreshold": 75,
        "criticalThreshold": 90,
        "checkInterval": 60,
        "recipients": ["admin@company.com"]
      },
      "disk": {
        "warningThreshold": 80,
        "criticalThreshold": 90,
        "checkInterval": 300,
        "recipients": ["admin@company.com"]
      }
    },
    "application": {
      "responseTime": {
        "warningThreshold": 2000,
        "criticalThreshold": 5000,
        "checkInterval": 30,
        "recipients": ["dev@company.com", "admin@company.com"]
      },
      "errorRate": {
        "warningThreshold": 5,
        "criticalThreshold": 10,
        "checkInterval": 60,
        "recipients": ["dev@company.com"]
      }
    },
    "database": {
      "connections": {
        "warningThreshold": 50,
        "criticalThreshold": 80,
        "checkInterval": 120,
        "recipients": ["dba@company.com", "admin@company.com"]
      },
      "cacheHitRatio": {
        "warningThreshold": 85,
        "criticalThreshold": 70,
        "checkInterval": 300,
        "recipients": ["dba@company.com"]
      }
    },
    "business": {
      "dailyUsers": {
        "minimumThreshold": 1,
        "checkInterval": 3600,
        "recipients": ["business@company.com"]
      }
    }
  },
  "notificationChannels": {
    "email": {
      "enabled": true,
      "smtpServer": "smtp.company.com",
      "port": 587,
      "username": "alerts@company.com",
      "password": "encrypted_password"
    },
    "webhook": {
      "enabled": true,
      "url": "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK"
    }
  }
}
```

### 告警处理脚本

```powershell
# alert-handler.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$AlertType,
    
    [Parameter(Mandatory=$true)]
    [string]$Severity,
    
    [Parameter(Mandatory=$true)]
    [string]$Message,
    
    [string]$Data = "",
    [string]$ConfigPath = "config\alert-rules.json"
)

# 读取告警配置
$alertConfig = Get-Content $ConfigPath -Raw | ConvertFrom-Json

function Send-EmailAlert($recipients, $subject, $body) {
    if (-not $alertConfig.notificationChannels.email.enabled) {
        return
    }
    
    $smtpConfig = $alertConfig.notificationChannels.email
    
    try {
        $credential = New-Object System.Management.Automation.PSCredential(
            $smtpConfig.username,
            (ConvertTo-SecureString $smtpConfig.password -AsPlainText -Force)
        )
        
        foreach ($recipient in $recipients) {
            Send-MailMessage -To $recipient -From $smtpConfig.username -Subject $subject -Body $body -SmtpServer $smtpConfig.smtpServer -Port $smtpConfig.port -Credential $credential -UseSsl
        }
        
        Write-Host "✅ 邮件告警发送成功" -ForegroundColor Green
    } catch {
        Write-Host "❌ 邮件告警发送失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Send-WebhookAlert($message) {
    if (-not $alertConfig.notificationChannels.webhook.enabled) {
        return
    }
    
    try {
        $webhook = $alertConfig.notificationChannels.webhook
        $payload = @{
            text = $message
            username = "CCPM-Alert"
            icon_emoji = ":warning:"
        } | ConvertTo-Json
        
        Invoke-RestMethod -Uri $webhook.url -Method Post -Body $payload -ContentType "application/json"
        Write-Host "✅ Webhook告警发送成功" -ForegroundColor Green
    } catch {
        Write-Host "❌ Webhook告警发送失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 处理告警
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$fullMessage = "[$timestamp] [$Severity] $Message"

# 记录到告警日志
$alertLogPath = "logs\alerts.log"
Add-Content -Path $alertLogPath -Value "$fullMessage | Data: $Data" -Encoding UTF8

# 确定收件人
$recipients = @()
switch ($AlertType) {
    "system" { $recipients = $alertConfig.alertRules.system.cpu.recipients }
    "application" { $recipients = $alertConfig.alertRules.application.responseTime.recipients }
    "database" { $recipients = $alertConfig.alertRules.database.connections.recipients }
    "business" { $recipients = $alertConfig.alertRules.business.dailyUsers.recipients }
}

# 发送告警
$subject = "CCPM系统告警 - $Severity"
$body = @"
CCPM系统告警通知

告警类型: $AlertType
严重程度: $Severity
告警消息: $Message
发生时间: $timestamp

$(if ($Data) { "详细数据: $Data" })

请及时检查系统状态并采取相应措施。

---
此邮件由CCPM监控系统自动发送
"@

if ($recipients.Count -gt 0) {
    Send-EmailAlert -Recipients $recipients -Subject $subject -Body $body
}

Send-WebhookAlert -Message $fullMessage

# 控制台输出
switch ($Severity) {
    "WARNING" { Write-Host "⚠️  $fullMessage" -ForegroundColor Yellow }
    "CRITICAL" { Write-Host "🚨 $fullMessage" -ForegroundColor Red }
    default { Write-Host "ℹ️  $fullMessage" -ForegroundColor Cyan }
}
```

## 监控仪表板

### 简化监控仪表板（HTML版本）

```html
<!-- monitoring-dashboard.html -->
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>CCPM 监控仪表板</title>
    <meta http-equiv="refresh" content="30">
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; }
        .header { text-align: center; margin-bottom: 30px; }
        .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .metric-card { background: white; border-radius: 8px; padding: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .metric-title { font-size: 18px; font-weight: bold; margin-bottom: 15px; color: #333; }
        .metric-value { font-size: 36px; font-weight: bold; margin-bottom: 10px; }
        .metric-unit { font-size: 14px; color: #666; }
        .status-ok { color: #4CAF50; }
        .status-warning { color: #FF9800; }
        .status-critical { color: #F44336; }
        .progress-bar { width: 100%; height: 20px; background-color: #e0e0e0; border-radius: 10px; overflow: hidden; margin: 10px 0; }
        .progress-fill { height: 100%; transition: width 0.3s ease; }
        .progress-ok { background-color: #4CAF50; }
        .progress-warning { background-color: #FF9800; }
        .progress-critical { background-color: #F44336; }
        .timestamp { text-align: center; color: #666; margin-top: 20px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>CCPM 系统监控仪表板</h1>
            <p>实时监控系统运行状态</p>
        </div>
        
        <div class="metrics-grid">
            <!-- CPU 使用率 -->
            <div class="metric-card">
                <div class="metric-title">CPU 使用率</div>
                <div class="metric-value status-ok" id="cpu-value">0%</div>
                <div class="progress-bar">
                    <div class="progress-fill progress-ok" id="cpu-bar" style="width: 0%"></div>
                </div>
                <div class="metric-unit">实时CPU占用率</div>
            </div>
            
            <!-- 内存使用率 -->
            <div class="metric-card">
                <div class="metric-title">内存使用率</div>
                <div class="metric-value status-ok" id="memory-value">0%</div>
                <div class="progress-bar">
                    <div class="progress-fill progress-ok" id="memory-bar" style="width: 0%"></div>
                </div>
                <div class="metric-unit">物理内存占用率</div>
            </div>
            
            <!-- API响应时间 -->
            <div class="metric-card">
                <div class="metric-title">API响应时间</div>
                <div class="metric-value status-ok" id="api-value">0ms</div>
                <div class="metric-unit">平均API响应延迟</div>
            </div>
            
            <!-- 数据库连接数 -->
            <div class="metric-card">
                <div class="metric-title">数据库连接</div>
                <div class="metric-value status-ok" id="db-connections">0</div>
                <div class="metric-unit">活跃数据库连接数</div>
            </div>
            
            <!-- 今日新增用户 -->
            <div class="metric-card">
                <div class="metric-title">今日新增用户</div>
                <div class="metric-value status-ok" id="new-users">0</div>
                <div class="metric-unit">今日注册用户数量</div>
            </div>
            
            <!-- 今日诊疗次数 -->
            <div class="metric-card">
                <div class="metric-title">今日诊疗次数</div>
                <div class="metric-value status-ok" id="consultations">0</div>
                <div class="metric-unit">今日完成诊疗数量</div>
            </div>
        </div>
        
        <div class="timestamp" id="last-update">
            最后更新时间: --
        </div>
    </div>
    
    <script>
        async function updateMetrics() {
            try {
                // 调用监控API获取实时数据
                const response = await fetch('/api/v1/monitoring/metrics');
                const data = await response.json();
                
                if (data.success) {
                    const metrics = data.data;
                    
                    // 更新CPU指标
                    updateMetric('cpu', metrics.cpu, 70, 85, '%');
                    
                    // 更新内存指标
                    updateMetric('memory', metrics.memory, 75, 90, '%');
                    
                    // 更新API响应时间
                    updateValueMetric('api', metrics.apiResponseTime, 2000, 5000, 'ms');
                    
                    // 更新数据库连接数
                    updateValueMetric('db-connections', metrics.dbConnections, 50, 80);
                    
                    // 更新业务指标
                    document.getElementById('new-users').textContent = metrics.todayUsers || 0;
                    document.getElementById('consultations').textContent = metrics.todayConsultations || 0;
                    
                    // 更新时间戳
                    document.getElementById('last-update').textContent = 
                        '最后更新时间: ' + new Date().toLocaleString();
                }
            } catch (error) {
                console.error('更新指标失败:', error);
            }
        }
        
        function updateMetric(id, value, warningThreshold, criticalThreshold, unit = '') {
            const valueElement = document.getElementById(id + '-value');
            const barElement = document.getElementById(id + '-bar');
            
            // 更新数值显示
            valueElement.textContent = value + unit;
            
            // 更新进度条
            barElement.style.width = Math.min(value, 100) + '%';
            
            // 更新颜色状态
            let statusClass = 'status-ok';
            let barClass = 'progress-ok';
            
            if (value >= criticalThreshold) {
                statusClass = 'status-critical';
                barClass = 'progress-critical';
            } else if (value >= warningThreshold) {
                statusClass = 'status-warning';
                barClass = 'progress-warning';
            }
            
            valueElement.className = 'metric-value ' + statusClass;
            barElement.className = 'progress-fill ' + barClass;
        }
        
        function updateValueMetric(id, value, warningThreshold, criticalThreshold, unit = '') {
            const element = document.getElementById(id);
            element.textContent = value + unit;
            
            let statusClass = 'status-ok';
            if (value >= criticalThreshold) {
                statusClass = 'status-critical';
            } else if (value >= warningThreshold) {
                statusClass = 'status-warning';
            }
            
            element.className = 'metric-value ' + statusClass;
        }
        
        // 页面加载时立即更新一次
        updateMetrics();
        
        // 每30秒自动更新一次
        setInterval(updateMetrics, 30000);
    </script>
</body>
</html>
```

### 监控API端点

```csharp
// MonitoringController.cs
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class MonitoringController : ControllerBase
{
    private readonly ISystemMetricsService _systemMetrics;
    private readonly IBusinessMetricsService _businessMetrics;
    private readonly ILogger<MonitoringController> _logger;
    
    public MonitoringController(
        ISystemMetricsService systemMetrics,
        IBusinessMetricsService businessMetrics,
        ILogger<MonitoringController> logger)
    {
        _systemMetrics = systemMetrics;
        _businessMetrics = businessMetrics;
        _logger = logger;
    }
    
    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<MonitoringMetrics>>> GetMetrics()
    {
        try
        {
            var systemMetrics = await _systemMetrics.GetCurrentMetricsAsync();
            var businessMetrics = await _businessMetrics.GetBusinessMetricsAsync();
            
            var result = new MonitoringMetrics
            {
                // 系统指标
                Cpu = systemMetrics.CpuUsage,
                Memory = systemMetrics.MemoryUsage,
                ApiResponseTime = systemMetrics.AverageResponseTime,
                DbConnections = systemMetrics.DatabaseConnections,
                
                // 业务指标
                TodayUsers = businessMetrics.TodayUsers,
                TodayConsultations = businessMetrics.TodayConsultations,
                TodayPrescriptions = businessMetrics.TodayPrescriptions,
                
                // 系统状态
                SystemStatus = GetOverallSystemStatus(systemMetrics),
                Timestamp = DateTime.Now
            };
            
            return Ok(ApiResponse<MonitoringMetrics>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取监控指标失败");
            return StatusCode(500, ApiResponse<MonitoringMetrics>.Error("获取监控指标失败", ex.Message));
        }
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> GetHealthStatus()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.Now,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };
        
        return Ok(health);
    }
    
    private string GetOverallSystemStatus(SystemMetrics metrics)
    {
        if (metrics.CpuUsage > 85 || metrics.MemoryUsage > 90)
            return "Critical";
        
        if (metrics.CpuUsage > 70 || metrics.MemoryUsage > 75 || metrics.AverageResponseTime > 2000)
            return "Warning";
            
        return "Healthy";
    }
}

public class MonitoringMetrics
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
    public long ApiResponseTime { get; set; }
    public int DbConnections { get; set; }
    public int TodayUsers { get; set; }
    public int TodayConsultations { get; set; }
    public int TodayPrescriptions { get; set; }
    public string SystemStatus { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 相关文档

- [CPM-维护流程.md](CPM-维护流程.md) - 日常维护操作流程
- [CPM-升级指南.md](CPM-升级指南.md) - 版本升级操作指南
- [../04-故障排除/CPM-故障排除指南.md](../04-故障排除/CPM-故障排除指南.md) - 故障诊断流程

## 更新记录

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2025-01-31 | 初始版本，建立完整的监控体系 | Claude |

---

**部署说明**:
1. 监控脚本需要管理员权限运行
2. 数据库监控需要适当的数据库权限
3. 邮件告警需要配置SMTP服务器信息
4. 根据实际环境调整监控阈值和告警规则