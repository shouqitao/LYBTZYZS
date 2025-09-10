# CCPM 常见问题 FAQ

## 快速导航
- [基础概念](#基础概念)
- [安装配置](#安装配置)  
- [日常使用](#日常使用)
- [错误处理](#错误处理)
- [性能优化](#性能优化)
- [高级话题](#高级话题)

---

## 基础概念

### Q1: 什么是CPM？为什么要使用CPM？

**A**: Central Package Management (CPM) 是MSBuild原生支持的包管理方式，通过Directory.Packages.props文件集中管理所有NuGet包版本。

**主要优势**:
- 🎯 **统一版本管理**: 避免同一包在不同项目中使用不同版本
- ⚡ **维护效率提升**: 包版本升级从2小时减少到10分钟
- 🔒 **减少冲突风险**: 自动解决传递依赖版本冲突
- 📊 **更好的可见性**: 集中查看和管理所有包依赖

### Q2: CPM与传统PackageReference有什么区别？

**传统方式** (.csproj):
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.8" />
```

**CPM方式**:
```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />

<!-- .csproj -->
<PackageReference Include="Microsoft.Extensions.Hosting" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />
```

### Q3: LYBTZYZS项目的CPM架构是怎样的？

**A**: LYBTZYZS采用分层包管理策略：

```
Directory.Packages.props (中央版本控制)
├── Core Framework (所有项目通用)
├── WPF and Desktop (前端UltraThink双层架构)
├── Web API and Services (后端传统三层架构)  
├── Testing Framework (测试相关)
├── Documentation (文档工具)
└── DevOps and CI/CD (构建部署)
```

---

## 安装配置

### Q4: 如何为现有项目启用CPM？

**A**: 使用自动化脚本迁移：

```bash
# 1. 创建备份分支
git checkout -b backup/before-cpm-migration

# 2. 运行迁移脚本
.\scripts\Migrate-ProjectToCPM.ps1 -ProjectPath "src/Server/LYBT.WebAPI"

# 3. 验证构建
dotnet build

# 4. 运行测试
dotnet test
```

### Q5: Directory.Packages.props应该放在哪里？

**A**: 必须放在解决方案根目录（与.sln文件同级）：

```
LYBTZYZS/                    # ✅ 正确位置
├── Directory.Packages.props
├── LYBT.All.sln
├── src/
└── tests/

src/Directory.Packages.props  # ❌ 错误位置
```

### Q6: 如何配置不同项目使用不同的包？

**A**: 使用条件引用和包分类：

```xml
<!-- 前端WPF项目专用包 -->
<ItemGroup Label="WPF and Desktop" Condition="$(MSBuildProjectName.Contains('Desktop'))">
  <PackageVersion Include="Prism.DryIoc" Version="9.0.537" />
</ItemGroup>

<!-- 后端API项目专用包 -->
<ItemGroup Label="Web API" Condition="$(MSBuildProjectName.Contains('WebAPI'))">
  <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
</ItemGroup>
```

---

## 日常使用

### Q7: 如何添加新的NuGet包？

**A**: 两步操作：

```bash
# 1. 在Directory.Packages.props中添加版本声明
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

# 2. 在项目.csproj中引用（不指定版本）
<PackageReference Include="Newtonsoft.Json" />
```

### Q8: 如何更新包版本？

**A**: 直接修改Directory.Packages.props：

```xml
<!-- 更新前 -->
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="8.0.8" />

<!-- 更新后 -->
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
```

**影响**: 所有引用该包的项目都会自动使用新版本。

### Q9: 如何查看项目使用的包列表？

**A**: 使用dotnet CLI命令：

```bash
# 查看直接依赖
dotnet list package

# 查看包含传递依赖的完整列表
dotnet list package --include-transitive

# 查看特定项目的包
dotnet list src/Server/LYBT.WebAPI package
```

### Q10: 如何检查包的安全漏洞？

**A**: 
```bash
# 检查漏洞
dotnet list package --vulnerable --include-transitive

# 自动化安全扫描
.\scripts\CPM-SecurityScan.ps1
```

---

## 错误处理

### Q11: 遇到NU1008错误怎么办？

**错误信息**: 
```
error NU1008: Projects that use central package version management should not define the version on the PackageReference items
```

**解决方案**:
```bash
# 1. 找到包含Version属性的PackageReference
grep -r 'PackageReference.*Version=' src/ --include="*.csproj"

# 2. 移除Version属性
# 从: <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
# 改为: <PackageReference Include="Microsoft.Extensions.Hosting" />

# 3. 确保Directory.Packages.props中有对应的PackageVersion声明
```

### Q12: 包还原失败怎么办？

**常见原因和解决方案**:

1. **网络问题**:
```bash
# 检查网络连接
ping api.nuget.org

# 使用镜像源
<add key="nuget.org-mirror" value="https://nuget-mirror.company.com/v3/index.json" />
```

2. **缓存损坏**:
```bash
# 清理所有缓存
dotnet nuget locals all --clear

# 强制重新评估
dotnet restore --force-evaluate
```

3. **权限问题**:
```bash
# 检查文件权限
ls -la Directory.Packages.props

# 检查NuGet配置
dotnet nuget list source
```

### Q13: 版本冲突怎么解决？

**A**: 使用版本冲突检测脚本：

```bash
# 1. 检测冲突
.\scripts\CPM-VersionConflictDetector.ps1

# 2. 查看详细依赖关系
dotnet list package --include-transitive | findstr "Microsoft.Extensions"

# 3. 统一到兼容的最高版本
# 在Directory.Packages.props中更新版本号
```

### Q14: Visual Studio中IntelliSense不工作怎么办？

**A**: 按顺序尝试以下解决方案：

1. **重建解决方案**:
   - Build → Clean Solution
   - Build → Rebuild Solution

2. **清理Visual Studio缓存**:
   - 关闭Visual Studio
   - 删除 `%localappdata%\Microsoft\VisualStudio\17.0_*\ComponentModelCache`
   - 重新打开Visual Studio

3. **重置NuGet包**:
```bash
dotnet nuget locals all --clear
dotnet restore
```

---

## 性能优化

### Q15: 构建变慢了怎么办？

**A**: 检查性能配置：

```xml
<!-- Directory.Build.props 性能优化配置 -->
<PropertyGroup>
  <!-- 启用包锁定文件 -->
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  
  <!-- 使用工件目录 -->
  <UseArtifactsOutput>true</UseArtifactsOutput>
  <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
  
  <!-- 传递依赖锁定 -->
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
```

### Q16: 包还原时间太长怎么办？

**A**: 优化缓存策略：

1. **配置本地缓存位置**:
```bash
# 查看当前缓存位置
dotnet nuget locals global-packages --list

# 设置缓存到SSD上
export NUGET_PACKAGES=D:/nuget-cache
```

2. **使用并行还原**:
```bash
dotnet restore --parallel
```

3. **配置企业内部包源镜像**:
```xml
<packageSources>
  <add key="internal-mirror" value="https://internal-nuget.company.com/" />
</packageSources>
```

### Q17: Visual Studio启动缓慢怎么办？

**A**: 优化解决方案配置：

1. **使用解决方案筛选器**:
   - 创建.slnf文件只加载需要的项目
   - File → New → Project From Existing Code

2. **分拆大型解决方案**:
```
LYBT.Frontend.sln    # 仅前端项目
LYBT.Backend.sln     # 仅后端项目  
LYBT.All.sln         # 完整解决方案（CI/CD使用）
```

3. **排除不必要的文件**:
   - 在Visual Studio中排除node_modules等大型目录

---

## 高级话题

### Q18: 如何实现不同环境使用不同的包版本？

**A**: 使用条件属性：

```xml
<!-- Development环境使用最新版本 -->
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" Condition="'$(Configuration)' == 'Debug'" />

<!-- Production环境使用稳定版本 -->
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="8.0.8" Condition="'$(Configuration)' == 'Release'" />
```

### Q19: 如何处理预发布版本的包？

**A**: 明确标记预发布包：

```xml
<ItemGroup Label="Preview Packages">
  <!-- 明确标记为预发布版本 -->
  <PackageVersion Include="Microsoft.AspNetCore.App" Version="9.0.0-preview.1" />
</ItemGroup>
```

**注意**: 生产环境避免使用预发布版本。

### Q20: 如何自定义CPM的MSBuild行为？

**A**: 创建自定义MSBuild目标：

```xml
<!-- Directory.Build.props -->
<Target Name="ValidateCPMPackages" BeforeTargets="Restore">
  <ItemGroup>
    <!-- 检查是否有禁用的包 -->
    <ForbiddenPackages Include="System.Data.SqlClient" />
    <ForbiddenPackages Include="Newtonsoft.Json" Condition="$(MSBuildProjectName.Contains('Core'))" />
  </ItemGroup>
  
  <Error Text="项目 $(MSBuildProjectName) 使用了禁用的包: %(PackageReference.Identity)"
         Condition="@(PackageReference->AnyHaveMetadataValue('Identity', '%(ForbiddenPackages.Identity)'))" />
</Target>
```

### Q21: 如何实现包的自动更新？

**A**: 使用PowerShell脚本：

```powershell
# Update-PackagesAutomatically.ps1
param(
    [string[]]$PackageNames,
    [switch]$PreviewVersions,
    [switch]$DryRun
)

foreach ($packageName in $PackageNames) {
    # 获取最新版本
    $latestVersion = Get-LatestPackageVersion -PackageName $packageName -IncludePreview:$PreviewVersions
    
    # 更新Directory.Packages.props
    if (-not $DryRun) {
        Update-PackageVersionInFile -PackageName $packageName -Version $latestVersion
    }
    
    Write-Host "更新 $packageName 到版本 $latestVersion" -ForegroundColor Green
}
```

### Q22: 如何监控CPM的健康状态？

**A**: 建立监控体系：

1. **定期健康检查**:
```bash
# 每日自动执行
.\scripts\CPM-HealthCheck.ps1 -AutoFix

# 结果发送到团队仪表板
```

2. **关键指标监控**:
   - 包版本一致性：100%
   - 构建成功率：≥99%  
   - 包安全漏洞：0个高危
   - 平均构建时间：监控趋势

3. **告警设置**:
   - 版本冲突：立即通知
   - 安全漏洞：4小时内响应
   - 构建失败：30分钟内处理

### Q23: 如何处理第三方包的许可证合规性？

**A**: 建立许可证管理流程：

```bash
# 1. 扫描包许可证
.\scripts\Scan-PackageLicenses.ps1

# 2. 生成许可证报告
.\scripts\Generate-LicenseReport.ps1 -OutputFormat Excel

# 3. 检查许可证兼容性
.\scripts\Check-LicenseCompliance.ps1 -CorporatePolicy ".\policies\license-policy.json"
```

---

## 获取帮助

### 内部支持
- **技术文档**: [docs/cpm/](../cpm/)
- **故障排除**: [CPM-故障排除指南.md](CPM-故障排除指南.md)
- **最佳实践**: [CPM-最佳实践.md](CPM-最佳实践.md)

### 外部资源
- [Microsoft CPM官方文档](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet包管理最佳实践](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore)

### 团队联系
- **日常问题**: dev-team@lybt.com
- **紧急故障**: dev-support@lybt.com  
- **架构决策**: architects@lybt.com

---
**文档版本**: v1.0  
**最后更新**: 2025-09-05  
**下次审查**: 2025-12-05  
**维护者**: CCPM项目团队