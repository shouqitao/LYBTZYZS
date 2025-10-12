# VS Code Snippets for Desktop Development

本目录包含用于 Desktop 端开发的 Visual Studio Code 代码片段，帮助开发者快速生成符合架构标准的代码。

## 📋 Snippet 文件列表

| 文件 | 说明 | Snippet 数量 |
|------|------|------------|
| `csharp-desktop-module.json` | Prism 模块注册相关 Snippet | 6 个 |
| `csharp-repository.json` | Repository 接口与实现 Snippet | 7 个 |

---

## 🏗️ Module Snippets

### 1. `prism-module` - 创建完整的 Prism 模块类

**触发器**: `prism-module`

**说明**: 生成包含 RegisterTypes 方法的完整 Prism 模块类

**生成的代码**:
```csharp
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.ModuleName;

/// <summary>
/// ModuleName 模块
/// </summary>
[Module(ModuleName = nameof(ModuleNameModule))]
// [ModuleDependency("DependencyModule")]
public class ModuleNameModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IEntityRepository, EntityRepository>();

        // 注册 ViewModel
        containerRegistry.Register<EntityManagementViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.EntityManagementView>();

        // 注册对话框（可选）
        // containerRegistry.RegisterDialog<Views.EntityEditorDialog, ViewModels.EntityEditorDialogViewModel>();
    }
}
```

**占位符**:
- `$1`: ModuleName（模块名称，如 Users、Patients）
- `$2`: ModuleDependency（模块依赖，可选）
- `$3`: Entity（实体名称，如 User、Patient）

---

### 2. `prism-register` - RegisterTypes 方法模板

**触发器**: `prism-register`

**说明**: 快速生成 RegisterTypes 方法内容

**生成的代码**:
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ADR-002 架构标准：
    // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
    // - Repository (数据访问层) 由各业务模块自行注册
    containerRegistry.RegisterSingleton<IEntityRepository, EntityRepository>();

    // 注册 ViewModel
    containerRegistry.Register<EntityManagementViewModel>();
    containerRegistry.Register<EntityDetailViewModel>();

    // 注册视图用于导航
    containerRegistry.RegisterForNavigation<Views.EntityManagementView>();
    containerRegistry.RegisterForNavigation<Views.EntityDetailView>();
}
```

---

### 3. `register-repository` - 注册 Repository

**触发器**: `register-repository`

**说明**: 快速注册 Repository 为单例

**生成的代码**:
```csharp
// Repository (数据访问层) 由各业务模块自行注册
containerRegistry.RegisterSingleton<IEntityRepository, EntityRepository>();
```

---

### 4. `register-viewmodel` - 注册 ViewModel

**触发器**: `register-viewmodel`

**说明**: 快速注册 ViewModel（瞬时生命周期）

**生成的代码**:
```csharp
// 注册 ViewModel
containerRegistry.Register<EntityManagementViewModel>();
```

---

### 5. `register-navigation` - 注册视图导航

**触发器**: `register-navigation`

**说明**: 快速注册视图用于导航

**生成的代码**:
```csharp
// 注册视图用于导航
containerRegistry.RegisterForNavigation<Views.EntityManagementView>();
```

---

### 6. `register-dialog` - 注册对话框

**触发器**: `register-dialog`

**说明**: 快速注册对话框（View + ViewModel）

**生成的代码**:
```csharp
// 注册对话框
containerRegistry.RegisterDialog<Views.EntityEditorDialog, ViewModels.EntityEditorDialogViewModel>();
```

---

## 📦 Repository Snippets

### 1. `repo-interface` - 创建 Repository 接口

**触发器**: `repo-interface`

**说明**: 生成完整的 Repository 接口（包含 CRUD 方法）

**生成的代码**:
```csharp
using LYBT.Shared.Dtos.Entity;

namespace LYBT.Desktop.ModuleName.Interfaces;

/// <summary>
/// 实体数据访问接口
/// </summary>
public interface IEntityRepository
{
    /// <summary>
    /// 获取所有实体
    /// </summary>
    Task<List<EntityDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<EntityDto?> GetByIdAsync(int id);

    /// <summary>
    /// 添加实体
    /// </summary>
    Task<EntityDto> AddAsync(CreateEntityDto dto);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task UpdateAsync(int id, UpdateEntityDto dto);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task DeleteAsync(int id);
}
```

**占位符**:
- `$1`: Entity（实体名称，如 User、Patient）
- `$2`: ModuleName（模块名称，如 Users、Patients）
- `$3`: 实体中文名称（如 用户、患者）

---

### 2. `repo-impl` - 创建 Repository 实现类

**触发器**: `repo-impl`

**说明**: 生成完整的 Repository 实现类（包含 CRUD 方法和异常处理）

**生成的代码**:
```csharp
using LYBT.Desktop.ModuleName.Interfaces;
using LYBT.Shared.ApiInterfaces;
using LYBT.Shared.Dtos.Entity;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.ModuleName.Repositories;

/// <summary>
/// 实体数据访问实现
/// </summary>
public class EntityRepository : IEntityRepository
{
    private readonly IEntityApi _api;
    private readonly ILogger<EntityRepository> _logger;

    public EntityRepository(
        IEntityApi api,
        ILogger<EntityRepository> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<List<EntityDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有实体...");
            return await _api.GetAllEntitysAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有实体失败");
            throw; // 异常向上抛出，由 ViewModel 处理
        }
    }

    // ... 其他 CRUD 方法
}
```

**占位符**:
- `$1`: ModuleName（模块名称）
- `$2`: Entity（实体名称）
- `$3`: 实体中文名称

---

### 3. `repo-getall` - GetAll 方法

**触发器**: `repo-getall`

**说明**: 生成 GetAll 方法

---

### 4. `repo-getbyid` - GetById 方法

**触发器**: `repo-getbyid`

**说明**: 生成 GetById 方法

---

### 5. `repo-add` - Add 方法

**触发器**: `repo-add`

**说明**: 生成 Add 方法

---

### 6. `repo-update` - Update 方法

**触发器**: `repo-update`

**说明**: 生成 Update 方法

---

### 7. `repo-delete` - Delete 方法

**触发器**: `repo-delete`

**说明**: 生成 Delete 方法

---

## 🚀 使用方法

### 1. 激活 Snippets

VS Code 会自动加载 `.vscode/snippets/` 目录下的 Snippet 文件。如果未生效，请：

1. 重启 VS Code
2. 或使用快捷键 `Ctrl+Shift+P`，输入 "Reload Window"

### 2. 使用 Snippet

1. 在 C# 文件中输入触发器前缀（如 `prism-module`）
2. 按 `Tab` 键或选择提示中的 Snippet
3. 按 `Tab` 键在占位符间切换
4. 填写必要的信息（模块名、实体名等）

### 3. 示例演示

#### 创建一个新的 Patients 模块

1. 创建文件 `PatientsModule.cs`
2. 输入 `prism-module` 并按 `Tab`
3. 填写占位符：
   - `$1`: Patients
   - `$2`: AuthenticationModule（如果有依赖）
   - `$3`: Patient
4. 完成！

#### 创建 Patient Repository

1. 创建文件 `IPatientRepository.cs`
2. 输入 `repo-interface` 并按 `Tab`
3. 填写占位符：
   - `$1`: Patient
   - `$2`: Patients
   - `$3`: 患者
4. 创建文件 `PatientRepository.cs`
5. 输入 `repo-impl` 并按 `Tab`
6. 填写占位符
7. 完成！

---

## 📚 相关文档

- [Desktop 架构标准文档](../../src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md)
- [ADR-002: Desktop.Services 移除与 Repository 注册位置](../../docs/architecture/decisions/ADR-002-desktop-services-removal.md)
- [Client 端统一设计标准](../../docs/architecture/client/unified-design-standard.md)

---

## 🔧 代码模板文件

除了 VS Code Snippets，我们还提供了完整的代码模板文件，可用于批量生成代码：

| 模板文件 | 位置 | 用途 |
|---------|------|------|
| `ModuleTemplate.cs` | `scripts/templates/` | Prism 模块类模板 |
| `RepositoryInterfaceTemplate.cs` | `scripts/templates/` | Repository 接口模板 |
| `RepositoryImplementationTemplate.cs` | `scripts/templates/` | Repository 实现类模板 |

**使用占位符**:
- `{{ModuleName}}`: 模块名称（如 Users、Patients）
- `{{Entity}}`: 实体名称（如 User、Patient）
- `{{EntityChinese}}`: 实体中文名称（如 用户、患者）

**替换示例**:
```bash
# 使用 PowerShell 批量替换
$content = Get-Content "scripts/templates/ModuleTemplate.cs" -Raw
$content = $content -replace "{{ModuleName}}", "Patients"
$content = $content -replace "{{Entity}}", "Patient"
$content | Set-Content "src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs"
```

---

## 💡 最佳实践

1. **遵循命名规范**
   - 模块名称使用复数（如 Users、Patients）
   - 实体名称使用单数（如 User、Patient）

2. **注册生命周期**
   - Repository: `RegisterSingleton`（单例）
   - ViewModel: `Register`（瞬时）
   - View: `RegisterForNavigation`（瞬时）
   - Dialog: `RegisterDialog`（瞬时）

3. **依赖注入**
   - 使用构造函数注入
   - 禁止使用 `Container.Resolve` 或 `ServiceLocator`

4. **异常处理**
   - Repository 只记录日志，然后向上抛出异常
   - ViewModel 处理异常并显示用户友好的错误消息

5. **返回值约定**
   - Repository 返回裸类型（如 `Task<List<UserDto>>`）
   - 不返回 `ServiceResult<T>`（Server 端专用）

---

**创建日期**: 2025-10-12
**维护者**: Claude Code
**版本**: v1.0
