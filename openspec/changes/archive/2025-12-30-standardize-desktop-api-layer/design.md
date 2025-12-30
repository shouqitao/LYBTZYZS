# Design: standardize-desktop-api-layer

## Desktop数据流架构

### 层次架构图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Desktop Client                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────┐                                                      │
│  │     View      │  XAML视图层 - 纯UI呈现，无业务逻辑                     │
│  │   (*.xaml)    │  职责: 数据绑定、用户交互、视觉呈现                     │
│  └───────┬───────┘                                                      │
│          │ DataBinding (INotifyPropertyChanged)                         │
│          ▼                                                              │
│  ┌───────────────┐                                                      │
│  │   ViewModel   │  视图模型层 - 展示逻辑，状态管理                        │
│  │ (*ViewModel)  │  职责: 命令处理、状态转换、验证协调                     │
│  └───────┬───────┘                                                      │
│          │ 依赖注入 (构造函数注入)                                        │
│          ▼                                                              │
│  ┌───────────────┐                                                      │
│  │   Service/    │  业务服务层 - 业务逻辑编排                              │
│  │  DataManager  │  职责: 业务规则、数据转换、跨Repository协调             │
│  └───────┬───────┘                                                      │
│          │ 依赖注入 (构造函数注入)                                        │
│          ▼                                                              │
│  ┌───────────────┐                                                      │
│  │  Repository   │  数据仓储层 - API调用封装                              │
│  │ (*Repository) │  职责: API调用、缓存管理、错误转换                      │
│  └───────┬───────┘                                                      │
│          │ Refit HTTP Client                                            │
│          ▼                                                              │
│  ┌───────────────┐                                                      │
│  │   API Client  │  API接口层 - Refit接口定义                             │
│  │    (I*Api)    │  职责: HTTP契约定义、请求/响应类型映射                   │
│  └───────┬───────┘                                                      │
│          │                                                              │
└──────────┼──────────────────────────────────────────────────────────────┘
           │ HTTP/HTTPS
           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        WebAPI Server                                    │
└─────────────────────────────────────────────────────────────────────────┘
```

### 数据流方向

```
[请求流] View → ViewModel → Service → Repository → API → Server
[响应流] Server → API → Repository → Service → ViewModel → View
```

### 各层职责边界

| 层 | 命名规范 | 核心职责 | 禁止行为 |
|----|---------|---------|---------|
| **API Client** | `I{Entity}Api` | 定义HTTP契约 | 包含业务逻辑 |
| **Repository** | `{Entity}Repository` | 封装API调用 | 直接操作UI |
| **Service** | `{Entity}Service` | 业务逻辑编排 | 直接调用API |
| **ViewModel** | `{Entity}ViewModel` | 展示逻辑 | 直接HTTP调用 |
| **View** | `{Entity}View` | UI呈现 | 包含业务逻辑 |

## API层标准功能矩阵

### 目标功能矩阵

每个业务实体API应具备以下标准方法（根据业务需要可选）：

| 功能类别 | 方法名 | 说明 | 适用场景 |
|---------|-------|------|---------|
| **基础CRUD** ||||
| 查询列表 | `Get{Entities}Async` | 获取实体列表 | 所有实体 |
| 查询详情 | `Get{Entity}ByIdAsync` | 获取单个实体 | 所有实体 |
| 创建 | `Create{Entity}Async` | 创建实体 | 所有实体 |
| 更新 | `Update{Entity}Async` | 更新实体 | 所有实体 |
| 删除 | `Delete{Entity}Async` | 删除实体 | 所有实体 |
| **批量操作** ||||
| 批量删除 | `BatchDeleteAsync` | 批量软删除 | 有列表选择的实体 |
| 批量启用 | `BatchEnableAsync` | 批量启用 | 有Status字段的实体 |
| 批量禁用 | `BatchDisableAsync` | 批量禁用 | 有Status字段的实体 |
| **状态管理** ||||
| 切换状态 | `ToggleStatusAsync` | 启用/禁用切换 | 有Status字段的实体 |
| 恢复 | `RestoreAsync` | 恢复已删除 | 支持软删除的实体 |
| **导入导出** ||||
| 批量导入 | `BatchImportAsync` | 批量导入数据 | 支持批量录入的实体 |
| 导出模板 | `ExportTemplateAsync` | 导出空模板 | 支持导入的实体 |
| 导出数据 | `Export{Entities}Async` | 导出实体数据 | 需要数据导出的实体 |
| **搜索** ||||
| 搜索 | `Search{Entities}Async` | 条件搜索 | 有复杂查询的实体 |

### 当前各实体功能对比（最终状态）

| 功能 | Patient | MedicalCase | Herb | Formula | User |
|-----|:-------:|:-----------:|:----:|:-------:|:----:|
| 基础CRUD | 5/5 | 5/5 | 5/5 | 5/5 | 5/5 |
| BatchDelete | Y | Y | Y | Y | Y |
| BatchEnable | N/A | N/A | Y | Y | Y |
| BatchDisable | N/A | N/A | Y | Y | Y |
| ToggleStatus | N/A | 特殊 | Y | Y | Y |
| Restore | Y | 待Server | Y | Y | Y |
| BatchImport | Y | N/A | Y | Y | Y |
| ExportTemplate | Y | N/A | Y | Y | 待Server |
| ExportData | Y | N/A | Y | Y | 待Server |
| Search | Y | Y | Y | Y | Y |

**说明**:
- `Y` = 已实现
- `N/A` = 不适用（业务不需要）
- `待Server` = Client已准备，等待Server端实现
- `特殊` = 有专用业务方法（如MedicalCase的UpdateStatus/CloseCase/Cancel等）

### 已修正问题

#### 1. 返回类型修正（6处，全部完成）

| 接口 | 方法 | 修正前 | 修正后 |
|-----|------|-------|-------|
| IPatientApi | DeletePatientAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IHerbApi | DeleteHerbAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IFormulaApi | DeleteFormulaAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IUserApi | DeleteUserAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IFormulaApi | ValidateFormulaHerbAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IUserApi | ChangePasswordAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |

#### 2. 删除重复方法（1处，已完成）

```csharp
// IMedicalCaseApi 中删除
// QueryMedicalCasesAsync - 与 SearchMedicalCasesAsync 功能重复
```

#### 3. 补充缺失功能（3处，已完成）

| 接口 | 新增方法 | 状态 |
|-----|---------|:----:|
| IFormulaApi | BatchImportAsync | Y |
| IFormulaApi | ExportTemplateAsync | Y |
| IFormulaApi | ExportFormulasAsync | Y |

#### 4. 待Server端实现后添加

| 接口 | 方法 | 说明 |
|-----|------|------|
| IMedicalCaseApi | RestoreAsync | 需Server端实现restore端点 |
| IUserApi | ExportTemplateAsync | 需Server端实现export端点 |
| IUserApi | ExportUsersAsync | 需Server端实现export端点 |

## 设计决策

### DR-1: Delete方法返回类型

**决策**: Delete方法返回 `Task<ApiResponse>` 而非 `Task<ApiResponse<T>>`

**理由**:
- Delete操作无需返回实体数据
- 仅需知道操作成功/失败
- 减少不必要的数据传输
- 与RESTful规范一致（204 No Content）

### DR-2: 保留SearchAsync而非QueryAsync

**决策**: 保留 `SearchMedicalCasesAsync`，删除 `QueryMedicalCasesAsync`

**理由**:
- Search语义更清晰（带条件筛选）
- 与其他实体API保持一致
- QueryAsync未被实际调用

### DR-3: 缺失功能按需添加

**决策**: 缺失功能根据Server端实现情况分批添加

**理由**:
- 避免Client定义了Server未实现的接口
- 分阶段验证，降低风险
- 先检查Server端，后添加Client接口

## 文件变更清单

### 需修改文件

1. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IPatientApi.cs`
2. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IHerbApi.cs`
3. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IFormulaApi.cs`
4. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs`
5. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
6. 对应的Repository实现文件

### 需验证文件（Server端）

1. `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` - Restore端点
2. `src/Server/Services/LYBT.WebAPI/Controllers/FormulaController.cs` - Import/Export端点
3. `src/Server/Services/LYBT.WebAPI/Controllers/UserController.cs` - Export端点
