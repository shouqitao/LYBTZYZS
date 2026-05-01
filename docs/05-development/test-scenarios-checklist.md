# WebApi 真实数据库测试清单

> **测试日期**: 2026-03-14
> **数据库**: LYBTDB (localhost)
> **测试方式**: 自动化测试 + 场景验证

---

## 场景1: 管理员创建医生用户

**角色**: sysadmin (Admin)
**目标**: 创建医生用户 shouqitao

| # | 步骤 | API端点 | 预期结果 | 状态 |
|---|------|---------|----------|------|
| 1.1 | sysadmin 登录 | POST /api/v1/auth/login | 返回 AccessToken + RefreshToken | ⬜ |
| 1.2 | 创建医生用户 | POST /api/v1/users | 创建成功，返回用户ID | ⬜ |
| | 请求体: `{username:"shouqitao", displayName:"寿祺涛", role:"Doctor", ...}` | | | |
| 1.3 | 验证用户创建 | GET /api/v1/users/{id} | 返回用户详情，role=Doctor | ⬜ |
| 1.4 | 重置密码 | POST /api/v1/users/{id}/reset-password | 密码重置成功 | ⬜ |

---

## 场景2: 医生登录与患者管理

**角色**: shouqitao (Doctor)
**目标**: 完整诊疗流程

| # | 步骤 | API端点 | 预期结果 | 状态 |
|---|------|---------|----------|------|
| 2.1 | shouqitao 登录 | POST /api/v1/auth/login | 返回 Token，角色为 Doctor | ⬜ |
| 2.2 | 创建患者 | POST /api/v1/patients | 患者创建成功 | ⬜ |
| | 请求体: `{name:"张三", gender:"Male", age:35, phone:"13800138001"}` | | | |
| 2.3 | 搜索患者 | GET /api/v1/patients/search?keyword=张三 | 返回患者列表 | ⬜ |
| 2.4 | 获取患者详情 | GET /api/v1/patients/{id} | 返回完整患者信息 | ⬜ |
| 2.5 | 更新患者信息 | PUT /api/v1/patients/{id} | 更新成功 | ⬜ |

---

## 场景3: 挂号与医案流程

**角色**: shouqitao (Doctor)
**目标**: 从挂号到完成医案

| # | 步骤 | API端点 | 预期结果 | 状态 |
|---|------|---------|----------|------|
| 3.1 | 创建医案 | POST /api/v1/medicalcases | 医案创建成功，状态=Draft | ⬜ |
| | 请求体: `{patientId:"...", chiefComplaint:"头痛发热"}` | | | |
| 3.2 | 更新诊断 | PUT /api/v1/medicalcases/{id}/consultation | 诊断记录保存成功 | ⬜ |
| | 请求体: `{inspection:"...", pulse:"...", diagnosis:"风寒感冒"}` | | | |
| 3.3 | 创建处方 | POST /api/v1/medicalcases/{id}/prescription | 处方创建成功 | ⬜ |
| | 请求体: `{items:[{herbId:"...", dosage:10, unit:"g"}]}` | | | |
| 3.4 | 完成医案 | POST /api/v1/medicalcases/{id}/close | 状态变为 Completed | ⚠️ 405 (使用PUT) |
| 3.5 | 查询医案详情 | GET /api/v1/medicalcases/{id} | 返回完整医案（含诊断+处方） | ⬜ |

---

## 场景4: 药材与验方管理

**角色**: shouqitao (Doctor)
**目标**: 管理药材和验方

| # | 步骤 | API端点 | 预期结果 | 状态 |
|---|------|---------|----------|------|
| 4.1 | 搜索药材 | GET /api/v1/herbs/search?keyword=人参 | 返回药材列表 | ⬜ |
| 4.2 | 创建药材 | POST /api/v1/herbs | 药材创建成功 | ⬜ |
| | 请求体: `{name:"测试药材", unitPrice:100, unit:"g"}` | | | |
| 4.3 | 搜索验方 | GET /api/v1/formulas/search | 返回验方列表 | ⬜ |
| 4.4 | 创建验方 | POST /api/v1/formulas | 验方创建成功 | ⬜ |
| | 请求体: `{name:"测试验方", items:[...]}` | | | |
| 4.5 | 克隆验方 | POST /api/v1/formulas/{id}/clone | 复制成功，返回新验方ID | ⬜ |

---

## 场景5: 系统功能验证

**目标**: 系统级功能检查

| # | 步骤 | API端点 | 预期结果 | 状态 |
|---|------|---------|----------|------|
| 5.1 | 健康检查 | GET /health | 返回 Healthy | ✅ |
| 5.2 | 刷新 Token | POST /api/v1/auth/refresh | 返回新 AccessToken | ⬜ |
| 5.3 | 验证 Token | GET /api/v1/auth/validate | 返回有效 | ⬜ |
| 5.4 | 获取当前用户 | GET /api/v1/users/current | 返回当前登录用户信息 | ⬜ |
| 5.5 | 修改密码 | PUT /api/v1/users/{id}/change-password | 密码修改成功 | ⬜ |
| 5.6 | 登出 | POST /api/v1/auth/logout | 登出成功 | ⬜ |

---

## 场景6: 批量操作测试

**角色**: sysadmin (Admin)
**目标**: 批量管理功能

| # | 步骤 | API端点 | 预期结果 | 状态 |
|---|------|---------|----------|------|
| 6.1 | 批量创建测试用户 | 循环 POST /api/v1/users | 创建多个用户 | ⬜ |
| 6.2 | 批量删除用户 | POST /api/v1/users/batch-delete | 批量删除成功 | ⬜ |
| 6.3 | 批量禁用用户 | POST /api/v1/users/batch-disable | 批量禁用成功 | ⬜ |
| 6.4 | 批量启用用户 | POST /api/v1/users/batch-enable | 批量启用成功 | ⬜ |

---

## 执行顺序

1. **场景5.1** - 先验证服务健康
2. **场景1** - 管理员创建医生
3. **场景2** - 医生登录+患者管理
4. **场景3** - 完整医案流程
5. **场景4** - 药材验方管理
6. **场景5.2-5.6** - 系统功能
7. **场景6** - 批量操作

---

## 测试数据

### 管理员账号
- 用户名: `sysadmin`
- 密码: `LybtAdmin2025@SecurePass#` (配置文件中默认密码)

### 医生用户
- 用户名: `shouqitao`
- 角色: `Doctor`
- 姓名: `寿祺涛`

### 测试患者
- 姓名: `张三`
- 性别: `男`
- 年龄: `35`
- 电话: `13800138001`
