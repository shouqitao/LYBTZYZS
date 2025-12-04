# Proposal: document-project-architecture

## Status
- **Phase**: Proposed
- **Created**: 2025-12-04
- **Author**: Claude Code

## Why

当前项目虽然有基本的架构设计(Server+Shared+Client三层)，但缺乏系统性的架构文档来约束代码实现。存在以下问题:

1. **职责边界模糊**: 各Project的定位和职责没有明确定义，导致代码放置位置不一致
2. **分层规范缺失**: 三层架构的依赖方向、通信模式没有形成规范文档
3. **模式不统一**: 不同模块采用的架构模式不一致(如MedicalCase用CQRS，其他用传统三层)
4. **新人上手困难**: 缺乏架构全景图，难以快速理解系统结构

## What

### 1. 建立项目架构规范 (NEW spec: project-architecture)

定义整体架构分层和各Project的职责:

#### 1.1 Server层架构 (13个项目)

| Project | 层级 | 职责 | 依赖方向 |
|---------|------|------|----------|
| LYBT.Entities | Domain | 领域实体、枚举、值对象 | 无依赖 |
| LYBT.Infrastructure | Infrastructure | DbContext、BaseRepository、Migrations | → Entities |
| LYBT.Module.* (8个) | Application | Service、Repository实现、业务逻辑 | → Infrastructure, Entities |
| LYBT.WebAPI | Presentation | Controller、API入口、中间件 | → Modules, Infrastructure |

#### 1.2 Shared层架构 (4个项目)

| Project | 职责 | 使用者 |
|---------|------|--------|
| LYBT.Shared.Models | DTO定义、API契约、枚举 | Server + Client |
| LYBT.Shared.Utilities | 通用工具类(加密、配置、文本处理) | Server + Client |
| LYBT.Shared.Validators | FluentValidation验证器 | Server + Client |
| LYBT.Shared.Components | 可复用业务组件(药材计算、验证) | Server + Client |

#### 1.3 Client层架构 (16个项目)

| 分类 | Project | 职责 |
|------|---------|------|
| Core (5) | LYBT.Desktop.Contracts | 接口定义(IApi, IService, IRepository) |
| | LYBT.Desktop.Foundation | HTTP客户端、缓存、安全、配置 |
| | LYBT.Desktop.Infrastructure | WPF服务、控件、转换器、主题 |
| | LYBT.Desktop.Models | 客户端模型、状态枚举 |
| | LYBT.Desktop.Presentation | UI基类(UnifiedViewModelBase)、控件 |
| Modules (8) | LYBT.Desktop.{Domain} | 业务模块(Views, ViewModels, Services) |
| Roles (2) | LYBT.Desktop.Admin/Clinical | 角色工作站配置 |
| Shell (1) | LYBT.Desktop.Shell | 应用入口、主窗口、导航 |

### 2. 明确各层文件组织规范

#### 2.1 Server Module标准结构

```
LYBT.Module.{Domain}/
├── {Domain}Module.cs          # 模块注册入口
├── Controllers/               # API控制器 (可选,部分模块无)
├── Repositories/              # Repository实现
│   └── {Entity}Repository.cs
├── Services/                  # Service实现
│   ├── I{Entity}Service.cs    # 接口
│   └── {Entity}Service.cs     # 实现
├── Validators/                # 输入验证器 (可选)
└── Dtos/                      # 模块私有DTO (可选)
```

#### 2.2 Desktop Module标准结构

```
LYBT.Desktop.{Domain}/
├── {Domain}Module.cs          # Prism模块注册
├── Views/                     # XAML视图
│   ├── {Feature}View.xaml
│   └── Dialogs/              # 弹窗视图
├── ViewModels/               # ViewModel
│   ├── {Feature}ViewModel.cs
│   ├── Dialogs/              # 弹窗ViewModel
│   └── Components/           # 组件(Handler/Coordinator)
└── Services/                 # 客户端服务 (可选)
```

### 3. 依赖规范

#### 3.1 Server层依赖矩阵

```
                 Entities  Infrastructure  Modules  WebAPI  Shared.Models
Entities            -           -            -        -          -
Infrastructure      Y           -            -        -          Y
Modules             Y           Y            -        -          Y
WebAPI              -           Y            Y        -          Y
```

#### 3.2 模块间通信规范 (已实现: decouple-server-modules)

**核心原则**: 模块间禁止直接依赖其他模块的Repository/Service，统一通过`ICrossModuleQueryService`进行跨模块只读查询。

**合法依赖类型**:
| 类型 | 实现方式 | 适用场景 |
|------|----------|----------|
| 高解耦 | ICrossModuleQueryService | 纯只读查询 (Prescriptions→Patients, Formula→Herbs) |
| 中解耦 | Service接口依赖 | 需要业务方法 (Auth→Users, MedicalCase→Patients) |
| 低解耦 | 聚合内直接引用 | DDD聚合根内部 (Consultation→MedicalCase) |

**已解耦模块**:
- Prescriptions: 移除5个模块依赖 → 全部通过ICrossModuleQueryService
- Formula: 移除1个模块依赖(Herbs) → 通过ICrossModuleQueryService

**引用规范**: `openspec/specs/module-communication/spec.md`

#### 3.3 Client层依赖矩阵

```
                 Contracts  Foundation  Infrastructure  Models  Presentation  Modules
Contracts            -          -            -            -          -           -
Foundation           Y          -            -            Y          -           -
Infrastructure       Y          Y            -            Y          -           -
Models               -          -            -            -          -           -
Presentation         Y          Y            Y            Y          -           -
Modules              Y          Y            Y            Y          Y           -
Roles                Y          Y            Y            Y          Y           Y
Shell                Y          Y            Y            Y          Y           Y
```

### 4. 架构模式规范

#### 4.1 MedicalCase模块 - CQRS模式 (示范)

```
MedicalCaseController
    ├── IMedicalCaseCommandService  (写操作: Create/Update/Delete)
    ├── IMedicalCaseQueryService    (读操作: Get/List/Search)
    ├── IMedicalCaseStateService    (状态变更: Submit/Archive)
    ├── IMedicalCasePermissionService (权限检查)
    └── IMedicalCaseAuditService    (审计日志)
```

#### 4.2 其他模块 - 传统三层模式

```
Controller → Service → Repository → DbContext
```

### 5. 规范文档产出

| 文档 | 内容 | 位置 | 状态 |
|------|------|------|------|
| module-communication/spec.md | 模块间通信规范、ICrossModuleQueryService | openspec/specs/ | 已完成 |
| project-architecture/spec.md | 项目架构总览、分层规范 | openspec/specs/ | 待创建 |
| server-layer-architecture/spec.md | Server层详细架构 | openspec/specs/ | 待创建 |
| client-layer-architecture/spec.md | Client层详细架构 | openspec/specs/ | 待创建 |
| shared-layer-architecture/spec.md | Shared层详细架构 | openspec/specs/ | 待创建 |

## Affected Files

### NEW Files (Specs)
- `openspec/specs/project-architecture/spec.md`
- `openspec/specs/server-layer-architecture/spec.md`
- `openspec/specs/client-layer-architecture/spec.md`
- `openspec/specs/shared-layer-architecture/spec.md`

### MODIFIED Files
- `openspec/project.md` - 更新架构部分引用新规范

## Out of Scope

- 代码重构(本提案仅创建规范文档)
- 新功能开发
- 测试覆盖率改进
- 性能优化

## Success Criteria

1. 所有4个架构规范文档创建完成
2. 每个规范包含明确的Requirements和Scenarios
3. 规范通过`openspec validate --strict`验证
4. project.md更新引用新规范

## Risk Assessment

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 规范过于严格导致不切实际 | 低 | 基于现有代码分析，规范反映实际情况 |
| 文档过时 | 中 | 建立代码审查时同步更新文档的机制 |

## References

- [现有repository-patterns规范](../specs/repository-patterns/spec.md)
- [现有service-conventions规范](../specs/service-conventions/spec.md)
- [现有viewmodel-conventions规范](../specs/viewmodel-conventions/spec.md)
- [Prism模块化架构](https://prismlibrary.com/docs/wpf/modules.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
