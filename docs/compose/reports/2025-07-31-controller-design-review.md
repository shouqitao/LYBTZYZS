# Controller 设计评估报告

> **审查日期**: 2025-07-31
> **审查范围**: 全部 Server + LocalWebAPI Controller 的继承体系、模块间集成、职责划分

---

## 一、Controller 继承体系总览

```
ControllerBase (ASP.NET Core)
└── BaseApiController (LYBT.Infrastructure.Web)
    ├── BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery>
    │   ├── BaseMedicalCasesController (Module.MedicalCase)
    │   │   └── MedicalCasesController (WebAPI + LocalWebAPI)
    │   ├── BaseRegistrationsController (Module.Registration)
    │   │   └── RegistrationsController (WebAPI + LocalWebAPI)
    │   ├── HerbsController (WebAPI + LocalWebAPI)
    │   ├── FormulasController (WebAPI + LocalWebAPI)
    │   └── PatientsController (WebAPI + LocalWebAPI)
    ├── BaseUsersController (Module.Users)
    │   └── UsersController (WebAPI + LocalWebAPI)
    ├── AuthController (WebAPI + LocalWebAPI)
    ├── MedicalCaseProcessingController (仅 WebAPI)
    ├── ConfigurationController (WebAPI + LocalWebAPI)
    ├── DeployController (WebAPI + LocalWebAPI)
    ├── DiagnosticsController (WebAPI + LocalWebAPI)
    ├── HealthController (WebAPI + LocalWebAPI)
    └── ReportsController (WebAPI + LocalWebAPI)
```

### 层级数量

| 继承链 | 层数 | 模块 |
|---|---|---|
| `BaseApiController` → `BaseCrudController` → `BaseMedicalCasesController` → `MedicalCasesController` | **4 层** | MedicalCase |
| `BaseApiController` → `BaseCrudController` → `BaseRegistrationsController` → `RegistrationsController` | **4 层** | Registration |
| `BaseApiController` → `BaseCrudController` → `HerbsController` | **3 层** | Herbs |
| `BaseApiController` → `BaseCrudController` → `FormulasController` | **3 层** | Formula |
| `BaseApiController` → `BaseCrudController` → `PatientsController` | **3 层** | Patients |
| `BaseApiController` → `BaseUsersController` → `UsersController` | **3 层** | Users |
| `BaseApiController` → `AuthController` | **2 层** | Auth |
| `BaseApiController` → `MedicalCaseProcessingController` | **2 层** | MedicalCase |

---

## 二、Base Controller 职责分析

### BaseApiController (Infrastructure 层)

| 职责 | 方法数 | 评价 |
|---|---|---|
| 操作者信息获取 | `GetOperator()` | ✅ 合理 |
| 日志记录 (带脱敏) | `LogOperation()` | ✅ 合理 |
| API 响应封装 | `Success/Error/NotFound/BusinessFail/ValidationFail/Forbid` | ✅ 合理 |
| Result 处理 | `HandleResult()` | ✅ 合理 |
| 参数验证 | `ValidateGuid/ValidatePagination/ValidateModel/ValidateOwnership` | ⚠️ 偏多 |
| 所有权检查 | `IsAdminOrOwner/GetEntityWithOwnershipCheckAsync` | ⚠️ 偏多 |

**评价**: 83 行代码，职责清晰。但"所有权检查"属于业务逻辑，放在基础设施层有些越界。

### BaseCrudController (Infrastructure 层)

| 方法 | 职责 | 评价 |
|---|---|---|
| `GetList` | 分页查询 | ✅ |
| `GetById` | 详情 (抽象) | ✅ |
| `Create` | 创建 | ✅ |
| `Update` | 更新 | ✅ |
| `Delete` | 删除 | ✅ |
| `ToggleStatus` | 切换状态 | ⚠️ 不是所有实体都有此概念 |
| `Restore` | 恢复 | ⚠️ 不是所有实体都有此概念 |
| `BatchDelete` | 批量删除 | ✅ |

**评价**: 193 行代码，提供了 7 个抽象方法。问题在于 **ToggleStatus 和 Restore 不是通用需求**，导致：
- MedicalCasesController: Override ToggleStatus/Restore 返回 "不支持"
- RegistrationsController: Override Update/Delete/ToggleStatus/Restore/BatchDelete 返回 "不支持"

**5/7 个方法被 Registration 覆盖为 "不支持"**，说明 BaseCrudController 的抽象边界过高。

---

## 三、各模块 Controller 详细评估

### 3.1 Users 模块

**继承链**: `BaseApiController` → `BaseUsersController` → `UsersController`

| 特征 | 数据 |
|---|---|
| BaseUsersController 方法数 | 12 个 |
| UsersController (WebAPI) 方法数 | 0 (纯继承) |
| UsersController (LocalWebAPI) 方法数 | 0 (纯继承) |
| 跨模块依赖 | 无 |

**BaseUsersController 方法清单**:
1. GetList (分页查询)
2. GetCurrentUser (当前用户)
3. GetById (详情)
4. Create (创建)
5. Update (更新)
6. Delete (删除)
7. ResetPassword (重置密码)
8. ChangeProfile (修改个人资料)
9. ChangePassword (修改密码)
10. ToggleStatus (切换状态)
11. BatchDelete (批量删除)
12. Restore (恢复)
13. BatchEnable (批量启用)
14. BatchDisable (批量禁用)

**设计评价**:
- ✅ **独立性强**: Users 不依赖任何其他模块，职责边界清晰
- ✅ **Base 复用好**: Server 和 LocalWebAPI 的 UsersController 都是纯继承，零重复代码
- ✅ **方法全面**: 14 个方法覆盖了用户管理的所有场景
- ⚠️ **BaseUsersController 未使用 BaseCrudController**: 自己重新实现了 GetList/GetById/Create/Update/Delete/ToggleStatus/BatchDelete/Restore，与 BaseCrudController 功能重叠
- ⚠️ **14 个方法偏多**: BaseUsersController 承担了过多职责（CRUD + 密码管理 + 状态管理 + 批量操作）

**建议**: BaseUsersController 应继承 BaseCrudController，只额外添加密码/状态相关方法。当前是 "绕过" 了通用 CRUD 基类。

---

### 3.2 Herbs 模块

**继承链**: `BaseApiController` → `BaseCrudController` → `HerbsController`

| 特征 | 数据 |
|---|---|
| Server HerbsController 方法数 | 13 (含继承) |
| LocalWebAPI HerbsController 方法数 | 6 (override 部分) |
| 跨模块依赖 | 无 |

**Server 额外端点** (在 BaseCrud 基础上):
- `POST batch-import` (批量导入)
- `GET {id}/check-reference` (引用检查)
- `POST batch-check-reference` (批量引用检查)
- `POST batch-enable` (批量启用)
- `POST batch-disable` (批量禁用)

**LocalWebAPI 额外端点**:
- `GET {id}` (GetById override)
- `POST batch-import`
- `GET {id}/check-reference`
- `POST batch-check-reference`
- `POST batch-enable`
- `POST batch-disable`

**设计评价**:
- ✅ **继承 BaseCrudController 正确使用**: GetList/Create/Update/Delete/ToggleStatus/Restore/BatchDelete 从基类继承
- ✅ **扩展端点合理**: batch-import/check-reference/batch-enable/disable 是业务特化
- ⚠️ **Server 和 LocalWebAPI 的额外端点几乎相同**: batch-import/check-reference/batch-enable/disable 在两边都有实现，存在重复
- ⚠️ **batch-enable/disable 语义与 ToggleStatus 重叠**: ToggleStatus 是单个切换，batch-enable/disable 是批量切换，功能相似但 API 不统一

---

### 3.3 Formulas 模块

**继承链**: `BaseApiController` → `BaseCrudController` → `FormulasController`

| 特征 | 数据 |
|---|---|
| Server FormulasController 方法数 | 13 (含继承) |
| LocalWebAPI FormulasController 方法数 | 7 |
| 跨模块依赖 | 无 |

**Server 额外端点**:
- `POST batch-import`
- `POST {formulaId}/herbs/{herbItemId}/validate` (配伍验证)
- `POST batch-enable`
- `POST batch-disable`

**设计评价**: 与 Herbs 模式一致，✅ 合理。

---

### 3.4 Patients 模块

**继承链**: `BaseApiController` → `BaseCrudController` → `PatientsController`

| 特征 | 数据 |
|---|---|
| Server PatientsController 方法数 | 11 |
| LocalWebAPI PatientsController 方法数 | 4 |
| 跨模块依赖 | 无 |

**Server 额外端点**:
- `GET {id}/check-reference`
- `POST batch-check-reference`

**LocalWebAPI 额外端点**:
- `GET {id}` (GetById)
- `GET by-id-number/{idNumber}` (身份证号查询 - LocalWebAPI 独有)

**设计评价**:
- ✅ 简洁，职责清晰
- ✅ LocalWebAPI 的 `by-id-number` 是合理的本地特化 (身份证读卡)

---

### 3.5 MedicalCase 模块 ⚠️ 复杂度最高

**继承链**: `BaseApiController` → `BaseCrudController` → `BaseMedicalCasesController` → `MedicalCasesController`

**Server 端有两个 Controller**:

| Controller | 路由 | 方法数 | 职责 |
|---|---|---|---|
| `MedicalCasesController` | `/api/v1/medicalcases` | 7 (override) | CRUD + 查询 + 审计 |
| `MedicalCaseProcessingController` | `/api/v1/medicalcases` | 4 | 状态流转 (close/suspend/cancel/status) |

**问题**: 两个 Controller 共享同一路由前缀 `/api/v1/medicalcases`，但职责分离不清晰。

**端点分布**:

| 端点 | MedicalCasesController | MedicalCaseProcessingController |
|---|---|---|
| `GET /` (列表) | ✅ | - |
| `GET {id}` (详情) | ✅ | - |
| `POST /` (创建) | ✅ | - |
| `PUT {id}` (保存) | ✅ | - |
| `DELETE {id}` (删除) | ✅ | - |
| `POST batch-delete` | ✅ | - |
| `PUT {id}/prescription-flag` | ✅ | - |
| `PUT {id}/print-completed` | ✅ | - |
| `PUT {id}/status` | - | ✅ |
| `PUT {id}/close` | - | ✅ |
| `PUT {id}/suspend` | - | ✅ |
| `PUT {id}/cancel` | - | ✅ |
| `GET search` | ✅ (继承) | - |
| `GET query` | ✅ (继承) | - |
| `GET patient/{id}/consultations` | ✅ (继承) | - |
| `GET patient/{id}/prescriptions` | ✅ (继承) | - |
| `GET {id}/consultations` | ✅ (继承) | - |
| `GET {id}/prescriptions` | ✅ (继承) | - |
| `POST batch-details` | ✅ (继承) | - |
| `GET {id}/permissions` | ✅ (继承) | - |
| `GET {id}/audit-logs` | ✅ (继承) | - |

**设计评价**:
- ❌ **路由冲突**: 两个 Controller 同一路由前缀，ASP.NET Core 会尝试匹配，可能导致路由歧义
- ❌ **职责分离不当**: MedicalCaseProcessingController 的 4 个端点 (status/close/suspend/cancel) 是状态流转，但 MedicalCasesController 的 Save/Delete 也是状态变更，边界模糊
- ❌ **LocalWebAPI 合并了两边功能**: LocalWebAPI 的 MedicalCasesController 一个类包含了 Server 端两个 Controller 的所有端点，说明 Server 端的拆分可能是过度设计
- ⚠️ **BaseMedicalCasesController Override 了 ToggleStatus/Restore 为 "不支持"**: 说明这两个方法对医案不适用，进一步印证 BaseCrudController 的抽象边界问题

---

### 3.6 Registration 模块 ⚠️ Override 最多

**继承链**: `BaseApiController` → `BaseCrudController` → `BaseRegistrationsController` → `RegistrationsController`

**Override 情况**:

| BaseCrudController 方法 | Registration 处理 |
|---|---|
| `GetList` | Override (7 参数过滤) |
| `GetById` | Override |
| `Create` | Override (WebAPI 添加 RateLimiting) |
| `Update` | ❌ 返回 "不支持" |
| `Delete` | ❌ 返回 "不支持" |
| `ToggleStatus` | ❌ 返回 "不支持" |
| `Restore` | ❌ 返回 "不支持" |
| `BatchDelete` | ❌ 返回 "不支持" |

**7 个抽象方法中 5 个返回 "不支持"**，实际只用了 GetList/GetById/Create。

**设计评价**:
- ❌ **继承 BaseCrudController 是错误选择**: Registration 不需要 Update/Delete/ToggleStatus/Restore/BatchDelete，继承后大量 Override 为 "不支持"，违反 Liskov 替换原则
- ❌ **GetRegistrationsQueryWrapped 是适配器模式的滥用**: 为了适配 BaseCrudController 的 `IRequest<Result<PagedResult<T>>>` 约束，专门创建了一个 Wrapper Record + Handler，增加了不必要的复杂度
- ✅ **特化方法合理**: queue/start-visit/cancel/quick-visit 是挂号业务的核心流程

---

### 3.7 Auth 模块

**继承链**: `BaseApiController` → `AuthController`

| 特征 | 数据 |
|---|---|
| Server AuthController 方法数 | 6 |
| LocalWebAPI AuthController 方法数 | 5 |

**Server 端点**:
- `POST login` (登录)
- `POST logout` (登出)
- `POST refresh` (刷新令牌)
- `POST auto-login` (自动登录)
- `GET validate` (验证令牌)
- `POST change-password` (修改密码)

**LocalWebAPI 端点**:
- `POST login` (LocalLoginCommand)
- `POST logout`
- `POST refresh` (LocalRefreshTokenCommand)
- `POST auto-login` (LocalAutoLoginCommand)
- `GET validate` (LocalValidateTokenQuery)

**设计评价**:
- ✅ **独立性强**: Auth 不依赖其他模块
- ✅ **Server 和 LocalWebAPI 使用不同 Command**: Server 用 `LoginCommand`，LocalWebAPI 用 `LocalLoginCommand`，正确区分了远程/本地认证
- ✅ **继承 BaseApiController 正确**: 不需要 CRUD，直接用 BaseApiController 的响应方法

---

### 3.8 其他 Controller

| Controller | 方法数 | 继承 | 评价 |
|---|---|---|---|
| `ConfigurationController` | 3 | BaseApiController | ✅ 简洁 |
| `DeployController` | 2 | BaseApiController | ✅ 简洁 |
| `DiagnosticsController` | 4 | BaseApiController | ✅ 简洁 |
| `HealthController` | 3 | BaseApiController | ✅ 简洁 |
| `ReportsController` | 3 | BaseApiController | ✅ 简洁 |

---

## 四、模块间集成关系评估

### 4.1 Server 模块间依赖

```
LYBT.WebAPI (入口)
├── Module.Auth (独立)
├── Module.Users (独立)
├── Module.Herbs (独立)
├── Module.Formula (独立)
├── Module.Patients → Module.MedicalCase ⚠️
├── Module.MedicalCase (独立)
├── Module.Registration (独立)
└── Module.Reports (独立)
```

**评价**:
- ✅ **8 个模块中 7 个完全独立**: 只有 Patients → MedicalCase 一处跨模块依赖
- ✅ **CrossModuleService 模式**: Herbs/Patients/Registration/Users 各有 CrossModuleService，通过接口解耦
- ⚠️ **Patients → MedicalCase 依赖**: 需要确认是否可通过 CrossModuleService 替代

### 4.2 Desktop 模块间依赖

```
LYBT.Desktop.Shell (入口)
├── Auth (独立)
├── Users (独立)
├── Patients (独立)
├── Herbs (独立)
├── Formula (独立)
├── MedicalCase (独立)
├── Registration → MedicalCase + Patients + Users ⚠️
├── Admin → Herbs + Formula + Patients + MedicalCase + Users ⚠️
└── Clinical → Herbs + Formula + Patients + MedicalCase + Registration ⚠️
```

**评价**:
- ⚠️ **Registration 依赖 3 个模块**: 需要创建医案、关联患者、关联医生，依赖合理但实现方式是直接引用而非接口
- ⚠️ **Admin/Clinical 是 "God Role"**: 各自引用 5 个业务模块，这是角色组合的必然结果，但增加了编译耦合
- ✅ **业务模块之间无循环依赖**: Registration → MedicalCase/Patients/Users 是单向的

### 4.3 Server ↔ LocalWebAPI 集成

| 方面 | Server WebAPI | LocalWebAPI | 评价 |
|---|---|---|---|
| Controller 数量 | 13 | 12 | ⚠️ 差 1 个 (MedicalCaseProcessing) |
| 继承 Base CrudController | 5 个 | 5 个 | ✅ 一致 |
| 使用 Module Base Controller | MedicalCases/Users/Registration | MedicalCases/Users/Registration | ✅ 一致 |
| 额外端点 (batch-enable 等) | Server 独有 | 部分复制 | ⚠️ 重复 |
| 路由前缀 | `api/v{version:apiVersion}` | `api/v1` | ✅ 合理差异 |
| 认证方式 | 远程 Identity + JWT | 本地简化 JWT | ✅ 合理差异 |

---

## 五、关键问题总结

### 🔴 严重问题

#### 1. BaseCrudController 抽象边界过高

**表现**: ToggleStatus 和 Restore 不是所有实体都需要的操作，导致 Registration Override 5/7 个方法为 "不支持"。

**影响**: 违反 Liskov 替换原则 — 子类不应该 "拒绝" 父类的行为。

**建议**: 将 ToggleStatus/Restore/BatchDelete 从 BaseCrudController 拆分到 `BaseSoftDeleteCrudController`，只有需要软删除/恢复的实体才继承。

#### 2. MedicalCase 模块两个 Controller 路由冲突

**表现**: `MedicalCasesController` 和 `MedicalCaseProcessingController` 共享 `/api/v1/medicalcases` 路由前缀。

**影响**: 路由歧义，维护困难，LocalWebAPI 已合并为一个 Controller。

**建议**: 将 MedicalCaseProcessingController 的 4 个端点合并回 MedicalCasesController，或将其路由改为 `/api/v1/medicalcases/{id}/workflow`。

#### 3. Users 模块绕过 BaseCrudController

**表现**: BaseUsersController 自己实现了 GetList/GetById/Create/Update/Delete/ToggleStatus/BatchDelete/Restore，与 BaseCrudController 功能完全重叠。

**影响**: 两套 CRUD 实现，维护成本翻倍。

**建议**: BaseUsersController 应继承 BaseCrudController，只额外添加密码/状态管理方法。

### 🟡 中等问题

#### 4. BaseRegistrationsController 的 Wrapper 适配器

**表现**: 为了适配 BaseCrudController 的泛型约束，创建了 `GetRegistrationsQueryWrapped` + `GetRegistrationsQueryWrappedHandler`。

**影响**: 增加了不必要的复杂度，只是为了 "假装" 符合基类约束。

**建议**: Registration 不应继承 BaseCrudController，直接继承 BaseApiController 即可。

#### 5. Herbs/Formula 的 batch-enable/disable 与 ToggleStatus 语义重叠

**表现**: ToggleStatus 是单个切换，batch-enable/disable 是批量切换，功能相似但 API 不统一。

**影响**: 客户端需要调用不同的端点实现相似功能。

**建议**: 统一为 `POST {id}/toggle-status` (单个) + `POST batch-toggle-status` (批量)，去掉 batch-enable/disable。

#### 6. Server 和 LocalWebAPI 的额外端点重复

**表现**: Herbs/Formula 的 batch-import/check-reference/batch-enable/disable 在 Server 和 LocalWebAPI 都有实现。

**影响**: 改一处必须同步改两处。

**建议**: 这些端点应通过 BaseCrudController 或共享基类统一实现。

---

## 六、整体架构评估矩阵

| 模块 | 继承合理性 | 方法数量 | Override 数量 | 跨模块依赖 | 重复代码 | 综合评分 |
|---|---|---|---|---|---|---|
| **Users** | ⚠️ 绕过 BaseCrud | 14 (偏多) | 0 | 无 | ✅ 零重复 | **B+** |
| **Herbs** | ✅ 正确使用 | 13 (合理) | 0 | 无 | ⚠️ Server/Local 部分重复 | **A-** |
| **Formula** | ✅ 正确使用 | 13 (合理) | 0 | 无 | ⚠️ Server/Local 部分重复 | **A-** |
| **Patients** | ✅ 正确使用 | 11 (合理) | 0 | 无 | ✅ 几乎无重复 | **A** |
| **MedicalCase** | ⚠️ 过度继承 | 21 (Server 2 Controller) | 3 (ToggleStatus/Restore/Delete) | 无 | ❌ Server 2 Controller vs Local 1 Controller | **C+** |
| **Registration** | ❌ 错误继承 | 8 (5 个 override 为不支持) | 5 | 无 | ✅ 零重复 | **C** |
| **Auth** | ✅ 正确使用 | 6 (合理) | 0 | 无 | ✅ 零重复 | **A+** |
| **Configuration/Deploy/Diagnostics/Health/Reports** | ✅ 正确使用 | 2-4 (合理) | 0 | 无 | ✅ 零重复 | **A+** |

---

## 七、重构建议优先级

| 优先级 | 建议 | 影响范围 | 预估工作量 |
|---|---|---|---|
| **P0** | 将 MedicalCaseProcessingController 合并回 MedicalCasesController | MedicalCase 模块 | 中 |
| **P0** | 拆分 BaseCrudController: 创建 BaseSoftDeleteCrudController | Infrastructure + 所有继承者 | 中 |
| **P1** | Registration 不继承 BaseCrudController，直接继承 BaseApiController | Registration 模块 | 小 |
| **P1** | Users 继承 BaseCrudController，BaseUsersController 只添加密码/状态方法 | Users 模块 | 中 |
| **P2** | 统一 ToggleStatus + batch-enable/disable 为 batch-toggle-status | Herbs/Formula/Users | 小 |
| **P2** | 将 Shared 端点 (batch-import/check-reference) 提取到共享基类 | Herbs/Formula | 中 |

---

*报告生成于 2025-07-31 by AI Agent*
