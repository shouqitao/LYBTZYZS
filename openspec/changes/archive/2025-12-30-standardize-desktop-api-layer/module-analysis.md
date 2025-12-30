# Desktop API层模块功能分析

## 一、模块业务定位

| 模块 | 业务定位 | 核心特点 |
|-----|---------|---------|
| **Patient** | 患者信息管理 | 基础数据，无状态字段，仅支持软删除 |
| **MedicalCase** | 医案管理(核心) | 业务状态机(Draft/Active/Completed/Cancelled)，聚合根 |
| **Herb** | 药材库管理 | 基础数据，有Status字段(启用/禁用) |
| **Formula** | 验方库管理 | 基础数据，有Status字段，支持克隆和药材校验 |
| **User** | 系统用户管理 | 有Status字段，支持密码管理 |

---

## 二、标准功能矩阵

### 2.1 功能分类说明

| 功能类别 | 适用条件 | 说明 |
|---------|---------|------|
| **基础CRUD** | 所有实体 | 列表查询、详情、创建、更新、删除 |
| **批量操作** | 有列表选择 | 批量删除、批量启用、批量禁用 |
| **状态管理** | 有Status字段 | 切换状态、恢复已删除 |
| **导入导出** | 需批量数据 | 批量导入、导出模板、导出数据 |
| **业务特有** | 特定模块 | 模块专有的业务功能 |

### 2.2 各模块当前实现状态

#### Patient (患者)

| 功能 | 状态 | 方法名 | 说明 |
|-----|:----:|-------|------|
| **基础CRUD** |||||
| 列表查询 | Y | GetPatientsAsync | 分页+关键词 |
| 详情查询 | Y | GetPatientByIdAsync | |
| 创建 | Y | CreatePatientAsync | |
| 更新 | Y | UpdatePatientAsync | |
| 删除 | Y | DeletePatientAsync | 软删除 |
| **批量操作** |||||
| 批量删除 | Y | BatchDeleteAsync | |
| 批量启用 | N/A | - | 无Status字段 |
| 批量禁用 | N/A | - | 无Status字段 |
| **状态管理** |||||
| 切换状态 | N/A | - | 无Status字段 |
| 恢复 | Y | RestoreAsync | |
| **导入导出** |||||
| 批量导入 | Y | BatchImportAsync | |
| 导出模板 | Y | ExportTemplateAsync | |
| 导出数据 | Y | ExportPatientsAsync | |

**结论**: 功能完整，无需调整

---

#### MedicalCase (医案)

| 功能 | 状态 | 方法名 | 说明 |
|-----|:----:|-------|------|
| **基础CRUD** |||||
| 列表查询 | Y | GetMedicalCasesAsync | 分页+关键词+includeAllDoctors |
| 详情查询 | Y | GetMedicalCaseByIdAsync | |
| 详情(含关联) | Y | GetMedicalCaseByIdWithDetailsAsync | |
| 创建 | Y | CreateMedicalCaseAsync | |
| 更新(聚合) | Y | SaveAsync | 诊断+处方一次性保存 |
| 删除 | Y | DeleteMedicalCaseAsync | 返回IApiResponse |
| **批量操作** |||||
| 批量删除 | Y | BatchDeleteAsync | |
| 批量启用 | N/A | - | 医案状态是业务状态 |
| 批量禁用 | N/A | - | 医案状态是业务状态 |
| **状态管理** |||||
| 切换状态 | N/A | - | 有专用状态方法 |
| 恢复 | ✗ | - | Server端未实现 |
| **导入导出** |||||
| 批量导入 | N/A | - | 业务复杂，不支持导入 |
| 导出模板 | N/A | - | |
| 导出数据 | N/A | - | |
| **业务特有** |||||
| 患者医案列表 | Y | GetMedicalCasesByPatientIdAsync | |
| 待看诊列表 | Y | GetPendingCasesAsync | 按医生筛选 |
| 跨医案搜索 | Y | SearchMedicalCasesAsync | 分页版 |
| 患者最近医案 | Y | GetPatientRecentMedicalCasesAsync | 处方参考用 |
| 未完成医案 | Y | GetUnfinishedCaseByPatientIdAsync | 防重复开单 |
| 处方标记 | Y | SetPrescriptionFlagAsync | |
| 保存草稿 | Y | SaveDraftAsync | |
| 更新状态 | Y | UpdateStatusAsync | |
| 关闭医案 | Y | CloseCaseAsync | |
| 取消医案 | Y | CancelMedicalCaseAsync | |
| 获取权限 | Y | GetPermissionsAsync | |
| 审计日志 | Y | GetAuditLogsAsync | |

**结论**: 核心业务模块，功能完整。Restore待Server端实现后添加。

---

#### Herb (药材)

| 功能 | 状态 | 方法名 | 说明 |
|-----|:----:|-------|------|
| **基础CRUD** |||||
| 列表查询 | Y | GetHerbsAsync | 分页+关键词+分类 |
| 详情查询 | Y | GetHerbByIdAsync | |
| 创建 | Y | CreateHerbAsync | |
| 更新 | Y | UpdateHerbAsync | |
| 删除 | Y | DeleteHerbAsync | |
| **批量操作** |||||
| 批量删除 | Y | BatchDeleteAsync | |
| 批量启用 | Y | BatchEnableAsync | |
| 批量禁用 | Y | BatchDisableAsync | |
| **状态管理** |||||
| 切换状态 | Y | ToggleStatusAsync | |
| 恢复 | Y | RestoreAsync | |
| **导入导出** |||||
| 批量导入 | Y | BatchImportAsync | Multipart上传 |
| 导出模板 | Y | ExportTemplateAsync | |
| 导出数据 | Y | ExportHerbsAsync | |

**结论**: 功能完整，无需调整

---

#### Formula (验方)

| 功能 | 状态 | 方法名 | 说明 |
|-----|:----:|-------|------|
| **基础CRUD** |||||
| 列表查询 | Y | GetFormulasAsync | 分页+关键词+分类 |
| 详情查询 | Y | GetFormulaByIdAsync | |
| 创建 | Y | CreateFormulaAsync | |
| 更新 | Y | UpdateFormulaAsync | |
| 删除 | Y | DeleteFormulaAsync | |
| **批量操作** |||||
| 批量删除 | Y | BatchDeleteAsync | |
| 批量启用 | Y | BatchEnableAsync | |
| 批量禁用 | Y | BatchDisableAsync | |
| **状态管理** |||||
| 切换状态 | Y | ToggleStatusAsync | |
| 恢复 | Y | RestoreAsync | |
| **导入导出** |||||
| 批量导入 | Y | BatchImportAsync | 刚添加 |
| 导出模板 | Y | ExportTemplateAsync | 刚添加 |
| 导出数据 | Y | ExportFormulasAsync | 刚添加 |
| **业务特有** |||||
| 克隆验方 | Y | CloneFormulaAsync | |
| 待校验列表 | Y | GetPendingValidationFormulasAsync | |
| 验证药材 | Y | ValidateFormulaHerbAsync | 返回类型已修正 |

**结论**: 功能完整

---

#### User (用户)

| 功能 | 状态 | 方法名 | 说明 |
|-----|:----:|-------|------|
| **基础CRUD** |||||
| 列表查询 | Y | GetUsersAsync | 分页+关键词 |
| 详情查询 | Y | GetUserByIdAsync | |
| 创建 | Y | CreateUserAsync | |
| 更新 | Y | UpdateUserAsync | |
| 删除 | Y | DeleteUserAsync | |
| **批量操作** |||||
| 批量删除 | Y | BatchDeleteAsync | |
| 批量启用 | Y | BatchEnableAsync | |
| 批量禁用 | Y | BatchDisableAsync | |
| **状态管理** |||||
| 切换状态 | Y | ToggleStatusAsync | |
| 恢复 | Y | RestoreAsync | |
| **导入导出** |||||
| 批量导入 | Y | BatchImportAsync | |
| 导出模板 | ✗ | - | Server端未实现 |
| 导出数据 | ✗ | - | Server端未实现 |
| **业务特有** |||||
| 修改个人资料 | Y | ChangeProfileAsync | |
| 修改密码 | Y | ChangePasswordAsync | 返回类型已修正 |
| 重置密码 | Y | ResetPasswordAsync | 管理员操作 |

**结论**: 导出功能待Server端实现

---

## 三、已发现问题及修正

### 3.1 返回类型问题（已修正）

| 接口 | 方法 | 原类型 | 修正后 | 状态 |
|-----|------|-------|--------|:----:|
| IPatientApi | DeletePatientAsync | `ApiResponse<ApiResponse>` | `ApiResponse` | Y |
| IHerbApi | DeleteHerbAsync | `ApiResponse<ApiResponse>` | `ApiResponse` | Y |
| IFormulaApi | DeleteFormulaAsync | `ApiResponse<ApiResponse>` | `ApiResponse` | Y |
| IUserApi | DeleteUserAsync | `ApiResponse<ApiResponse>` | `ApiResponse` | Y |
| IFormulaApi | ValidateFormulaHerbAsync | `ApiResponse<ApiResponse>` | `ApiResponse` | Y |
| IUserApi | ChangePasswordAsync | `ApiResponse<ApiResponse>` | `ApiResponse` | Y |

### 3.2 重复方法（已删除）

| 接口 | 方法 | 说明 |
|-----|------|------|
| IMedicalCaseApi | QueryMedicalCasesAsync | 与SearchMedicalCasesAsync重复 |

### 3.3 缺失功能（待Server端）

| 接口 | 方法 | 说明 |
|-----|------|------|
| IMedicalCaseApi | RestoreAsync | Server端未实现 |
| IUserApi | ExportTemplateAsync | Server端未实现 |
| IUserApi | ExportUsersAsync | Server端未实现 |

---

## 四、功能完整性总结

| 模块 | CRUD | 批量 | 状态 | 导入导出 | 业务特有 | 总评 |
|-----|:----:|:----:|:----:|:-------:|:-------:|:----:|
| Patient | 5/5 | 1/1 | 1/1 | 3/3 | - | 完整 |
| MedicalCase | 5/5 | 1/1 | 0/1 | N/A | 12/12 | 优秀 |
| Herb | 5/5 | 3/3 | 2/2 | 3/3 | - | 完整 |
| Formula | 5/5 | 3/3 | 2/2 | 3/3 | 3/3 | 完整 |
| User | 5/5 | 3/3 | 2/2 | 1/3 | 3/3 | 待补 |

---

## 五、后续建议

1. **Server端补充**:
   - MedicalCaseController: 添加 Restore 端点
   - UsersController: 添加 ExportTemplate 和 ExportUsers 端点

2. **Client端同步**:
   - Server实现后，在对应API接口添加方法

3. **规范沉淀**:
   - 更新 client-api-conventions spec，固化功能矩阵标准
