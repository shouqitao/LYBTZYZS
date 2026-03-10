# v1.0 Release Roadmap

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **基于**: MoSCoW 优先级排序 (user-story-map.md) + Code-PRD 审计 (2026-02-28 + 2026-03-06)
> **审计基线**: 138 US -- 初始 110 Implemented / 19 Partial / 2 Not Implemented / 7 New (Registration) -> 最终 137 Done / 1 Removed

---

## Sprint 规划原则

| 原则 | 说明 |
|------|------|
| Sprint 周期 | 2 周 |
| Must Have 优先 | Partial Must Have US 排入最早可用 Sprint |
| 依赖顺序 | 按模块依赖链顺序排列 |
| 已完成标记 | 审计确认已实现的 US 标记为 Done |
| 容量估算 | 每 Sprint 预估 8-12 US (视复杂度) |

---

## 模块依赖图

### 依赖链

```
基础设施层 (独立):
  Auth, Config, ERR, LOG, SYS, Shell

数据层 (依赖 Auth):
  Auth -> Users
  Auth -> Patients
  Auth -> Herbs

业务层 (依赖数据层):
  Herbs -> Formulas -> MedicalCase
  Patients -> MedicalCase
  MedicalCase -> Printing

增强层 (增强依赖):
  CardReader -.-> Patients (增强: 非阻塞)
  Sync -.-> Herbs, Patients, Formulas (增强: 需数据层就绪)
```

### 依赖类型定义

| 类型 | 含义 | 示例 |
|------|------|------|
| 阻塞依赖 (`->`) | A 未完成则 B 无法开始 | Herbs -> Formulas (验方需要药材数据) |
| 增强依赖 (`-.->`) | A 完成可增强 B 但非阻塞 | CardReader -.-> Patients (身份证自动填充) |

---

## Release 定义

| Release | 范围 | 标准 |
|---------|------|------|
| v1.0-alpha | 全部 Must Have (51 US) 实现并通过测试 | MVP 可用 |
| v1.0-beta | Must + Should (105 US) 实现 | 功能完整 |
| v1.0-rc | Must + Should + 部分 Could | 生产就绪 |

---

## Must Have US 分配 (51 US)

### Done (43 US) -- 审计确认已实现

| 模块 | US 编号 | 名称 |
|------|---------|------|
| Auth | US-AUTH-001, 002, 003, 005, 008, 009, 010, 012 | 登录/自动登录/Token/登出/凭证存储/状态机/登录界面 |
| Patients | US-PAT-001, 002, 003, 004 | CRUD + 拼音码搜索 |
| Herbs | US-HERB-001, 002, 003, 004, 005 | CRUD + 引用检查 |
| Formulas | US-FORM-001, 003, 004, 005, 006 | 创建/详情/更新/删除/启用禁用 |
| MedicalCase | US-MC-001, 002, 003, 004, 005, 006, 007, 009, 013 | 完整 CRUD + 聚合保存 + 权限 |
| Users | US-USER-001, 002, 003, 004, 005 | CRUD |
| Shell | US-SHELL-001, 002, 003, 004, 005 | 启动/登录/会话/导航/菜单 |
| Config | US-CFG-001, 002 | 服务端+客户端配置 |

### Sprint 2: Registration 模块 + Must 补全 (8 US) -- COMPLETE

**目标**: 实现挂号管理模块，补全 Must Have

| US 编号 | 模块 | 名称 | 完成状态 | 实现说明 |
|---------|------|------|---------|---------|
| US-REG-001 | Registration | 前台创建挂号 | Done | RegistrationsController + RegistrationService 全栈实现; EF Migration |
| US-REG-002 | Registration | 医生快速看诊 | Done | RegistrationListViewModel 队列接诊流程 |
| US-REG-003 | Registration | 查看挂号队列 | Done | RegistrationListView 按日期/医生/状态过滤 |
| US-REG-004 | Registration | 前台取消挂号 | Done | CancelAsync + 权限校验 |
| US-REG-005 | Registration | 状态自动跟随医案完成 | Done | MedicalCase 完成时联动 Registration 状态 |
| US-REG-006 | Registration | 医案取消联动 | Done | MedicalCase 取消时联动 Registration 状态 |
| US-FORM-002 | Formulas | 查看验方列表 | Done | CODE-23 修复: HerbCount 正确显示; TotalPrice 列已移除 |
| US-SYNC-008 | Sync | 模式切换 | Done | 切换前同步变更检查 + pre-validation 实现 |

**Sprint 2 完成 = v1.0-alpha 达成** (51/51 Must Have)

---

## Should Have US 分配 (54 US)

### Done (37 US) -- 审计确认已实现

| 模块 | US 编号 | 名称 |
|------|---------|------|
| Auth | US-AUTH-004, 006, 011 | 重放检测/不活跃超时/Token刷新失败分级 |
| Patients | US-PAT-005 | 删除患者 |
| Herbs | US-HERB-006, 009, 011 | 启用禁用/Excel导入/导出 |
| Formulas | US-FORM-008, 009, 010, 012 | 共享/延迟绑定/待验证/导出 |
| MedicalCase | US-MC-008, 014, 017 | 取消/锁定规则/待诊队列 |
| Users | US-USER-008, 009, 010, 011, 012 | 重置密码/修改密码/个人资料/启用禁用/获取当前 |
| Sync | US-SYNC-001, 002, 003, 004, 005 | 元数据/比对/上传/下载 |
| Error | US-ERR-001, 002, 003, 004, 005, 006 | 全局异常/ProblemDetails/客户端异常/类型体系/严重度/消息映射 |
| Logging | US-LOG-001, 002, 003, 007 | 结构化日志/审计/脱敏/API日志 |
| Shell | US-SHELL-006 | 启动诊断 |
| Config | US-CFG-003, 004 | 环境配置/启动验证 |
| Printing | US-PRINT-002, 003 | 打印预览/版本管理 |

### Sprint 3: 核心业务补全 (9 US) -- COMPLETE (2026-03-08)

**目标**: 补全 MedicalCase 高级功能 + Printing + Registration 历史 + 关键修复

| US 编号 | 模块 | 名称 | 完成状态 | 提交 |
|---------|------|------|---------|------|
| US-MC-016 | MedicalCase | 验方导入到处方 | Done | CODE-08 价格同步修复 |
| US-MC-018 | MedicalCase | 复制历史处方 | Done | CODE-08 价格同步修复 |
| US-MC-010 | MedicalCase | 跨医案搜索 | Done | EditModeStateMachine 延期为 MC-011 |
| US-MC-015 | MedicalCase | 打印触发 | Done | 审计确认已完成 |
| US-PRINT-001 | Printing | 处方打印 | Done | CODE-24/36/37 全部修复 |
| US-HERB-008 | Herbs | 批量删除 | Done | 审计确认已完成 |
| US-PAT-013 | Patients | 患者状态管理 | Done | CODE-22 活跃医案检查 |
| US-AUTH-013 | Auth | 认证事件体系 | Done | LoginStarted/LogoutStarted/SessionExtended |
| US-REG-007 | Registration | 挂号历史查询 | Done | 4 层过滤参数 (date/patient/doctor) |

### Sprint 4: 同步与外设 (5 US) -- COMPLETE (2026-03-09)

**目标**: 补全 Sync 高级功能 + CardReader + 编辑模式

**注**: US-AUTH-007 已被设计决策移除 (simplify-auth)，标记为 **Removed**，不占 Sprint 容量。

| US 编号 | 模块 | 名称 | 完成状态 | 实现说明 |
|---------|------|------|---------|---------|
| US-CARD-001 | CardReader | 读卡器连接与读取 | Done | CardReaderOptions 从 appsettings.json 读取 (PRD-13); MatchPatientAsync 降级链 IdNumber->Name+BirthDate->MultipleCandidates->NoMatch (PRD-15); PRD-14 (照片加密) v1.0 不阻塞 |
| US-CARD-002 | CardReader | 读卡数据填充 | Done | PRD-16 已实现; 8 个验收测试补全 |
| US-SYNC-006 | Sync | 同步删除 | Done | SyncResolution.ToDelete + SyncExecutionResult.DeletedCount/DeleteRejections; ExecuteSyncAsync Step 3 删除执行; SyncViewModel 状态消息含删除计数 |
| US-SYNC-007 | Sync | 完整同步工作流 | Done | SyncPhase enum (6状态 FSM); SyncResultSummary per-entity 结果摘要; SyncRetryDescriptor + SyncErrorCategory; SyncView 底栏增强 (步骤指示+错误状态+结果卡片+重试/重置) |
| US-MC-011 | MedicalCase | 编辑模式 | Done | IEditModeStateMachine + EditModeStateMachine (6状态 10事件 转换表驱动); WorkspaceEditState/WorkspaceEditEvent 枚举; 75 单元测试 |
| US-AUTH-007 | Auth | 登出前警告 | Removed | 设计决策已移除 (simplify-auth) |

---

## Could Have US 分配 (33 US)

### Done (30 US) -- 审计确认已实现

| 模块 | US 编号 | 名称 |
|------|---------|------|
| Patients | US-PAT-006, 007, 008, 009, 010, 011, 012 | 恢复/批量删除/导入/模板/导出/引用检查 |
| Herbs | US-HERB-007, 010, 012, 013 | 恢复/JSON导入/模板/引用检查 |
| Formulas | US-FORM-007, 011, 013 | 恢复/批量导入/模板 |
| MedicalCase | US-MC-012 | 审计日志 |
| Users | US-USER-006, 007 | 恢复/批量删除 |
| Health | US-SYS-001~009 | 健康检查/诊断/日志管理 |
| Error | US-ERR-008 | 异常通知映射 |
| Logging | US-LOG-004, 005, 006 | 日志级别/系统清理/审计清理 |
| Printing | US-PRINT-004 | 打印日志 |

### Sprint 5: v1.0-rc 生产就绪 (3 项 + 1 NFR) -- COMPLETE (2026-03-09)

**目标**: 关闭最后 2 个开放审计项 + 实现 NFR-AVAIL-001 备份，达成 v1.0-rc 生产就绪

| US/NFR 编号 | 模块 | 名称 | 完成状态 | 实现说明 |
|-------------|------|------|---------|---------|
| US-ERR-007 | Error | 错误追踪码 + TokenExpired | Done | ErrorCode.AuthAccessTokenExpired=10206; UnauthorizedException.TokenExpired() 工厂方法; ErrorCodeExtensions.ToCategory() 补全映射 |
| US-SHELL-007 | Shell | 状态栏用户名/版本号 | Done | GlobalStatusBar DP (CurrentUserName/AppVersion); XAML Grid.Column 3/4; Shell 绑定 CurrentUserDisplayName |
| NFR-AVAIL-001 | Infrastructure | 本地数据库启动自动备份 | Done | ILocalDbBackupService + LocalDbBackupService; BACKUP DATABASE T-SQL; 7天保留策略; LoginCoordinator fire-and-forget 集成 |

**Sprint 5 完成 = v1.0-rc 达成** (CODE-25/CODE-21 关闭, NFR-AVAIL-001 满足, 1621 tests 全通过)

---

## 时间线视图

| Sprint | 周期 | 重点模块 | Must | Should | Could/NFR | 目标 |
|--------|------|---------|------|--------|-----------|------|
| Done | - | 全模块 | 43 | 37 | 30 | 审计确认 |
| Sprint 2 | W1-W2 | Registration + Formulas + Sync | 8 | 0 | 0 | **COMPLETE** **v1.0-alpha** (Must 100%) |
| Sprint 3 | W3-W4 | MC + Printing + Herbs + Auth + REG | 0 | 9 | 0 | **COMPLETE** (2026-03-08) |
| Sprint 4 | W5-W6 | Sync + CardReader + MC | 0 | 5 | 0 | **COMPLETE** (2026-03-09) **v1.0-beta** |
| Sprint 5 | W7 | Error + Shell + Backup | 0 | 0 | 2+1NFR | **COMPLETE** (2026-03-09) **v1.0-rc** |
| Sprint 6 | W8 | DataSource 重构 + v2.0 提前 | 0 | 0 | 6 功能 | **COMPLETE** (2026-03-09) |

### Sprint 6: DataSource 重构 + v2.0 功能提前 (6 项) -- COMPLETE (2026-03-09)

**目标**: 废除 DataSource 抽象层，实现运行时模式切换，同时完成 4 项 v2.0 功能提前

| 编号 | 内容 | 完成状态 | 实现说明 |
|------|------|---------|---------|
| SYNC-D02 | DataSource 抽象层废除 | Done | 删除 ~24 DataSource 文件; Factory + Dual Repository; 6 个 Repository 接口迁移到 Contracts |
| SYNC-D03 | 运行时模式切换 | Done | IConnectionModeProvider 5 步切换; SidebarControl 按钮; MainWindow 遮罩层; 16 tests |
| D2 | 诊所信息配置化 | Done | clinic-settings.json + reloadOnChange 热更新; SystemSettingsView 配置区域 |
| D1 | PDF 处方导出 | Done | QuestPDF 2025.4.0; ExportPdfCommand; MedicalCaseWorkspaceView 导出按钮 |
| C2 | 照片 DPAPI 加密 | Done | DpapiPhotoStorageService; 集成读卡流程; 11 tests |
| D3 | 草稿水印 | Done | 4 XAML 模板 + PDF 水印; IsDraft = CaseStatus != Completed |

### 汇总

| 指标 | 数值 |
|------|------|
| 总 US | 138 (15 模块) + 6 Sprint 6 功能 |
| 已完成 US | 137 / 138 (99.3%) + 6 Sprint 6 功能 |
| 已移除 US | 1 (US-AUTH-007) |
| 有效未完成 | 0 (SYNC-D02 + SYNC-D03 已在 Sprint 6 实施) |
| **v1.0-alpha 达成** | Sprint 2 结束 (已达成) |
| **v1.0-beta 达成** | Sprint 4 结束 (2026-03-09, 已达成) |
| **v1.0-rc 达成** | Sprint 5 结束 (2026-03-09, 已达成) |
| **v1.0 发布就绪** | Sprint 6 结束 (2026-03-09, 全部技术债务清零) |

---

## 未完成 US 依赖约束分析

### Partial US 依赖满足情况

| US 编号 | 前置依赖 | 依赖状态 | 可立即开始 |
|---------|---------|---------|-----------|
| US-FORM-002 | Herbs CRUD (Done) | 已满足 | Yes |
| US-SYNC-008 | Sync 基础 (Done) | 已满足 | Yes |
| US-MC-016 | Herbs + Formulas (Done) | 已满足 | Yes |
| US-MC-018 | MC CRUD (Done) | 已满足 | Yes |
| US-MC-010 | MC CRUD (Done) | 已满足 | Yes |
| US-PRINT-001 | MC + Printing 基础 (Done) | 已满足 | Yes |
| US-HERB-008 | Herbs CRUD (Done) | 已满足 | Yes |
| US-PAT-013 | Patients + MC CRUD (Done) | 已满足 | Yes |
| US-AUTH-013 | Auth 基础 (Done) | 已满足 | Yes |
| US-SYNC-006 | Sync 基础 (Done) | 已满足 | Yes |
| US-SYNC-007 | US-SYNC-006 (Sprint 3) | **Sprint 3 内序** | Yes (在 SYNC-006 后) |
| US-CARD-001 | 独立 | 已满足 | Yes |
| US-CARD-002 | US-CARD-001 | **Sprint 3 内序** | Yes (在 CARD-001 后) |
| US-MC-011 | MC CRUD (Done) | 已满足 | Yes |

**结论**: 无循环依赖。2 个 Sprint 内序约束 (SYNC-007 依赖 SYNC-006; CARD-002 依赖 CARD-001)，不影响 Sprint 分配。所有 Partial US 的前置依赖均已满足，可立即开始。

---

## Release 验收标准

### v1.0-alpha Exit Criteria (Sprint 2 结束)

- [ ] 51 Must Have US 全部通过验收测试
- [ ] 核心流程端到端可用 (患者登记 -> 创建医案 -> 诊断 -> 处方 -> 保存 -> 完成)
- [ ] Registration 双模式 (前台/医生) 完整可用
- [ ] 挂号队列显示正确，医生可从队列接诊
- [ ] 取消挂号权限和前置校验正确
- [ ] Formula 列表页 TotalPrice/HerbCount 正确显示
- [ ] 模式切换前同步变更检查实现
- [ ] 编译零错误
- [ ] Server + Desktop + Architecture 测试全通过

### v1.0-beta Exit Criteria (Sprint 4 结束)

- [ ] 105 US (Must + Should) 全部通过验收测试 (去除 1 Removed)
- [ ] 处方打印功能完整可用 (空处方校验 + A4 适配)
- [ ] 数据同步完整工作流可用 (含冲突解决)
- [ ] 身份证读卡器集成可用 (含降级链)
- [ ] 验方导入/历史复制实时价格同步正确
- [ ] 编辑模式状态机实现
- [ ] CRITICAL/HIGH 审计项全部关闭

### v1.0-rc Exit Criteria (Sprint 5 结束, 2026-03-09)

- [x] 所有 CRITICAL/HIGH 技术债务清零 (Sprint 3/4 完成)
- [x] Code-PRD 审计 OPEN 项清零 (CODE-25/CODE-21 Sprint 5 关闭)
- [x] Could Have Backlog 评估处理 (US-ERR-007/US-SHELL-007 完成; NFR-AVAIL-001 实现)
- [x] 全量测试通过 (Server 1050 + Desktop 493 + Architecture 78 = 1621 tests, 0 failures)
- [ ] 用户验收测试 (UAT) -- 待生产部署后执行
- [ ] 性能指标满足 nfr.md 要求 -- 待实测校准

---

## 开放审计项跟踪

以下审计项需在对应 Sprint 中关闭:

| 审计项 | 严重度 | 对应 US | Sprint | 状态 |
|--------|--------|---------|--------|------|
| CODE-03 | CRITICAL | (Auth 安全) | Sprint 2 | Closed |
| CODE-04 | CRITICAL | (Auth 安全) | Sprint 2 | Closed |
| CODE-08 | HIGH | US-MC-016/018 | Sprint 3 | Closed (2026-03-08) |
| CODE-11 | HIGH | US-HERB-008 | Sprint 3 | Closed (审计确认已实现) |
| CODE-22 | MEDIUM | US-PAT-013 | Sprint 3 | Closed (2026-03-08) |
| CODE-23 | MEDIUM | US-FORM-002 | Sprint 2 | Closed |
| CODE-24 | MEDIUM | US-PRINT-001 | Sprint 3 | Closed (2026-03-08) |
| CODE-25 | MEDIUM | US-ERR-007 | Sprint 5 | Closed (2026-03-09) |
| CODE-21 | MEDIUM | US-SHELL-007 | Sprint 5 | Closed (2026-03-09) |
| CODE-36/37 | LOW | US-PRINT-001 | Sprint 3 | Closed (2026-03-08) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始版本: 依赖分析 + Sprint 分配 + Release 验收标准 |
| 2026-03-06 | v1.1 | 新增 Registration 模块 (7 US) 纳入 Sprint; Sprint 重编号 (1->2, 2->3, 3->4); 总量 131->138 US |
| 2026-03-08 | v1.2 | Sprint 3 完成 (9 US); CODE-08/11/22/24/36/37 关闭; 审计项更新状态列 |
| 2026-03-09 | v1.3 | Sprint 4 完成 (5 US) v1.0-beta 达成; Sprint 5 完成 (2 US + NFR-AVAIL-001) v1.0-rc 达成; CODE-25/CODE-21 关闭; 审计项全部清零 |
| 2026-03-09 | v1.4 | Sprint 6 完成: SYNC-D02 DataSource 废除 + SYNC-D03 运行时切换 + D1/D2/C2/D3 四项 v2.0 功能; 全部技术债务清零; 1654 tests 全通过 |
| 2026-03-09 | v1.5 | Sprint 2 状态修正: 8 US 全部标记 Done + COMPLETE; 审计基线更新为最终状态; 时间线视图 Sprint 2 添加 COMPLETE 标记 |
