# 依赖引用清理分析报告

## 📊 总体情况
- 分析时间: 2025-09-07
- 项目类型: .NET 8 多项目解决方案 (后端 + WPF 前端)
- 分析范围: PackageReference、ProjectReference、using 语句

## 📦 NuGet 包引用分析

### 后端项目依赖状态

#### 核心依赖 (必需保留)
```xml
<!-- 核心框架包 -->
<PackageReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.17" />

<!-- 认证和安全 -->  
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />

<!-- API 文档 -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.1" />

<!-- 对象映射 -->
<PackageReference Include="AutoMapper" Version="15.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" />
```

#### ✅ 确认使用的特殊包
- **Refit**: REST API 客户端生成 - ✅ 前端大量使用
- **FluentValidation**: 数据验证 - ✅ 后端验证逻辑使用
- **Serilog**: 日志记录 - ✅ 企业级日志系统

### 前端项目依赖状态

#### WPF 核心依赖 (必需)
```xml
<!-- WPF 框架 -->
<UseWPF>true</UseWPF>
<TargetFramework>net8.0-windows</TargetFramework>

<!-- MVVM 框架 -->
<PackageReference Include="Prism.DryIoc" Version="9.0.537" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />

<!-- HTTP 客户端 -->
<PackageReference Include="Refit" Version="7.2.1" />
<PackageReference Include="Refit.HttpClientFactory" />
```

#### 🟡 可疑的依赖包 (需要验证)

**潜在未使用包**:
```xml
<!-- 以下包需要代码扫描确认使用情况 -->
<PackageReference Include="Microsoft.Extensions.Http.Polly" />
<!-- 用途: HTTP 重试策略，需要确认是否在 Refit 配置中使用 -->

<PackageReference Include="System.Text.Json" />  
<!-- 用途: JSON 序列化，可能被 Refit 间接使用，需要确认 -->

<PackageReference Include="Microsoft.Extensions.Configuration.Json" />
<!-- 用途: 配置管理，需要确认 appsettings.json 的加载方式 -->
```

## 🔗 项目间引用分析

### 当前项目引用架构
```
LYBT.WebAPI
├── LYBT.Module.Auth
├── LYBT.Module.Users  
├── LYBT.Module.Patients
├── LYBT.Module.MedicalCase
├── LYBT.Module.Consultation
├── LYBT.Module.Prescriptions
├── LYBT.Module.Herbs
├── LYBT.Module.Formula
└── LYBT.Infrastructure

LYBT.Desktop.Shell
├── LYBT.Desktop.Auth
├── LYBT.Desktop.Users
├── LYBT.Desktop.Patients
├── LYBT.Desktop.MedicalCase
├── LYBT.Desktop.Consultation  
├── LYBT.Desktop.Prescriptions
├── LYBT.Desktop.Herbs
├── LYBT.Desktop.Formula
├── LYBT.Desktop.Core
├── LYBT.Desktop.Infrastructure
└── LYBT.Shared.Models
```

### ✅ 项目引用验证结果
- **模块化架构**: 符合预期，每个业务模块独立
- **共享依赖**: Shared.Models 被正确引用
- **基础设施**: Infrastructure 层被正确使用
- **循环引用检查**: ✅ 无循环引用发现

### 🔍 引用优化机会
**暂无发现明显的无用项目引用**，当前架构合理

## 📝 Using 语句分析

### 检测方法和工具
建议使用以下方式检测未使用的 using:

#### Visual Studio 内置功能
```
右键项目 → "移除和排序 Using" 
或 Ctrl+R, Ctrl+G
```

#### EditorConfig 配置 (推荐)
```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.IDE0005.severity = warning
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
```

#### 命令行工具
```bash
# 使用 dotnet format 清理
dotnet format --include-generated --severity warn
```

### 🟡 常见未使用 Using 模式

#### 1. 系统命名空间过度引用
```csharp
// 可能未使用的系统命名空间
using System.Collections.Generic; // 未使用泛型集合
using System.Linq; // 未使用 LINQ 查询
using System.Threading.Tasks; // 同步方法中未使用
using System.ComponentModel; // 未实现 INotifyPropertyChanged
```

#### 2. 框架命名空间冗余
```csharp  
// WPF 项目中可能冗余
using System.Windows.Controls; // 未直接使用控件
using System.Windows.Data; // 未使用数据绑定
using Microsoft.Extensions.Logging; // 未使用日志记录
```

#### 3. 第三方库未使用引用
```csharp
// 可能的冗余引用
using AutoMapper; // 只在构造函数注入，可能可以移除
using Refit; // 只用于接口定义，实现中可能可以移除
using Prism.Regions; // 未使用区域导航的类中
```

## 🧹 清理执行计划

### 阶段1: 安全清理 (立即执行)
```bash
# 1. 自动清理 using 语句
dotnet format --include-generated --severity warn

# 2. 移除明显未使用的 using
# 通过 Visual Studio 的"移除和排序 Using"功能
```

### 阶段2: 包依赖验证 (需要测试)
```bash
# 1. 分析包使用情况
dotnet list package --outdated
dotnet list package --deprecated

# 2. 实验性移除可疑包 (一个个测试)
# 移除包 → 编译 → 测试 → 提交或回滚
```

### 阶段3: 深度优化 (谨慎执行)
1. **依赖版本统一**: 确保相同包在不同项目中版本一致
2. **间接依赖清理**: 移除不再需要的传递依赖
3. **包引用合并**: 将重复的包引用提升到 Directory.Packages.props

## 📊 预估清理效果

### Using 语句清理
- **清理文件数**: 预估 50-80 个文件
- **平均每文件减少**: 2-5 个未使用 using
- **编译性能**: 提升 3-5%
- **代码可读性**: 显著改善

### 包依赖优化
- **包数量减少**: 可能减少 2-4 个未使用包
- **包大小减少**: 5-10 MB (发布包)
- **启动性能**: 提升 1-2%
- **安全性**: 减少潜在的安全漏洞面

### 项目引用优化
- **当前状态**: 已经较为优化
- **潜在改进**: 主要在版本管理和配置统一

## ⚠️ 风险评估和缓解

### 低风险操作 (放心执行)
- ✅ Using 语句清理 - Visual Studio 工具足够可靠
- ✅ 明显未使用的包移除 - 编译器会立即报错

### 中风险操作 (需要测试)
- 🟡 可疑包移除 - 可能在运行时才发现依赖
- 🟡 版本升级 - 可能引入兼容性问题

### 高风险操作 (深度评估)
- 🔴 项目引用重构 - 影响模块化架构
- 🔴 核心框架包变更 - 可能影响整体稳定性

## 🔧 推荐的清理工具

### 内置工具
1. **Visual Studio**: "移除和排序 Using"
2. **dotnet CLI**: `dotnet format` 命令  
3. **ReSharper**: 代码清理功能 (如果可用)

### 第三方工具  
1. **NuGet Package Manager**: 分析包使用情况
2. **Dependency Graph**: Visual Studio 依赖关系图
3. **SonarQube/SonarLint**: 静态代码分析

### 自动化清理脚本
```powershell
# PowerShell 脚本: 批量清理 using
Get-ChildItem -Recurse -Filter "*.cs" | ForEach-Object {
    Write-Host "处理文件: $($_.FullName)"
    # 这里可以集成其他清理逻辑
}
```

## 📋 执行检查清单

### 执行前检查
- [ ] 确保代码已提交到 Git
- [ ] 创建清理专用分支
- [ ] 确保所有测试通过
- [ ] 备份重要配置文件

### 执行后验证
- [ ] 所有项目编译通过
- [ ] 单元测试全部通过
- [ ] 集成测试验证通过
- [ ] 应用启动和核心功能正常

### 回滚计划
```bash
# 如果出现问题，快速回滚
git checkout main
git branch -D chore/cleanup-references
```