# Issue #1829 文档同步清单

## 变更范围

- **基准提交**: 19a8abfe (Issue #1796 文档同步)
- **目标提交**: HEAD
- **涉及提交**: 13个commits
- **生成时间**: 2025-11-05
- **Epic关联**: #1822 启动到工作台流程端到端重构优化

---

## 🔴 必须更新（Critical）

### 1. API文档更新

#### 1.1 新增认证API端点

**文件**: `docs/api/auth-api.md`（如不存在需创建）

**变更内容**:
- 新增端点: `POST /api/v1/auth/validate`
- Controller: `AuthController.ValidateTokenFromBodyAsync`
- 请求DTO: `ValidateTokenRequest`
- 响应DTO: `ValidateTokenResponse`

**文档要求**:
```markdown
### POST /api/v1/auth/validate
从请求体验证Token并返回详细信息（Issue #1824）

**请求体**:
```json
{
  "token": "string (JWT token)"
}
```

**响应**:
```json
{
  "isSuccess": true,
  "data": {
    "isValid": true,
    "userId": 123,
    "username": "doctor",
    "role": "Doctor",
    "expiresAt": "2025-11-05T12:00:00Z",
    "errorMessage": null
  }
}
```

**状态码**:
- 200: Token验证完成（无论有效或无效）
- 400: 请求参数错误
```

**相关文件**:
- `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:178-199`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/ValidateTokenRequest.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/ValidateTokenResponse.cs`

---

### 2. 配置文档更新

#### 2.1 配置字段名称变更

**文件**: `docs/quick-reference/config-templates.md` 或 `docs/how-to/configuration.md`

**变更内容**:
- `Lybt:SystemAdmin:Username` → `Lybt:SystemAdmin:UserName`（大小写调整）

**文档要求**:
更新所有涉及超级管理员配置的示例：

```json
// ❌ 旧配置（已废弃）
"SystemAdmin": {
  "Username": "sysadmin"  // 已废弃
}

// ✅ 新配置（Issue #1761 Phase 3.1）
"SystemAdmin": {
  "UserName": "sysadmin"  // 统一使用PascalCase
}
```

**影响文件**:
- `src/Server/Services/LYBT.WebAPI/appsettings.json:116`
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs:58`

---

## 🟡 建议更新（Recommended）

### 3. Desktop架构文档更新

#### 3.1 新增应用状态服务

**文件**: `docs/explanation/architecture/client/desktop-services.md`

**变更内容**:
- 新增: `ApplicationStateService` / `IApplicationStateService`
- 功能: 管理应用全局状态（当前用户、连接状态等）
- 模块: `LYBT.Desktop.Core`

**文档要求**:
```markdown
### ApplicationStateService

**用途**: 管理Desktop应用全局状态

**核心功能**:
- 当前登录用户信息管理
- 应用连接状态跟踪
- 跨模块状态共享

**注入方式**:
```csharp
services.AddSingleton<IApplicationStateService, ApplicationStateService>();
```

**使用示例**:
```csharp
public class MyViewModel
{
    private readonly IApplicationStateService _appState;

    public MyViewModel(IApplicationStateService appState)
    {
        _appState = appState;
        var currentUser = _appState.CurrentUser;
    }
}
```
```

**相关文件**:
- `src/Client/Desktop/Core/LYBT.Desktop.Core/Services/ApplicationStateService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Core/Services/IApplicationStateService.cs`

---

#### 3.2 新增连接设置服务

**文件**: `docs/explanation/architecture/client/desktop-services.md`

**变更内容**:
- 新增: `ConnectionSettingsService` / `IConnectionSettingsService`
- 新增枚举: `ConnectionMode`
- 功能: 管理Server连接配置（API地址、超时等）

**文档要求**:
```markdown
### ConnectionSettingsService

**用途**: 管理Desktop与Server的连接配置

**核心功能**:
- API基地址管理
- 连接超时配置
- 连接模式切换（开发/生产）

**ConnectionMode枚举**:
```csharp
public enum ConnectionMode
{
    Development,  // 开发模式（localhost）
    Production    // 生产模式（配置地址）
}
```

**注入方式**:
```csharp
services.AddSingleton<IConnectionSettingsService, ConnectionSettingsService>();
```
```

**相关文件**:
- `src/Client/Desktop/Core/LYBT.Desktop.Core/Services/ConnectionSettingsService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Core/Services/IConnectionSettingsService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Core/Models/ConnectionMode.cs`

---

#### 3.3 MedicalCase模块组件化重构

**文件**: `docs/explanation/architecture/client/medicalcase-architecture.md`

**变更内容**:
- 重构: `MedicalCaseFlowViewModel` 组件化拆分（Issue #1807）
- 新增6个服务类（从992行重构至629行）

**新增服务类**:
1. `FormulaImportHandler` - 验方导入处理
2. `HerbSelectionManager` - 药材选择管理
3. `MedicalCaseDataLoader` - 病历数据加载
4. `MedicalCaseFlowManager` - 流程状态管理
5. `MedicalCaseLifecycleHandler` - 生命周期处理
6. `PrescriptionCalculator` - 处方计算

**文档要求**:
```markdown
### MedicalCase模块组件化架构（Issue #1807）

**设计原则**: 单一职责 + 组件化解耦

**组件划分**:

#### 1. FormulaImportHandler
- **职责**: 处理验方导入到处方的业务逻辑
- **依赖**: IFormulaRepository
- **方法**: ImportFormulaAsync(formulaId)

#### 2. HerbSelectionManager
- **职责**: 管理药材选择与剂量计算
- **依赖**: IHerbRepository
- **方法**: AddHerb(), UpdateDosage(), RemoveHerb()

#### 3. MedicalCaseDataLoader
- **职责**: 加载病历相关数据（患者、历史病历）
- **依赖**: IMedicalCaseRepository, IPatientRepository
- **方法**: LoadPatientDataAsync(), LoadHistoryAsync()

#### 4. MedicalCaseFlowManager
- **职责**: 管理三步诊疗流程状态（辨证→开方→处方）
- **依赖**: 无
- **方法**: MoveToNextStep(), ValidateCurrentStep()

#### 5. MedicalCaseLifecycleHandler
- **职责**: 处理病历生命周期事件（创建、保存、完成）
- **依赖**: IMedicalCaseRepository
- **方法**: CreateCaseAsync(), SaveDraftAsync(), CompleteCaseAsync()

#### 6. PrescriptionCalculator
- **职责**: 处方计算逻辑（总价、剂量校验）
- **依赖**: 无
- **方法**: CalculateTotal(), ValidateDosage()

**重构成果**:
- 代码量: 992行 → 629行（减少36%）
- 单一职责: ViewModel专注UI逻辑，业务逻辑委托给服务类
- 可测试性: 6个服务类可独立单元测试
```

**相关文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/FormulaImportHandler.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/HerbSelectionManager.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseDataLoader.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseFlowManager.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseLifecycleHandler.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/PrescriptionCalculator.cs`

---

#### 3.4 视图删除记录

**文件**: `docs/explanation/architecture/client/deprecated-components.md`（建议新建）

**变更内容**:
- 删除: `ConsultationManagementViewModel` / `ConsultationManagementView`
- 删除: `ViewFormulaDialogViewModel` / `ViewFormulaDialog`
- 原因: 功能合并至MedicalCaseFlowView（Issue #1806）

**文档要求**:
```markdown
# 已废弃组件清单

## 2025-11 Epic #1822 废弃

### ConsultationManagementView（已删除）
- **废弃原因**: 功能已合并至 `MedicalCaseFlowView`
- **废弃时间**: Issue #1806
- **迁移路径**: 使用 `MedicalCaseFlowView` 三步流程（辨证→开方→处方）

### ViewFormulaDialog（已删除）
- **废弃原因**: 功能已集成至 `MedicalCaseFlowView` 的验方选择
- **废弃时间**: Issue #1806
- **迁移路径**: 使用 `MedicalCaseFlowView` 的内联验方选择
```

**相关删除文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationManagementView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Dialogs/ViewFormulaDialogViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Dialogs/ViewFormulaDialog.xaml`

---

## ⚪ 可选更新（Optional）

### 4. 快速参考更新

#### 4.1 更新服务注册清单

**文件**: `docs/quick-reference/service-registration.md`

**变更内容**:
添加新增的8个服务类注册示例：

```csharp
// Core服务（Singleton）
services.AddSingleton<IApplicationStateService, ApplicationStateService>();
services.AddSingleton<IConnectionSettingsService, ConnectionSettingsService>();

// MedicalCase服务（Transient）
services.AddTransient<FormulaImportHandler>();
services.AddTransient<HerbSelectionManager>();
services.AddTransient<MedicalCaseDataLoader>();
services.AddTransient<MedicalCaseFlowManager>();
services.AddTransient<MedicalCaseLifecycleHandler>();
services.AddTransient<PrescriptionCalculator>();
```

---

## 📊 更新统计

### 变更类型分布

| 类型 | 数量 | 优先级 |
|-----|------|--------|
| API端点 | 1 | 🔴 Critical |
| 配置字段 | 1 | 🔴 Critical |
| 新增服务类 | 8 | 🟡 Recommended |
| 删除视图 | 4 | 🟡 Recommended |
| **合计** | **14** | - |

### 文档更新工作量估算

| 文档类型 | 预估时间 | 优先级 |
|---------|---------|--------|
| API文档 | 15分钟 | P0 |
| 配置文档 | 10分钟 | P0 |
| 架构文档 | 30分钟 | P1 |
| 快速参考 | 10分钟 | P2 |
| **总计** | **65分钟** | - |

---

## ✅ 验证清单

完成文档更新后，请验证以下内容：

- [ ] API文档包含完整的请求/响应示例
- [ ] 配置文档已移除所有旧配置字段引用
- [ ] 架构文档准确反映6个新增服务类的职责
- [ ] 所有文档链接有效（docs/index.md导航正确）
- [ ] 示例代码可编译通过
- [ ] 提交信息包含 `Closes #1829`

---

## 📝 实施建议

### 执行顺序

1. **Phase 1（必须）**: API文档 + 配置文档（25分钟）
2. **Phase 2（建议）**: 架构文档 + 废弃组件文档（40分钟）
3. **Phase 3（可选）**: 快速参考更新（10分钟）

### 文档工具

- **Markdown编辑器**: VS Code + Markdown Preview
- **代码验证**: 直接从源文件复制代码示例
- **链接检查**: `docs/index.md` → 各子文档导航

---

**生成时间**: 2025-11-05
**Issue**: #1829
**Epic**: #1822
**生成工具**: lybtzyzs-doc-sync skill
