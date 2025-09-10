# CCPM 故障排除指南

## 概述

本指南提供 CCPM (Code-Claude Project Manager) 系统运行过程中常见问题的诊断和解决方案。基于LYBTZYZS项目实际实施经验编写，涵盖编译错误、依赖冲突、运行时问题等核心故障场景。

## 问题分类与优先级

### 🔴 P0 - 阻塞性问题
- 系统无法启动
- 编译完全失败
- 数据库连接失败
- 关键业务功能完全不可用

### 🟡 P1 - 高优先级问题  
- 部分功能异常
- 性能严重下降
- 依赖版本冲突
- 非关键服务故障

### 🟢 P2 - 一般性问题
- 界面显示异常
- 配置警告
- 非核心功能缺陷
- 文档错误

## 快速诊断流程

### 步骤 1: 问题初步定位

```bash
# 检查系统基础状态
git status
dotnet --version
node --version  # 如果使用前端工具

# 检查编译状态
dotnet build LYBT.Server.sln
dotnet build LYBT.Desktop.sln
```

### 步骤 2: 日志收集

```bash
# 收集应用日志
type logs\app-*.log | findstr "ERROR\|FATAL"

# 收集系统事件日志
Get-EventLog -LogName Application -EntryType Error -Newest 10
```

### 步骤 3: 环境验证

```bash
# 验证数据库连接
scripts\database-manager.bat test-connection

# 验证API可用性
curl -X GET "https://localhost:7001/health" -k
```

## 常见问题诊断树

### 编译错误问题

```
编译失败
├── CS0246 类型或命名空间不存在
│   ├── 检查 using 语句
│   ├── 验证项目引用
│   └── 重建解决方案
├── CS1061 不包含定义
│   ├── 检查方法签名
│   ├── 验证接口实现
│   └── 更新 NuGet 包
└── CS0234 命名空间中不存在
    ├── 检查包引用
    ├── 验证版本兼容性
    └── 清理 bin/obj 目录
```

### 运行时错误问题

```
应用启动失败
├── DI 容器错误
│   ├── 检查服务注册
│   ├── 验证依赖关系
│   └── 查看构造函数
├── 数据库连接错误
│   ├── 验证连接字符串
│   ├── 检查数据库状态
│   └── 运行迁移脚本
└── 配置错误
    ├── 检查 appsettings.json
    ├── 验证环境变量
    └── 比较配置模板
```

## 自动化诊断脚本

### 系统健康检查脚本

```powershell
# scripts\health-check.ps1
param(
    [switch]$Detailed,
    [switch]$FixIssues
)

Write-Host "=== CCPM 系统健康检查 ===" -ForegroundColor Green

# 检查必需工具
$tools = @(
    @{Name=".NET SDK"; Command="dotnet --version"; Required=$true},
    @{Name="Git"; Command="git --version"; Required=$true},
    @{Name="SQL Server"; Command="sqlcmd -S localhost -E -Q 'SELECT @@VERSION'"; Required=$false}
)

foreach ($tool in $tools) {
    try {
        $result = Invoke-Expression $tool.Command 2>$null
        Write-Host "✅ $($tool.Name): $($result.Split([Environment]::NewLine)[0])" -ForegroundColor Green
    } catch {
        if ($tool.Required) {
            Write-Host "❌ $($tool.Name): 未安装或配置错误" -ForegroundColor Red
        } else {
            Write-Host "⚠️  $($tool.Name): 可选工具未安装" -ForegroundColor Yellow
        }
    }
}

# 检查项目结构
$requiredPaths = @(
    "src\Server\Services\LYBT.WebAPI",
    "src\Client\Desktop",
    "docs\cpm"
)

foreach ($path in $requiredPaths) {
    if (Test-Path $path) {
        Write-Host "✅ 项目路径: $path" -ForegroundColor Green
    } else {
        Write-Host "❌ 缺少路径: $path" -ForegroundColor Red
    }
}

# 检查编译状态
Write-Host "`n=== 编译检查 ===" -ForegroundColor Cyan
try {
    $serverBuild = dotnet build "LYBT.Server.sln" --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 后端编译成功" -ForegroundColor Green
    } else {
        Write-Host "❌ 后端编译失败" -ForegroundColor Red
        if ($Detailed) {
            Write-Host $serverBuild -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ 后端编译检查失败: $_" -ForegroundColor Red
}

try {
    $clientBuild = dotnet build "LYBT.Desktop.sln" --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 前端编译成功" -ForegroundColor Green
    } else {
        Write-Host "❌ 前端编译失败" -ForegroundColor Red
        if ($Detailed) {
            Write-Host $clientBuild -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ 前端编译检查失败: $_" -ForegroundColor Red
}

# 自动修复选项
if ($FixIssues) {
    Write-Host "`n=== 自动修复 ===" -ForegroundColor Cyan
    Write-Host "执行清理操作..."
    
    # 清理编译产物
    Get-ChildItem -Path . -Recurse -Directory -Name "bin" | ForEach-Object {
        Remove-Item $_ -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✅ 清理: $($_)\bin" -ForegroundColor Green
    }
    
    Get-ChildItem -Path . -Recurse -Directory -Name "obj" | ForEach-Object {
        Remove-Item $_ -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✅ 清理: $($_)\obj" -ForegroundColor Green
    }
    
    # 还原包
    Write-Host "还原 NuGet 包..."
    dotnet restore 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ NuGet 包还原成功" -ForegroundColor Green
    } else {
        Write-Host "❌ NuGet 包还原失败" -ForegroundColor Red
    }
}

Write-Host "`n=== 检查完成 ===" -ForegroundColor Green
Write-Host "使用 -Detailed 参数查看详细信息"
Write-Host "使用 -FixIssues 参数执行自动修复"
```

### 依赖冲突检查脚本

```powershell
# scripts\dependency-check.ps1
Write-Host "=== 依赖冲突检查 ===" -ForegroundColor Green

# 检查 NuGet 包冲突
Write-Host "检查 NuGet 包版本冲突..."
$packageFiles = Get-ChildItem -Recurse -Name "*.csproj"

$allPackages = @{}
foreach ($file in $packageFiles) {
    [xml]$proj = Get-Content $file
    $packages = $proj.Project.ItemGroup.PackageReference
    if ($packages) {
        foreach ($pkg in $packages) {
            if ($pkg.Include) {
                $name = $pkg.Include
                $version = $pkg.Version
                if (-not $allPackages.ContainsKey($name)) {
                    $allPackages[$name] = @()
                }
                $allPackages[$name] += @{File=$file; Version=$version}
            }
        }
    }
}

# 查找版本冲突
$conflicts = @()
foreach ($pkg in $allPackages.Keys) {
    $versions = $allPackages[$pkg] | Select-Object -ExpandProperty Version -Unique
    if ($versions.Count -gt 1) {
        $conflicts += @{Package=$pkg; Versions=$versions; Files=$allPackages[$pkg]}
    }
}

if ($conflicts.Count -eq 0) {
    Write-Host "✅ 未发现包版本冲突" -ForegroundColor Green
} else {
    Write-Host "⚠️  发现 $($conflicts.Count) 个包版本冲突:" -ForegroundColor Yellow
    foreach ($conflict in $conflicts) {
        Write-Host "   📦 $($conflict.Package)" -ForegroundColor Cyan
        foreach ($file in $conflict.Files) {
            Write-Host "      $($file.File): $($file.Version)" -ForegroundColor Gray
        }
    }
}

# 检查缺失引用
Write-Host "`n检查缺失项目引用..."
# 这里可以添加更多检查逻辑
```

## 问题升级流程

### 内部升级路径

1. **开发人员自助** (0-30分钟)
   - 查阅本指南和FAQ
   - 运行自动化诊断脚本
   - 尝试标准解决方案

2. **团队协作** (30分钟-2小时)
   - 在团队群组求助
   - 共享问题现象和日志
   - 集体讨论解决方案

3. **技术负责人介入** (2小时-1天)
   - 复杂架构问题
   - 跨模块集成问题
   - 需要架构调整的问题

4. **外部支持** (1天以上)
   - 第三方库或工具问题
   - 基础设施问题
   - 需要供应商支持的问题

### 问题记录模板

```markdown
## 问题报告 - [问题ID]

**报告时间**: YYYY-MM-DD HH:mm  
**报告人**: [姓名]  
**优先级**: P0/P1/P2  
**影响范围**: [受影响的功能/用户]

### 问题描述
[详细描述问题现象]

### 复现步骤
1. [步骤1]
2. [步骤2]
3. [步骤3]

### 预期结果
[应该发生什么]

### 实际结果
[实际发生了什么]

### 环境信息
- 操作系统: [Windows/Linux版本]
- .NET版本: [版本号]
- 浏览器: [如果相关]
- 数据库: [版本信息]

### 错误日志
```
[粘贴相关错误日志]
```

### 已尝试的解决方案
- [ ] [解决方案1] - [结果]
- [ ] [解决方案2] - [结果]

### 临时解决方案
[如果有临时绕过方案]

### 根本原因分析
[问题解决后填写]

### 最终解决方案
[问题解决后填写]
```

## 性能问题诊断

### 性能监控检查点

```powershell
# 检查应用性能指标
function Check-Performance {
    Write-Host "=== 性能监控 ===" -ForegroundColor Green
    
    # 检查进程资源使用
    $processes = Get-Process | Where-Object {$_.ProcessName -like "*LYBT*" -or $_.ProcessName -eq "dotnet"}
    foreach ($proc in $processes) {
        $cpu = [math]::Round($proc.CPU, 2)
        $memory = [math]::Round($proc.WorkingSet64 / 1MB, 2)
        Write-Host "进程 $($proc.ProcessName) - CPU: ${cpu}s, 内存: ${memory}MB" -ForegroundColor Cyan
    }
    
    # 检查数据库连接池
    # 这里需要添加具体的数据库监控逻辑
    
    # 检查API响应时间
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck
        $sw.Stop()
        Write-Host "API健康检查响应时间: $($sw.ElapsedMilliseconds)ms" -ForegroundColor Green
    } catch {
        Write-Host "API健康检查失败: $_" -ForegroundColor Red
    }
}
```

## 相关文档

- [CPM-常见问题FAQ.md](CPM-常见问题FAQ.md) - 常见问题快速解答
- [CPM-错误代码参考.md](CPM-错误代码参考.md) - 错误代码含义和解决方案
- [CPM-应急响应预案.md](CPM-应急响应预案.md) - 紧急情况处理流程
- [../05-维护运营/CPM-维护流程.md](../05-维护运营/CPM-维护流程.md) - 日常维护操作

## 更新日志

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2025-01-31 | 初始版本，基于LYBTZYZS项目实践经验 | Claude |

---

**注意事项**:
1. 本指南基于LYBTZYZS项目的实际实施经验编写
2. 问题解决后请更新相应的文档和脚本
3. 新发现的问题请及时补充到FAQ和错误代码参考中
4. 定期回顾和更新诊断脚本的有效性