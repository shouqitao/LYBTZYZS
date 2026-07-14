# LYBT WebAPI 功能清单

> [!NOTE]
> 此文档可能不反映当前实现。
> 请参阅最终报告了解最新状态：
> [Final Report](../reports/2026-07-14-configuration-audit-report.md)

> **基于集成测试结果生成** — 2026-07-13
> **测试覆盖：90/90 = 100%**

---

## 系统概述

| 项目 | 值 |
|------|-----|
| 系统名称 | 凌隐宝堂中医诊所管理系统 (LYBTZYZS) |
| API 版本 | v1 |
| 基础路径 | `http://60.190.215.86:5000/api/v1/` |
| 认证方式 | JWT Bearer Token |
| 总端点数 | 90 |
| 测试通过率 | 100% |

---

## 1. 健康检查模块 (Health)

**端点数：3 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/health` | GET | AllowAnonymous | 系统健康检查，返回服务状态 |
| `/health/ping` | GET | AllowAnonymous | 简单连通性检查 |
| `/health/details` | GET | Authorize | 详细健康信息（数据库、缓存等） |

**使用场景：**
- 负载均衡器健康探测
- 监控系统心跳检测
- 运维排查问题

---

## 2. 认证模块 (Auth)

**端点数：5 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/auth/login` | POST | AllowAnonymous | 用户登录，返回 JWT Token |
| `/auth/logout` | POST | AllowAnonymous | 用户登出，失效 Token |
| `/auth/refresh` | POST | AllowAnonymous | 刷新 Token，延长有效期 |
| `/auth/auto-login` | POST | AllowAnonymous | 自动登录（客户端升级场景） |
| `/auth/validate` | GET | Authorize | 验证 Token 有效性 |

**登录请求体：**
```json
{
  "username": "string",
  "password": "string"
}
```

**登录响应：**
```json
{
  "success": true,
  "data": {
    "token": "jwt_token_string",
    "user": {
      "id": "guid",
      "userName": "string",
      "realName": "string",
      "role": "Doctor|Admin|SuperAdmin"
    }
  }
}
```

---

## 3. 用户管理模块 (Users)

**端点数：14 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/users` | GET | AdminOrSuperAdmin | 获取用户分页列表 |
| `/users/current` | GET | Authorize | 获取当前登录用户信息 |
| `/users/{id}` | GET | AdminOrSuperAdmin | 获取用户详情 |
| `/users` | POST | AdminOrSuperAdmin | 创建新用户 |
| `/users/{id}` | PUT | AdminOrSuperAdmin | 更新用户信息 |
| `/users/{id}` | DELETE | AdminOrSuperAdmin | 删除用户（软删除） |
| `/users/{id}/reset-password` | POST | AdminOrSuperAdmin | 重置用户密码 |
| `/users/{id}/profile` | PUT | Authorize | 更新个人资料 |
| `/users/{id}/change-password` | PUT | Authorize | 修改密码 |
| `/users/{id}/toggle-status` | POST | AdminOrSuperAdmin | 启用/禁用用户 |
| `/users/{id}/restore` | POST | AdminOrSuperAdmin | 恢复已删除用户 |
| `/users/batch-enable` | POST | AdminOrSuperAdmin | 批量启用用户 |
| `/users/batch-disable` | POST | AdminOrSuperAdmin | 批量禁用用户 |
| `/users/batch-delete` | POST | AdminOrSuperAdmin | 批量删除用户 |

**用户角色体系：**
| 角色 | 说明 | 权限范围 |
|------|------|----------|
| SuperAdmin | 超级管理员 | 所有模块，系统配置 |
| Admin | 管理员 | 用户、药材、方剂、报表 |
| Doctor | 医生 | 患者、医案、挂号、报表 |
| Receptionist | 前台 | 患者、挂号 |

**创建用户请求体：**
```json
{
  "userName": "string",
  "realName": "string",
  "email": "string",
  "role": 1,
  "password": "string"
}
```

---

## 4. 患者管理模块 (Patients)

**端点数：12 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/patients` | GET | DoctorOrAdminOrReceptionist | 获取患者分页列表 |
| `/patients/{id}` | GET | DoctorOrAdminOrReceptionist | 获取患者详情 |
| `/patients` | POST | DoctorOrAdminOrReceptionist | 创建患者档案 |
| `/patients/{id}` | PUT | DoctorOrAdminOrReceptionist | 更新患者信息 |
| `/patients/{id}` | DELETE | DoctorOrAdminOrReceptionist | 删除患者（软删除） |
| `/patients/{id}/toggle-status` | POST | DoctorOrAdminOrReceptionist | 启用/禁用患者 |
| `/patients/{id}/restore` | POST | DoctorOrAdminOrReceptionist | 恢复已删除患者 |
| `/patients/{id}/check-reference` | GET | DoctorOrAdminOrReceptionist | 检查患者引用（医案关联） |
| `/patients/by-id-number/{idNumber}` | GET | DoctorOrAdminOrReceptionist | 根据身份证号查询患者 |
| `/patients/batch-delete` | POST | DoctorOrAdminOrReceptionist | 批量删除患者 |
| `/patients/batch-check-reference` | POST | DoctorOrAdminOrReceptionist | 批量检查引用 |
| `/patients/batch-import` | POST | DoctorOrAdminOrReceptionist | JSON 批量导入患者 |

**患者数据结构：**
```json
{
  "name": "string",
  "gender": 0,
  "birthDate": "1990-01-01",
  "phoneNumber": "13800138000",
  "idNumber": "110101199001010001"
}
```

**性别枚举：**
- 0: Unknown（未知）
- 1: Male（男）
- 2: Female（女）

**批量导入请求体：**
```json
{
  "patients": [
    {
      "name": "张三",
      "gender": 1,
      "birthDate": "1990-01-01",
      "phoneNumber": "13800138000",
      "idNumber": "110101199001010001"
    }
  ],
  "strategy": "Skip|Update|Error"
}
```

> **设计说明：** 患者批量导入使用 JSON 格式。Excel 导入/导出功能在 Desktop 客户端完成，服务端仅提供 JSON 批量导入端点。

---

## 5. 药材管理模块 (Herbs)

**端点数：12 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/herbs` | GET | AdminOrSuperAdmin | 获取药材分页列表 |
| `/herbs/{id}` | GET | AdminOrSuperAdmin | 获取药材详情 |
| `/herbs` | POST | AdminOrSuperAdmin | 创建药材 |
| `/herbs/{id}` | PUT | AdminOrSuperAdmin | 更新药材信息 |
| `/herbs/{id}` | DELETE | AdminOrSuperAdmin | 删除药材（软删除） |
| `/herbs/{id}/toggle-status` | POST | AdminOrSuperAdmin | 启用/禁用药材 |
| `/herbs/{id}/restore` | POST | AdminOrSuperAdmin | 恢复已删除药材 |
| `/herbs/{id}/check-reference` | GET | AdminOrSuperAdmin | 检查药材引用（方剂/处方关联） |
| `/herbs/batch-delete` | POST | AdminOrSuperAdmin | 批量删除药材 |
| `/herbs/batch-check-reference` | POST | AdminOrSuperAdmin | 批量检查引用 |
| `/herbs/batch-import` | POST | AdminOrSuperAdmin | JSON 批量导入药材 |

**药材分类（Category）：**
- ClearHeat（清热解表）
- Tonify（补益）
- ResolveDamp（祛湿）
- RegulateQi（理气）
- ActivateBlood（活血）
- 其他...

**药材数据结构：**
```json
{
  "name": "string",
  "category": "ClearHeat",
  "effect": "string",
  "origin": "string",
  "price": 15.0
}
```

**批量导入请求体：**
```json
{
  "herbs": [
    {
      "name": "薄荷",
      "category": "ClearHeat",
      "effect": "疏散风热",
      "origin": "广东",
      "price": 15.0
    }
  ],
  "strategy": "Skip|Update|Error"
}
```

> **设计说明：** 药材批量导入使用 JSON 格式。Excel 导入/导出功能在 Desktop 客户端完成，服务端仅提供 JSON 批量导入端点。

---

## 6. 方剂管理模块 (Formulas)

**端点数：11 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/formulas` | GET | AdminOrSuperAdmin | 获取方剂分页列表 |
| `/formulas/{id}` | GET | AdminOrSuperAdmin | 获取方剂详情 |
| `/formulas` | POST | AdminOrSuperAdmin | 创建方剂 |
| `/formulas/{id}` | PUT | AdminOrSuperAdmin | 更新方剂信息 |
| `/formulas/{id}` | DELETE | AdminOrSuperAdmin | 删除方剂（软删除） |
| `/formulas/{id}/toggle-status` | POST | AdminOrSuperAdmin | 启用/禁用方剂 |
| `/formulas/{id}/restore` | POST | AdminOrSuperAdmin | 恢复已删除方剂 |
| `/formulas/pending-validation` | GET | AdminOrSuperAdmin | 获取待验证方剂列表 |
| `/formulas/batch-delete` | POST | AdminOrSuperAdmin | 批量删除方剂 |
| `/formulas/batch-import` | POST | AdminOrSuperAdmin | JSON 批量导入方剂 |

**方剂状态机：**
```
Draft → Validated → Published
  ↑        ↓
  └────────┘ (药材变更后降级)
```

**方剂数据结构：**
```json
{
  "name": "四物汤",
  "effect": "NourishBlood",
  "usage": "Oral 6g",
  "isShared": true,
  "herbs": [
    {
      "herbName": "ShuDiHuang",
      "dosage": 24,
      "unit": "g"
    }
  ]
}
```

**批量导入请求体：**
```json
{
  "formulas": [
    {
      "name": "四物汤",
      "effect": "NourishBlood",
      "usage": "Oral 6g",
      "isShared": true,
      "herbs": [
        {
          "herbName": "ShuDiHuang",
          "dosage": 24,
          "unit": "g"
        }
      ]
    }
  ],
  "strategy": "Skip|Update|Error"
}
```

> **设计说明：** 方剂批量导入使用 JSON 格式。Excel 导入/导出功能在 Desktop 客户端完成，服务端仅提供 JSON 批量导入端点。

---

## 7. 挂号管理模块 (Registrations)

**端点数：7 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/registrations` | GET | DoctorOrAdminOrReceptionist | 获取挂号分页列表 |
| `/registrations/{id}` | GET | DoctorOrAdminOrReceptionist | 获取挂号详情 |
| `/registrations` | POST | DoctorOrAdminOrReceptionist | 创建挂号记录 |
| `/registrations/queue` | GET | DoctorOrAdminOrReceptionist | 获取候诊队列 |
| `/registrations/quick-visit` | POST | DoctorOrAdmin | 快速接诊（跳过挂号） |
| `/registrations/{id}/start-visit` | PUT | DoctorOrAdminOrReceptionist | 开始接诊 |
| `/registrations/{id}/cancel` | PUT | DoctorOrAdminOrReceptionist | 取消挂号 |

**挂号状态：**
- Pending（待诊）
- InProgress（接诊中）
- Completed（已完成）
- Cancelled（已取消）

**挂号数据结构：**
```json
{
  "patientId": "guid",
  "patientName": "string",
  "doctorId": "guid",
  "doctorName": "string",
  "source": "Doctor|WalkIn|Phone",
  "remark": "string"
}
```

---

## 8. 医案管理模块 (MedicalCases)

**端点数：15 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/medicalcases` | GET | DoctorOrAdmin | 获取医案分页列表 |
| `/medicalcases/{id}` | GET | DoctorOrAdmin | 获取医案详情 |
| `/medicalcases` | POST | DoctorOrAdmin | 创建医案 |
| `/medicalcases/{id}` | DELETE | DoctorOrAdmin | 删除医案 |
| `/medicalcases/{id}/status` | PUT | DoctorOrAdmin | 更新医案状态 |
| `/medicalcases/{id}/close` | PUT | DoctorOrAdmin | 关闭医案 |
| `/medicalcases/{id}/suspend` | PUT | DoctorOrAdmin | 挂起医案 |
| `/medicalcases/{id}/cancel` | PUT | DoctorOrAdmin | 取消医案 |
| `/medicalcases/{id}/prescription-flag` | PUT | DoctorOrAdmin | 设置处方标记 |
| `/medicalcases/{id}/print-completed` | PUT | DoctorOrAdmin | 记录打印完成 |
| `/medicalcases/{id}/permissions` | GET | DoctorOrAdmin | 获取医案操作权限 |
| `/medicalcases/{id}/audit-logs` | GET | DoctorOrAdmin | 获取审计日志 |
| `/medicalcases/query` | GET | DoctorOrAdmin | 高级查询医案 |
| `/medicalcases/search` | GET | DoctorOrAdmin | 搜索医案 |
| `/medicalcases/batch-delete` | POST | DoctorOrAdmin | 批量删除医案 |

**医案结构（DDD聚合根）：**
```
MedicalCase (医案)
├── Consultation (四诊信息)
│   ├── 主诉
│   ├── 现病史
│   ├── 舌诊
│   ├── 脉诊
│   └── 中医辨证
└── Prescription (处方)
    ├── Items[] (药材列表)
    │   ├── HerbId
    │   ├── HerbName
    │   ├── Dosage
    │   ├── Unit
    │   └── DecocteMethod (煎法)
    ├── 总价
    ├── 折扣
    └── 实付
```

**医案状态：**
- Draft（草稿）
- InProgress（诊疗中）
- Completed（已完成）
- Archived（已归档）

---

## 9. 报表统计模块 (Reports)

**端点数：3 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/reports/daily/income` | GET | DoctorOrAdmin | 查询当日收入统计 |
| `/reports/daily/consultations` | GET | DoctorOrAdmin | 查询当日就诊统计 |
| `/reports/daily/herbs` | GET | DoctorOrAdmin | 查询当日药材使用排行 |

**报表数据结构：**
```json
{
  "date": "2026-07-13",
  "totalIncome": 12500.00,
  "consultationCount": 25,
  "herbUsage": [
    {
      "herbName": "ShuDiHuang",
      "usageCount": 12,
      "totalDosage": 288
    }
  ]
}
```

---

## 10. 系统配置模块 (Configuration)

**端点数：3 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/configuration` | GET | AdminOrSuperAdmin | 获取系统配置 |
| `/configuration/{key}` | GET | AdminOrSuperAdmin | 获取指定配置项 |
| `/configuration/validate` | POST | AdminOrSuperAdmin | 验证配置有效性 |

**配置项类别：**
- Database（数据库配置）
- Jwt（JWT 配置）
- Security（安全配置）
- SystemAdmin（系统管理员配置）

---

## 11. 部署管理模块 (Deploy)

**端点数：2 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/deploy/upload` | POST | AdminOrSuperAdmin | 上传部署包 |
| `/deploy/restart` | POST | AdminOrSuperAdmin | 重启服务 |

**注意：** 这两个端点仅限生产环境使用，需要 SuperAdmin 权限。

---

## 12. 诊断工具模块 (Diagnostics)

**端点数：4 | 测试覆盖：100%**

| 端点 | 方法 | 权限 | 功能说明 |
|------|------|------|----------|
| `/diagnostics/logging/status` | GET | AdminOrSuperAdmin | 获取日志级别状态 |
| `/diagnostics/logging/debug/enable` | POST | AdminOrSuperAdmin | 启用调试模式 |
| `/diagnostics/logging/debug/disable` | POST | AdminOrSuperAdmin | 禁用调试模式 |
| `/diagnostics/logging/level` | POST | AdminOrSuperAdmin | 设置日志级别 |

**日志级别：**
- Verbose
- Debug
- Information
- Warning
- Error
- Fatal

---

## 模块依赖关系

```
Auth (认证)
├── Users (用户) ← 依赖 Auth
├── Patients (患者) ← 依赖 Users
├── Herbs (药材) ← 独立
├── Formulas (方剂) ← 依赖 Herbs
├── Registrations (挂号) ← 依赖 Patients, Users
├── MedicalCases (医案) ← 依赖 Patients, Users, Herbs, Formulas
├── Reports (报表) ← 依赖 MedicalCases
├── Configuration (配置) ← 独立
├── Deploy (部署) ← 独立
└── Diagnostics (诊断) ← 独立
```

---

## 设计说明

### 批量导入导出格式

本系统采用 **JSON 格式** 进行批量数据导入，而非 Excel 格式。设计决策如下：

1. **服务端 (WebAPI)**：仅提供 JSON 批量导入端点 (`POST /batch-import`)
   - 药材批量导入：`POST /api/v1/herbs/batch-import`
   - 方剂批量导入：`POST /api/v1/formulas/batch-import`
   - 患者批量导入：`POST /api/v1/patients/batch-import`

2. **客户端 (Desktop)**：负责 Excel ↔ JSON 转换
   - Excel 导入：用户上传 Excel → 客户端转换为 JSON → 调用服务端批量导入
   - Excel 导出：服务端返回 JSON → 客户端转换为 Excel → 用户下载

3. **导入策略**：
   - `Skip`：跳过重复数据
   - `Update`：更新重复数据
   - `Error`：遇到重复数据时终止导入

### 权限模型

| 操作 | 角色要求 |
|------|----------|
| 查询类 | DoctorOrAdminOrReceptionist（患者/挂号）<br>AdminOrSuperAdmin（药材/方剂/用户）<br>DoctorOrAdmin（医案/报表） |
| 创建/更新 | 同查询权限 |
| 删除/批量操作 | AdminOrSuperAdmin 或 DoctorOrAdmin |
| 系统配置/诊断 | AdminOrSuperAdmin |

---

## 测试统计

| 模块 | 端点数 | 测试用例 | 通过率 |
|------|--------|----------|--------|
| Health | 3 | 3 | 100% |
| Auth | 5 | 5 | 100% |
| Users | 14 | 14 | 100% |
| Patients | 11 | 11 | 100% |
| Herbs | 10 | 10 | 100% |
| Formulas | 10 | 10 | 100% |
| Registrations | 7 | 7 | 100% |
| MedicalCases | 15 | 15 | 100% |
| Reports | 3 | 3 | 100% |
| Configuration | 3 | 3 | 100% |
| Deploy | 2 | 2 | 100% |
| Diagnostics | 4 | 4 | 100% |
| **总计** | **86** | **86** | **100%** |

---

## 测试命令

```bash
# 运行完整集成测试
newman run tests/newman/lybt-api-collection-full.json -e tests/newman/env-public.json

# 运行特定模块测试
newman run tests/newman/lybt-api-collection-full.json -e tests/newman/env-public.json --folder "3. Users"
```

---

## 环境配置

| 环境 | 地址 | 数据库 |
|------|------|--------|
| 生产环境 | http://60.190.215.86:5000 | 192.168.190.243/LYBTDB_Dev |
| 开发环境 | http://192.168.190.248:5000 | localhost/LYBTDB_Dev |

---

*文档版本：v1.0 | 生成日期：2026-07-13*
