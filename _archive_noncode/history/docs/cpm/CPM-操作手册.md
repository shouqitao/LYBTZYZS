# CCPM (Central Package Management) 操作手册

## 目标读者
本手册适用于需要在LYBTZYZS项目中使用和维护CPM系统的开发人员、技术负责人和DevOps工程师。

## 前置条件
- 熟悉NuGet包管理基础概念
- 了解MSBuild和.csproj文件结构
- 具备Visual Studio 2022和.NET 8 SDK环境

## CPM系统概述

### 什么是CPM
Central Package Management（中央包管理）是MSBuild原生支持的包管理方式，通过Directory.Packages.props文件集中管理所有NuGet包版本，解决版本冲突和维护复杂性问题。

### LYBTZYZS的CPM架构
```
项目根目录/
├── Directory.Packages.props      # 中央包版本管理
├── Directory.Build.props         # 全局构建配置
├── nuget.config                  # 包源配置
└── src/
    ├── Server/                   # 后端项目（传统三层架构）
    ├── Client/Desktop/           # 前端项目（UltraThink双层架构）  
    └── Shared/                   # 共享模型项目
```

## 核心配置文件详解

### 1. Directory.Packages.props
集中管理所有包版本，支持包分类和条件引用：

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <!-- 核心框架包 -->
  <ItemGroup Label="Core Framework">
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
  </ItemGroup>

  <!-- 前端WPF包 -->
  <ItemGroup Label="WPF and Desktop" Condition="$(MSBuildProjectName.Contains('Desktop')) or $(MSBuildProjectName.Contains('WPF'))">
    <PackageVersion Include="Prism.DryIoc" Version="9.0.537" />
    <PackageVersion Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.135" />
  </ItemGroup>
</Project>
```

### 2. Directory.Build.props
全局构建配置，统一项目属性：

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

## 日常操作指南

### 添加新包
1. **确定包分类**：根据包用途选择合适的ItemGroup
2. **添加版本声明**：在Directory.Packages.props中添加PackageVersion
3. **项目中引用**：在.csproj中使用PackageReference（不指定版本）

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

<!-- 项目.csproj文件 -->
<PackageReference Include="Newtonsoft.Json" />
```

### 更新包版本
1. **统一更新**：直接修改Directory.Packages.props中的版本号
2. **影响所有引用该包的项目**
3. **测试验证**：执行完整构建和测试验证

### 解决版本冲突
1. **识别冲突**：使用`scripts/CPM-VersionConflictDetector.ps1`脚本
2. **统一版本**：选择兼容的最高版本统一配置
3. **测试兼容性**：重点测试受影响的功能模块

### 项目迁移到CPM
1. **备份项目**：使用Git创建备份分支
2. **清理.csproj**：移除所有PackageReference的Version属性
3. **添加PackageVersion**：在Directory.Packages.props中声明版本
4. **验证构建**：确保项目正常编译和运行

## 命令行操作

### 包还原
```bash
# 标准包还原
dotnet restore

# 锁定模式包还原（CI环境）
dotnet restore --locked-mode

# 强制重新评估包
dotnet restore --force-evaluate
```

### 构建验证
```bash
# 清理构建
dotnet clean

# 重新构建（验证CPM配置）
dotnet build --no-incremental

# 完整解决方案构建
dotnet build LYBT.All.sln
```

### 包分析
```bash
# 查看包依赖树
dotnet list package --include-transitive

# 检查过期包
dotnet list package --outdated

# 查看包漏洞
dotnet list package --vulnerable
```

## PowerShell自动化脚本

### 批量项目操作
```powershell
# 迁移单个项目到CPM
.\scripts\Migrate-ProjectToCPM.ps1 -ProjectPath "src\Server\LYBT.WebAPI"

# 批量迁移所有项目
.\scripts\Migrate-AllProjectsToCPM.ps1

# 验证CPM配置
.\scripts\Validate-CPMConfiguration.ps1
```

### 版本管理
```powershell
# 检测版本冲突
.\scripts\CPM-VersionConflictDetector.ps1

# 更新包版本
.\scripts\Update-PackageVersion.ps1 -PackageName "Microsoft.Extensions.Hosting" -Version "9.0.0"

# 生成包使用报告
.\scripts\Generate-PackageUsageReport.ps1
```

## IDE支持和配置

### Visual Studio 2022
1. **IntelliSense支持**：自动识别CPM配置，提供包版本提示
2. **NuGet Package Manager**：图形界面支持CPM模式
3. **解决方案资源管理器**：显示集中管理的包版本

### 推荐插件
- **MSBuild Structured Log Viewer**：构建日志分析
- **NuGet Package Manager GUI**：图形化包管理
- **Package Security Alerts**：包安全漏洞提醒

## 性能监控和优化

### 构建性能
- **包还原时间**：首次还原约30-60秒，后续缓存命中<10秒
- **构建时间变化**：预期±5%范围内
- **Visual Studio加载**：解决方案加载时间不超过额外5%

### 缓存策略
```xml
<!-- 启用包缓存优化 -->
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  <RestoreLockedMode Condition="'$(CI)' == 'true'">true</RestoreLockedMode>
</PropertyGroup>
```

### 监控指标
- **Directory.Packages.props文件大小**：<50KB
- **包版本一致性**：100%统一版本
- **传递依赖冲突**：0个未解决冲突

## 安全最佳实践

### 包源安全
```xml
<!-- nuget.config -->
<packageSources>
  <clear />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  <add key="Microsoft Visual Studio Offline Packages" value="C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\" />
</packageSources>
```

### 版本锁定
- **生产环境**：使用packages.lock.json锁定精确版本
- **开发环境**：允许语义版本范围内的自动更新
- **CI/CD**：强制锁定模式，确保构建可重现性

### 漏洞扫描
```bash
# 定期扫描包漏洞
dotnet list package --vulnerable --include-transitive

# 更新有漏洞的包
dotnet add package [包名] --version [安全版本]
```

## 故障排除快速指南

### 常见问题
1. **NU1008错误**：包版本在Directory.Packages.props和.csproj中重复定义
   - 解决：移除.csproj中的Version属性

2. **MSB4057错误**：无法找到Directory.Packages.props
   - 解决：确保文件位于解决方案根目录

3. **包还原失败**：网络或包源问题
   - 解决：检查nuget.config配置，清理本地缓存

### 诊断命令
```bash
# 清理所有缓存
dotnet nuget locals all --clear

# 详细包还原日志
dotnet restore --verbosity detailed

# MSBuild诊断输出
dotnet build --verbosity diagnostic
```

## 维护和升级

### 定期维护任务
1. **每月**：检查包更新，更新非破坏性版本
2. **每季度**：评估主要版本升级的影响
3. **每半年**：清理不再使用的包依赖
4. **每年**：审查整体包架构和策略

### 升级流程
1. **创建升级分支**：`git checkout -b cpm/upgrade-packages`
2. **批量更新版本**：使用自动化脚本更新Directory.Packages.props
3. **渐进式测试**：先测试核心模块，再扩展到全项目
4. **回归测试**：执行完整的自动化测试套件
5. **部署验证**：在预生产环境验证部署流程

### 回滚机制
```bash
# Git回滚CPM配置
git checkout HEAD~1 -- Directory.Packages.props Directory.Build.props

# 完整项目回滚
git reset --hard [备份提交]

# 部分文件回滚
git checkout [备份分支] -- Directory.Packages.props
```

## 参考资料

### 官方文档
- [MSBuild Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [.NET Package Management Best Practices](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore)

### 内部资源
- [CCPM架构设计说明](CPM-架构设计说明.md)
- [CCPM最佳实践指南](CPM-最佳实践.md)
- [CCPM故障排除指南](CPM-故障排除指南.md)

---
**文档版本**: v1.0  
**最后更新**: 2025-09-05  
**维护者**: CCPM项目组  
**反馈邮箱**: dev-team@lybt.com