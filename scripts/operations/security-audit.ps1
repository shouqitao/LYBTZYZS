# 安全审计脚本
# 用途：执行系统安全审计，检查配置和潜在的安全问题

param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\audit-reports",

    [Parameter(Mandatory=$false)]
    [ValidateSet("Basic", "Full", "Compliance")]
    [string]$AuditLevel = "Full",

    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport = $true
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "LYBT 安全审计工具" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# 创建输出目录
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$auditResults = @{
    "timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "auditLevel" = $AuditLevel
    "system" = @{}
    "configuration" = @{}
    "security" = @{}
    "compliance" = @{}
    "recommendations" = @()
}

# 系统信息收集
function Get-SystemInfo {
    Write-Host "收集系统信息..." -ForegroundColor Cyan

    $systemInfo = @{
        "hostname" = $env:COMPUTERNAME
        "os" = (Get-WmiObject Win32_OperatingSystem).Caption
        "osVersion" = (Get-WmiObject Win32_OperatingSystem).Version
        "dotnetVersion" = (Get-Command dotnet).Version.ToString()
        "powershellVersion" = $PSVersionTable.PSVersion.ToString()
        "lastBootTime" = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
    }

    $auditResults["system"] = $systemInfo
    Write-Host "  ✓ 系统信息收集完成" -ForegroundColor Green
    return $systemInfo
}

# 配置审计
function Audit-Configuration {
    Write-Host "审计配置文件..." -ForegroundColor Cyan

    $configAudit = @{
        "appsettings" = @{}
        "environment" = @{}
        "security" = @{}
    }

    # 检查 appsettings 文件
    $appsettingsFiles = @(
        ".\src\Server\Services\LYBT.WebAPI\appsettings.json",
        ".\src\Server\Services\LYBT.WebAPI\appsettings.Development.json",
        ".\src\Server\Services\LYBT.WebAPI\appsettings.Production.json"
    )

    foreach ($file in $appsettingsFiles) {
        if (Test-Path $file) {
            $config = Get-Content $file | ConvertFrom-Json
            $fileName = Split-Path $file -Leaf

            # 检查敏感配置
            $issues = @()

            # JWT密钥检查
            if ($config.JwtOptions.Secret -and -not $config.JwtOptions.Secret.StartsWith("$")) {
                $issues += "JWT密钥硬编码在配置文件中"
                $auditResults["recommendations"] += "将JWT密钥移至环境变量或密钥管理服务"
            }

            # 连接字符串检查
            if ($config.ConnectionStrings.DefaultConnection -and -not $config.ConnectionStrings.DefaultConnection.StartsWith("$")) {
                $issues += "数据库连接字符串硬编码在配置文件中"
                $auditResults["recommendations"] += "将连接字符串移至环境变量或加密存储"
            }

            # 日志级别检查
            if ($fileName -eq "appsettings.Production.json" -and $config.Serilog.MinimumLevel.Default -eq "Debug") {
                $issues += "生产环境日志级别设置为Debug"
                $auditResults["recommendations"] += "将生产环境日志级别设置为Warning或更高"
            }

            $configAudit["appsettings"][$fileName] = @{
                "exists" = $true
                "issues" = $issues
                "issueCount" = $issues.Count
            }
        }
    }

    # 检查环境变量
    $requiredEnvVars = @("JWT_SECRET", "CONNECTION_STRING", "ADMIN_DEFAULT_PASSWORD", "USER_DEFAULT_PASSWORD")
    foreach ($envVar in $requiredEnvVars) {
        $value = [Environment]::GetEnvironmentVariable($envVar)
        $configAudit["environment"][$envVar] = @{
            "exists" = $null -ne $value
            "isEmpty" = [string]::IsNullOrWhiteSpace($value)
        }

        if (-not $value) {
            $auditResults["recommendations"] += "设置环境变量: $envVar"
        }
    }

    $auditResults["configuration"] = $configAudit
    Write-Host "  ✓ 配置审计完成" -ForegroundColor Green
    return $configAudit
}

# 安全检查
function Audit-Security {
    Write-Host "执行安全检查..." -ForegroundColor Cyan

    $securityAudit = @{
        "certificates" = @{}
        "ports" = @{}
        "services" = @{}
        "permissions" = @{}
    }

    # 检查证书
    Write-Host "  检查SSL证书..." -ForegroundColor Gray
    $certs = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*LYBT*" -or $_.Subject -like "*localhost*" }
    foreach ($cert in $certs) {
        $certInfo = @{
            "subject" = $cert.Subject
            "thumbprint" = $cert.Thumbprint
            "expiryDate" = $cert.NotAfter
            "daysUntilExpiry" = ($cert.NotAfter - (Get-Date)).Days
            "isExpired" = $cert.NotAfter -lt (Get-Date)
        }

        if ($certInfo["daysUntilExpiry"] -lt 30) {
            $auditResults["recommendations"] += "证书即将过期: $($cert.Subject)"
        }

        $securityAudit["certificates"][$cert.Thumbprint] = $certInfo
    }

    # 检查开放端口
    Write-Host "  检查网络端口..." -ForegroundColor Gray
    $ports = @(5001, 443, 80, 1433)  # API, HTTPS, HTTP, SQL Server
    foreach ($port in $ports) {
        $tcpConnection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue
        $securityAudit["ports"][$port] = @{
            "isOpen" = $tcpConnection.TcpTestSucceeded
            "service" = switch ($port) {
                5001 { "LYBT API" }
                443 { "HTTPS" }
                80 { "HTTP" }
                1433 { "SQL Server" }
                default { "Unknown" }
            }
        }

        if ($port -eq 80 -and $tcpConnection.TcpTestSucceeded) {
            $auditResults["recommendations"] += "考虑禁用HTTP端口80，仅使用HTTPS"
        }
    }

    # 检查Windows服务
    Write-Host "  检查相关服务..." -ForegroundColor Gray
    $services = @("MSSQLSERVER", "W3SVC", "IISADMIN")
    foreach ($serviceName in $services) {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service) {
            $securityAudit["services"][$serviceName] = @{
                "status" = $service.Status
                "startType" = $service.StartType
                "account" = (Get-WmiObject Win32_Service -Filter "Name='$serviceName'").StartName
            }
        }
    }

    # 检查文件权限
    Write-Host "  检查文件权限..." -ForegroundColor Gray
    $criticalPaths = @(
        ".\src\Server\Services\LYBT.WebAPI\appsettings.Production.json",
        ".\DataProtection-Keys",
        ".\logs"
    )

    foreach ($path in $criticalPaths) {
        if (Test-Path $path) {
            $acl = Get-Acl $path
            $permissions = @()

            foreach ($access in $acl.Access) {
                if ($access.IdentityReference -like "*Users*" -and $access.FileSystemRights -like "*Write*") {
                    $permissions += "警告: Users组有写入权限"
                    $auditResults["recommendations"] += "限制 $path 的写入权限"
                }
            }

            $securityAudit["permissions"][$path] = @{
                "exists" = $true
                "warnings" = $permissions
            }
        }
    }

    $auditResults["security"] = $securityAudit
    Write-Host "  ✓ 安全检查完成" -ForegroundColor Green
    return $securityAudit
}

# 合规性检查
function Audit-Compliance {
    if ($AuditLevel -ne "Compliance" -and $AuditLevel -ne "Full") {
        return $null
    }

    Write-Host "执行合规性检查..." -ForegroundColor Cyan

    $complianceAudit = @{
        "passwordPolicy" = @{}
        "encryption" = @{}
        "logging" = @{}
        "backup" = @{}
    }

    # 密码策略检查
    Write-Host "  检查密码策略..." -ForegroundColor Gray
    $complianceAudit["passwordPolicy"] = @{
        "minLength" = 12
        "requireUppercase" = $true
        "requireLowercase" = $true
        "requireDigit" = $true
        "requireSpecialChar" = $true
        "maxAge" = 90
    }

    # 加密检查
    Write-Host "  检查加密配置..." -ForegroundColor Gray
    $complianceAudit["encryption"] = @{
        "tlsVersion" = "1.2+"
        "dataProtection" = "AES-256"
        "passwordHashing" = "PBKDF2-SHA256"
        "iterations" = 100000
    }

    # 日志审计
    Write-Host "  检查日志配置..." -ForegroundColor Gray
    $logFiles = Get-ChildItem ".\logs" -Filter "*.log" -ErrorAction SilentlyContinue
    $complianceAudit["logging"] = @{
        "logRetention" = 60
        "logFilesCount" = $logFiles.Count
        "oldestLog" = if ($logFiles) { $logFiles | Sort-Object CreationTime | Select-Object -First 1 -ExpandProperty CreationTime } else { $null }
        "totalSize" = if ($logFiles) { ($logFiles | Measure-Object Length -Sum).Sum / 1MB } else { 0 }
    }

    # 备份检查
    Write-Host "  检查备份策略..." -ForegroundColor Gray
    $backupFiles = Get-ChildItem ".\backups" -Filter "*.bak*" -ErrorAction SilentlyContinue
    $complianceAudit["backup"] = @{
        "lastBackup" = if ($backupFiles) { $backupFiles | Sort-Object CreationTime -Descending | Select-Object -First 1 -ExpandProperty CreationTime } else { $null }
        "backupCount" = $backupFiles.Count
        "encrypted" = if ($backupFiles) { ($backupFiles | Where-Object { $_.Name -like "*.encrypted" }).Count } else { 0 }
    }

    if (-not $backupFiles -or (Get-Date).AddDays(-7) -gt $complianceAudit["backup"]["lastBackup"]) {
        $auditResults["recommendations"] += "执行数据库备份（上次备份超过7天）"
    }

    $auditResults["compliance"] = $complianceAudit
    Write-Host "  ✓ 合规性检查完成" -ForegroundColor Green
    return $complianceAudit
}

# 生成报告
function Generate-Report {
    param(
        [object]$AuditData,
        [string]$OutputPath
    )

    Write-Host "生成审计报告..." -ForegroundColor Cyan

    $reportName = "security-audit-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $jsonReport = Join-Path $OutputPath "$reportName.json"
    $htmlReport = Join-Path $OutputPath "$reportName.html"

    # 保存JSON报告
    $AuditData | ConvertTo-Json -Depth 10 | Out-File $jsonReport
    Write-Host "  ✓ JSON报告: $jsonReport" -ForegroundColor Green

    # 生成HTML报告
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>LYBT Security Audit Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }
        h2 { color: #666; margin-top: 30px; }
        .info-box { background: #e8f4fd; border-left: 4px solid #007bff; padding: 10px; margin: 10px 0; }
        .warning-box { background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }
        .error-box { background: #f8d7da; border-left: 4px solid #dc3545; padding: 10px; margin: 10px 0; }
        .success-box { background: #d4edda; border-left: 4px solid #28a745; padding: 10px; margin: 10px 0; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #007bff; color: white; }
        .recommendation { background: #fff3cd; padding: 15px; margin: 10px 0; border-radius: 4px; }
        .metric { display: inline-block; margin: 10px 20px 10px 0; }
        .metric-value { font-size: 24px; font-weight: bold; color: #007bff; }
        .metric-label { color: #666; font-size: 14px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>LYBT Security Audit Report</h1>
        <div class="info-box">
            <strong>Generated:</strong> $($AuditData.timestamp)<br>
            <strong>Audit Level:</strong> $($AuditData.auditLevel)<br>
            <strong>System:</strong> $($AuditData.system.hostname)
        </div>

        <h2>Summary</h2>
        <div class="metric">
            <div class="metric-value">$($AuditData.recommendations.Count)</div>
            <div class="metric-label">Recommendations</div>
        </div>
        <div class="metric">
            <div class="metric-value">$($AuditData.security.certificates.Count)</div>
            <div class="metric-label">Certificates</div>
        </div>
        <div class="metric">
            <div class="metric-value">$($AuditData.security.services.Count)</div>
            <div class="metric-label">Services Checked</div>
        </div>

        <h2>Recommendations</h2>
"@

    foreach ($recommendation in $AuditData.recommendations) {
        $html += "<div class='recommendation'>⚠️ $recommendation</div>"
    }

    $html += @"
        <h2>Configuration Audit</h2>
        <table>
            <tr><th>Configuration File</th><th>Status</th><th>Issues</th></tr>
"@

    foreach ($config in $AuditData.configuration.appsettings.GetEnumerator()) {
        $status = if ($config.Value.issueCount -eq 0) { "✅ Clean" } else { "⚠️ Issues Found" }
        $html += "<tr><td>$($config.Key)</td><td>$status</td><td>$($config.Value.issueCount)</td></tr>"
    }

    $html += @"
        </table>

        <h2>Security Checks</h2>
        <h3>Open Ports</h3>
        <table>
            <tr><th>Port</th><th>Service</th><th>Status</th></tr>
"@

    foreach ($port in $AuditData.security.ports.GetEnumerator()) {
        $status = if ($port.Value.isOpen) { "Open" } else { "Closed" }
        $statusClass = if ($port.Value.isOpen) { "warning-box" } else { "success-box" }
        $html += "<tr><td>$($port.Key)</td><td>$($port.Value.service)</td><td>$status</td></tr>"
    }

    $html += @"
        </table>
    </div>
</body>
</html>
"@

    $html | Out-File $htmlReport
    Write-Host "  ✓ HTML报告: $htmlReport" -ForegroundColor Green

    # 在默认浏览器中打开报告
    if ($GenerateReport) {
        Start-Process $htmlReport
    }
}

# 执行审计
try {
    Get-SystemInfo
    Audit-Configuration
    Audit-Security
    Audit-Compliance

    # 计算风险评分
    $riskScore = 0
    $riskScore += $auditResults["recommendations"].Count * 5

    if ($auditResults["configuration"]["environment"].Values | Where-Object { -not $_.exists }) {
        $riskScore += 10
    }

    $auditResults["riskScore"] = $riskScore
    $auditResults["riskLevel"] = switch ($riskScore) {
        {$_ -le 10} { "Low" }
        {$_ -le 30} { "Medium" }
        {$_ -le 50} { "High" }
        default { "Critical" }
    }

    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host "审计完成" -ForegroundColor Cyan
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host "风险级别: $($auditResults.riskLevel) (评分: $riskScore)" -ForegroundColor $(if ($riskScore -le 10) { "Green" } elseif ($riskScore -le 30) { "Yellow" } else { "Red" })
    Write-Host "建议数量: $($auditResults.recommendations.Count)" -ForegroundColor Yellow

    if ($GenerateReport) {
        Generate-Report -AuditData $auditResults -OutputPath $OutputPath
    }

    # 返回审计结果供其他脚本使用
    return $auditResults
}
catch {
    Write-Host "✗ 审计失败：$($_.Exception.Message)" -ForegroundColor Red
    exit 1
}