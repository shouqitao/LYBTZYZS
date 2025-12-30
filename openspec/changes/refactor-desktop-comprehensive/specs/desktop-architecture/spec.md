# Desktop Architecture Specification Delta

**Change ID**: refactor-desktop-comprehensive
**Capability**: desktop-architecture
**Version**: 1.0

---

## MODIFIED Requirements

### Requirement: Desktop模块目录结构规范

All Desktop business modules SHALL follow a unified directory structure specification.

**规范定义**:
```
LYBT.Desktop.{ModuleName}/
├── {ModuleName}Module.cs           # 模块注册入口
├── README.md                       # 模块文档
├── Interfaces/                     # 接口定义 (非Contracts/)
│   ├── I{Name}Service.cs
│   └── I{Name}Repository.cs
├── Services/                       # 服务实现
├── Repositories/                   # 数据访问
├── ViewModels/                     # 视图模型
├── Views/                          # 视图
├── Controls/                       # 模块专用控件 (可选)
├── Models/                         # 模块内部模型 (可选)
├── Handlers/                       # 特殊处理器 (可选)
└── Events/                         # 模块事件 (可选)
```

#### Scenario: 新建业务模块目录结构验证

**Given** 开发者创建新的Desktop业务模块  
**When** 模块包含Service和Repository  
**Then** 接口文件必须放在 `Interfaces/` 目录下  
**And** 服务实现必须放在 `Services/` 目录下  
**And** 仓储实现必须放在 `Repositories/` 目录下  

#### Scenario: 已有模块目录结构迁移

**Given** 已存在使用 `Contracts/` 目录的模块  
**When** 执行架构重构  
**Then** 接口文件迁移到 `Interfaces/` 目录  
**And** 更新所有命名空间引用  
**And** 删除空的 `Contracts/` 目录  

---

### Requirement: Service层统一模式

All Desktop business modules MUST use a unified Service+Repository layering pattern.

**接口命名规范**:
- 服务接口: `I{Name}Service`
- 仓储接口: `I{Name}Repository`

**返回类型规范**:
- CRUD操作返回: `Task<(bool success, TDto? data, string? error)>`
- 分页查询返回: `Task<PagedResult<TListDto>>`

#### Scenario: Service方法返回类型验证

**Given** Service接口定义CRUD方法  
**When** 方法返回结果  
**Then** 必须使用元组 `(bool success, T? data, string? error)` 格式  
**And** 不抛出业务异常，通过返回值传递错误信息  

#### Scenario: Repository依赖注入

**Given** Service实现类  
**When** 需要数据访问  
**Then** 必须通过构造函数注入 `I{Name}Repository`  
**And** 禁止在Service中直接使用HttpClient  

---

### Requirement: ViewModel组合模式

ViewModel layer SHALL adopt composition pattern instead of inheritance, achieving functionality reuse through injected standard services.

**标准服务接口**:
- `ILoadingStateManager` - 加载状态管理
- `IPaginationService<T>` - 分页服务
- `ISearchService` - 搜索服务
- `ISelectionService<T>` - 选择服务
- `IDetailEditorService<T>` - 详情编辑服务
- `IDialogManager` - 对话框管理
- `IViewNavigationService` - 视图导航服务
- `IErrorHandler` - 错误处理

#### Scenario: MasterDetail ViewModel服务注入

**Given** 创建MasterDetail类型ViewModel  
**When** 需要列表分页、搜索、选择功能  
**Then** 通过构造函数注入 `IMasterDetailServices<TList, TDetail>`  
**And** ViewModel代码行数不超过400行  

#### Scenario: ViewModel职责分离

**Given** ViewModel包含业务逻辑  
**When** 业务逻辑超过简单的UI状态管理  
**Then** 业务逻辑下沉到Service层  
**And** ViewModel只负责UI状态和命令绑定  

---

## ADDED Requirements

### Requirement: 模块间依赖规则

The system MUST enforce inter-module dependency rules to ensure clear architecture.

**依赖规则**:
```
Shell
  └── 可依赖: 所有Business Modules, Core

Business Modules
  └── 可依赖: Core, Shared.Models
  └── 禁止: 模块间直接依赖

Core
  └── 可依赖: Shared.Models
  └── 禁止: 依赖Business Modules, Shell

特例:
  - MedicalCase可依赖Patients (聚合根引用)
  - Formula可依赖Herbs (配方包含药材)
```

#### Scenario: 模块间通信验证

**Given** 模块A需要与模块B交互  
**When** 不属于允许的依赖特例  
**Then** 必须通过EventAggregator发布/订阅事件  
**And** 禁止直接引用另一个模块的类型  

#### Scenario: 聚合根跨模块引用

**Given** MedicalCase模块需要引用Patient信息  
**When** 显示病历中的患者信息  
**Then** 可以依赖Patients模块的DTO类型  
**And** 不直接依赖Patients模块的ViewModel或Service  

---

### Requirement: 废弃代码清理标准

The codebase SHALL NOT contain deprecated code, and cleanup standards MUST be followed.

**废弃代码定义**:
1. 被注释掉的代码块
2. 标记为 `[Obsolete]` 但未删除的代码
3. 空实现或占位符方法
4. 未被任何代码引用的public成员
5. 残留的测试代码或调试代码

#### Scenario: 注释代码清理

**Given** 代码文件中存在被注释的代码块  
**When** 注释超过10行  
**Then** 必须删除注释代码  
**And** 如需保留历史，通过git history查看  

#### Scenario: 废弃模块清理

**Given** 模块已被标记为废弃或已迁移  
**When** 目录中只剩obj/bin缓存  
**Then** 删除整个模块目录  
**And** 从解决方案文件中移除引用  

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-30 | 1.0 | 初始规范定义 |
