---
feature: controller-inheritance-simplification
status: delivered
updated: 2026-07-31
branch: refactor/controller-simplify
commits: 8291b88f9..HEAD
---

# Controller 继承体系简化

## Report

**What was built** — 将 `BaseCrudController` 从4泛型+5抽象方法模式简化为非泛型+virtual工厂方法模式。消除了每个 concrete controller 必须实现5个工厂方法的仪式，同时保留了 Sender 注入、GetOperator、响应助手等有用功能。

**Verification** — `dotnet build LYBTZYZS.sln` 通过（0 错误），架构测试 84/86 通过（1 pre-existing AR001 失败，1 pre-existing skip）。

**Journey log**:
- 原始 spec 提议 BaseSoftDeleteCrudController 中间层，实际采用更简洁的方案：直接移除泛型
- MedicalCaseProcessingController 合并已在前序 commit (fb95e586e) 完成
- LocalWebAPI controllers 也需要同步更新（容易遗漏）

## [S1] Problem

Server 端 Controller 继承体系存在设计问题：

1. **MedicalCase 路由冲突**（已修复）：`MedicalCaseProcessingController` 和 `MedicalCasesController` 共享路由前缀，已合并。

2. **BaseCrudController 泛型仪式过高**：4个泛型类型参数 + 5个抽象工厂方法强制每个子类实现大量样板代码。Registration 模块 3/5 个工厂方法 throw NotSupportedException，违反 Liskov 替换原则。所有 concrete controller 都 override 了 action 方法，使工厂方法形同虚设。

## [S2] Solution

### S2.1 MedicalCase Controller 合并（已完成）

前序 commit fb95e586e 已将 `MedicalCaseProcessingController` 合并到 `MedicalCasesController`。

### S2.2 BaseCrudController 简化

**核心变更：移除泛型，抽象改虚拟**

```
旧: BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery>  (4泛型 + 5抽象)
新: BaseCrudController                                         (非泛型 + 5 virtual throw)
```

新的 `BaseCrudController`：
- 移除4个泛型类型参数
- 5个工厂方法从 `abstract` 改为 `virtual`（默认 throw NotSupportedException）
- 暴露 `Sender` 属性供子类直接使用 MediatR
- ToggleStatus/Restore 保持 virtual throw（已在前序 commit 完成）

**继承者变更：**
- `BaseMedicalCasesController`：移除泛型参数，移除5个工厂方法实现
- `BaseRegistrationsController`：移除泛型参数，移除 `GetRegistrationsQueryWrapped` 包装类和 `GetRegistrationsQueryWrappedHandler`
- `BaseUsersController`：移除泛型参数，直接使用 Sender 发送命令
- 所有 concrete controllers：移除泛型参数，移除 `#region 基类抽象方法实现` 块

## [S3] Out of Scope

- BaseService 权限验证与 Controller 所有权检查的统一
- Repository 抽象层简化
- Server/LocalWebAPI 额外端点重复（batch-enable/disable 等）

## Tasks

- [x] T1: 合并 MedicalCaseProcessingController（前序 commit 已完成）
- [x] T2: 重写 BaseCrudController — 移除泛型，抽象改 virtual（covers: S2.2）
- [x] T3: 更新 BaseMedicalCasesController — 移除泛型和工厂方法（covers: S2.2）
- [x] T4: 更新 BaseRegistrationsController — 移除泛型和包装类（covers: S2.2）
- [x] T5: 更新 BaseUsersController — 移除泛型（covers: S2.2）
- [x] T6: 更新 WebAPI concrete controllers（covers: S2.2）
- [x] T7: 更新 LocalWebAPI controllers（covers: S2.2）
- [x] T8: 更新架构测试排除列表（covers: S2.2）
- [x] T9: 验证 dotnet build 通过（covers: S2.2）
