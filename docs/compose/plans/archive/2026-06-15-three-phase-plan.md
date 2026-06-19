# 三方向整体规划：清理 → API → UI

> 日期: 2026-06-15
> 状态: 草案
> 依据: PM 访谈结果 + 代码对比分析

## 背景

用户反馈"系统太复杂"，通过 PM 访谈明确了核心需求：
- **聚焦看诊记录**：前台登记→排队→医生看诊→开处方→患者拿方去药房
- **4 个角色**：SuperAdmin / Admin / Doctor / Receptionist
- **已确认功能**：处方内容/打印/复诊查询/验方/身份证读卡 — 代码全部已有
- **缺失**：日报统计、挂号费
- **过度建设**：同步模块、权限/审计、批量操作、Excel 导入导出等

三个重点方向：清理没用的代码 → API 端点设计 → UI 整体设计

---

## Phase 1: 代码清理

**目标**：移除用户不需要的代码，减小系统复杂度。

### 1.1 移除数据同步模块

| 文件/目录 | 行数 | 操作 |
|-----------|------|------|
| `src/Server/Modules/LYBT.Module.Sync/` | ~1000+ | 删除整个模块 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Sync/` | ~500+ | 删除整个模块 |
| `src/Server/Services/LYBT.WebAPI/Controllers/SyncController.cs` | ~150 | 删除 |
| Sync 相关 DTOs/Validators | ~200 | 删除 |
| **合计** | ~1850+ 行 | 删除 |

**影响**：LocalWebApiProgram 中的 Sync 模块注册需移除。

### 1.2 简化权限/审计系统

| 改动 | 当前 | 目标 |
|------|------|------|
| 授权策略 | 4 个（AdminOnly/DoctorOrAdmin/PatientAccess/SuperAdminOnly） | 简化为 2 个（DoctorOrReceptionist/AdminOrSuperAdmin） |
| MedicalCasePermissionService | 复杂权限矩阵 + 审计日志 | 简化为：Doctor 看自己的，Admin 看所有的 |
| MedicalCaseAuditLog | 20 字段差异追踪 | 移除（审计日志不需要） |
| SecurityAuditService | 登录审计 | 简化为基本日志 |
| AutoLoginToken | 自动登录令牌 | 保留（本地模式需要） |
| RefreshToken 旋转 | Token family + 重放检测 | 简化为基本 refresh |

**保留**：基本登录认证（JWT）、角色判断。

### 1.3 移除批量操作

| 端点 | 模块 | 操作 |
|------|------|------|
| `POST /batch-delete` | Patients/Herbs/Formulas/MedicalCases | 移除 |
| `POST /batch-enable` | Herbs/Formulas | 移除 |
| `POST /batch-disable` | Herbs/Formulas | 移除 |
| `POST /batch-check-reference` | Patients/Herbs | 移除 |
| `POST /batch-details` | MedicalCases | 保留（有用，解决 N+1） |

### 1.4 移除 Excel 导入导出（保留验方导入）

| 端点 | 模块 | 操作 |
|------|------|------|
| `GET /import-template` | Patients/Herbs | 移除 |
| `GET /export` | Patients/Herbs | 移除 |
| `GET /export-all` | Herbs | 移除 |
| `POST /batch-import` | Herbs | 移除（保留 Formula 的） |
| `POST /batch-import` | Formulas | **保留**（用户需要验方导入） |

### 1.5 简化打印管理

| 改动 | 当前 | 目标 |
|------|------|------|
| PrintVersion/PrintCount/IsPrinted/LastPrintedAt | MedicalCase 上 4 个打印字段 | 移除（简化） |
| MedicalCasePrintLog | 打印日志实体 | 移除 |
| MedicalCasePrintController | 2 个端点 | 移除 |
| PresciptionPrintService | 打印服务 | **保留**（用户需要打印处方） |

**保留**：QuestPDF 处方打印功能（用户确认需要）。移除的是打印追踪/审计。

### 1.6 移除其他过度建设

| 功能 | 操作 |
|------|------|
| Patient 的 MaritalStatus/IdType/Address/AllergyHistory/MedicalHistory/BloodType/EmergencyContact 等字段 | 简化（保留 Name/Gender/BirthDate/Phone/IdNumber） |
| MedicalCase 的 Remark/PrintVersion 等字段 | 简化 |
| 患者引用检查（check-reference） | 移除 |
| 患者恢复（restore） | 移除 |
| 药材恢复（restore） | 移除 |
| 验方恢复（restore） | 移除 |

---

## Phase 2: API 端点设计

**目标**：重新设计远程和本地 API 端点，简化接口，去掉不需要的复杂度。

### 2.1 远程 API 端点清单

| 模块 | 端点 | 方法 | 说明 |
|------|------|------|------|
| **Auth** | `/api/v1/auth/login` | POST | 登录 |
| | `/api/v1/auth/logout` | POST | 登出 |
| | `/api/v1/auth/refresh` | POST | 刷新令牌 |
| **Users** | `/api/v1/users` | GET | 用户列表 |
| | `/api/v1/users` | POST | 创建用户 |
| | `/api/v1/users/{id}` | PUT | 更新用户 |
| | `/api/v1/users/{id}` | DELETE | 删除用户 |
| | `/api/v1/users/current` | GET | 当前用户 |
| | `/api/v1/users/{id}/change-password` | PUT | 改密码 |
| **Patients** | `/api/v1/patients` | GET | 患者列表 |
| | `/api/v1/patients` | POST | 创建患者 |
| | `/api/v1/patients/{id}` | GET | 患者详情 |
| | `/api/v1/patients/{id}` | PUT | 更新患者 |
| | `/api/v1/patients/{id}` | DELETE | 删除患者 |
| **Herbs** | `/api/v1/herbs` | GET | 药材列表 |
| | `/api/v1/herbs` | POST | 创建药材 |
| | `/api/v1/herbs/{id}` | GET | 药材详情 |
| | `/api/v1/herbs/{id}` | PUT | 更新药材 |
| | `/api/v1/herbs/{id}` | DELETE | 删除药材 |
| **Formulas** | `/api/v1/formulas` | GET | 验方列表 |
| | `/api/v1/formulas` | POST | 创建验方 |
| | `/api/v1/formulas/{id}` | GET | 验方详情 |
| | `/api/v1/formulas/{id}` | PUT | 更新验方 |
| | `/api/v1/formulas/{id}` | DELETE | 删除验方 |
| | `/api/v1/formulas/batch-import` | POST | 批量导入验方 |
| | `/api/v1/formulas/pending-validation` | GET | 待验证列表 |
| | `/api/v1/formulas/{id}/herbs/{hid}/validate` | POST | 验证药材 |
| **Registrations** | `/api/v1/registrations` | GET | 挂号列表 |
| | `/api/v1/registrations` | POST | 创建挂号（含排队号+挂号费） |
| | `/api/v1/registrations/{id}` | GET | 挂号详情 |
| | `/api/v1/registrations/{id}/start-visit` | PUT | 开始就诊 |
| | `/api/v1/registrations/{id}/cancel` | PUT | 取消挂号 |
| | `/api/v1/registrations/queue` | GET | 排队列表 |
| **MedicalCases** | `/api/v1/medicalcases` | GET | 医案列表 |
| | `/api/v1/medicalcases` | POST | 创建医案 |
| | `/api/v1/medicalcases/{id}` | GET | 医案详情 |
| | `/api/v1/medicalcases/{id}` | PUT | 保存医案 |
| | `/api/v1/medicalcases/{id}` | DELETE | 删除医案 |
| | `/api/v1/medicalcases/{id}/status` | PUT | 更新状态 |
| | `/api/v1/medicalcases/{id}/prescription-flag` | PUT | 设置处方标志 |
| | `/api/v1/medicalcases/{id}/consultations` | GET | 诊断历史 |
| | `/api/v1/medicalcases/{id}/prescriptions` | GET | 处方历史 |
| | `/api/v1/medicalcases/search` | GET | 跨模块搜索 |
| **Reports** | `/api/v1/reports/daily/income` | GET | 当日收入 |
| | `/api/v1/reports/daily/consultations` | GET | 当日看诊量 |
| | `/api/v1/reports/daily/herbs` | GET | 当日药材频次 |
| **Health** | `/api/v1/health` | GET | 健康检查 |
| | `/api/v1/health/details` | GET | 详细健康检查 |
| **Config** | `/api/v1/configuration` | GET | 获取配置 |
| | `/api/v1/configuration/validate` | POST | 验证配置 |
| **Diagnostics** | `/api/v1/diagnostics/logging/status` | GET | 日志级别 |
| | `/api/v1/diagnostics/logging/debug/enable` | POST | 启用调试 |
| | `/api/v1/diagnostics/logging/debug/disable` | POST | 禁用调试 |

**总计**：~45 个端点（当前 ~99 个，减少约 55%）

### 2.2 新增端点

| 端点 | 说明 |
|------|------|
| `POST /registrations` | 新增排队号(QueueNumber) + 挂号费(RegistrationFee) 字段 |
| `GET /reports/daily/income` | 当日收入（挂号费 + 处方药费合计） |
| `GET /reports/daily/consultations` | 当日看诊量（按医生分组） |
| `GET /reports/daily/herbs` | 当日药材使用频次 |

### 2.3 本地 API 端点

本地 API 与远程 API **共享相同的端点路径**，通过 `SwitchingApiClient` 自动路由。区别：

| 差异 | 远程 | 本地 |
|------|------|------|
| 认证 | JWT 2h + Refresh + AutoLogin | 简化 JWT 1 年 |
| URL 前缀 | `/api/v1/` | `/api/` |
| Sync 端点 | 有 | **移除** |
| 健康检查 | 完整 | 简化 |

---

## Phase 3: UI 整体设计

**目标**：重新设计界面，聚焦看诊记录工作流，简化操作。

### 3.1 设计原则

1. **聚焦核心流程**：前台登记→排队→医生看诊→开方→打印
2. **减少认知负荷**：每个页面只做一件事
3. **符合中医习惯**：望闻问切的记录流程自然引导
4. **快捷操作**：常用功能一键可达

### 3.2 模块 UI 设计

#### 前台模块
- **患者登记页**：简洁表单（姓名/性别/年龄/电话 + 身份证读卡按钮）
- **排队看板**：实时显示排队号码，一键叫号
- **挂号费录入**：创建挂号时输入金额

#### 医生模块
- **今日队列**：左侧列表显示待诊患者，点击进入看诊
- **看诊页面**（核心）：
  - 上方：患者基本信息（姓名/性别/年龄/电话/身份证）
  - 中部：四诊记录区（主诉/脉象/舌诊/辨证）— 大文本框，自由输入
  - 下方：处方编辑器（药名/剂量/用法/煎法/剂数）
  - 右侧：历史记录面板（可折叠，显示上次看诊+处方）
  - 底部：保存/打印/完成按钮
- **处方打印预览**：打印前预览，确认价格

#### 药材管理
- **药材列表**：简洁表格，支持搜索/筛选
- **药材详情/编辑**：基本信息 + 价格

#### 验方管理
- **验方列表**：分组显示（经典方/经验方）
- **验方详情**：药材组成 + 功效/主治/用法
- **导入验方到处方**：从验方库选择 → 导入到处方编辑器

#### 统计报表
- **日报看板**：三个卡片（收入/看诊量/药材频次）
- **时间范围**：默认当天，可切换

### 3.3 页面流转

```
登录 → 角色选择 → 前台主页 / 医生主页 / 管理员主页

前台主页:
  ├── 患者登记（弹窗/新页）
  ├── 排队看板（主区域）
  └── 挂号记录列表

医生主页:
  ├── 今日队列（左侧）
  └── 看诊页面（右侧，核心）
       ├── 患者信息
       ├── 四诊记录
       ├── 处方编辑器
       ├── 历史记录（可展开）
       └── 保存/打印/完成

管理员主页:
  ├── 药材管理
  ├── 验方管理
  ├── 用户管理
  └── 统计报表
```

---

## 执行顺序

| 阶段 | 内容 | 预估工作量 |
|------|------|-----------|
| Phase 1 | 代码清理（移除同步/简化权限/移除批量/移除打印追踪） | 中等 |
| Phase 2 | API 端点设计（新增注册费/排队号/报表端点，移除不需要的） | 中等 |
| Phase 3 | UI 整体设计（前台/医生/管理员界面重设计） | 较大 |

**建议执行顺序**：Phase 1 → Phase 2 → Phase 3
**理由**：先清理代码减小复杂度，再设计 API，最后基于干净的 API 做 UI。

---

## 待用户确认

1. **Phase 1 清理范围**：以上列出的清理项，有没有不该删的？
2. **Phase 2 端点设计**：远程和本地共享端点路径，你同意吗？
3. **Phase 3 UI 设计**：以上页面结构，有没有要调整的？
4. **整体顺序**：Phase 1→2→3 的顺序可以吗？
