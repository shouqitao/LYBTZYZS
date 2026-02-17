# Progress Log: PRD 深化讨论与补全

## Session: 2026-02-12

### Phase 0: 深化讨论提纲编制
- **Status:** complete
- Actions taken:
  1. 全量读取 14 个 PRD 文档 + 产品层 4 文件 + 架构层 13 文件 + API 10 文件 + 开发指南 5 文件
  2. 逐模块深度评估 (验收标准/业务规则/数据模型/错误码/状态机/UI/NFR/交互/决策)
  3. 质量分级: 9 个完善 + 5 个良好
  4. 识别 5 大系统级薄弱面: NFR/UI-UX/Sync/模块交互/Shell
  5. 编制 10 轮讨论提纲 (3 系统级 + 5 薄弱模块 + 2 核心补强)
- Files created:
  - docs/plans/2026-02-12-prd-deepening-outline.md (讨论提纲 v1.0)

---

## Session: 2026-02-17

### Round 1: 非功能性需求 (NFR)
- **Status:** complete
- **讨论主题**: 性能指标 / 数据量预估 / 可用性与可靠性 / 安全性
- Actions taken:
  1. 全面搜索代码中已有的 NFR 配置 (17个 Options 类, ~100 个参数)
  2. 逐主题结构化讨论 (4 个主题, 8 个决策点)
  3. 生成 NFR 文档: docs/02-requirements/nfr.md
  4. 更新 README 索引: docs/02-requirements/README.md
- Files created:
  - docs/02-requirements/nfr.md (NFR 文档 v1.0)
- Files updated:
  - docs/02-requirements/README.md (新增 NFR 索引)
- Key decisions:
  - NFR-D01: API 响应时间四级分类
  - NFR-D02: 并发用户 1-3 人
  - NFR-D03: SQLite 字段级加密 (IdCardNumber + PhoneNumber)
  - NFR-D04: 安全审计保留 1 年
  - NFR-D05: RTO=30min, RPO=24h
  - NFR-D06: 数据备份策略 (SQL Server 日备 + SQLite 启动备份)

### Round 5: Desktop Shell 深化
- **Status:** complete
- **讨论主题**: 菜单结构 / Region定义 / 状态栏 / 启动画面 / 导航历史
- Files updated:
  - docs/02-requirements/desktop-shell.md (v1.0 -> v2.0)
- Key decisions:
  - SHELL-D04: 状态栏标准信息 (用户+模式+版本)
  - SHELL-D05: 进度报告型启动画面
  - SHELL-D06: 仅支持后退导航

### Round 4: 数据同步深化
- **Status:** complete
- **讨论主题**: 同步进度 UI / 失败恢复 / 冲突解决 UI / 模式切换检查 / DTO 定义
- Actions taken:
  1. 讨论确认 4 个同步模块决策 (决策#5~#8)
  2. 补充 4 个 DTO 定义 (Metadata/DiffResult/ConflictDetail/Result)
  3. 新增冲突解决 UI 左右对比布局规格
  4. 深化 FR-SYNC-007 (进度 UI + 失败恢复 + 结果汇总)
  5. 深化 FR-SYNC-008 (切换前检查 + 切换流程 + 回退策略)
- Files updated:
  - docs/02-requirements/sync.md (v1.2 -> v2.0)
- Key decisions:
  - SYNC-D05: 步骤指示器进度 UI
  - SYNC-D06: 失败后重新开始 (Checksum 防重复)
  - SYNC-D07: 冲突左右对比 + 差异字段高亮
  - SYNC-D08: 切换前未同步检查 + 自动回退

### Round 3: UI/UX 交互模式与规范
- **Status:** complete
- **讨论主题**: 搜索模式 / 表单布局 / 保存行为 / 删除确认 / 工作区模式 / 校验提示
- Actions taken:
  1. 讨论确认 6 个 UI/UX 决策
  2. 生成全局 UI/UX 交互规范文档: docs/02-requirements/ui-patterns.md
  3. 更新 README 索引
- Files created:
  - docs/02-requirements/ui-patterns.md (UI/UX 规范 v1.0)
- Files updated:
  - docs/02-requirements/README.md (新增 ui-patterns.md 索引)
- Key decisions:
  - UI-D01: 即时搜索 + 300ms 防抖
  - UI-D02: 保存后返回列表 (医案特殊处理)
  - UI-D03: 统一删除确认
  - UI-D04: Clinical/Management 菜单过滤
  - UI-D05: 双列表单布局
  - UI-D06: 失焦即时校验

### Round 2: 核心业务流程与模块交互
- **Status:** complete
- **讨论主题**: 端到端业务流程 / 模块依赖矩阵 / 跨模块事件 / 数据一致性
- Actions taken:
  1. 分析 Server 端 Service/Controller/Repository 依赖关系
  2. 分析 Desktop 端 EventAggregator 事件系统
  3. 分析 EF Core 实体外键关系
  4. 讨论确认 6 个跨模块数据规则
  5. 大幅扩展 vision.md: 新增 4 个详细业务流程 (Mermaid 序列图) + 模块依赖矩阵 + 跨模块规则 + 事件架构
- Files updated:
  - docs/01-product/vision.md (v1.1 -> v2.0: 从 130 行扩展到 319 行)
- Key decisions:
  - R2-D01: 复诊支持复制历史处方
  - R2-D02: 保存后流程内提示打印
  - R2-D03: 药材价格快照 (历史价保持)
  - R2-D04: 患者引用保护 (禁止删除有医案的患者)
  - R2-D05: 禁用药材标记展示
  - R2-D06: 聚合根事务边界

### Round 7: 日志 + 健康诊断 深化
- **Status:** complete
- **讨论主题**: 保留策略矛盾 / 未文档化功能 / Desktop日志配置 / 医案审计归属 / 异常告警范围
- Actions taken:
  1. 全面探索日志+诊断代码 (2500+ 行, 20+ 文件)
  2. 发现 NFR vs 代码矛盾: SecurityAuditCleanupService 30天 vs NFR 365天
  3. 识别 6 个已实现但未文档化的功能
  4. 讨论确认 5 个决策点
  5. 更新 3 个 PRD 文档: logging.md, health-diagnostics.md, medical-cases.md
- Files updated:
  - docs/02-requirements/logging.md (v1.0 -> v2.0): 新增 FR-LOG-005~007, Desktop 配置表, 审计交叉引用, 4 条决策
  - docs/02-requirements/health-diagnostics.md (v1.1 -> v2.0): 新增 FR-SYS-008~009, 2 条决策
  - docs/02-requirements/medical-cases.md (v1.2 -> v1.3): FR-MC-012 增强 (数据模型+字段级diff+交叉引用)
- Key decisions:
  - LOG-D04: 审计保留以 NFR 365 天为准，代码改可配置
  - LOG-D05: Error/Fatal 永久保留
  - LOG-D06: 异常告警 v2.0 范围
  - LOG-D07: 医案审计归属 medical-cases.md
  - SYS-D05: 启动诊断不阻塞应用
  - SYS-D06: Desktop 慢步骤阈值 3 秒
- Code debt identified:
  - SecurityAuditCleanupService 需改为可配置 + 默认 365 天 + 分批删除

### Round 8: 配置 + 异常处理 深化
- **Status:** complete
- **讨论主题**: 配置变更管理 / FeatureToggle治理 / 异常处理链 / 异常展示映射
- Actions taken:
  1. 代码调研: 配置热更新机制 (ValidateOnStart vs 无ValidateOnStart)
  2. 代码调研: FeatureToggle 实现方式 + 16个开关状态
  3. 代码调研: DesktopExceptionHandler + ClientErrorMessageMapper (40+ 错误码)
  4. 代码调研: 追踪码功能 (GetSafeMessageWithTrackingCode)
  5. 代码调研: ProductionConfigurationValidator 启动验证
  6. 发现代码与PRD差距: MessageBox统一展示 vs ui-patterns.md Toast分层规范
  7. 讨论确认 5 个决策点
  8. 更新 2 个 PRD 文档
- Files updated:
  - docs/02-requirements/configuration.md (v1.0 -> v2.0): 新增 FR-CFG-004、配置变更行为表、FeatureToggle UI规则+v1.0状态表、CardReader开关、3条决策
  - docs/02-requirements/error-handling.md (v1.0 -> v2.0): 新增 FR-ERR-006~008、错误消息映射体系、追踪码、异常通知类型映射、3条决策
- Key decisions:
  - CFG-D04: FeatureToggle=false -> 隐藏(Collapsed)
  - CFG-D05: 安全配置需重启，运维配置支持热更新
  - CFG-D06: Production环境Critical配置缺失阻止启动
  - ERR-D05: 异常展示遵循 ui-patterns.md 3.3 节
  - ERR-D06: ClientErrorMessageMapper 40+ 错误码映射纳入 v1.0
  - ERR-D07: 追踪码纳入 v1.0
- Code debt identified:
  - Desktop异常展示: 当前统一MessageBox，需重构为Toast+对话框分层 (与ui-patterns.md 3.3节对齐)

### Round 9: 医案管理边界条件深化
- **Status:** complete
- **讨论主题**: 生命周期边界 / 处方药材联动 / 并发锁定 / 搜索排序
- Actions taken:
  1. 代码调研: MedicalCase 4 个方面边界条件 (生命周期/药材联动/并发锁定/搜索)
  2. 讨论确认 7 个问题 (Q1~Q7)，产出 9 条决策 (MC-D04~D12)
  3. 更新 3 个 PRD 文档
- Files updated:
  - docs/02-requirements/medical-cases.md (v1.3 -> v1.5): 新增边界条件章节，FR-MC-009/016/017 更新，9 条决策
  - docs/02-requirements/patients.md (v1.2 -> v1.3): FR-PAT-005 引用检查，FR-PAT-011 CanDelete 变更
  - docs/02-requirements/formulas.md (v1.2 -> v1.3): FR-FORM-006 处方导入过滤规则
- Key decisions:
  - MC-D04: 患者删除引用检查
  - MC-D05: 草稿不自动清理
  - MC-D06: DB 索引接受现状
  - MC-D07: 禁用药材展示"(已停用)"
  - MC-D08: 验方导入仅 Validated
  - MC-D09: 禁用药材跳过导入
  - MC-D10: 乐观锁并发策略
  - MC-D11: 排序规则明确化
  - MC-D12: 验方导入数据复制独立性

### Round 10: 五模块快速扫描
- **Status:** complete
- **讨论主题**: 认证多端登录 / 角色变更会话 / Admin保护 / 身份证唯一 / 患者合并 / 验方共享
- Actions taken:
  1. 代码调研: 5 个模块各 2 个边界问题
  2. 讨论确认 5 个问题 (Q8~Q12)，产出 6 条决策
  3. 更新 4 个 PRD 文档
- Files updated:
  - docs/02-requirements/auth.md (v1.0 -> v1.1): FR-AUTH-001 单会话登录，AUTH-D06/D07
  - docs/02-requirements/users.md (v1.1 -> v1.2): FR-USER-004 角色变更即时生效，FR-USER-011 禁用保护，USER-D03
  - docs/02-requirements/patients.md (v1.3 -> v1.4): FR-PAT-001 身份证必填+唯一，PAT-D03/D04
  - docs/02-requirements/medical-cases.md (v1.5): MC-D12 补充
- Key decisions:
  - AUTH-D06: 单会话登录
  - AUTH-D07: 角色变更即时撤销 Token
  - USER-D03: 最后 Admin 禁用保护
  - PAT-D03: 身份证号必填+唯一
  - PAT-D04: v1.0 无患者合并
  - MC-D12: 验方导入数据独立性

---

## Phase 3 完成总结

| 指标 | R9 | R10 | 合计 |
|------|-----|-----|------|
| 讨论问题 | 7 | 5 | 12 |
| 决策产出 | 9 | 6 | 15 |
| 更新文档 | 3 | 4 | 7 (去重后 6) |

---
*Updated: 2026-02-17*
