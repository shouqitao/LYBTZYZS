# CCPM 故障排除指南

## 目标读者
本指南适用于需要诊断和解决CPM相关问题的开发人员、运维工程师和技术支持人员。

## 快速诊断流程

### 1. 问题分类决策树
```
CPM问题报告
├── 构建失败
│   ├── NU1008: 包版本重复定义 → 清理.csproj中的Version属性
│   ├── MSB4057: Directory.Packages.props未找到 → 检查文件位置
│   └── 其他MSBuild错误 → 详细日志分析
├── 包还原失败  
│   ├── 网络连接问题 → 检查包源配置
│   ├── 缓存损坏 → 清理本地缓存
│   └── 版本冲突 → 运行冲突检测脚本
├── 性能问题
│   ├── 构建缓慢 → 检查缓存配置
│   ├── Visual Studio卡顿 → 优化解决方案配置
│   └── 包还原超时 → 网络和包源优化
└── 功能异常
    ├── UltraThink架构问题 → 依赖注入检查
    ├── API服务异常 → 后端包兼容性验证
    └── 部署失败 → CI/CD配置检查
```

### 2. 快速自检命令
```bash
# 第一步：基本环境检查
dotnet --version                # 确认.NET 8 SDK
dotnet --list-sdks             # 查看所有SDK版本

# 第二步：CPM配置验证
ls Directory.Packages.props    # 确认配置文件存在
.\scripts\CPM-VersionConflictDetector.ps1  # 检测版本冲突

# 第三步：构建诊断
dotnet restore --verbosity detailed        # 详细包还原日志
dotnet build --verbosity diagnostic       # 诊断级构建日志
```

## 常见错误及解决方案

### 错误类型 1: NU1008 - 包版本重复定义

**错误表现**:
```
error NU1008: Projects that use central package version management should not define the version on the PackageReference items but on the PackageVersion items: 'Microsoft.Extensions.Hosting'.
```

**根本原因**: .csproj文件中PackageReference仍包含Version属性，与CPM冲突

**解决步骤**:
```bash
# 1. 定位问题文件
grep -r 'PackageReference.*Version=' src/ --include="*.csproj"

# 2. 批量清理Version属性
.\scripts\Remove-PackageVersionsFromProjects.ps1

# 3. 验证修复
dotnet restore
dotnet build
```

**预防措施**:
- 使用自动化脚本迁移项目到CPM
- 设置Git pre-commit hook检查
- 在CI/CD中添加验证步骤

### 错误类型 2: MSB4057 - Directory.Packages.props未找到

**错误表现**:
```
error MSB4057: The target "Restore" does not exist in the project.
```

**根本原因**: MSBuild无法找到Directory.Packages.props文件

**解决步骤**:
```bash
# 1. 确认文件存在和位置
find . -name "Directory.Packages.props"

# 2. 检查文件权限
ls -la Directory.Packages.props

# 3. 验证XML格式
xmllint --noout Directory.Packages.props  # Linux/Mac
# 或在PowerShell中: [xml]$xml = Get-Content Directory.Packages.props

# 4. 重新创建文件（如果损坏）
.\scripts\Initialize-CPMConfiguration.ps1
```

**预防措施**:
- 将Directory.Packages.props加入版本控制
- 设置文件备份机制
- 在构建脚本中添加文件存在性检查

### 错误类型 3: 包还原超时或失败

**错误表现**:
```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.
warn : Failed to retrieve information about 'Microsoft.Extensions.Hosting' from remote source.
```

**诊断命令**:
```bash
# 1. 网络连通性测试
ping api.nuget.org
curl -I https://api.nuget.org/v3/index.json

# 2. 检查包源配置
dotnet nuget list source
cat nuget.config

# 3. 清理缓存重试
dotnet nuget locals all --clear
dotnet restore --force-evaluate
```

**解决方案**:
```bash
# 方案1: 配置包源故障切换
# 添加多个包源到nuget.config
<packageSources>
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  <add key="nuget.org-backup" value="https://www.nuget.org/api/v2" />
</packageSources>

# 方案2: 使用企业内部镜像源
<packageSources>
  <add key="internal-mirror" value="https://internal-nuget-mirror.company.com/v3/index.json" />
</packageSources>

# 方案3: 离线包还原
.\scripts\Create-OfflinePackageCache.ps1
dotnet restore --packages ./offline-packages
```

### 错误类型 4: 版本冲突和依赖解析失败

**错误表现**:
```
error NU1107: Version conflict detected for Microsoft.Extensions.DependencyInjection.
  Project references:
    Microsoft.Extensions.Hosting 9.0.0 -> Microsoft.Extensions.DependencyInjection (>= 9.0.0)
    But package Microsoft.Extensions.DependencyInjection 8.0.8 was selected.
```

**诊断和解决**:
```bash
# 1. 生成依赖关系图
dotnet list package --include-transitive > package-dependencies.txt

# 2. 运行冲突检测脚本
.\scripts\CPM-VersionConflictDetector.ps1 -DetailedAnalysis

# 3. 查看具体包的依赖链
dotnet list package --include-transitive | findstr "Microsoft.Extensions.DependencyInjection"

# 4. 解决冲突（统一到最高兼容版本）
# 修改Directory.Packages.props:
<PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
```

**版本冲突解决策略**:
1. **向上兼容**: 选择最高的兼容版本
2. **生态系统一致性**: Microsoft.Extensions.*包使用相同版本
3. **LTS优先**: 长期支持版本优于最新版本
4. **破坏性变更评估**: 重大版本更新需要完整测试

### 错误类型 5: UltraThink架构集成问题

**错误表现**:
```
System.InvalidOperationException: Unable to resolve service for type 'IUserService' while attempting to activate 'UserViewModel'.
```

**特定于LYBTZYZS的诊断**:
```bash
# 1. 检查Prism.DryIoc版本一致性
grep -r "Prism.DryIoc" Directory.Packages.props src/

# 2. 验证服务注册
# 检查App.xaml.cs或Bootstrapper.cs中的服务注册代码

# 3. 验证依赖注入配置
dotnet build src/Client/Desktop/ --verbosity diagnostic | grep -i "dependency"
```

**解决方案**:
```xml
<!-- 确保UltraThink架构相关包版本一致 -->
<ItemGroup Label="UltraThink Frontend Architecture">
  <PackageVersion Include="Prism.DryIoc" Version="9.0.537" />
  <PackageVersion Include="Prism.Wpf" Version="9.0.537" />
  <PackageVersion Include="DryIoc.dll" Version="5.4.3" />
  <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
</ItemGroup>
```

## 性能问题诊断

### 构建性能问题

**问题表现**: 构建时间显著增加（>原时间的115%）

**诊断工具**:
```bash
# 1. MSBuild性能分析
dotnet build --verbosity diagnostic 2>&1 | Select-String "Time Elapsed"

# 2. 包还原性能分析
Measure-Command { dotnet restore }

# 3. 缓存命中率检查
.\scripts\Get-BuildCacheStatistics.ps1
```

**性能优化清单**:
- [ ] 启用包锁定文件：`<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`
- [ ] 配置输出目录：`<UseArtifactsOutput>true</UseArtifactsOutput>`  
- [ ] 启用并行还原：`dotnet restore --parallel`
- [ ] 检查包缓存位置：确保在SSD上
- [ ] 清理不必要的包引用

### Visual Studio响应缓慢

**诊断步骤**:
```bash
# 1. 检查解决方案大小和项目数量
Get-ChildItem -Recurse -Filter "*.csproj" | Measure-Object

# 2. 分析Directory.Packages.props大小
Get-Item Directory.Packages.props | Select-Object Length

# 3. 检查IntelliSense缓存
# Visual Studio -> Tools -> Options -> Text Editor -> C# -> IntelliSense
```

**优化建议**:
- 拆分大型解决方案为多个较小的解决方案
- 使用解决方案筛选器(.slnf)仅加载需要的项目
- 优化Directory.Packages.props结构，使用条件引用减少包数量
- 配置Visual Studio排除不必要的文件夹

## 高级故障排除

### MSBuild调试技术

**生成详细构建日志**:
```bash
# 1. 创建二进制日志文件
dotnet build -bl:build.binlog

# 2. 使用MSBuild Structured Log Viewer分析
# 下载: https://msbuildlog.com/
MSBuildStructuredLogViewer.exe build.binlog
```

**自定义MSBuild目标调试**:
```xml
<!-- 添加到Directory.Build.props -->
<Target Name="DebugCPMConfiguration" BeforeTargets="Restore">
  <Message Text="=== CPM Debug Information ===" Importance="high" />
  <Message Text="ManagePackageVersionsCentrally: $(ManagePackageVersionsCentrally)" Importance="high" />
  <Message Text="Directory.Packages.props path: $(MSBuildProjectDirectory)/../Directory.Packages.props" Importance="high" />
  
  <ItemGroup>
    <PackageReference Condition="'%(PackageReference.Version)' != ''" Remove="@(PackageReference)" />
  </ItemGroup>
</Target>
```

### 包依赖深度分析

**复杂依赖冲突诊断**:
```powershell
# CPM-AdvancedDependencyAnalysis.ps1
function Analyze-PackageDependencyTree {
    param(
        [string]$PackageName,
        [string]$ProjectPath = "."
    )
    
    # 获取包的完整依赖树
    $dependencies = dotnet list $ProjectPath package --include-transitive --format json | ConvertFrom-Json
    
    # 分析特定包的依赖路径
    $packagePaths = @()
    foreach ($framework in $dependencies.projects[0].frameworks) {
        foreach ($topLevel in $framework.topLevelPackages) {
            if ($topLevel.id -eq $PackageName -or $topLevel.transitivePackages -contains $PackageName) {
                $packagePaths += @{
                    Framework = $framework.framework
                    Path = Get-DependencyPath -From $topLevel -To $PackageName
                }
            }
        }
    }
    
    return $packagePaths
}
```

### 紧急故障响应流程

**重大故障快速回滚**:
```bash
# 1. 立即回滚到最后已知良好状态
git stash                                    # 保存当前更改
git checkout $(git rev-list -n 1 HEAD~1)   # 回退一个提交

# 2. 禁用CPM进行紧急修复
export DisableCPM=true
dotnet restore
dotnet build

# 3. 验证系统功能正常
.\scripts\Run-SmokeTests.ps1

# 4. 创建热修复分支
git checkout -b hotfix/cpm-emergency-fix
```

**故障报告模板**:
```markdown
## CPM故障报告

### 基本信息
- **发生时间**: 2025-09-05 10:30:00
- **影响范围**: [构建失败|功能异常|性能问题]
- **紧急程度**: [低|中|高|紧急]

### 故障表现
- **错误信息**: 
- **影响的项目**: 
- **复现步骤**: 

### 环境信息
- **.NET SDK版本**: 
- **Visual Studio版本**: 
- **操作系统**: 
- **Git提交**: 

### 诊断结果
- **根本原因**: 
- **触发因子**: 
- **影响评估**: 

### 解决方案
- **临时措施**: 
- **永久修复**: 
- **验证步骤**: 

### 预防措施
- **流程改进**: 
- **工具增强**: 
- **监控添加**: 
```

## 自动化诊断脚本

### 综合健康检查脚本
```powershell
# CPM-HealthCheck.ps1
param(
    [switch]$Detailed,
    [switch]$AutoFix,
    [string]$LogPath = "./cpm-health-check.log"
)

function Test-CPMHealth {
    $results = @{
        ConfigurationFiles = Test-CPMConfigurationFiles
        PackageVersionConsistency = Test-PackageVersionConsistency  
        BuildHealth = Test-SolutionBuild
        DependencyConflicts = Test-DependencyConflicts
        PerformanceMetrics = Test-BuildPerformance
        SecurityVulnerabilities = Test-PackageSecurity
    }
    
    # 生成健康报告
    $healthScore = ($results.Values | Where-Object { $_ -eq $true }).Count / $results.Count * 100
    
    Write-Host "🏥 CPM系统健康评分: $healthScore%" -ForegroundColor $(if($healthScore -ge 90) { "Green" } elseif($healthScore -ge 70) { "Yellow" } else { "Red" })
    
    if ($Detailed) {
        $results | Format-Table -AutoSize
    }
    
    # 自动修复选项
    if ($AutoFix -and $healthScore -lt 100) {
        Write-Host "🔧 启动自动修复..." -ForegroundColor Yellow
        Start-AutoRepair -Issues $results
    }
    
    # 记录到日志文件
    $results | Out-File -FilePath $LogPath -Append
    
    return $results
}

# 运行健康检查
Test-CPMHealth -Detailed
```

## 知识库更新机制

### 新问题记录流程
1. **问题收集**: 每个解决的问题都要记录到知识库
2. **分类标记**: 按问题类型、紧急程度、解决复杂度分类
3. **解决方案验证**: 确保解决方案在多个环境中验证有效
4. **文档更新**: 将解决方案添加到相应的故障排除文档
5. **自动化集成**: 常见问题开发自动检测和修复脚本

### 故障排除知识库结构
```
docs/cpm/troubleshooting/
├── common-errors/           # 常见错误快速解决
├── performance-issues/      # 性能问题诊断
├── integration-problems/    # 架构集成问题
├── emergency-procedures/    # 紧急故障处理
├── diagnostic-scripts/      # 自动化诊断工具
└── case-studies/           # 复杂故障案例分析
```

---
**文档版本**: v1.0  
**最后更新**: 2025-09-05  
**维护者**: CCPM技术支持团队  
**紧急联系**: dev-support@lybt.com  
**更新频率**: 每月或重大问题解决后立即更新