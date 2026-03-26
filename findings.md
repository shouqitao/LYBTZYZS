# 研究发现

**最后更新**: 2026-03-26

---

## 第一部分：WebAPI 构建错误分析 (2026-03-26)

### 1. 构建错误详细分析

#### 1.1 错误统计

| 文件 | 错误数 | 主要问题 |
|------|--------|----------|
| HerbsController.cs | 16 | 泛型类型推断 + cancellationToken 未定义 |
| PatientsController.cs | 8 | 泛型类型推断 |
| **总计** | **25** | |

#### 1.2 错误详情

**CS0411: 无法推断泛型类型参数**

位置：
- HerbsController.cs:103, 128, 350
- PatientsController.cs:123, 155, 267

错误代码示例：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");
```

问题原因：
- `GetEntityWithOwnershipCheckAsync<TDto>` 是泛型方法
- 编译器无法从 `Func<Guid, Task<Result<TDto>>>` 推断 `TDto` 类型
- 需要显式指定类型参数

修复方案：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, _herbService.GetByIdAsync, "药材");
```

**CS8130/CS8183: 隐式类型弃元推断失败**

位置：与 CS0411 相同行

问题原因：
- 弃元 `_` 和 `ownershipError` 依赖于泛型类型推断
- 当泛型类型推断失败时，弃元类型也无法推断

修复方案：
- 修复 CS0411 后，此问题自动解决
- 或显式声明变量类型

**CS0103: cancellationToken 未定义**

位置：HerbsController.cs:428

问题原因：
- 方法签名中参数名为 `CancellationToken cancellationToken`
- 但代码中使用了未定义的 `cancellationToken`（可能拼写错误或作用域问题）

修复方案：
- 检查方法签名和变量名是否一致
- 确保在正确的作用域内使用

---

### 2. MedicalCase 模块状态

#### 2.1 已完成的拆分工作

| 新控制器 | 方法数 | 状态 |
|----------|--------|------|
| MedicalCasesController | 12 | ✅ 已创建 |
| MedicalCaseWorkflowController | 4 | ✅ 已创建 |
| MedicalCasePrintController | 2 | ✅ 已创建 |
| MedicalCaseAuditController | 2 | ✅ 已创建 |

#### 2.2 单元测试

| 测试文件 | 测试方法数 | 状态 |
|----------|-----------|------|
| MedicalCasesControllerTests.cs | 24 | ✅ 已创建 |
| MedicalCaseWorkflowControllerTests.cs | 11 | ✅ 已创建 |
| MedicalCasePrintControllerTests.cs | 8 | ✅ 已创建 |
| MedicalCaseAuditControllerTests.cs | 9 | ✅ 已创建 |

---

### 3. 关键发现

#### 3.1 泛型方法调用模式

在 BaseApiController 中，`GetEntityWithOwnershipCheckAsync<TDto>` 方法被多个控制器使用。但在 HerbsController 和 PatientsController 中调用时，未显式指定类型参数，导致编译器无法推断。

影响范围：
- 所有使用 `GetEntityWithOwnershipCheckAsync` 的地方都需要显式指定类型参数

#### 3.2 MedicalCase 模块拆分策略

MedicalCase 模块采用 CQRS 模式拆分：
- **读操作**：MedicalCasesController (查询、搜索、批量获取)
- **写操作**：MedicalCasesController (创建、更新、删除)
- **工作流**：MedicalCaseWorkflowController (状态更新、关闭、挂起、取消)
- **打印**：MedicalCasePrintController (打印记录、打印日志)
- **审计**：MedicalCaseAuditController (权限查询、审计日志)

#### 3.3 路由冲突解决

旧控制器通过 `[NonController]` 属性禁用，避免与新控制器的路由冲突。文件保留用于参考。

---

## 第二部分：Desktop 架构分析发现 (2026-03-19) [历史记录]

**分析时间**: 2026-03-19
**分析范围**: src/Client/Desktop (WPF + Prism)

---

## 架构评分

| 维度 | 评分 | 说明 |
|------|------|------|
| 模块划分 | 8/10 | 整体清晰，Models依赖方向需调整 |
| MVVM合规 | 7/10 | 基础良好，Dispatcher直接引用是主要问题 |
| 依赖注入 | 9/10 | 工厂模式实现优秀 |
| Repository模式 | 8/10 | 双模式架构设计良好 |
| 服务层设计 | 7/10 | 职责分离可进一步优化 |
| 跨模块通信 | 8/10 | EventAggregator和接口解耦使用得当 |
| 代码质量 | 8/10 | 无明显超大类，近期重构效果显著 |
| **总体** | **7.9/10** | 架构设计良好，优先处理P0和P1问题 |

---

## Critical Issues (P0)

### 1. CoreViewModelBase 直接依赖 WPF Dispatcher
**位置**: `CoreViewModelBase.cs` 第262-291行

**问题描述**:
```csharp
protected void RunOnUIThread(Action action)
{
    if (Application.Current?.Dispatcher == null)  // 直接依赖WPF Application
    {
        action();
        return;
    }
    // ...
}
```

**影响**:
- 违反 MVVM 原则，ViewModel 与 WPF 框架强耦合
- 无法单元测试（需要真实的 WPF Application）
- 阻碍跨平台移植

**优化方案**:
创建 `IUiThreadDispatcher` 接口，通过 DI 注入:
```csharp
public interface IUiThreadDispatcher
{
    void RunOnUIThread(Action action);
    Task RunOnUIThreadAsync(Func<Task> action);
}
```

---

## 修正说明 (代码调研后)

| 原报告问题 | 实际情况 | 处理 |
|-----------|---------|------|
| "IViewModelServices 定义在 Infrastructure" | 已在 Contracts 层 (`Services/IViewModelServices.cs`) | 取消 Phase 2 |
| "ErrorHandlingServiceExtensions 死代码" | 文件已不存在 | 无需处理 |
| "[COMPAT] 标记" | 代码中未用此标记，而是注释"兼容性保留" | 按实际处理 |

---

## High/Medium Issues (P1)

### 2. ISessionManager 兼容方法未清理 (已验证)
**位置**: `ISessionManager.cs:48,58,68` / `SessionManager.cs:35,48,58`

**问题描述**:
`SetCurrentUser`, `SetUserSession`, `ClearUserSession` 3个方法标注"兼容性保留"，
经搜索确认在Desktop层无任何调用方（`.SetCurrentUser(`等搜索结果为空）。

**影响**:
- 接口污染，增加实现者负担
- 混淆 API 语义（SetSession/ClearSession 是正式 API）

**优化方案**:
直接从接口和实现中删除这3个方法。

---

## Medium Issues (P2)

### 4. Repository 与 DataSource 职责边界模糊

**问题描述**:
部分 Repository 直接转发给 DataSource，部分有额外逻辑，职责边界不够清晰。

**优化方案**:
- 明确 Repository 是业务层，DataSource 是数据访问层
- 文档化职责边界
- 重构职责模糊处

---

### 5. 服务命名不一致

**问题描述**:
部分叫 `XxxService`，部分叫 `XxxManager`，部分叫 `XxxCoordinator`。
例如: `SessionManager` vs `NavigationCoordinator`

**优化方案**:
统一命名规范:
- `Service` - 业务服务
- `Manager` - 资源/生命周期管理
- `Coordinator` - 流程编排

---

## Low Issues (P3)

### 6. 死代码

| 文件 | 位置 | 说明 |
|------|------|------|
| `ProblemDetails.cs` | Models/Http/ | 疑似死代码，无外部引用 |
| `ErrorHandlingServiceExtensions.cs` | Shell/Extensions/ | 未使用方法 |
| 空 ItemGroup | Infrastructure.csproj 第75-77行 | 遗留迁移痕迹 |

---

### 7. 其他小问题

- `MasterDetailViewModelBase` 继承 `ObservableObject` 而非 `CoreViewModelBase`（项目依赖限制）
- `ContainerLocator` 服务定位器使用（`AccountSettingsControl.xaml.cs`）
- 反射获取私有属性（`ApplicationInitializationService.cs`）

---

## 架构优势总结

| 优势 | 说明 |
|------|------|
| 双模式架构 | 远程/本地模式运行时切换，工厂模式实现优雅 |
| ViewModel服务聚合 | `IViewModelServices` 将7个服务聚合为1个，简化子类 |
| Composite ViewModel | `MedicalCaseWorkspaceViewModel` 拆分为5个子VM，职责清晰 |
| 接口驱动设计 | Contracts层定义清晰，模块间通过接口解耦 |
| 源生成器使用 | 广泛使用CommunityToolkit.Mvvm源生成器，减少样板代码 |
