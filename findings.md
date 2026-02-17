# Research Findings: PRD 深化讨论

## PRD 文档体系评估结果 (2026-02-12)

### 文档质量分级

| 评级 | 模块 | FR 数 | 说明 |
|------|------|-------|------|
| **最完善** | 医案管理 | 17 | 29 错误场景 + 状态机 Mermaid + 审计矩阵 + UI 描述 |
| **完善** | 认证 | 13 | 登录状态机 + DPAPI/HMAC 安全细节 + Token Family |
| **完善** | 用户管理 | 12 | 19 错误场景 + 防枚举攻击 |
| **完善** | 患者管理 | 12 | 导入导出 + 敏感数据掩码 |
| **完善** | 药材管理 | 13 | 双导入 (Excel+JSON) + 重复处理策略 |
| **完善** | 验方管理 | 13 | 延迟绑定 + ValidationStatus 流转 |
| **完善** | 异常处理 | 5 | 异常继承体系 + 11 种异常映射 |
| **完善** | 配置参数 | 3 | 17 个 Options 类 ~100 个参数 |
| **完善** | 身份证读卡器 | 2 | 13 个设备错误码 + 完整接口定义 |
| **良好** | 数据同步 | 8 | **缺**: DTO 定义、冲突 UI、模式切换细节 |
| **良好** | 打印 | 4 | **缺**: 模板排版细节、样例 |
| **良好** | 系统健康 | 7 | **缺**: 响应 JSON 模型 |
| **良好** | 日志审计 | 4 | **缺**: 与 health-diagnostics 功能重叠 |
| **良好** | Desktop Shell | 7 | **缺**: 菜单层级、Region 列表、账户设置 |

### 5 大系统级薄弱面 (原始评估)

1. ~~**NFR 完全缺失**~~ -- **已解决** (R1, nfr.md)
2. ~~**UI/UX 描述普遍不足**~~ -- **已解决** (R3, ui-patterns.md)
3. ~~**数据同步深度不足**~~ -- **已解决** (R4, sync.md DTO+UI+切换)
4. ~~**模块间交互图缺失**~~ -- **已解决** (R2, vision.md 依赖矩阵+事件架构)
5. ~~**Desktop Shell 部分薄弱**~~ -- **已解决** (R5, desktop-shell.md 菜单+Region+状态栏+启动画面)

---

## Round 1: NFR 讨论发现 (2026-02-17)

### 代码 vs PRD 对齐情况

代码中已有完善的 NFR 配置实现 (17个 Options 类, ~100 个配置参数)，但 PRD 文档中完全缺失 NFR 描述。
R1 讨论将代码现状正式化为 NFR 文档，并填补了以下缺失指标:

| 指标类型 | 之前状态 | R1 后状态 |
|----------|---------|----------|
| API 响应时间目标 | 仅有慢查询阈值 1000ms | 四级分级: 500ms/1s/2s/5s |
| 数据量预估 | 无 | 全实体规模定义 + 5年容量预估 |
| RTO/RPO | 无 | RTO=30min, RPO=24h |
| 备份策略 | 无文档 | SQL Server 日备 + SQLite 启动备份 |
| SQLite 加密 | 明文存储 | 字段级加密 (IdCardNumber + PhoneNumber) |
| 审计日志保留 | 代码中无明确配置 | 安全审计 1年, 系统日志 90天 |

### 关键决策

- SQLite 采用**字段级加密**而非 SQLCipher 整库加密 (NFR-D03)
- 安全审计日志保留期从未定义提升到 **1年** (NFR-D04)

---

## Round 7: 日志+健康诊断 讨论发现 (2026-02-17)

### 代码 vs PRD 差距分析

**关键矛盾**:
- NFR-SEC-005 规定安全审计保留 365 天，但 SecurityAuditCleanupService 硬编码 30 天

**6 个已实现但未文档化的功能**:

| 功能 | 代码位置 | PRD 补充目标 |
|------|---------|-------------|
| SecurityAuditCleanupService | WebAPI/BackgroundServices/ | logging.md FR-LOG-006 |
| LogCleanupService | Infrastructure/Logging/ | logging.md FR-LOG-005 |
| DatabaseStartupDiagnostics | WebAPI/HealthCheck/ | health-diagnostics.md FR-SYS-008 |
| StartupDiagnostics (Desktop) | Desktop/Shell/Services/Diagnostics/ | health-diagnostics.md FR-SYS-009 |
| ApiLoggingFilter | WebAPI/Filters/ | logging.md FR-LOG-007 |
| MedicalCaseAuditLog 数据模型 | Module.MedicalCase/Services/ | medical-cases.md FR-MC-012 (增强) |

### 两个清理服务对比

| 特性 | SecurityAuditCleanupService | LogCleanupService |
|------|---------------------------|-------------------|
| 保留天数 | 硬编码 30 天 (需改为 365) | 可配置 (默认 90 天) |
| 执行时间 | 每日凌晨 3 点 | 每 24 小时 |
| 分批删除 | 否 (EF 一次加载) | 是 (每批 1000 条) |
| Error/Fatal 保留 | 不区分 | 永久保留 |

### R7 决策

| 决策 | 结论 |
|------|------|
| 审计保留策略 | 以 NFR 365 天为准，代码改为可配置 |
| 未文档化功能 | 6 个全部补充到 PRD |
| Desktop 日志配置 | 补充到 logging.md 配置参数表 |
| 医案审计归属 | medical-cases.md，logging.md 交叉引用 |
| 异常告警 | v2.0 范围，v1.0 仅保留日志 |

---

## Round 8: 配置+异常处理 代码调研发现 (2026-02-17)

### 配置模块: 代码 vs PRD 差距

**1. 热更新 vs 重启 -- PRD 未文档化**

代码中已区分两类配置注册方式:

| 配置项 | 注册方式 | 热更新 | ValidateOnStart |
|--------|---------|--------|-----------------|
| JwtOptions | ValidateDataAnnotations + 自定义Validator + ValidateOnStart | 否 (需重启) | 是 |
| DatabaseOptions | ValidateDataAnnotations + 自定义Validator + ValidateOnStart | 否 (需重启) | 是 |
| SecurityOptions | ValidateDataAnnotations + 自定义Validator + ValidateOnStart | 否 (需重启) | 是 |
| FeatureToggleOptions | ValidateDataAnnotations (无 ValidateOnStart) | 是 | 否 |
| LoggingOptions | ValidateDataAnnotations (无 ValidateOnStart) | 是 | 否 |
| PrescriptionOptions | ValidateDataAnnotations (无 ValidateOnStart) | 是 | 否 |

Desktop 端 Prism 容器直接绑定 Options.Create()，本质不支持热更新。

**2. 生产环境启动验证 -- PRD 未文档化**

ProductionConfigurationValidator 在 Production 环境启动时强制检查:
- ConnectionStrings:DefaultConnection (Critical)
- Jwt:SecretKey >= 32 字节 (Critical)
- DefaultPasswords (Important)
- SystemAdmin 配置 (Important)

验证失败直接 Environment.Exit(1)，控制台输出详细错误+修复指导。

**3. FeatureToggles 数量差异**

PRD 文档列出 16 个开关，代码中实际 13 个 (ConsultationViewDetail/Search + PrescriptionClone/Export/ViewDetail/Search + MedicalCase 5个)。需核对差异。

### 异常处理模块: 代码 vs PRD 差距

**1. ClientErrorMessageMapper -- PRD 未文档化**

代码中存在完整的客户端错误消息映射体系:
- HTTP 状态码映射 (~10 个状态码 -> 中文消息)
- 业务错误码映射 (40+ 条，按模块分组: 用户/患者/医案/处方/药材/验方)
- 追踪码支持 (GetShortTrackingCode)

**2. 异常展示方式 -- PRD 未明确**

代码中使用两个通知服务:
- UserNotificationService: 基于 MessageBox
- NotificationService: 基于 MessageBox + EventAggregator 事件

当前展示方式统一为 **MessageBox 对话框**，无 Toast / StatusBar 通知。

**3. 追踪码功能 -- PRD 未文档化**

ClientErrorMessageMapper.GetSafeMessageWithTrackingCode() 支持在错误消息末尾附加追踪码，格式: "如需帮助，请提供追踪码: XXXX"

---

## Round 9: 医案管理边界条件发现 (2026-02-17)

### 代码调研关键发现

| 方面 | 发现 |
|------|------|
| 患者删除级联 | 软删除无级联，医案独立 IsDeleted。需增加引用检查 |
| 草稿清理 | 无自动清理，BR-001 卡点足够 |
| DB 唯一索引 | 仅 Active 状态，代码层 BR-001 补充覆盖 Draft |
| 药材禁用展示 | PrescriptionItem 保留快照，但无禁用标记 UI |
| 验方导入过滤 | 当前不过滤 ValidationStatus，允许 Draft 验方导入 |
| 锁定机制 | 计算属性，无后台任务，0 点自动生效 |
| 并发控制 | RowVersion 乐观锁 + 3 次重试 |
| 排序规则 | 列表 DESC、待诊 ASC，代码已实现但 PRD 未记录 |

---

## Round 10: 五模块快速扫描发现 (2026-02-17)

### 关键发现

| 模块 | 发现 | 处理 |
|------|------|------|
| 认证 | 允许多点登录 (Token Family 独立) | 改为单会话 (AUTH-D06) |
| 认证 | 角色变更后 Token 继续有效至过期 | 改为即时撤销 (AUTH-D07) |
| 用户 | 删除保护已实现，禁用保护缺失 | 补充禁用保护 (USER-D03) |
| 患者 | IdNumber 可选且无唯一约束 | 改必填+唯一 (PAT-D03) |
| 患者 | 无合并功能 | v1.0 不含 (PAT-D04) |
| 药材 | Category 自由文本，Price decimal(18,2) | 已覆盖，无需变更 |
| 验方 | IsShared 可见性 PRD 已覆盖，导入为数据复制 | 补充说明 (MC-D12) |

---
*Updated: 2026-02-17*
