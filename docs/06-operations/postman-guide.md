# Postman API 测试指南

> LYBTZYZS 凌隐宝堂中医诊所管理系统 API 测试工具使用说明

## 文件

| 文件 | 说明 |
|------|------|
| `LYBTZYZS_API_Collection.json` | Postman Collection v2.1，102 个 API 端点 |
| `LYBTZYZS_API_Tests.md` | 详细测试用例文档，120+ 测试场景 |

---

## 快速开始

### 1. 导入

1. 打开 Postman → **Import**
2. 选择 `LYBTZYZS_API_Collection.json`
3. 导入后显示 "LYBTZYZS - 凌隐宝堂中医诊所管理系统 API"

### 2. 配置环境

Collection 内置变量：

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `baseUrl` | `https://localhost:7001` | API 地址 |
| `authToken` | (自动) | JWT Token |
| `refreshToken` | (自动) | 刷新令牌 |
| `currentUserId` | (自动) | 当前用户 ID |
| `currentUsername` | `sysadmin` | 默认管理员 |

### 3. 登录

运行 **Auth → 用户登录**，Token 自动保存。

### 4. 测试顺序

```
Health → Auth → Users → Patients → Herbs → Formulas
→ Registrations → Medical Cases → Workflow → Audit → Print → Sync → Diagnostics
```

---

## API 目录

| 文件夹 | 端点 | 权限 |
|--------|------|------|
| Auth | 5 | 部分匿名 |
| Users | 14 | Admin / SuperAdmin |
| Patients | 12 | PatientAccess |
| Medical Cases | 12 | DoctorOrAdmin |
| Medical Case Workflow | 4 | DoctorOrAdmin |
| Medical Case Audit | 2 | DoctorOrAdmin |
| Medical Case Print | 2 | DoctorOrAdmin |
| Herbs | 17 | DoctorOrAdmin |
| Formulas | 15 | DoctorOrAdmin |
| Sync | 6 | DoctorOrAdmin |
| Registrations | 6 | PatientAccess / DoctorOrAdmin |
| Diagnostics | 4 | SuperAdmin |
| Health | 3 | 部分匿名 |

---

## 测试脚本

- 登录端点自动保存 Token
- POST 创建端点自动保存资源 ID
- 全局验证 ApiResponse 结构 + 响应时间
- 列表端点验证 PagedResult 结构

---

## 角色权限

| 角色 | 值 | 权限范围 |
|------|-----|---------|
| Receptionist | 0 | 挂号 |
| Doctor | 1 | 医案、药材、验方、挂号 |
| Admin | 10 | 全部 + 用户管理 |
| SuperAdmin | 100 | 全部 + 系统诊断 + 密码重置 |

---

## 疑难解答

- 登录失败 → 检查 baseUrl、服务器状态、SSL 证书、密码
- Token 过期 → 运行 "刷新令牌" 或重新登录
- 403 Forbidden → 检查用户角色是否满足端点要求
- 连接失败 → 确认 localhost:7001 可访问，检查防火墙
