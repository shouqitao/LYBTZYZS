# 凌隐宝堂中医诊所系统 - 冒烟测试脚本
# UltraThink Phase 3 实用化优化 - PowerShell自动化测试

param(
    [string]$BaseUrl = "https://localhost:7001",
    [string]$OutputPath = "temp/smoke-test-results.json",
    [bool]$StartWebAPI = $true,
    [int]$TimeoutSeconds = 30
)

# 测试结果收集器
$TestResults = @{
    StartTime = Get-Date
    Tests = @()
    Summary = @{
        Total = 0
        Passed = 0
        Failed = 0
        Warnings = 0
    }
}

# 日志函数
function Write-TestLog {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "SUCCESS" { "Green" }
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "INFO" { "Cyan" }
        default { "White" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

# 添加测试结果
function Add-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = "",
        [object]$Details = $null,
        [double]$Duration = 0
    )
    
    $result = @{
        TestName = $TestName
        Passed = $Passed
        Message = $Message
        Details = $Details
        Duration = $Duration
        Timestamp = Get-Date
    }
    
    $TestResults.Tests += $result
    $TestResults.Summary.Total++
    
    if ($Passed) {
        $TestResults.Summary.Passed++
        Write-TestLog "✅ $TestName - $Message" "SUCCESS"
    } else {
        $TestResults.Summary.Failed++
        Write-TestLog "❌ $TestName - $Message" "ERROR"
    }
}

# HTTP请求函数（忽略SSL证书错误）
function Invoke-TestRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int]$TimeoutSec = 30
    )
    
    try {
        # 忽略SSL证书错误（开发环境）
        if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
            $certCallback = @"
                using System;
                using System.Net;
                using System.Net.Security;
                using System.Security.Cryptography.X509Certificates;
                public class ServerCertificateValidationCallback {
                    public static void Ignore() {
                        if(ServicePointManager.ServerCertificateValidationCallback == null) {
                            ServicePointManager.ServerCertificateValidationCallback += 
                                delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                                    return true;
                                };
                        }
                    }
                }
"@
            Add-Type $certCallback
        }
        [ServerCertificateValidationCallback]::Ignore()
        
        $parameters = @{
            Uri = $Uri
            Method = $Method
            Headers = $Headers
            TimeoutSec = $TimeoutSec
            UseBasicParsing = $true
        }
        
        if ($Body -and ($Method -eq "POST" -or $Method -eq "PUT" -or $Method -eq "PATCH")) {
            $parameters.Body = $Body
            $parameters.ContentType = "application/json"
        }
        
        return Invoke-RestMethod @parameters
    }
    catch {
        throw $_
    }
}

# 启动WebAPI服务
function Start-WebAPIService {
    if (-not $StartWebAPI) {
        Write-TestLog "跳过启动WebAPI服务（StartWebAPI=$StartWebAPI）" "INFO"
        return
    }
    
    Write-TestLog "正在启动WebAPI服务..." "INFO"
    
    # 检查端口是否被占用
    try {
        $connection = Test-NetConnection -ComputerName "localhost" -Port 7001 -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) {
            Write-TestLog "端口7001已被占用，假设服务已运行" "WARNING"
            return
        }
    }
    catch {
        # 忽略错误，继续启动服务
    }
    
    # 启动dotnet进程
    try {
        $processInfo = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Backend/Services/LYBT.WebAPI --urls `"https://localhost:7001`"" -WindowStyle Hidden -PassThru
        
        # 等待服务启动
        $maxWaitTime = 30
        $waitTime = 0
        
        do {
            Start-Sleep -Seconds 2
            $waitTime += 2
            
            try {
                $healthCheck = Invoke-TestRequest -Uri "$BaseUrl/health" -TimeoutSec 5
                if ($healthCheck) {
                    Write-TestLog "WebAPI服务启动成功！" "SUCCESS"
                    return $processInfo
                }
            }
            catch {
                # 继续等待
            }
        }
        while ($waitTime -lt $maxWaitTime)
        
        Write-TestLog "WebAPI服务启动超时" "ERROR"
        return $null
    }
    catch {
        Write-TestLog "启动WebAPI服务失败: $($_.Exception.Message)" "ERROR"
        return $null
    }
}

# 测试健康检查
function Test-HealthCheck {
    $testName = "Health Check"
    $startTime = Get-Date
    
    try {
        $response = Invoke-TestRequest -Uri "$BaseUrl/health" -TimeoutSec 10
        $duration = (Get-Date) - $startTime
        
        if ($response -and $response.status -eq "Healthy") {
            Add-TestResult -TestName $testName -Passed $true -Message "系统健康检查通过" -Details $response -Duration $duration.TotalSeconds
        } else {
            Add-TestResult -TestName $testName -Passed $false -Message "健康检查响应异常" -Details $response -Duration $duration.TotalSeconds
        }
    }
    catch {
        $duration = (Get-Date) - $startTime
        Add-TestResult -TestName $testName -Passed $false -Message "健康检查失败: $($_.Exception.Message)" -Duration $duration.TotalSeconds
    }
}

# 测试数据库连接
function Test-DatabaseConnection {
    $testName = "Database Connection"
    $startTime = Get-Date
    
    try {
        $response = Invoke-TestRequest -Uri "$BaseUrl/health/database" -TimeoutSec 15
        $duration = (Get-Date) - $startTime
        
        if ($response -and ($response.status -eq "Healthy" -or $response.Status -eq "Healthy")) {
            Add-TestResult -TestName $testName -Passed $true -Message "数据库连接正常" -Details $response -Duration $duration.TotalSeconds
        } else {
            Add-TestResult -TestName $testName -Passed $false -Message "数据库连接异常" -Details $response -Duration $duration.TotalSeconds
        }
    }
    catch {
        $duration = (Get-Date) - $startTime
        Add-TestResult -TestName $testName -Passed $false -Message "数据库连接测试失败: $($_.Exception.Message)" -Duration $duration.TotalSeconds
    }
}

# 测试登录功能
function Test-LoginFunctionality {
    $testName = "Login Functionality"
    $startTime = Get-Date
    
    try {
        $loginData = @{
            username = "admin"
            password = "admin"
            loginType = "Password"
            rememberMe = $false
        } | ConvertTo-Json
        
        $response = Invoke-TestRequest -Uri "$BaseUrl/api/v1/auth/login" -Method "POST" -Body $loginData -TimeoutSec 10
        $duration = (Get-Date) - $startTime
        
        if ($response -and $response.success -and $response.data -and $response.data.token) {
            Add-TestResult -TestName $testName -Passed $true -Message "登录功能正常" -Details @{TokenLength = $response.data.token.Length} -Duration $duration.TotalSeconds
            return $response.data.token
        } else {
            Add-TestResult -TestName $testName -Passed $false -Message "登录响应异常" -Details $response -Duration $duration.TotalSeconds
            return $null
        }
    }
    catch {
        $duration = (Get-Date) - $startTime
        Add-TestResult -TestName $testName -Passed $false -Message "登录测试失败: $($_.Exception.Message)" -Duration $duration.TotalSeconds
        return $null
    }
}

# 测试需要认证的API
function Test-AuthenticatedAPI {
    param([string]$Token)
    
    if (-not $Token) {
        Add-TestResult -TestName "Authenticated API" -Passed $false -Message "无有效Token，跳过认证测试"
        return
    }
    
    $testName = "Authenticated API"
    $startTime = Get-Date
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
        }
        
        $response = Invoke-TestRequest -Uri "$BaseUrl/api/v1/users" -Headers $headers -TimeoutSec 10
        $duration = (Get-Date) - $startTime
        
        if ($response) {
            Add-TestResult -TestName $testName -Passed $true -Message "认证API访问正常" -Details @{ResponseType = $response.GetType().Name} -Duration $duration.TotalSeconds
        } else {
            Add-TestResult -TestName $testName -Passed $false -Message "认证API无响应" -Duration $duration.TotalSeconds
        }
    }
    catch {
        $duration = (Get-Date) - $startTime
        if ($_.Exception.Message -like "*401*" -or $_.Exception.Message -like "*Unauthorized*") {
            Add-TestResult -TestName $testName -Passed $false -Message "认证失败，可能是Token无效" -Duration $duration.TotalSeconds
        } else {
            Add-TestResult -TestName $testName -Passed $false -Message "认证API测试失败: $($_.Exception.Message)" -Duration $duration.TotalSeconds
        }
    }
}

# 测试Swagger文档
function Test-SwaggerDocumentation {
    $testName = "Swagger Documentation"
    $startTime = Get-Date
    
    try {
        $response = Invoke-TestRequest -Uri "$BaseUrl/swagger/v1/swagger.json" -TimeoutSec 10
        $duration = (Get-Date) - $startTime
        
        if ($response -and $response.info) {
            Add-TestResult -TestName $testName -Passed $true -Message "Swagger文档可访问" -Details @{Title = $response.info.title; Version = $response.info.version} -Duration $duration.TotalSeconds
        } else {
            Add-TestResult -TestName $testName -Passed $false -Message "Swagger文档响应异常" -Details $response -Duration $duration.TotalSeconds
        }
    }
    catch {
        $duration = (Get-Date) - $startTime
        Add-TestResult -TestName $testName -Passed $false -Message "Swagger文档访问失败: $($_.Exception.Message)" -Duration $duration.TotalSeconds
    }
}

# 测试基础API端点
function Test-BasicEndpoints {
    param([string]$Token)
    
    $endpoints = @(
        @{Name = "Herbs API"; Url = "$BaseUrl/api/v1/herbs"; RequireAuth = $true},
        @{Name = "Patients API"; Url = "$BaseUrl/api/v1/patients"; RequireAuth = $true},
        @{Name = "Version API"; Url = "$BaseUrl/api/version"; RequireAuth = $false}
    )
    
    foreach ($endpoint in $endpoints) {
        $testName = $endpoint.Name
        $startTime = Get-Date
        
        try {
            $headers = @{}
            if ($endpoint.RequireAuth -and $Token) {
                $headers["Authorization"] = "Bearer $Token"
            }
            
            $response = Invoke-TestRequest -Uri $endpoint.Url -Headers $headers -TimeoutSec 10
            $duration = (Get-Date) - $startTime
            
            if ($response) {
                Add-TestResult -TestName $testName -Passed $true -Message "API端点正常" -Duration $duration.TotalSeconds
            } else {
                Add-TestResult -TestName $testName -Passed $false -Message "API端点无响应" -Duration $duration.TotalSeconds
            }
        }
        catch {
            $duration = (Get-Date) - $startTime
            $statusCode = "Unknown"
            if ($_.Exception.Response) {
                $statusCode = $_.Exception.Response.StatusCode
            }
            
            if ($statusCode -eq 401 -and $endpoint.RequireAuth) {
                Add-TestResult -TestName $testName -Passed $true -Message "API端点正确返回401认证要求" -Duration $duration.TotalSeconds
            } else {
                Add-TestResult -TestName $testName -Passed $false -Message "API端点测试失败 (${statusCode}): $($_.Exception.Message)" -Duration $duration.TotalSeconds
            }
        }
    }
}

# 生成测试报告
function Generate-TestReport {
    $TestResults.EndTime = Get-Date
    $TestResults.TotalDuration = ($TestResults.EndTime - $TestResults.StartTime).TotalSeconds
    
    # 确保输出目录存在
    $outputDir = Split-Path -Path $OutputPath -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    
    # 生成JSON报告
    $TestResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Encoding UTF8
    
    # 控制台输出摘要
    Write-Host "`n" + "="*60 -ForegroundColor Cyan
    Write-Host "🧪 凌隐宝堂中医诊所系统 - 冒烟测试报告" -ForegroundColor Cyan
    Write-Host "="*60 -ForegroundColor Cyan
    
    Write-Host "📊 测试摘要:" -ForegroundColor Yellow
    Write-Host "   总计测试: $($TestResults.Summary.Total)" -ForegroundColor White
    Write-Host "   通过测试: $($TestResults.Summary.Passed)" -ForegroundColor Green
    Write-Host "   失败测试: $($TestResults.Summary.Failed)" -ForegroundColor Red
    Write-Host "   测试时长: $([math]::Round($TestResults.TotalDuration, 2)) 秒" -ForegroundColor White
    
    $passRate = if ($TestResults.Summary.Total -gt 0) { [math]::Round($TestResults.Summary.Passed / $TestResults.Summary.Total * 100, 2) } else { 0 }
    Write-Host "   通过率: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } elseif ($passRate -ge 60) { "Yellow" } else { "Red" })
    
    Write-Host "`n📋 详细结果:" -ForegroundColor Yellow
    foreach ($test in $TestResults.Tests) {
        $icon = if ($test.Passed) { "✅" } else { "❌" }
        $duration = [math]::Round($test.Duration, 2)
        Write-Host "   $icon $($test.TestName) ($duration 秒) - $($test.Message)" -ForegroundColor $(if ($test.Passed) { "Green" } else { "Red" })
    }
    
    Write-Host "`n📄 详细报告已保存至: $OutputPath" -ForegroundColor Cyan
    Write-Host "="*60 -ForegroundColor Cyan
    
    return $passRate
}

# 主执行流程
function Main {
    Write-TestLog "🚀 开始凌隐宝堂中医诊所系统冒烟测试" "INFO"
    Write-TestLog "目标URL: $BaseUrl" "INFO"
    
    # 启动服务
    $webApiProcess = Start-WebAPIService
    
    try {
        # 执行测试
        Test-HealthCheck
        Test-DatabaseConnection
        Test-SwaggerDocumentation
        
        # 登录测试并获取Token
        $token = Test-LoginFunctionality
        
        # 需要认证的测试
        Test-AuthenticatedAPI -Token $token
        Test-BasicEndpoints -Token $token
        
        # 生成报告
        $passRate = Generate-TestReport
        
        # 返回退出码
        if ($passRate -ge 80) {
            Write-TestLog "🎉 冒烟测试完成 - 系统状态良好" "SUCCESS"
            exit 0
        } elseif ($passRate -ge 60) {
            Write-TestLog "⚠️ 冒烟测试完成 - 系统有轻微问题" "WARNING"
            exit 1
        } else {
            Write-TestLog "🚨 冒烟测试完成 - 系统有严重问题" "ERROR"
            exit 2
        }
    }
    finally {
        # 清理：如果我们启动了WebAPI进程，需要停止它
        if ($webApiProcess -and $StartWebAPI) {
            try {
                Write-TestLog "正在停止WebAPI服务..." "INFO"
                Stop-Process -Id $webApiProcess.Id -Force -ErrorAction SilentlyContinue
            }
            catch {
                Write-TestLog "停止WebAPI服务时出错: $($_.Exception.Message)" "WARNING"
            }
        }
    }
}

# 运行主流程
try {
    Main
}
catch {
    Write-TestLog "冒烟测试过程中发生未处理的错误: $($_.Exception.Message)" "ERROR"
    Write-TestLog "错误详情: $($_.ScriptStackTrace)" "ERROR"
    exit 3
}