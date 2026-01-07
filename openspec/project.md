# Project Context

## Purpose
凌隐宝堂中医诊所管理系统（LYBTZYZS）- 为中医诊所提供完整的患者管理、诊疗记录、处方开具和药材管理功能。

**GitHub**: https://github.com/shouqitao/LYBTZYZS

## Tech Stack

### Runtime & SDK
- **.NET SDK**: 8.0.406
- **Target Framework**: net8.0 / net8.0-windows (WPF)

### Backend
- **Framework**: ASP.NET Core 8.0 Web API
- **ORM**: Entity Framework Core 8.0.20
- **Database**: SQL Server
- **Authentication**: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer)
- **Validation**: FluentValidation 12.0
- **Mapping**: Riok.Mapperly 4.1.1 (编译时源生成器，替代AutoMapper)
- **Password Hashing**: BCrypt.Net-Next 4.0.3

### Frontend (Desktop)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **MVVM Framework**: Prism 9.0
- **DI Container**: DryIoc (via Prism)
- **Target OS**: Windows only

### Testing
- **Test Framework**: xUnit
- **Mocking**: NSubstitute
- **Assertions**: FluentAssertions
- **In-Memory DB**: Microsoft.EntityFrameworkCore.InMemory

### Build & Tooling
- **Build System**: MSBuild, dotnet CLI
- **Package Management**: Central Package Management (Directory.Packages.props)
- **SDK Extensions**: Microsoft.Build.Traversal 3.4.0

## Solution Structure

```
LYBTZYZS/
├── src/
│   ├── Client/Desktop/           # WPF 客户端
│   │   ├── Core/                 # 核心库
│   │   │   ├── LYBT.Desktop.Contracts/      # 接口定义
│   │   │   ├── LYBT.Desktop.Foundation/     # 基础设施
│   │   │   ├── LYBT.Desktop.Infrastructure/ # 通用服务、控件
│   │   │   ├── LYBT.Desktop.Models/         # 客户端模型
│   │   │   ├── LYBT.Desktop.Printing/       # 打印服务 (2026-01新增)
│   │   │   └── LYBT.Desktop.Utilities/      # 工具类库
│   │   ├── Modules/              # 业务模块 (Prism Modules)
│   │   │   ├── LYBT.Desktop.Auth/           # 认证模块
│   │   │   ├── LYBT.Desktop.Consultation/   # 诊断模块
│   │   │   ├── LYBT.Desktop.Formula/        # 经验方模块
│   │   │   ├── LYBT.Desktop.Herbs/          # 药材模块
│   │   │   ├── LYBT.Desktop.MedicalCase/    # 医案模块 (核心，含处方功能)
│   │   │   ├── LYBT.Desktop.Patients/       # 患者模块
│   │   │   └── LYBT.Desktop.Users/          # 用户模块
│   │   ├── Roles/                # 角色入口
│   │   │   ├── LYBT.Desktop.Admin/          # 管理员端
│   │   │   └── LYBT.Desktop.Clinical/       # 临床端
│   │   └── Shell/                # 应用外壳
│   │       └── LYBT.Desktop.Shell/
│   ├── Server/                   # 后端服务
│   │   ├── Core/
│   │   │   ├── LYBT.Entities/               # 领域实体
│   │   │   └── LYBT.Infrastructure/         # 基础设施
│   │   ├── Modules/              # 业务模块
│   │   │   ├── LYBT.Module.Auth/
│   │   │   ├── LYBT.Module.Consultation/
│   │   │   ├── LYBT.Module.Formula/
│   │   │   ├── LYBT.Module.Herbs/
│   │   │   ├── LYBT.Module.MedicalCase/
│   │   │   ├── LYBT.Module.Patients/
│   │   │   ├── LYBT.Module.Prescriptions/
│   │   │   └── LYBT.Module.Users/
│   │   └── Services/
│   │       └── LYBT.WebAPI/                 # Web API 入口
│   ├── Shared/                   # 共享库
│   │   ├── LYBT.Shared.Components/
│   │   ├── LYBT.Shared.Models/              # 共享 DTO/Contracts
│   │   └── LYBT.Shared.Utilities/
│   ├── Services/                 # 独立服务
│   └── Tools/                    # 开发工具
├── tests/                        # 测试项目
│   ├── UnitTests/
│   └── IntegrationTests/
├── docs/                         # 文档
└── openspec/                     # OpenSpec 配置
```

**注意**: Desktop.Prescriptions模块已于2026-01-05完全移除，处方功能已迁移至MedicalCase模块。

## Project Conventions

### Code Style
- C# 编码规范遵循 Microsoft 官方指南
- **命名规范**:
  - PascalCase: 类、方法、属性、公共字段
  - camelCase: 局部变量、参数
  - _camelCase: 私有字段
  - I前缀: 接口 (IRepository, IService)
- **所有代码注释使用中文**
- 文件头部包含功能说明和关联 Issue/Epic 引用

### File Naming
- 视图: `*View.xaml` / `*View.xaml.cs`
- 控件: `*Control.xaml` / `*Control.xaml.cs`
- 视图模型: `*ViewModel.cs`
- 服务: `*Service.cs`
- 仓储: `*Repository.cs`
- DTO: `*Dto.cs` (ListDto, DetailDto, InputDto)
- 映射器: `*Mapper.cs` (Mapperly)
- 实体: 无后缀 (如 `MedicalCase.cs`)

### Mapping Conventions (Mapperly)
- **映射器配置**: 使用 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- **字段忽略**: 使用 `[MapperIgnoreTarget]` 忽略目标中源没有的字段
- **集合映射**: 自动继承元素映射器配置，无需重复声明

```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class XxxMapper
{
    // Entity → DTO: 忽略DTO中Entity没有的计算字段
    [MapperIgnoreTarget(nameof(XxxDto.CalculatedField))]
    public partial XxxDto ToDto(Xxx entity);

    // DTO → Entity: 忽略Entity中由Service层管理的字段
    [MapperIgnoreTarget(nameof(Xxx.Id))]
    [MapperIgnoreTarget(nameof(Xxx.Status))]
    [MapperIgnoreTarget(nameof(Xxx.CreatedAt))]
    public partial Xxx ToEntity(XxxInputDto dto);
}
```

### Architecture Patterns

#### Backend: 三层架构
```
Controller (API) → Service (业务逻辑) → Repository (数据访问) → DbContext
```
- **Controller**: 仅处理 HTTP 请求/响应，调用 Service
- **Service**: 业务逻辑，事务管理
- **Repository**: 数据访问，LINQ 查询

#### Frontend: MVVM (Prism)
```
View (XAML) ←绑定→ ViewModel (逻辑) → Service/Repository → API
```
- **View**: 纯 UI，无代码逻辑（除初始化）
- **ViewModel**: 实现 `BindableBase`，使用 `DelegateCommand`
- **Module**: 实现 `IModule`，注册视图和服务

#### DDD: 聚合根设计
- **MedicalCase** 是核心聚合根
- Consultation、Prescription 通过 MedicalCase 访问
- 聚合根边界内保持一致性

### Dependency Injection

#### Desktop (Prism)
```csharp
// Module 注册
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IRepository, Repository>();
    containerRegistry.Register<ViewModel>();
    containerRegistry.RegisterForNavigation<View>();
    containerRegistry.RegisterDialog<Dialog, DialogViewModel>();
}
```

#### Server (ASP.NET Core)
```csharp
// Program.cs / Startup
services.AddScoped<IService, Service>();
services.AddScoped<IRepository, Repository>();
services.AddSingleton<XxxMapper>(); // Mapperly映射器
```

### Testing Strategy
- **单元测试**: xUnit + NSubstitute (Mock)
- **测试命名**: `方法名_场景_期望结果`
- **AAA 模式**: Arrange-Act-Assert
- **测试覆盖**: Repository / Service / ViewModel 层
- **测试数据库**: EF Core InMemory 或 SQLite

### Git Workflow
- **主分支**: master
- **提交格式**: `type(scope): description #issue-number`
- **类型**: feat / fix / docs / refactor / test / chore
- **提交尾部**: 包含 Claude Code 标记

```
feat(MedicalCase): 实现验方导入和历史复制弹窗功能 #2246

- 添加 FormulaImportDialog 和 HistoryCopyDialog
- 更新 PrescriptionPanelViewModel 支持弹窗命令
- 注册新弹窗到 MedicalCaseModule

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
```

## Domain Context

### 核心实体
| 中文名 | 英文名 | 说明 |
|--------|--------|------|
| 医案 | MedicalCase | 核心聚合根，完整诊疗记录 |
| 诊断 | Consultation | 中医诊断（望闻问切、辨证），仅指诊断部分 |
| 处方 | Prescription | 药材配伍和剂量 |
| 处方项 | PrescriptionItem | 单味药材及用量 |
| 经验方 | Formula | 可复用的处方模板 |
| 药材 | Herb | 中药材库 |
| 患者 | Patient | 患者基本信息 |
| 用户 | User | 系统用户（医生、管理员） |

### 术语规范
- **Consultation**: 仅指中医诊断部分（望闻问切、辨证），不是"问诊"或"就诊"
- **MedicalCase**: 医案，完整的诊疗记录
- **Formula**: 经验方/验方，可复用的处方模板

### 业务流程
1. **患者登记** → 创建 Patient
2. **开始诊疗** → 创建 MedicalCase
3. **中医诊断** → 填写 Consultation（望闻问切）
4. **开具处方** → 创建 Prescription + PrescriptionItems
5. **完成医案** → 保存完整 MedicalCase

## Important Constraints

### MVP 原则
- 最小可行产品，避免过度设计
- 不添加未明确要求的功能
- 简单解决方案优先

### 三层对齐
- View / ViewModel / Service / Repository 命名保持一致
- 例: `PatientListView` → `PatientListViewModel` → `PatientService` → `PatientRepository`

### 聚合根边界
- MedicalCase 是唯一聚合根
- 其他实体通过 MedicalCase 访问
- 跨聚合根引用使用 ID

### 中文界面
- 所有用户界面使用简体中文
- 代码注释使用中文
- 变量/类名使用英文

## External Dependencies

### Runtime Requirements
- Windows 10/11 (WPF 仅支持 Windows)
- .NET 8.0 Runtime
- SQL Server 2019+ 或 SQL Server Express

### Development Requirements
- Visual Studio 2022 或 Rider
- .NET SDK 8.0.406+
- SQL Server Management Studio (可选)

## API Conventions

### RESTful Endpoints
```
GET    /api/{resource}           # 列表
GET    /api/{resource}/{id}      # 详情
POST   /api/{resource}           # 创建
PUT    /api/{resource}/{id}      # 更新
DELETE /api/{resource}/{id}      # 删除
```

### Response Format
```json
{
  "success": true,
  "data": { ... },
  "message": "操作成功"
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "验证失败",
    "details": [ ... ]
  }
}
```

## Database Conventions

### Naming
- 表名: PascalCase 复数 (如 `MedicalCases`)
- 列名: PascalCase (如 `PatientId`)
- 外键: `{RelatedEntity}Id` (如 `PatientId`)

### Common Columns
- `Id`: Guid, 主键
- `CreatedAt`: DateTime, 创建时间
- `UpdatedAt`: DateTime, 更新时间
- `CreatedBy`: Guid?, 创建人
- `IsDeleted`: bool, 软删除标记

### EF Core Configuration
- Fluent API 配置优先于 Data Annotations
- 配置类: `{Entity}Configuration : IEntityTypeConfiguration<Entity>`

## OpenSpec Architecture Specifications

### 核心架构规范

| 规范 | 说明 |
|------|------|
| [project-architecture](specs/project-architecture/spec.md) | 三层架构定义、项目命名规范、依赖方向约束 |
| [server-layer-architecture](specs/server-layer-architecture/spec.md) | Server层详细架构、CQRS/三层模式选择 |
| [shared-layer-architecture](specs/shared-layer-architecture/spec.md) | Shared层职责、DTO继承层次规范 |
| [client-layer-architecture](specs/client-layer-architecture/spec.md) | Client层架构、MVVM模式、模块注册 |
| [desktop-architecture](specs/desktop-architecture/spec.md) | Desktop综合架构规范 |

### 模式与约定规范

| 规范 | 说明 |
|------|------|
| [repository-patterns](specs/repository-patterns/spec.md) | Repository模式、BaseRepository方法规范 |
| [service-conventions](specs/service-conventions/spec.md) | Service层约定、返回Result类型规范 |
| [viewmodel-conventions](specs/viewmodel-conventions/spec.md) | ViewModel基类、命令规范 |
| [dialog-patterns](specs/dialog-patterns/spec.md) | Prism对话框模式、IDialogAware实现 |
| [dto-architecture](specs/dto-architecture/spec.md) | DTO设计模式、继承层次 |
| [data-layer-conventions](specs/data-layer-conventions/spec.md) | 数据层约定、EF Core配置 |
| [client-api-conventions](specs/client-api-conventions/spec.md) | 客户端API调用约定、Refit接口 |
| [ui-style-conventions](specs/ui-style-conventions/spec.md) | UI样式约定、主题规范 |
| [module-communication](specs/module-communication/spec.md) | 模块间通信、ICrossModuleQueryService |
| [error-handling](specs/error-handling/spec.md) | 错误处理规范、异常类型定义 |
| [logging-infrastructure](specs/logging-infrastructure/spec.md) | 日志基础设施、日志级别规范 |
| [converter-conventions](specs/converter-conventions/spec.md) | 值转换器约定 |
| [desktop-code-patterns](specs/desktop-code-patterns/spec.md) | Desktop代码模式规范 |
| [shared-utilities](specs/shared-utilities/spec.md) | 共享工具类规范 |
| [testing-infrastructure](specs/testing-infrastructure/spec.md) | 测试基础设施规范 |
| [code-quality](specs/code-quality/spec.md) | 代码质量规范 |

### 功能规范

| 规范 | 说明 |
|------|------|
| [authentication](specs/authentication/spec.md) | JWT认证、登录流程 |
| [api-authorization](specs/api-authorization/spec.md) | API授权、权限控制 |
| [global-audit](specs/global-audit/spec.md) | 全局审计日志规范 |
| [user-management](specs/user-management/spec.md) | 用户管理功能规范 |
| [medicalcase-edit-modes](specs/medicalcase-edit-modes/spec.md) | 医案编辑模式(新建/编辑/只读) |
| [medicalcase-lifecycle](specs/medicalcase-lifecycle/spec.md) | 医案生命周期状态流转 |
| [medicalcase-ui-layout](specs/medicalcase-ui-layout/spec.md) | 医案工作区UI布局 |
| [login-credential-handling](specs/login-credential-handling/spec.md) | 登录凭据处理 |
| [login-ui](specs/login-ui/spec.md) | 登录界面UI规范 |
| [login-state-machine](specs/login-state-machine/spec.md) | 登录状态机规范 |
| [herb-card-control](specs/herb-card-control/spec.md) | 药材卡片控件规范 |
| [formula-copy-flow](specs/formula-copy-flow/spec.md) | 经验方复制流程规范 |
| [auth-events](specs/auth-events/spec.md) | 认证事件规范 |
| [credential-vault](specs/credential-vault/spec.md) | 凭据保险库规范 |
| [token-management](specs/token-management/spec.md) | Token管理规范 |
| [printing-infrastructure](specs/printing-infrastructure/spec.md) | 打印基础设施规范 (2026-01新增) |

### UI规范

| 规范 | 说明 |
|------|------|
| [shell-layout](specs/shell-layout/spec.md) | Shell布局规范 |
| [desktop-detail-views](specs/desktop-detail-views/spec.md) | 桌面端详情视图规范 |

### 文档规范

| 规范 | 说明 |
|------|------|
| [readme-documentation](specs/readme-documentation/spec.md) | README文档统一标准(100-300行、禁止大段代码) |

### 清理/迁移规范

| 规范 | 说明 |
|------|------|
| [webapi-cleanup](specs/webapi-cleanup/spec.md) | WebAPI清理和重构 |
| [repository-cleanup](specs/repository-cleanup/spec.md) | Repository层清理规范 |
| [service-cleanup](specs/service-cleanup/spec.md) | Service层清理规范 |
| [dto-cleanup](specs/dto-cleanup/spec.md) | DTO清理规范 |
| [desktop-structure-cleanup](specs/desktop-structure-cleanup/spec.md) | Desktop结构清理规范 |
| [desktop-core-cleanup](specs/desktop-core-cleanup/spec.md) | Desktop Core清理规范 |
| [service-permission-separation](specs/service-permission-separation/spec.md) | Service权限分离规范 |

---

## Related Documents

### Development Standards
- **开发规范**: `docs/guides/development-standards.md`
  - 用户上下文传递规范 (GetOperator() 模式)
  - 枚举使用规范 (禁止字符串比较)
  - 测试规范 (待补充)

### Architecture Decision Records (ADR)
- **ADR-001**: 用户上下文传递模式 (`docs/architecture/decisions/ADR-001-user-context-propagation-pattern.md`)
  - Controller 层显式提取 userId
  - Service 层参数签名包含 userId
  - 禁止 Service 层注入 IHttpContextAccessor

---

**最后更新**: 2026-01-07
**文档版本**: v2.1
