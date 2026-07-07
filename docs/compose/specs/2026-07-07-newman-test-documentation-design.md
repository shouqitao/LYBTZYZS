# Newman 测试说明文档设计

## [S1] Problem

当前项目已有两套 Newman/Postman 测试集合：
- `tests/newman/lybt-api-collection.json`：Remote WebAPI 测试（~25 请求，基础 happy-path）
- `tests/postman/local-api-tests.postman_collection.json`：LocalWebAPI 测试（~97 请求，~310 断言）

缺少一份**结构化的测试说明文档**，需要：
1. 按角色顺序（SysAdmin → Admin → Receptionist → Doctor）描述完整测试流程
2. 每个端点覆盖 happy-path + negative/failure 场景
3. 覆盖所有 API 端点和权限边界
4. 作为 Newman 测试集合的设计蓝图

## [S2] Solution Overview

按角色顺序组织测试流程，每个端点均包含 happy-path 和 negative 场景：

### 测试执行顺序

```
Phase 1: SysAdmin（系统运维）
  ├─ 登录认证（6 场景：成功/密码错误/用户不存在/token验证/token刷新/登出）
  ├─ 用户管理-创建Admin（10 场景：成功+9种失败）
  ├─ 用户管理-CRUD（成功/更新/删除/列表）
  ├─ 系统配置
  ├─ 系统诊断
  ├─ 部署管理
  └─ 权限边界（SysAdmin不能访问herbs/formulas）

Phase 2: Admin（管理员）
  ├─ 登录认证
  ├─ 用户管理（创建Doctor/Receptionist + 失败场景）
  ├─ 患者管理（CRUD + 6种失败场景）
  ├─ 医案管理（CRUD + 失败场景）
  ├─ 挂号管理（CRUD + 失败场景）
  ├─ 报表统计
  └─ 权限边界（Admin不能访问herbs/formulas）

Phase 3: Receptionist（前台接待）
  ├─ 登录认证
  ├─ 患者管理（CRUD + 失败场景）
  ├─ 挂号管理（CRUD + 失败场景）
  └─ 权限边界（7个受限端点）

Phase 4: Doctor（医生）
  ├─ 登录认证
  ├─ 患者管理（CRUD + 失败场景）
  ├─ 药材管理（CRUD + 11种失败场景）
  ├─ 验方管理（CRUD + 8种失败场景）
  ├─ 医案管理（完整流程 + 10种失败场景）
  ├─ 挂号管理（CRUD + 6种失败场景）
  ├─ 报表统计
  └─ 权限边界（4个受限端点）
```

### 角色权限矩阵

| 端点 | SysAdmin | Admin | Doctor | Receptionist |
|------|----------|-------|--------|--------------|
| Auth | ✅ | ✅ | ✅ | ✅ |
| Users CRUD | ✅ | ✅ | ❌ | ❌ |
| Patients CRUD | ✅ | ✅ | ✅ | ✅ |
| Herbs CRUD | ❌ | ❌ | ✅ | ✅ |
| Formulas CRUD | ❌ | ❌ | ✅ | ✅ |
| MedicalCases | ✅ | ✅ | ✅ | ❌ |
| Registrations | ✅ | ✅ | ✅ | ✅ |
| Reports | ✅ | ✅ | ✅ | ❌ |
| Configuration | ✅ | ✅ | ❌ | ❌ |
| Diagnostics | ✅ | ✅ | ❌ | ❌ |
| Deploy | ✅ | ✅ | ❌ | ❌ |

---

## [S3] Phase 1: SysAdmin 测试场景

### 1.1 登录认证

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.1.1 | 登录成功 | `POST /api/v1/auth/login` `{username:"sysadmin", password:"SysAdmin@2026!"}` | 200, 返回 token |
| 1.1.2 | 密码错误 | `POST /api/v1/auth/login` `{username:"sysadmin", password:"wrong"}` | 401 |
| 1.1.3 | 用户不存在 | `POST /api/v1/auth/login` `{username:"nonexistent", password:"xxx"}` | 401 |
| 1.1.4 | 空用户名 | `POST /api/v1/auth/login` `{username:"", password:"SysAdmin@2026!"}` | 400, 验证错误 |
| 1.1.5 | 空密码 | `POST /api/v1/auth/login` `{username:"sysadmin", password:""}` | 400, 验证错误 |
| 1.1.6 | Token 验证 | `GET /api/v1/auth/validate` (Bearer token) | 200, isValid=true |
| 1.1.7 | Token 验证-无token | `GET /api/v1/auth/validate` (无 Authorization) | 401 |
| 1.1.8 | Token 刷新 | `POST /api/v1/auth/refresh` `{userName:"sysadmin"}` (Bearer token) | 200 |
| 1.1.9 | Token 刷新-无token | `POST /api/v1/auth/refresh` `{userName:"sysadmin"}` (空 Authorization) | 401 |
| 1.1.10 | 登出 | `POST /api/v1/auth/logout` `{userName:"sysadmin"}` | 200 |

### 1.2 用户管理 - 创建 Admin 账户

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.2.1 | 创建成功 | `POST /api/v1/users` `{userName:"newadmin", realName:"测试管理员", email:"test@lybt.com", password:"Admin@123456", role:1}` | 201, 返回 userId |
| 1.2.2 | 获取详情 | `GET /api/v1/users/{id}` | 200 |
| 1.2.3 | 更新信息 | `PUT /api/v1/users/{id}` `{realName:"更新后管理员"}` | 200 |
| 1.2.4 | 切换状态 | `POST /api/v1/users/{id}/toggle-status` | 200 |
| 1.2.5 | 重置密码 | `POST /api/v1/users/{id}/reset-password` | 200 |
| 1.2.6 | 获取列表 | `GET /api/v1/users?page=1&pageSize=10` | 200 |
| 1.2.7 | 修改密码 | `PUT /api/v1/users/{id}/change-password` `{oldPassword:"Admin@123456", newPassword:"Admin@789012"}` | 200 |
| 1.2.8 | 恢复用户 | `POST /api/v1/users/{id}/restore` | 200 |

#### Negative - 密码复杂度

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.2.9 | 无大写字母 | `{password:"admin123!"}` | 400, 密码复杂度 |
| 1.2.10 | 无小写字母 | `{password:"ADMIN123!"}` | 400, 密码复杂度 |
| 1.2.11 | 无数字 | `{password:"AdminAdmin!"}` | 400, 密码复杂度 |
| 1.2.12 | 太短(7位) | `{password:"Ab1!234"}` | 400, 密码长度不足 |
| 1.2.13 | 空密码 | `{password:""}` | 400 |

#### Negative - 用户名/字段验证

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.2.14 | 用户名已存在 | `{userName:"admin", ...}` | 400/409 |
| 1.2.15 | 用户名太短(2位) | `{userName:"ab", ...}` | 400, 验证错误 |
| 1.2.16 | 用户名含特殊字符 | `{userName:"test@user", ...}` | 400, 格式错误 |
| 1.2.17 | 保留用户名(admin) | `{userName:"administrator", ...}` | 400 |
| 1.2.18 | 缺少realName | `{userName:"test99", password:"..."}` | 400 |
| 1.2.19 | 缺少role | `{userName:"test99", realName:"...", password:"..."}` | 400 |
| 1.2.20 | 邮箱格式错误 | `{email:"not-an-email", ...}` | 400 |
| 1.2.21 | 手机号格式错误 | `{phoneNumber:"abc", ...}` | 400 |

#### Negative - 不存在/禁止操作

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.2.22 | 获取不存在的用户 | `GET /api/v1/users/00000000-0000-0000-0000-000000000999` | 404 |
| 1.2.23 | 更新不存在的用户 | `PUT /api/v1/users/00000000-...` `{realName:"x"}` | 404 |
| 1.2.24 | 删除不存在的用户 | `DELETE /api/v1/users/00000000-...` | 404 |
| 1.2.25 | 删除sysadmin | `DELETE /api/v1/users/{sysadminId}` | 400, 禁止删除 |
| 1.2.26 | 禁用sysadmin | `POST /api/v1/users/{sysadminId}/toggle-status` | 400, 禁止禁用 |
| 1.2.27 | 修改sysadmin角色 | `PUT /api/v1/users/{sysadminId}` `{role:2}` | 400, 禁止改角色 |
| 1.2.28 | 修改密码-旧密码错误 | `PUT /api/v1/users/{id}/change-password` `{oldPassword:"wrong", newPassword:"..."}` | 400 |
| 1.2.29 | 修改密码-新密码太短 | `PUT /api/v1/users/{id}/change-password` `{oldPassword:"...", newPassword:"Ab1!"}` | 400 |

### 1.3 系统配置

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.3.1 | 获取配置 | `GET /api/v1/configuration` | 200 |
| 1.3.2 | 更新配置 | `PUT /api/v1/configuration` `{...}` | 200 |

### 1.4 系统诊断

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.4.1 | 数据库信息 | `GET /api/v1/diagnostics/db-info` | 200 |
| 1.4.2 | 版本信息 | `GET /api/v1/diagnostics/version` | 200 |
| 1.4.3 | 最近日志 | `GET /api/v1/diagnostics/logs/recent` | 200 |

### 1.5 部署管理

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.5.1 | 部署状态 | `GET /api/v1/deploy/status` | 200 |

### 1.6 SysAdmin 权限边界

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 1.6.1 | 访问药材管理 | `GET /api/v1/herbs` | 403 (DoctorOrReceptionist) |
| 1.6.2 | 访问验方管理 | `GET /api/v1/formulas` | 403 (DoctorOrReceptionist) |

---

## [S4] Phase 2: Admin 测试场景

### 2.1 登录认证

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.1.1 | 登录成功 | `POST /api/v1/auth/login` `{username:"newadmin", password:"Admin@123456"}` | 200 |
| 2.1.2 | 密码错误 | `POST /api/v1/auth/login` `{username:"newadmin", password:"wrong"}` | 401 |

### 2.2 用户管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.2.1 | 创建Doctor | `POST /api/v1/users` `{userName:"doctor1", realName:"测试医生", password:"Doctor@123", role:2}` | 201 |
| 2.2.2 | 创建Receptionist | `POST /api/v1/users` `{userName:"reception1", realName:"测试前台", password:"Recep@123", role:0}` | 201 |
| 2.2.3 | 获取列表 | `GET /api/v1/users` | 200 |
| 2.2.4 | 更新用户 | `PUT /api/v1/users/{id}` `{realName:"更新医生"}` | 200 |
| 2.2.5 | 删除用户 | `DELETE /api/v1/users/{id}` | 200 |
| 2.2.6 | 切换状态 | `POST /api/v1/users/{id}/toggle-status` | 200 |
| 2.2.7 | 恢复用户 | `POST /api/v1/users/{id}/restore` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.2.8 | 重复用户名 | `{userName:"doctor1", ...}` (再次创建) | 400/409 |
| 2.2.9 | 删除不存在的用户 | `DELETE /api/v1/users/00000000-...` | 404 |
| 2.2.10 | 创建-密码不符合复杂度 | `{password:"admin123"}` | 400 |
| 2.2.11 | 创建-缺少必填字段 | `{userName:"test99"}` (无realName/password/role) | 400 |
| 2.2.12 | Admin不能删除sysadmin | `DELETE /api/v1/users/{sysadminId}` | 400 |

### 2.3 患者管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.3.1 | 创建患者 | `POST /api/v1/patients` `{name:"测试患者A", gender:1, birthDate:"1990-01-15", phoneNumber:"13800138001", idNumber:"110101199001150001"}` | 201 |
| 2.3.2 | 获取列表 | `GET /api/v1/patients?page=1&pageSize=10` | 200 |
| 2.3.3 | 关键字搜索 | `GET /api/v1/patients?keyword=测试` | 200 |
| 2.3.4 | 获取详情 | `GET /api/v1/patients/{id}` | 200 |
| 2.3.5 | 更新患者 | `PUT /api/v1/patients/{id}` `{name:"更新后患者"}` | 200 |
| 2.3.6 | 按身份证查询 | `GET /api/v1/patients/by-id-number/110101199001150001` | 200 |
| 2.3.7 | 检查引用关系 | `GET /api/v1/patients/{id}/check-reference` | 200 |
| 2.3.8 | 切换状态 | `POST /api/v1/patients/{id}/toggle-status` | 200 |
| 2.3.9 | 删除患者 | `DELETE /api/v1/patients/{id}` | 200 |
| 2.3.10 | 恢复患者 | `POST /api/v1/patients/{id}/restore` | 200 |
| 2.3.11 | 批量检查引用 | `POST /api/v1/patients/batch-check-reference` `{ids:["{id}"]}` | 200 |
| 2.3.12 | 批量删除 | `POST /api/v1/patients/batch-delete` `{ids:["{id}"]}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.3.13 | 缺少姓名 | `{gender:1, idNumber:"..."}` (无name) | 400 |
| 2.3.14 | 姓名超长(>100字) | `{name:"很长的姓名...x101"}` | 400 |
| 2.3.15 | 身份证格式错误 | `{idNumber:"12345"}` | 400 |
| 2.3.16 | 身份证重复 | 创建第二个患者用相同idNumber | 400/409 |
| 2.3.17 | 手机号格式错误 | `{phoneNumber:"abc"}` | 400 |
| 2.3.18 | 出生日期在未来 | `{birthDate:"2030-01-01"}` | 400 |
| 2.3.19 | 获取不存在的患者 | `GET /api/v1/patients/00000000-...` | 404 |
| 2.3.20 | 更新不存在的患者 | `PUT /api/v1/patients/00000000-...` | 404 |
| 2.3.21 | 删除不存在的患者 | `DELETE /api/v1/patients/00000000-...` | 404 |
| 2.3.22 | 身份证查询-不存在 | `GET /api/v1/patients/by-id-number/000000000000000000` | 404 |

### 2.4 医案管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.4.1 | 创建医案 | `POST /api/v1/medicalcases` `{patientId:"{patientId}", userId:"{adminId}", remark:"测试医案"}` | 201 |
| 2.4.2 | 获取列表 | `GET /api/v1/medicalcases` | 200 |
| 2.4.3 | 获取详情 | `GET /api/v1/medicalcases/{id}` | 200 |
| 2.4.4 | 更新医案(聚合保存) | `PUT /api/v1/medicalcases/{id}` `{consultation:{...}, prescription:{...}}` | 200 |
| 2.4.5 | 设置处方标记 | `PUT /api/v1/medicalcases/{id}/prescription-flag` `{needPrescription:true}` | 200 |
| 2.4.6 | 搜索医案 | `GET /api/v1/medicalcases/search?keyword=测试` | 200 |
| 2.4.7 | 按患者查询 | `GET /api/v1/medicalcases/query?patientId={id}` | 200 |
| 2.4.8 | 最近医案 | `GET /api/v1/medicalcases/recent` | 200 |
| 2.4.9 | 批量获取 | `POST /api/v1/medicalcases/batch-details` `{ids:["{id}"]}` | 200 |
| 2.4.10 | 审计日志 | `GET /api/v1/medicalcases/{id}/audit-logs` | 200 |
| 2.4.11 | 打印日志 | `GET /api/v1/medicalcases/{id}/print-logs` | 200 |
| 2.4.12 | 标记打印完成 | `POST /api/v1/medicalcases/{id}/print-completed` | 200 |
| 2.4.13 | 删除医案 | `DELETE /api/v1/medicalcases/{id}` | 200 |
| 2.4.14 | 批量删除 | `POST /api/v1/medicalcases/batch-delete` `{ids:["{id}"]}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.4.15 | 缺少patientId | `{userId:"...", remark:"..."}` | 400 |
| 2.4.16 | patientId不存在 | `{patientId:"00000000-...", userId:"..."}` | 400 |
| 2.4.17 | 获取不存在的医案 | `GET /api/v1/medicalcases/00000000-...` | 404 |
| 2.4.18 | 更新不存在的医案 | `PUT /api/v1/medicalcases/00000000-...` | 404 |
| 2.4.19 | 删除不存在的医案 | `DELETE /api/v1/medicalcases/00000000-...` | 404 |

### 2.5 挂号管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.5.1 | 创建挂号 | `POST /api/v1/registrations` `{patientId:"{id}", patientName:"测试患者"}` | 201 |
| 2.5.2 | 获取列表 | `GET /api/v1/registrations` | 200 |
| 2.5.3 | 获取候诊队列 | `GET /api/v1/registrations/queue` | 200 |
| 2.5.4 | 开始就诊 | `POST /api/v1/registrations/{id}/start-visit` | 200 |
| 2.5.5 | 取消挂号 | `POST /api/v1/registrations/{id}/cancel` | 200 |
| 2.5.6 | 删除挂号 | `DELETE /api/v1/registrations/{id}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.5.7 | 缺少patientId | `{}` | 400 |
| 2.5.8 | patientId不存在 | `{patientId:"00000000-..."}` | 400 |
| 2.5.9 | 获取不存在的挂号 | `GET /api/v1/registrations/00000000-...` | 404 |
| 2.5.10 | 取消不存在的挂号 | `POST /api/v1/registrations/00000000-.../cancel` | 404 |

### 2.6 报表统计

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.6.1 | 日收入报表 | `GET /api/v1/reports/daily-income` | 200 |
| 2.6.2 | 就诊统计 | `GET /api/v1/reports/consultations` | 200 |
| 2.6.3 | 药材统计 | `GET /api/v1/reports/herbs` | 200 |

### 2.7 Admin 权限边界

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 2.7.1 | 访问药材管理 | `GET /api/v1/herbs` | 403 (DoctorOrReceptionist) |
| 2.7.2 | 访问验方管理 | `GET /api/v1/formulas` | 403 (DoctorOrReceptionist) |
| 2.7.3 | 访问系统配置 | `GET /api/v1/configuration` | 200 (Admin在AdminOrSuperAdmin中) |
| 2.7.4 | 访问系统诊断 | `GET /api/v1/diagnostics/db-info` | 200 |

---

## [S5] Phase 3: Receptionist 测试场景

### 3.1 登录认证

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 3.1.1 | 登录成功 | `POST /api/v1/auth/login` `{username:"reception1", password:"Recep@123"}` | 200 |
| 3.1.2 | 密码错误 | `{username:"reception1", password:"wrong"}` | 401 |
| 3.1.3 | 空用户名 | `{username:"", password:"..."}` | 400 |

### 3.2 患者管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 3.2.1 | 创建患者 | `POST /api/v1/patients` `{name:"前台创建患者", gender:2, idNumber:"110101199001150010"}` | 201 |
| 3.2.2 | 获取列表 | `GET /api/v1/patients` | 200 |
| 3.2.3 | 获取详情 | `GET /api/v1/patients/{id}` | 200 |
| 3.2.4 | 更新患者 | `PUT /api/v1/patients/{id}` `{name:"更新前台患者"}` | 200 |
| 3.2.5 | 按身份证查询 | `GET /api/v1/patients/by-id-number/110101199001150010` | 200 |
| 3.2.6 | 删除患者 | `DELETE /api/v1/patients/{id}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 3.2.7 | 缺少姓名 | `{gender:1}` | 400 |
| 3.2.8 | 身份证格式错误 | `{idNumber:"123"}` | 400 |
| 3.2.9 | 获取不存在的患者 | `GET /api/v1/patients/00000000-...` | 404 |

### 3.3 挂号管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 3.3.1 | 创建挂号 | `POST /api/v1/registrations` `{patientId:"{id}", patientName:"前台创建患者"}` | 201 |
| 3.3.2 | 获取列表 | `GET /api/v1/registrations` | 200 |
| 3.3.3 | 取消挂号 | `POST /api/v1/registrations/{id}/cancel` | 200 |
| 3.3.4 | 删除挂号 | `DELETE /api/v1/registrations/{id}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 3.3.5 | 快速就诊(权限不足) | `POST /api/v1/registrations/{id}/quick-visit` | 403 (DoctorOrAdmin) |
| 3.3.6 | 开始就诊(权限不足) | `POST /api/v1/registrations/{id}/start-visit` | 403 (DoctorOrAdmin) |
| 3.3.7 | 缺少patientId | `{}` | 400 |

### 3.4 Receptionist 权限边界

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 3.4.1 | 访问用户管理 | `GET /api/v1/users` | 403 (AdminOrSuperAdmin) |
| 3.4.2 | 访问药材管理 | `GET /api/v1/herbs` | 200 (DoctorOrReceptionist) |
| 3.4.3 | 访问验方管理 | `GET /api/v1/formulas` | 200 (DoctorOrReceptionist) |
| 3.4.4 | 访问医案管理 | `GET /api/v1/medicalcases` | 403 (DoctorOrAdmin) |
| 3.4.5 | 访问报表 | `GET /api/v1/reports/daily-income` | 403 (DoctorOrAdmin) |
| 3.4.6 | 访问系统配置 | `GET /api/v1/configuration` | 403 (AdminOrSuperAdmin) |
| 3.4.7 | 访问系统诊断 | `GET /api/v1/diagnostics/db-info` | 403 (AdminOrSuperAdmin) |

---

## [S6] Phase 4: Doctor 测试场景

### 4.1 登录认证

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.1.1 | 登录成功 | `POST /api/v1/auth/login` `{username:"doctor1", password:"Doctor@123"}` | 200 |
| 4.1.2 | 密码错误 | `{username:"doctor1", password:"wrong"}` | 401 |

### 4.2 患者管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.2.1 | 创建患者 | `POST /api/v1/patients` `{name:"医生创建患者", gender:1, idNumber:"110101199001150020"}` | 201 |
| 4.2.2 | 获取列表 | `GET /api/v1/patients` | 200 |
| 4.2.3 | 获取详情 | `GET /api/v1/patients/{id}` | 200 |
| 4.2.4 | 更新患者 | `PUT /api/v1/patients/{id}` `{name:"更新医生患者"}` | 200 |
| 4.2.5 | 按身份证查询 | `GET /api/v1/patients/by-id-number/110101199001150020` | 200 |
| 4.2.6 | 删除患者 | `DELETE /api/v1/patients/{id}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.2.7 | 缺少姓名 | `{gender:1}` | 400 |
| 4.2.8 | 身份证格式错误 | `{idNumber:"abc"}` | 400 |
| 4.2.9 | 获取不存在的患者 | `GET /api/v1/patients/00000000-...` | 404 |

### 4.3 药材管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.3.1 | 创建药材 | `POST /api/v1/herbs` `{name:"_test_薄荷", pinYinCode:"BH", category:"清热药", properties:"凉", origin:"广东", spec:"统货", unit:"克", price:15.0, costPrice:10.0, effect:"疏散风热", usage:"煎服", status:1}` | 201 |
| 4.3.2 | 获取列表 | `GET /api/v1/herbs?page=1&pageSize=10` | 200 |
| 4.3.3 | 关键字搜索 | `GET /api/v1/herbs?keyword=薄荷` | 200 |
| 4.3.4 | 分类筛选 | `GET /api/v1/herbs?category=清热药` | 200 |
| 4.3.5 | 获取详情 | `GET /api/v1/herbs/{id}` | 200 |
| 4.3.6 | 更新药材 | `PUT /api/v1/herbs/{id}` `{name:"_test_薄荷Updated"}` | 200 |
| 4.3.7 | 切换状态 | `POST /api/v1/herbs/{id}/toggle-status` | 200 |
| 4.3.8 | 恢复药材 | `POST /api/v1/herbs/{id}/restore` | 200 |
| 4.3.9 | 检查引用关系 | `GET /api/v1/herbs/{id}/check-reference` | 200 |
| 4.3.10 | 批量检查引用 | `POST /api/v1/herbs/batch-check-reference` `{ids:["{id}"]}` | 200 |
| 4.3.11 | 批量删除 | `POST /api/v1/herbs/batch-delete` `{ids:["{id}"]}` | 200 |
| 4.3.12 | 获取分类列表 | `GET /api/v1/herbs/categories` | 200 |
| 4.3.13 | 导出Excel | `GET /api/v1/herbs/export` | 200 (Excel) |
| 4.3.14 | 下载导入模板 | `GET /api/v1/herbs/import-template` | 200 (Excel) |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.3.15 | 缺少name | `{category:"清热药", ...}` (无name) | 400 |
| 4.3.16 | name超长(>100) | `{name:"很长的药材名...x101"}` | 400 |
| 4.3.17 | price为负数 | `{price:-1}` | 400 |
| 4.3.18 | 获取不存在的药材 | `GET /api/v1/herbs/00000000-...` | 404 |
| 4.3.19 | 更新不存在的药材 | `PUT /api/v1/herbs/00000000-...` | 404 |
| 4.3.20 | 删除不存在的药材 | `DELETE /api/v1/herbs/00000000-...` | 404 |
| 4.3.21 | 切换不存在药材状态 | `POST /api/v1/herbs/00000000-.../toggle-status` | 404 |
| 4.3.22 | 恢复不存在药材 | `POST /api/v1/herbs/00000000-.../restore` | 404 |
| 4.3.23 | 检查不存在药材引用 | `GET /api/v1/herbs/00000000-.../check-reference` | 404 |
| 4.3.24 | 批量删除空列表 | `POST /api/v1/herbs/batch-delete` `{ids:[]}` | 400 |
| 4.3.25 | 批量删除超限(>100) | `POST /api/v1/herbs/batch-delete` `{ids:[...x101]}` | 400 |

### 4.4 验方管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.4.1 | 创建验方 | `POST /api/v1/formulas` `{name:"_test_四物汤", pinYinCode:"SWT", category:"补血方", description:"测试验方", herbs:[]}` | 201 |
| 4.4.2 | 获取列表 | `GET /api/v1/formulas` | 200 |
| 4.4.3 | 关键字搜索 | `GET /api/v1/formulas?keyword=四物汤` | 200 |
| 4.4.4 | 分类筛选 | `GET /api/v1/formulas?category=补血方` | 200 |
| 4.4.5 | 获取详情 | `GET /api/v1/formulas/{id}` | 200 |
| 4.4.6 | 更新验方 | `PUT /api/v1/formulas/{id}` `{name:"_test_四物汤Updated"}` | 200 |
| 4.4.7 | 删除验方 | `DELETE /api/v1/formulas/{id}` | 200 |
| 4.4.8 | 待验证列表 | `GET /api/v1/formulas/pending-validation` | 200 |
| 4.4.9 | 克隆验方 | `POST /api/v1/formulas/{id}/clone` | 201 |
| 4.4.10 | 批量删除 | `POST /api/v1/formulas/batch-delete` `{ids:["{id}"]}` | 200 |
| 4.4.11 | 获取分类列表 | `GET /api/v1/formulas/categories` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.4.12 | 缺少name | `{category:"补血方"}` (无name) | 400 |
| 4.4.13 | name超长 | `{name:"很长的验方名...x101"}` | 400 |
| 4.4.14 | 获取不存在的验方 | `GET /api/v1/formulas/00000000-...` | 404 |
| 4.4.15 | 更新不存在的验方 | `PUT /api/v1/formulas/00000000-...` | 404 |
| 4.4.16 | 删除不存在的验方 | `DELETE /api/v1/formulas/00000000-...` | 404 |
| 4.4.17 | 克隆不存在的验方 | `POST /api/v1/formulas/00000000-.../clone` | 404 |
| 4.4.18 | 批量删除空列表 | `POST /api/v1/formulas/batch-delete` `{ids:[]}` | 400 |

### 4.5 医案管理（完整流程）

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.5.1 | 创建医案 | `POST /api/v1/medicalcases` `{patientId:"{pid}", userId:"{docId}", remark:"医生创建医案"}` | 201 |
| 4.5.2 | 获取列表 | `GET /api/v1/medicalcases` | 200 |
| 4.5.3 | 获取详情 | `GET /api/v1/medicalcases/{id}` | 200 |
| 4.5.4 | 聚合保存 | `PUT /api/v1/medicalcases/{id}` `{consultation:{symptoms:"头痛"}, prescription:{herbs:[...]}}` | 200 |
| 4.5.5 | 设置处方标记 | `PUT /api/v1/medicalcases/{id}/prescription-flag` `{needPrescription:true}` | 200 |
| 4.5.6 | 关闭医案 | `POST /api/v1/medicalcases/{id}/processing` `{action:"close"}` | 200 |
| 4.5.7 | 挂起医案 | `POST /api/v1/medicalcases/{id}/processing` `{action:"suspend"}` | 200 |
| 4.5.8 | 取消医案 | `POST /api/v1/medicalcases/{id}/processing` `{action:"cancel"}` | 200 |
| 4.5.9 | 搜索 | `GET /api/v1/medicalcases/search?keyword=医生` | 200 |
| 4.5.10 | 按患者查询 | `GET /api/v1/medicalcases/query?patientId={pid}` | 200 |
| 4.5.11 | 最近医案 | `GET /api/v1/medicalcases/recent` | 200 |
| 4.5.12 | 批量获取 | `POST /api/v1/medicalcases/batch-details` `{ids:["{id}"]}` | 200 |
| 4.5.13 | 审计日志 | `GET /api/v1/medicalcases/{id}/audit-logs` | 200 |
| 4.5.14 | 打印日志 | `GET /api/v1/medicalcases/{id}/print-logs` | 200 |
| 4.5.15 | 标记打印完成 | `POST /api/v1/medicalcases/{id}/print-completed` | 200 |
| 4.5.16 | 删除医案 | `DELETE /api/v1/medicalcases/{id}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.5.17 | 缺少patientId | `{userId:"...", remark:"..."}` | 400 |
| 4.5.18 | patientId不存在 | `{patientId:"00000000-...", userId:"..."}` | 400 |
| 4.5.19 | 获取不存在的医案 | `GET /api/v1/medicalcases/00000000-...` | 404 |
| 4.5.20 | 更新不存在的医案 | `PUT /api/v1/medicalcases/00000000-...` | 404 |
| 4.5.21 | 无效状态流转 | `POST /api/v1/medicalcases/{id}/processing` `{action:"invalid"}` | 400 |
| 4.5.22 | 对已关闭医案再关闭 | 对已close的医案再发close | 400 |
| 4.5.23 | 删除不存在的医案 | `DELETE /api/v1/medicalcases/00000000-...` | 404 |
| 4.5.24 | 审计日志-不存在 | `GET /api/v1/medicalcases/00000000-.../audit-logs` | 404 |
| 4.5.25 | 打印日志-不存在 | `GET /api/v1/medicalcases/00000000-.../print-logs` | 404 |
| 4.5.26 | 批量删除空列表 | `POST /api/v1/medicalcases/batch-delete` `{ids:[]}` | 400 |

### 4.6 挂号管理

#### Happy-path

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.6.1 | 创建挂号 | `POST /api/v1/registrations` `{patientId:"{pid}", patientName:"医生创建挂号"}` | 201 |
| 4.6.2 | 获取列表 | `GET /api/v1/registrations` | 200 |
| 4.6.3 | 获取候诊队列 | `GET /api/v1/registrations/queue` | 200 |
| 4.6.4 | 开始就诊 | `POST /api/v1/registrations/{id}/start-visit` | 200 |
| 4.6.5 | 取消挂号 | `POST /api/v1/registrations/{id}/cancel` | 200 |
| 4.6.6 | 快速就诊 | `POST /api/v1/registrations/{id}/quick-visit` | 200 |
| 4.6.7 | 删除挂号 | `DELETE /api/v1/registrations/{id}` | 200 |

#### Negative

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.6.8 | 缺少patientId | `{}` | 400 |
| 4.6.9 | patientId不存在 | `{patientId:"00000000-..."}` | 400 |
| 4.6.10 | 获取不存在的挂号 | `GET /api/v1/registrations/00000000-...` | 404 |
| 4.6.11 | 取消不存在的挂号 | `POST /api/v1/registrations/00000000-.../cancel` | 404 |
| 4.6.12 | 开始不存在的就诊 | `POST /api/v1/registrations/00000000-.../start-visit` | 404 |
| 4.6.13 | 快速就诊-不存在 | `POST /api/v1/registrations/00000000-.../quick-visit` | 404 |

### 4.7 报表统计

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.7.1 | 日收入报表 | `GET /api/v1/reports/daily-income` | 200 |
| 4.7.2 | 就诊统计 | `GET /api/v1/reports/consultations` | 200 |
| 4.7.3 | 药材统计 | `GET /api/v1/reports/herbs` | 200 |

### 4.8 Doctor 权限边界

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| 4.8.1 | 访问用户管理 | `GET /api/v1/users` | 403 (AdminOrSuperAdmin) |
| 4.8.2 | 访问系统配置 | `GET /api/v1/configuration` | 403 (AdminOrSuperAdmin) |
| 4.8.3 | 访问系统诊断 | `GET /api/v1/diagnostics/db-info` | 403 (AdminOrSuperAdmin) |
| 4.8.4 | 访问部署管理 | `GET /api/v1/deploy/status` | 403 (AdminOrSuperAdmin) |

---

## [S7] 无Token/未认证场景（全局）

适用于所有需要认证的端点：

| # | 场景 | 请求 | 预期 |
|---|------|------|------|
| G.1 | 无token访问patients | `GET /api/v1/patients` (无Authorization) | 401 |
| G.2 | 无token访问herbs | `GET /api/v1/herbs` (无Authorization) | 401 |
| G.3 | 无token访问users | `GET /api/v1/users` (无Authorization) | 401 |
| G.4 | 伪造token | `GET /api/v1/patients` (Bearer fake.token.here) | 401 |
| G.5 | 过期token | 使用已logout的token访问 | 401 |

---

## [S8] 测试统计

| Phase | Happy-path | Negative | 权限边界 | 合计 |
|-------|-----------|----------|----------|------|
| SysAdmin | 18 | 21 | 2 | 41 |
| Admin | 25 | 17 | 4 | 46 |
| Receptionist | 10 | 9 | 7 | 26 |
| Doctor | 38 | 33 | 4 | 75 |
| 全局 | 0 | 5 | 0 | 5 |
| **合计** | **91** | **85** | **17** | **193** |

## [S9] Environment Variables

```json
{
  "base_url": "http://192.168.190.246:5000",
  "sysadmin_token": "",
  "sysadmin_user_id": "",
  "admin_token": "",
  "admin_user_id": "",
  "receptionist_token": "",
  "receptionist_user_id": "",
  "doctor_token": "",
  "doctor_user_id": "",
  "test_patient_id": "",
  "test_herb_id": "",
  "test_formula_id": "",
  "test_medical_case_id": "",
  "test_registration_id": ""
}
```

## [S10] Newman 执行命令

```powershell
# 完整测试
newman run lybt-full-api-collection.json -e env.json --reporters cli,htmlextra

# 仅 SysAdmin
newman run lybt-full-api-collection.json -e env.json --folder "Phase 1: SysAdmin"

# 仅权限边界
newman run lybt-full-api-collection.json -e env.json --folder "Permission Boundary"

# 仅 Negative 场景
newman run lybt-full-api-collection.json -e env.json --folder "Negative"
```

## [S11] 测试数据清理

每个 Phase 结束后执行清理：
1. 删除测试创建的用户（排除 sysadmin/admin）
2. 删除测试创建的患者
3. 删除测试创建的药材
4. 删除测试创建的验方
5. 删除测试创建的医案
6. 删除测试创建的挂号

## [S12] Success Criteria

1. 所有 happy-path 测试通过（200/201）
2. 所有 negative 场景返回正确错误码（400/401/403/404/409）
3. 权限边界验证覆盖所有受限端点（17个场景）
4. 测试数据在每个 Phase 后清理
5. 测试文档与实际 API 端点一致
6. 总计 193 个测试场景
