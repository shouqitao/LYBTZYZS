# Findings

## 审查范围

### PRD体系 (02-requirements/)
- **功能需求**: 14模块, 131条 FR
- **错误码**: MCCEE体系, ~90条 ERR
- **业务规则**: BR-xxx (分布在各模块)
- **数据需求/决策**: xx-Dxx (PAT-D, MC-D, NFR-D 等)
- **非功能需求**: 16条 NFR
- **UI规范**: UI-D01~D06

### 设计文档体系
- **03-architecture/**: 6文档 + ADR目录
- **04-api-reference/**: 9模块文档 + README

---

## Phase 1: PRD 需求提取结果

| 模块 | FR | ERR | BR | D/决策 |
|------|-----|------|-----|--------|
| auth | 13 | 7 | 0 | 2 |
| users | 12 | 7 | 0 | 2 |
| patients | 13 | 17 | 1 | 5 |
| herbs | 13 | 16 | 1 | 0 |
| formulas | 13 | 17 | 0 | 3 |
| medical-cases | 18 | 29 | 4 | 13 |
| sync | 8 | 20 | 0 | 8 |
| printing | 4 | 0 | 0 | 6 |
| card-reader | 2 | 13 | 0 | 2 |
| health-diagnostics | 9 | 0 | 0 | 6 |
| error-handling | 8 | 全局 | 0 | 7 |
| logging | 7 | 0 | 0 | 7 |
| desktop-shell | 7 | 0 | 0 | 0 |
| configuration | 4 | 0 | 0 | 0 |
| nfr | - | - | - | 16 NFR + 10 D |
| ui-patterns | - | - | - | 6 UI-D |
| **合计** | **131** | **~133** | **6** | **~93** |

---

## Phase 3: FR 交叉比对结果

### 汇总

| 状态 | 数量 | 占比 |
|------|------|------|
| COVERED | 109 | 83.2% |
| PARTIAL | 14 | 10.7% |
| MISSING | 8 | 6.1% |
| **总计** | **131** | 100% |

### 100% COVERED 模块 (无缺口)
- Users (12/12), Herbs (13/13), Formulas (13/13), Medical Cases (18/18), Card Reader (2/2), Printing (4/4)

### MISSING 清单 (8项)

| # | FR编号 | 模块 | 描述 | 缺失说明 |
|---|--------|------|------|----------|
| 1 | FR-PAT-011 | Patients | 检查患者引用 | patients API 缺少 check-reference 端点 |
| 2 | FR-PAT-012 | Patients | 批量检查患者引用 | patients API 缺少 batch-check-reference 端点 |
| 3 | FR-ERR-005 | Error | 异常严重度分级 | desktop.md/server.md 均无对应设计 |
| 4 | FR-ERR-007 | Error | 错误追踪码 | 8位短追踪码机制无设计文档 |
| 5 | FR-ERR-008 | Error | 异常通知类型映射 | Toast/对话框映射规则无设计文档 |
| 6 | FR-LOG-003 | Logging | 敏感数据脱敏 | server.md 无 SensitiveDataMasker 设计 |
| 7 | FR-LOG-007 | Logging | API请求自动日志 | server.md 无 ApiLoggingFilter 设计 |
| 8 | FR-CFG-004 | Config | 生产环境启动验证 | server.md 无 ConfigurationValidator 设计 |

### PARTIAL 清单 (14项)

| # | FR编号 | 模块 | 描述 | 缺失部分 |
|---|--------|------|------|----------|
| 1 | FR-AUTH-009 | Auth | 凭证本地存储 | desktop.md 缺 CredentialVault/DPAPI 架构设计 |
| 2 | FR-AUTH-011 | Auth | Token刷新失败处理 | 无指数退避+AutoLogin fallback 策略设计 |
| 3 | FR-SYNC-001 | Sync | 同步实体类型 | API文档返回列表不含MedicalCase，与PRD不一致 |
| 4 | FR-SYNC-007 | Sync | 完整同步工作流 | 桌面端进度UI/冲突解决UI无设计 |
| 5 | FR-SYNC-008 | Sync | 模式切换 | 切换前检查/回退策略无详细设计 |
| 6 | FR-SYS-008 | Health | Server启动诊断 | server.md 无 DatabaseStartupDiagnostics 章节 |
| 7 | FR-SHELL-005 | Shell | 菜单与快捷键 | desktop.md 缺完整菜单层级和角色可见性 |
| 8 | FR-SHELL-006 | Shell | 启动诊断 | desktop.md 缺 StartupDiagnostics 详细设计 |
| 9 | FR-SHELL-007 | Shell | 账户设置 | desktop.md 无 AccountSettings 架构章节 |
| 10 | FR-ERR-003 | Error | 客户端异常处理 | desktop.md 缺 DesktopExceptionHandler 行为设计 |
| 11 | FR-ERR-006 | Error | 错误消息映射 | 无 ClientErrorMessageMapper 架构设计 |
| 12 | FR-LOG-002 | Logging | 安全审计日志 | server.md 无 SecurityAuditLog 表和写入机制 |
| 13 | FR-LOG-005 | Logging | 日志后台清理 | server.md 无 LogCleanupService 设计 |
| 14 | FR-LOG-006 | Logging | 审计日志清理 | server.md 无 SecurityAuditCleanupService 设计 |

---

## Phase 3: ERR 交叉比对结果 (~133 条)

### 方法论
- PRD 定义 MCCEE 5位数错误码体系 (7个模块范围 + 通用) + 读卡器客户端错误码 (13个)
- 设计文档覆盖层: server.md MCCEE总表 + 各 API reference 文档端点级错误码

### 汇总

| 层级 | 状态 | 说明 |
|------|------|------|
| 架构层 (server.md MCCEE) | COVERED | 7个模块范围表 + 总计 90+ 场景，与 PRD 完全对齐 |
| API 端点层 (04-api-reference/) | PARTIAL | 仅 auth 模块列出具体码，其他 7 模块仅泛化 HTTP 状态 |
| 客户端层 (card-reader) | N/A | 纯客户端错误码，PRD 自包含，无需设计文档 |

### 按模块明细

| 模块 | PRD ERR 数 | server.md 范围 | API Ref 覆盖 | 状态 |
|------|-----------|---------------|-------------|------|
| Auth | 7 (10101-10300) | 1xxxx ~15 | auth.md: 6码 + README: 14码 | COVERED |
| Users | 7 (10001-10006) | 1xxxx (共享) | users.md: 仅泛化 HTTP | PARTIAL |
| Patients | 17 (20001-20805) | 2xxxx ~18 | patients.md: 3码 (status端点) | PARTIAL |
| Herbs | 16 (50101-50305) | 5xxxx ~15 | herbs.md: 仅泛化 HTTP | PARTIAL |
| Formulas | 17 (60101-60304) | 6xxxx ~17 | formulas.md: 仅泛化 HTTP | PARTIAL |
| Medical Cases | ~29 (30101-30607) | 3xxxx ~29 | medical-cases.md: 2码引用 | PARTIAL |
| Sync | 20 (70101-70505) | 7xxxx ~20 | sync.md: 仅泛化 HTTP | PARTIAL |
| Card Reader | 13 (客户端) | N/A | N/A | N/A |
| General | ~5 (00xxx) | 0xxxx ~5 | - | COVERED |

### DISCREPANCY: API README 多出 4 个 Auth 错误码

API reference README.md 定义了 14 个认证错误码，其中以下 4 个 **不在 auth.md PRD 中**:
- `PasswordExpired` (401) - PRD 未定义密码过期错误
- `SessionNotFound` (401) - PRD 未定义会话不存在错误
- `SessionExpired` (401) - PRD 未定义会话过期错误
- `ConcurrentSessionLimit` (401) - PRD 未定义并发会话超限错误

**影响**: 设计文档包含超出 PRD 范围的错误码定义，需确认是预留扩展还是需要回补到 PRD。

### ERR 维度结论
- **架构层**: 完全覆盖 (server.md MCCEE 表对齐)
- **API 端点层**: 7/9 模块仅用泛化 HTTP 状态码，缺少具体 MCCEE 错误码
- **建议**: 各 API reference 文档补充端点级 MCCEE 错误码映射 (参考 auth.md + patients.md status 端点的做法)

---

## Phase 3: BR 交叉比对结果 (6 条)

### PRD 业务规则清单

| 编号 | 模块 | 规则 | 设计文档覆盖 | 状态 |
|------|------|------|-------------|------|
| BR-001 | Medical Cases | 同一患者单活跃医案约束 | data-model.md Active唯一索引 (MC-D06) + ERR-30103/30104 | COVERED |
| BR-002 | Medical Cases | 医案离开界面操作 (保存/暂存/放弃/完成) | desktop.md 无 UX 流程设计 | PARTIAL |
| BR-003 | Medical Cases | 医案完成校验 (NeedsPrescription + Prescription) | ERR-30302/30303 + API 完成端点设计 | COVERED |
| BR-DEL-001 | 全系统 | 统一删除策略 (有引用禁删仅禁用/无引用软删) | server.md BaseEntity.IsDeleted + 各模块 check-reference | COVERED |
| (Patients) | Patients | 引用检查: 有医案禁止删除 | ERR-20004 + MC-D04 | COVERED |
| (Herbs) | Herbs | 引用检查: 有处方引用禁止删除 | herbs API check-reference 端点 | COVERED |

### BR 维度结论
- **COVERED**: 5/6 (BR-001, BR-003, BR-DEL-001, Patients引用, Herbs引用)
- **PARTIAL**: 1/6 (BR-002 离开界面操作 -- desktop.md 缺具体 UX 交互流程)
- **MISSING**: 0

---

## Phase 3: D 交叉比对结果 (~93 条决策)

### 方法论
- D 条目分布在 16 个 PRD 文件中，包含 AUTH-D, USER-D, PAT-D, MC-D, NFR-D, UI-D 等编号
- 设计文档应在架构设计中体现这些决策的技术实现

### 按模块比对

#### Auth 决策 (4条)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| AUTH-D06 | 单会话登录策略 | API README 有 ConcurrentSessionLimit 码，但 server.md 无 Token Family 撤销设计 | PARTIAL |
| AUTH-D07 | 角色变更即时生效 (撤销 Token Family) | server.md 无此机制设计，仅 PRD 和 auth API 暗含 | PARTIAL |
| Decision 1 | 本地模式不支持自动登录 | dual-mode.md TBD-01 明确列出 | COVERED |
| Decision 2 | 本地模式有不活跃超时 (15分钟) | dual-mode.md 未提及超时时间 | PARTIAL |

#### User 决策 (2条)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| USER-D03 | 最后一个 Admin 禁用保护 | users API 应有此约束，但 API doc 未明确 | PARTIAL |
| AUTH-D07 | 角色变更即时生效 (交叉引用) | 同上 | PARTIAL |

#### Patient 决策 (4条)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| PAT-D03 | 身份证号必填+唯一性 | data-model.md v1.1 更新 | COVERED |
| PAT-D04 | 患者合并 v1.0 不含 | N/A (明确排除) | COVERED |
| PAT-D05 | 禁用场景 (已故) | data-model.md Patient.Status + patients API status 端点 | COVERED |
| PAT-D06 | 关系转移 v2.0 | N/A (明确排除) | COVERED |

#### Medical Cases 决策 (13条: MC-D04~D16)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| MC-D04 | 有医案禁止删除患者 | ERR-20004 + patients API | COVERED |
| MC-D05 | 草稿不自动清理 v1.0 | N/A (明确排除) | COVERED |
| MC-D06 | 仅 Active 唯一索引 | data-model.md 索引策略应包含 | PARTIAL |
| MC-D07 | 禁用药材名称后缀"(已停用)" | API/DTO 设计未提及此 UI 行为 | PARTIAL |
| MC-D08 | 验方导入仅 Validated | formulas API + MC API | COVERED |
| MC-D09 | 禁用药材跳过+提示 | MC API 导入设计 | COVERED |
| MC-D10 | 乐观锁 RowVersion + 3次重试 | data-model.md BaseEntity + ERR-30501/502 | COVERED |
| MC-D11 | 排序: 列表 DESC, 队列 ASC | API 端点默认排序 | COVERED |
| MC-D12 | 验方导入为数据复制 | API 设计已体现 | COVERED |
| MC-D13 | 历史处方复制价格从药材库实时获取 | API doc FR-MC-018 组合路径 | COVERED |
| MC-D14 | 处方总价公式 | shared.md DTO 或 API 应体现 | PARTIAL |
| MC-D15 | IsPrinted 提升到聚合根 | data-model.md v1.1 + API ERR-30403/404 | COVERED |
| MC-D16 | 患者禁用与医案联动 | ERR-30105 + patients API status 端点 | COVERED |

#### Sync 决策 (8条)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| Decision 1 | 冲突手动逐条解决 | dual-mode.md 冲突解决章节 | COVERED |
| Decision 2 | 本地模式功能受限 | dual-mode.md TBD-01 | **DISCREPANCY** |
| Decision 3 | MedicalCase 同步已确定 | dual-mode.md 仍标记 v2.0 规划 | **DISCREPANCY** |
| Decision 4 | 自动同步提示 v1.0 不实现 | N/A (明确排除) | COVERED |
| Decision 5 | 同步进度 UI: 步骤指示器+进度条 | desktop.md 无 SyncViewModel UI 设计 | PARTIAL |
| Decision 6 | 失败恢复: 重新开始 + Checksum 跳过 | sync API + PRD 自包含 | COVERED |
| Decision 7 | 冲突解决 UI: 左右对比+差异高亮 | desktop.md 无此 UI 设计 | PARTIAL |
| Decision 8 | 模式切换前检查未同步变更 | dual-mode.md 切换流程仅4步，无检查 | PARTIAL |

#### **CRITICAL DISCREPANCY: MedicalCase 同步状态不一致**

| 文档 | MedicalCase 同步状态 |
|------|---------------------|
| sync.md PRD (v3.0) | "已确定: 详细设计已完成" -- 包含完整聚合级同步方案 |
| dual-mode.md 设计文档 | "v1.0 不支持... v2.0 规划" (TBD-01, TBD-03, 实体表) |

**根因**: sync.md PRD 在 v3.0 (2026-02-18) 新增了 MedicalCase 同步详细设计，但 dual-mode.md 未同步更新。
**影响**: 开发时会遇到 PRD 要求实现但设计文档明确排除的矛盾。

#### NFR 决策 (10条: NFR-D01~D10)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| NFR-D01 | API 响应四级分类 | nfr.md 自包含 (NFR-PERF-001) | COVERED |
| NFR-D02 | 并发 1-3 人 | nfr.md 自包含 (NFR-PERF-004) | COVERED |
| NFR-D03 | SQLite 字段级加密 | nfr.md 详细实现路径 + 需 desktop.md 配合 | PARTIAL |
| NFR-D04 | 审计日志保留 1 年 | nfr.md NFR-SEC-005 + logging 决策4 | COVERED |
| NFR-D05 | RTO=30min, RPO=24h | nfr.md NFR-AVAIL-002 | COVERED |
| NFR-D06 | 备份策略 | nfr.md NFR-AVAIL-001 | COVERED |
| NFR-D07 | 缓存失效: 主动标签+TTL | server.md 缓存策略章节 + nfr.md 5.3 | COVERED |
| NFR-D08 | PrescriptionsCache 删除 | server.md + nfr.md 5.1 已删除 | COVERED |
| NFR-D09 | 推荐 8GB 内存 | nfr.md NFR-PERF-003 | COVERED |
| NFR-D10 | 分页参数全局统一 | nfr.md NFR-API-001 + 各模块 ERR 对齐 | COVERED |

#### UI Patterns 决策 (6条: UI-D01~D06)

| 决策 | 内容 | 设计覆盖 | 状态 |
|------|------|---------|------|
| UI-D01 | 即时搜索 + 300ms 防抖 | desktop.md 无全局 UI 模式章节 | PARTIAL |
| UI-D02 | 保存后返回列表 | desktop.md 无导航规则设计 | PARTIAL |
| UI-D03 | 统一删除确认 | desktop.md 无删除确认组件设计 | PARTIAL |
| UI-D04 | Clinical/Management 菜单过滤 | desktop.md 无菜单模式设计 | PARTIAL |
| UI-D05 | 双列表单布局 | desktop.md 无表单布局规范 | PARTIAL |
| UI-D06 | 失焦校验+提交校验 | desktop.md 无验证策略设计 | PARTIAL |

#### 其他模块决策 (合并统计)

| 模块 | D 条数 | COVERED | PARTIAL | MISSING |
|------|--------|---------|---------|---------|
| Herbs | 2 | 2 | 0 | 0 |
| Formulas | 3 | 3 | 0 | 0 |
| Printing | 6 | 6 | 0 | 0 |
| Card Reader | 2 | 2 | 0 | 0 |
| Health Diagnostics | 6 | 5 | 1 | 0 |
| Error Handling | 7 | 4 | 3 | 0 |
| Logging | 7 | 4 | 3 | 0 |

### D 维度汇总

| 状态 | 数量 | 占比 |
|------|------|------|
| COVERED | 62 | ~67% |
| PARTIAL | 28 | ~30% |
| DISCREPANCY | 2 | ~2% |
| MISSING | 1 | ~1% |
| **总计** | **~93** | 100% |

### 关键发现
1. **CRITICAL**: MedicalCase 同步 -- PRD (sync.md v3.0) 已确定支持，dual-mode.md 仍标 v2.0
2. **desktop.md 欠债**: UI-D01~D06 全部 PARTIAL，缺少全局 UI 模式/导航/验证设计章节
3. **server.md 欠债**: Token Family 撤销、审计日志写入机制、日志清理服务等缺设计

---

## Phase 3: NFR 交叉比对结果 (16 条)

### 按条目比对

| NFR 编号 | 内容 | 设计覆盖 | 状态 |
|----------|------|---------|------|
| NFR-PERF-001 | API 响应时间 (四级 P95) | server.md 慢查询阈值引用 | COVERED |
| NFR-PERF-002 | Desktop 客户端响应 | desktop.md 无性能预算章节 | PARTIAL |
| NFR-PERF-003 | 客户端运行环境 (推荐 8GB) | nfr.md 自包含，desktop.md 无引用 | PARTIAL |
| NFR-PERF-004 | 并发能力 (1-3人, 连接池20) | server.md 未显式引用，但配置对齐 | COVERED |
| NFR-DATA-001 | 数据规模 (5年预估) | data-model.md 实体设计对齐 | COVERED |
| NFR-DATA-002 | 数据库容量 (<200MB) | nfr.md 自包含 | COVERED |
| NFR-DATA-003 | 索引策略 (B-Tree 4实体) | data-model.md 索引章节对齐 | COVERED |
| NFR-AVAIL-001 | 数据备份策略 | nfr.md 自包含，server.md/desktop.md 无备份章节 | PARTIAL |
| NFR-AVAIL-002 | 故障恢复 (RTO/RPO) | dual-mode.md 降级模式 | COVERED |
| NFR-AVAIL-003 | 数据库重试与容错 | server.md EF Core RetryPolicy | COVERED |
| NFR-SEC-001 | 认证安全 (Token/限流) | server.md + auth API | COVERED |
| NFR-SEC-002 | 密码策略 | server.md + users API | COVERED |
| NFR-SEC-003 | 数据传输安全 (HTTPS/HSTS) | server.md 安全配置 | COVERED |
| NFR-SEC-004 | SQLite 加密 (AES-256+DPAPI) | nfr.md 含完整实现路径，desktop.md 无 | PARTIAL |
| NFR-SEC-005 | 审计日志保留 (365天/90天) | nfr.md 自包含，server.md 无清理服务设计 | PARTIAL |
| NFR-API-001 | 分页参数全局规范 | nfr.md + 各模块 ERR 码对齐 | COVERED |

### NFR 维度汇总

| 状态 | 数量 | 占比 |
|------|------|------|
| COVERED | 11 | 68.75% |
| PARTIAL | 5 | 31.25% |
| MISSING | 0 | 0% |
| **总计** | **16** | 100% |

### 关键发现
1. **NFR PRD 质量高**: nfr.md 自身包含实现路径 (如 NFR-SEC-004 EF Core Value Converter)，部分弥补了设计文档缺失
2. **PARTIAL 根因一致**: 5 个 PARTIAL 项均因 server.md/desktop.md 缺少对应设计章节 (备份、性能预算、加密、清理服务)
3. **无 MISSING**: 所有 NFR 在 PRD + 设计文档组合下均有覆盖

---

## 全维度综合汇总

| 维度 | 条目数 | COVERED | PARTIAL | MISSING | DISCREPANCY |
|------|--------|---------|---------|---------|-------------|
| FR | 131 | 109 (83.2%) | 14 (10.7%) | 8 (6.1%) | 0 |
| ERR | ~133 | ~133 (架构层) | 7模块API端点层 | 0 | 4码 (README多出) |
| BR | 6 | 5 (83.3%) | 1 (16.7%) | 0 | 0 |
| D | ~93 | ~62 (67%) | ~28 (30%) | ~1 (1%) | 2 (MC同步) |
| NFR | 16 | 11 (68.75%) | 5 (31.25%) | 0 | 0 |
| **合计** | **~379** | **~320 (84%)** | **~55 (15%)** | **~9 (2%)** | **6** |

## 遗漏清单 (Phase 4 报告用)

### 严重级别: CRITICAL (1项)
1. **MedicalCase 同步 PRD-设计文档不一致**: sync.md PRD v3.0 已确定支持，dual-mode.md 仍标注 v2.0 规划

### 严重级别: HIGH (8项 - FR MISSING)
1. FR-PAT-011/012: 患者引用检查端点
2. FR-ERR-005/007/008: 客户端异常体系 (严重度/追踪码/通知映射)
3. FR-LOG-003: 敏感数据脱敏 (SensitiveDataMasker)
4. FR-LOG-007: API请求自动日志 (ApiLoggingFilter)
5. FR-CFG-004: 生产环境启动配置验证

### 严重级别: MEDIUM (系统性欠债)
1. **API Reference 错误码缺失**: 7/9 模块 API 文档缺少端点级 MCCEE 码
2. **desktop.md UI 模式缺失**: UI-D01~D06 无对应设计，缺全局 UI 规范章节
3. **server.md 运维设计缺失**: Token Family 撤销、审计日志写入、日志清理、备份服务无设计
4. **NFR-SEC-004 实现设计**: SQLite 加密在 nfr.md 有路径但 desktop.md 无架构集成

### 严重级别: LOW (4项 - README 多出)
1. API README 定义了 4 个 PRD 未涵盖的 Auth 错误码 (PasswordExpired/SessionNotFound/SessionExpired/ConcurrentSessionLimit)
