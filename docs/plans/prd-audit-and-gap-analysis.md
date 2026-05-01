# PRD 审查与代码差距分析报告

> **创建日期**: 2026-04-26
> **审查范围**: 138 User Stories (15 模块) + NFR 文档
> **审查方法**: PRD 文档 vs 实际代码比对
> **状态**: 草案 (等待探索代理完成)

---

## 1. 执行摘要

### 1.1 当前状态概览

| 指标 | PRD 声称 | 实际 (待验证) | 差异 |
|------|---------|-------------|------|
| 总 User Stories | 138 | 待验证 | 待验证 |
| Must Have (51) | 全部 Done | 待验证 | 待验证 |
| Should Have (54) | 全部 Done | 待验证 | 待验证 |
| Could Have (33) | 全部 Done | 待验证 | 待验证 |
| 测试总数 | 1654+ | 2021 | +367 (增长) |
| 技术债务 | 清零 | 待验证 | 待验证 |

### 1.2 关键发现 (初步)

1. **LocalWebAPI 架构变更**: PRD 描述的本地模式是 SQLite 直连 (LocalDbContext)，实际已改为嵌入式 Kestrel WebAPI + HTTP Proxy Repository 模式。PRD 未反映此架构变更。
2. **三模式 vs 双模式**: PRD 和 roadmap 仍描述为"双模式" (Remote/Local)，实际已实现三模式 (Remote/Local/LocalWebAPI)。
3. **测试数量增长**: Roadmap 声称 1654 tests，实际已达 2021 tests (Server 1185 + Desktop 760 + Architecture 76)。

---

## 2. PRD 文档体系审查

### 2.1 文档结构完整性

| 文档类别 | 文件数 | 状态 | 问题 |
|---------|--------|------|------|
| 01-product/ | 待验证 | 待验证 | 待验证 |
| 02-requirements/ | 22 文件 | ✅ 完整 | 部分文档未反映最新实现 |
| 03-architecture/ | 待验证 | 待验证 | dual-mode.md 需更新 |
| 04-api-reference/ | 待验证 | 待验证 | 待验证 |
| 05-development/ | 待验证 | 待验证 | 待验证 |
| 06-operations/ | 待验证 | 待验证 | 待验证 |

### 2.2 PRD 文档过期项

| 文档 | 过期内容 | 应更新为 | 优先级 |
|------|---------|---------|--------|
| dual-mode.md | 描述双模式 (Remote/Local) | 三模式 (Remote/Local/LocalWebAPI) | HIGH |
| roadmap.md | 测试数 1654 | 测试数 2021 | MEDIUM |
| prd.md | LocalDB 为本地数据库 | SQLite (LocalWebAPI 模式) | HIGH |
| sync.md | 同步基于 LocalDbContext | 同步基于 LocalWebApiDbContext | MEDIUM |

---

## 3. 模块级差距分析

### 3.1 认证模块 (Auth) - US-AUTH-001~013

| US 编号 | 名称 | PRD 状态 | 代码状态 | 差距 |
|---------|------|---------|---------|------|
| US-AUTH-001 | 用户登录 | Done | 待验证 | 待验证 |
| US-AUTH-002 | 自动登录 | Done | 待验证 | 待验证 |
| US-AUTH-003 | Token 滑动刷新 | Done | 待验证 | 待验证 |
| US-AUTH-004 | 重放攻击检测 | Done | 待验证 | 待验证 |
| US-AUTH-005 | 凭证安全存储 | Done | 待验证 | 待验证 |
| US-AUTH-006 | 不活跃超时 | Done | 待验证 | 待验证 |
| US-AUTH-007 | 登出前警告 | Removed | Removed | ✅ 一致 |
| US-AUTH-008 | 状态机 | Done | 待验证 | 待验证 |
| US-AUTH-009 | 登录界面 | Done | 待验证 | 待验证 |
| US-AUTH-010 | Token 刷新失败分级 | Done | 待验证 | 待验证 |
| US-AUTH-011 | 不活跃超时 UI | Done | 待验证 | 待验证 |
| US-AUTH-012 | 登录状态管理 | Done | 待验证 | 待验证 |
| US-AUTH-013 | 认证事件体系 | Done | 待验证 | 待验证 |

### 3.2 医案管理 (MedicalCase) - US-MC-001~018

| US 编号 | 名称 | PRD 状态 | 代码状态 | 差距 |
|---------|------|---------|---------|------|
| US-MC-001 | 创建医案 | Done | 待验证 | 待验证 |
| US-MC-002 | 填写诊断 | Done | 待验证 | 待验证 |
| US-MC-003 | 标记处方需求 | Done | 待验证 | 待验证 |
| US-MC-004 | 开具处方 | Done | 待验证 | 待验证 |
| US-MC-005 | 聚合保存 | Done | 待验证 | 待验证 |
| US-MC-006 | 挂起医案 | Done | 待验证 | 待验证 |
| US-MC-007 | 完成医案 | Done | 待验证 | 待验证 |
| US-MC-008 | 取消医案 | Done | 待验证 | 待验证 |
| US-MC-009 | 医案列表 | Done | 待验证 | 待验证 |
| US-MC-010 | 跨医案搜索 | Done | 待验证 | 待验证 |
| US-MC-011 | 编辑模式 | Done | 待验证 | 待验证 |
| US-MC-012 | 审计日志 | Done | 待验证 | 待验证 |
| US-MC-013 | 权限控制 | Done | 待验证 | 待验证 |
| US-MC-014 | 锁定规则 | Done | 待验证 | 待验证 |
| US-MC-015 | 打印触发 | Done | 待验证 | 待验证 |
| US-MC-016 | 验方导入处方 | Done | 待验证 | 待验证 |
| US-MC-017 | 待诊队列 | Done | 待验证 | 待验证 |
| US-MC-018 | 复制历史处方 | Done | 待验证 | 待验证 |

### 3.3 数据同步 (Sync) - US-SYNC-001~008

| US 编号 | 名称 | PRD 状态 | 代码状态 | 差距 |
|---------|------|---------|---------|------|
| US-SYNC-001 | 获取可同步实体 | Done | 待验证 | 待验证 |
| US-SYNC-002 | 获取同步元数据 | Done | 待验证 | 待验证 |
| US-SYNC-003 | 数据比对 | Done | 待验证 | 待验证 |
| US-SYNC-004 | 上传本地变更 | Done | 待验证 | 待验证 |
| US-SYNC-005 | 下载服务端变更 | Done | 待验证 | 待验证 |
| US-SYNC-006 | 同步删除 | Done | 待验证 | 待验证 |
| US-SYNC-007 | 完整同步工作流 | Done | 待验证 | 待验证 |
| US-SYNC-008 | 模式切换 | Done | 待验证 | **需更新**: 三模式切换 |

---

## 4. 架构变更影响分析

### 4.1 LocalWebAPI 引入的变更

| 变更项 | 影响范围 | PRD 是否反映 | 需要更新 |
|--------|---------|-------------|---------|
| LocalWebApiDbContext | 数据模型文档 | ❌ 否 | ✅ 是 |
| LocalWebApiHost | 架构文档 | ❌ 否 | ✅ 是 |
| HTTP Proxy Repository | 数据访问层文档 | ❌ 否 | ✅ 是 |
| 三模式切换 | dual-mode.md, sync.md | ❌ 否 | ✅ 是 |
| SQLite 替代 LocalDB | 部署文档, NFR | ❌ 否 | ✅ 是 |
| 简化 JWT 认证 | auth.md | ❌ 否 | ✅ 是 |

### 4.2 未实现的 PRD 功能 (待验证)

| 模块 | 功能 | PRD 描述 | 代码中是否存在 | 优先级 |
|------|------|---------|-------------|--------|
| 待验证 | 待验证 | 待验证 | 待验证 | 待验证 |

---

## 5. NFR 符合性检查

### 5.1 性能指标

| NFR 编号 | 指标 | 目标 | 当前状态 | 符合 |
|---------|------|------|---------|------|
| NFR-PERF-001 | API 简单查询 P95 | < 500ms | 待实测 | 待验证 |
| NFR-PERF-001 | API 列表查询 P95 | < 1s | 待实测 | 待验证 |
| NFR-PERF-001 | 聚合保存 P95 | < 2s | 待实测 | 待验证 |
| NFR-PERF-002 | Desktop 启动 | < 5s | 待实测 | 待验证 |
| NFR-PERF-002 | 页面切换 | < 1s | 待实测 | 待验证 |

### 5.2 安全指标

| NFR 编号 | 指标 | 目标 | 当前状态 | 符合 |
|---------|------|------|---------|------|
| NFR-SEC-001 | 未授权访问 | 0 | 待验证 | 待验证 |
| NFR-SEC-002 | 数据加密 | DPAPI | 待验证 | 待验证 |
| NFR-SEC-003 | 密码策略 | BCrypt | 待验证 | 待验证 |

---

## 6. v2.0 路线图建议

基于当前 PRD 审查，建议 v2.0 包含以下功能:

| 功能 | 来源 | 优先级 | 说明 |
|------|------|--------|------|
| MedicalCase 数据同步 | PRD Out of Scope | HIGH | 聚合根多表级联同步 |
| 自动同步提示 | PRD Out of Scope | HIGH | NetworkStatusService |
| 医保对接 | PRD Out of Scope | MEDIUM | 第三方接口 |
| 排班和预约 | PRD Out of Scope | MEDIUM | 小型诊所需求增长后可考虑 |
| 移动端支持 | PRD Out of Scope | LOW | iOS/Android |
| EMR 标准对接 | PRD Out of Scope | LOW | 卫生部门要求 |

---

## 7. 待讨论问题

### 7.1 架构方向

1. **LocalWebAPI 是否应成为默认本地模式？** 当前 Local (LocalDB) 仍作为遗留模式存在，是否应标记为 deprecated？
2. **三模式切换的 UI 是否完整？** 用户是否清楚知道三种模式的区别？
3. **同步策略是否需要调整？** LocalWebAPI 模式下的同步流程是否与 PRD 描述一致？

### 7.2 功能优先级

1. **MedicalCase 同步** 是否应纳入 v1.0 还是保持 v2.0？
2. **NFR 性能指标** 是否需要实测校准？
3. **测试覆盖率** 是否满足 PRD 要求的 100% 代码-PRD 对齐率？

### 7.3 文档维护

1. **PRD 文档更新流程** 是否应纳入 CI/CD 自动化检查？
2. **架构决策记录 (ADR)** 是否完整记录了 LocalWebAPI 的决策过程？

---

## 8. 下一步行动

| 行动项 | 负责人 | 优先级 | 预计完成 |
|--------|--------|--------|---------|
| 完成探索代理结果收集 | AI | HIGH | 立即 |
| 逐项验证 138 US 实现状态 | AI + 用户 | HIGH | 讨论后 |
| 更新 dual-mode.md 为三模式 | AI | HIGH | 确认后 |
| 更新 roadmap.md 测试数据 | AI | MEDIUM | 确认后 |
| 创建 LocalWebAPI ADR | AI | MEDIUM | 确认后 |
| NFR 性能实测 | 用户 | MEDIUM | 生产部署前 |
| 确定 v2.0 优先级 | 用户 | HIGH | 本次讨论后 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-04-26 | v0.1 | 初始草案，等待探索代理完成 |
