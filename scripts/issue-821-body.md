## 📋 概述

清理 Desktop 项目中的冗余文件夹，优化资源文件位置，提升项目结构清晰度。

## 🎯 目标

1. 删除未使用的 `Configuration/` 文件夹
2. 将 `Assets/` 移动到 `Shell/` 项目内（更合适、更直观的位置）

## 📊 现状分析

### 1. Configuration 文件夹 - ❌ 未使用

**位置**: `src/Client/Desktop/Configuration/`

**内容**:
- `ModuleConfiguration.cs` (244 行)
  - `ModuleConfiguration` - 模块配置类
  - `ServiceLifetimeType` - 服务生命周期枚举
  - `SessionIntegrationSettings` - 会话集成设置
  - `ModuleConfigurationManager` - 模块配置管理器

**使用情况**:
- ❌ 搜索整个 `Desktop` 目录，完全未被引用
- ❌ 没有任何项目引用该文件
- ❌ 没有任何代码导入该命名空间

**结论**: 这是未使用的遗留代码，可以安全删除。

---

### 2. Assets 文件夹 - ✅ 使用中但位置不当

**当前位置**: `src/Client/Desktop/Assets/`

**内容**:
- `Assets/Icons/App/app.ico` - 应用程序图标 (1.3MB)
- `Assets/RESOURCE_MANAGEMENT.md` - 资源管理规范文档

**当前引用**:
1. `Shell/LYBT.Desktop.Shell.csproj` - 第 34、116 行
2. `Core/LYBT.Desktop.Infrastructure/Constants/ResourcePaths.cs` - 第 12 行
   ```csharp
   private const string AssetsBase = "pack://application:,,,/LYBT.Desktop.Shell;component/Assets/";
   ```
3. `Resources/Dictionaries/IconResources.xaml` - 第 10-31 行

**问题**:
- 📂 `Assets` 在 `Desktop` 根目录，但实际上是 `Shell` 项目的专属资源
- 🔗 `ResourcePaths.cs` 已经使用 `LYBT.Desktop.Shell;component/Assets/` 作为路径前缀
- 📁 应该与 `Shell` 项目放在一起，更符合 WPF 资源管理最佳实践

**建议**: 移动到 `Shell/Assets/`，使项目结构更清晰。

---

## ✅ 模块化任务清单

### 阶段 1: 删除未使用的 Configuration 文件夹

- [ ] **[CLEAN-1]** 删除 `src/Client/Desktop/Configuration/` 文件夹及其所有内容
  - 验收: `git status` 显示删除 `Configuration/ModuleConfiguration.cs`
  - 风险: 无（完全未使用）

### 阶段 2: 移动 Assets 到 Shell 项目

- [ ] **[MOVE-1]** 使用 `git mv` 移动 `Assets/` 文件夹
  - 源路径: `src/Client/Desktop/Assets/`
  - 目标路径: `src/Client/Desktop/Shell/Assets/`
  - 保留 Git 历史记录
  - 验收: 文件夹在新位置且 Git 历史完整

- [ ] **[UPDATE-1]** 更新 `Shell/LYBT.Desktop.Shell.csproj` 中的路径引用
  - 修改第 34 行: `<ApplicationIcon>` 路径从 `..\Assets\` → `Assets\`
  - 修改第 116 行: `<Resource Include="..\Assets\` → `Assets\`
  - 验收: `.csproj` 文件中所有路径正确指向 `Shell/Assets/`

- [ ] **[UPDATE-2]** 更新 `Resources/Dictionaries/IconResources.xaml` 路径
  - 修改第 10-31 行: Pack URI 路径从 `pack://application:,,,/Assets/` → `pack://application:,,,/LYBT.Desktop.Shell;component/Assets/`
  - 验收: 所有 BitmapImage 的 UriSource 使用完整程序集限定路径

- [ ] **[DOC-1]** 更新 `Assets/RESOURCE_MANAGEMENT.md` 文档
  - 更新目录结构示例（第 7 行起）
  - 更新 Pack URI 示例（第 87、91、200 行）
  - 验收: 文档中所有路径示例反映新位置

### 阶段 3: 编译验证

- [ ] **[BUILD-1]** 编译 `LYBT.Desktop.sln`
  - 命令: `dotnet build LYBT.Desktop.sln -c Release`
  - 验收: 0 errors，警告数与基线一致

- [ ] **[BUILD-2]** 编译 `LYBTZYZS.sln`
  - 命令: `dotnet build LYBTZYZS.sln -c Release`
  - 验收: 0 errors，警告数与基线一致

- [ ] **[TEST-1]** 运行 Desktop 应用程序
  - 验证应用图标正常显示
  - 验证资源字典加载正常
  - 验收: 应用启动无错误，图标资源正常

### 阶段 4: Git 提交

- [ ] **[COMMIT-1]** 创建 Git 提交
  - 提交信息: "refactor(desktop): 清理未使用文件夹并优化资源位置 - Issue #821"
  - 包含内容:
    - 删除 `Configuration/`
    - 移动 `Assets/` 到 `Shell/`
    - 更新所有路径引用
    - 更新文档
  - 验收: `git log` 显示提交，`git status` 显示干净工作树

---

## 🎨 预期结果

### 文件夹结构变化

**变更前**:
```
src/Client/Desktop/
├── Assets/                    # ❌ 位置不当
│   ├── Icons/App/app.ico
│   └── RESOURCE_MANAGEMENT.md
├── Configuration/             # ❌ 未使用
│   └── ModuleConfiguration.cs
├── Core/
├── Modules/
└── Shell/
```

**变更后**:
```
src/Client/Desktop/
├── Core/
├── Modules/
└── Shell/
    └── Assets/                # ✅ 合理位置
        ├── Icons/App/app.ico
        └── RESOURCE_MANAGEMENT.md
```

### 路径引用变化

| 文件 | 变更前 | 变更后 |
|------|--------|--------|
| `Shell.csproj` | `..\Assets\Icons\App\app.ico` | `Assets\Icons\App\app.ico` |
| `IconResources.xaml` | `pack://application:,,,/Assets/...` | `pack://application:,,,/LYBT.Desktop.Shell;component/Assets/...` |
| `ResourcePaths.cs` | 无需修改（已使用正确前缀） | 无需修改 |

---

## ⚠️ 风险评估

| 风险项 | 严重程度 | 影响范围 | 缓解措施 |
|--------|----------|----------|----------|
| 删除 Configuration | 低 | 无 | 完全未使用，无影响 |
| Assets 移动导致资源加载失败 | 中 | Desktop 应用图标和资源字典 | 仔细更新所有路径引用，编译验证后再提交 |
| Pack URI 路径错误 | 中 | 运行时资源加载 | 遵循 WPF Pack URI 规范，使用完整程序集限定路径 |
| Git 历史丢失 | 低 | 文件历史追溯 | 使用 `git mv` 而非删除+创建 |

---

## 📚 参考资料

- WPF Pack URI 规范: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf
- Issue #820: Desktop 架构优化 - 统一文件夹命名规范
- `docs/development/file-organization-guidelines.md` - 文件组织规范

---

## 🏷️ 标签

- `refactor` - 代码重构
- `cleanup` - 清理冗余
- `desktop` - Desktop 客户端
- `architecture` - 架构优化

---

**创建时间**: 2025-09-30
**预计工作量**: 0.5 小时
**优先级**: 中
**依赖**: Issue #820 (已完成)