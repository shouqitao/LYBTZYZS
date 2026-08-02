# 文档审稿报告（完整版）

> 日期: 2026-08-02 | 3 个并行子任务审查结果汇总

## 审查摘要

| 维度 | 发现问题数 | P0 | P1 | P2 |
|------|:----------:|:--:|:--:|:--:|
| 需求文档内部一致性 | 14 | 3 | 5 | 6 |
| 架构 vs 需求一致性 | 18 | 5 | 6 | 7 |
| API 文档 vs 代码一致性 | 10 | 3 | 4 | 3 |
| **合计（去重后）** | **~25** | **8** | **10** | **7** |

---

## P0：必须修复（影响安全边界/核心理解）

### 1. 权限矩阵变更日志与表格矛盾
- **位置**：`12-permissions-matrix.md`
- **问题**：v1.3 变更日志声称「药材创建 Doctor✅ 已反映代码现状」，但矩阵表格明确 Doctor ❌
- **修复**：变更日志措辞修正（矩阵表格正确，Doctor ❌ 是产品决策）

### 2. 03-users.md 验方权限错误
- **位置**：`03-users.md` 业务端点权限概览表
- **问题**：说 Receptionist 可「查看」验方，实际 06-formulas.md 和权限矩阵均确认 Receptionist ❌
- **修复**：03-users.md 验方行 Receptionist 改为 ❌

### 3. 03-users.md 报表权限错误
- **位置**：`03-users.md:92`
- **问题**：说 Doctor ❌ 无报表权限，但 10-reports.md 三个 US 均声明 DoctorOrAdmin（Doctor ✅）
- **修复**：03-users.md 报表行 Doctor 改为 ✅

### 4. DoctorOrReceptionist 策略描述四方矛盾
- **位置**：12-permissions-matrix / 01-prd / 03-users / 代码
- **问题**：矩阵说含 Doctor+Receptionist（2角色），PRD 和 03-users 说含全部4角色，代码注册仅 Doctor/Receptionist
- **修复**：统一为矩阵定义（Doctor+Receptionist），PRD 和 03-users 更新

### 5. 医案创建权限三方矛盾
- **位置**：12-permissions-matrix / 07-medical-cases.md / 01-prd.md
- **问题**：矩阵允许 Receptionist ✅(代建)，但医案文档和 PRD 均说「仅 Doctor」
- **修复**：需产品确认。BR-000 已决策医案由医生创建，矩阵应改为 Doctor only

### 6. 药材创建权限两方矛盾
- **位置**：12-permissions-matrix vs 05-herbs.md
- **问题**：矩阵说 Doctor ❌（Admin+ only），但 05-herbs.md US-HERB-003 声明 Doctor 可创建
- **修复**：产品已确认 Admin+ only，05-herbs.md 需更新 US 角色描述

### 7. Deploy Controller 未文档化
- **位置**：04-api-reference/README.md
- **问题**：代码有 DeployController（upload + restart），README 完全未记录
- **修复**：README 补充 Deploy 端点

### 8. ADR-0005 严重过时
- **位置**：decisions/0005-superadmin-auth-module.md
- **问题**：仍描述已移除的 AdminSecrets 方案，未标记废弃
- **修复**：标记为「已废弃」，添加替代说明

---

## P1：应该修复（影响准确性）

| # | 问题 | 位置 | 修复 |
|---|------|------|------|
| 9 | `GET /auth/validate` 需认证，README 标 AllowAnonymous | 04-api-reference/README.md | 改为「已认证」 |
| 10 | 策略名缺 DoctorOrAdminOrReceptionist | 04-api-reference/README.md | 补充策略 |
| 11 | 药材端点少列 5 个（check-reference, batch-enable/disable, restore） | 04-api-reference/README.md | 补充端点 |
| 12 | 验方端点少列 3 个（batch-enable/disable, restore） | 04-api-reference/README.md | 补充端点 |
| 13 | 患者端点少列 1 个（by-id-number） | 04-api-reference/README.md | 补充端点 |
| 14 | MedicalCases 端点少列 3 个（基类端点未计入） | 04-api-reference/README.md | 补充端点 |
| 15 | 挂号权限：Admin 也能创建，03-users.md 未体现 | 03-users.md | 更新 |
| 16 | 08-registration.md US-REG-001/006 标 🔴 但文档说已实现 | 08-registration.md | 更新状态 |
| 17 | 05-herbs.md 多个 US 角色声明与目标态矛盾 | 05-herbs.md | 更新 US 角色 |
| 18 | 技术栈描述：MediatR 缺失、Prism 版本冲突 | 03-architecture 多文件 | 统一 |

---

## P2：建议修复（可读性/一致性）

| # | 问题 | 位置 |
|---|------|------|
| 19 | 03-users.md:270 US-USER-007 角色写 SuperAdmin，策略为 AdminOrSuperAdmin | 03-users.md |
| 20 | 03-users.md:330 US-USER-011 状态 ✅ 但 AC 标 🚧 | 03-users.md |
| 21 | 05-herbs.md:103 US-HERB-003 角色写「医生/管理员」但策略含 Receptionist | 05-herbs.md |
| 22 | 02-requirements/README 挂号写「全部完成」但 5/8 US 未实现 | 02-requirements/README.md |
| 23 | 02-requirements/README 药材写「核心完成」但 6/13 US 未实现 | 02-requirements/README.md |
| 24 | 总计 US 数：README 写 141，实际求和 142 | 02-requirements/README.md |
| 25 | Health 端点：README 写 4，实际 3 | 04-api-reference/README.md |

---

## 修复建议

**可直接修复（A 类，无需产品确认）**：1, 2, 3, 4, 7, 8, 9, 10, 11, 12, 13, 14, 15, 19, 20, 21, 22, 23, 24, 25

**需产品确认（B 类）**：5（医案创建权限），6（药材创建权限——已确认 Admin+，可直接改）

**建议**：先批量修复 A 类，再确认 B 类。
