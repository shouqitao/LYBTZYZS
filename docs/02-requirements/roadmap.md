# v1.0 Release Roadmap

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **基于**: MoSCoW 优先级排序 (user-story-map.md) + Code-PRD 审计 (2026-02-28 + 2026-03-06)
> **审计基线**: 138 US -- 110 Implemented / 19 Partial / 2 Not Implemented / 7 New (Registration)

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

### Sprint 2: Registration 模块 + Must 补全 (8 US)

**目标**: 实现挂号管理模块，补全 Must Have

| US 编号 | 模块 | 名称 | 当前状态 | 依赖 | 工作量 |
|---------|------|------|---------|------|--------|
| US-REG-001 | Registration | 前台创建挂号 | Not Impl | Patients + Users Done | L |
| US-REG-002 | Registration | 医生快速看诊 | Not Impl | Patients + Users + MC Done | L |
| US-REG-003 | Registration | 查看挂号队列 | Not Impl | US-REG-001/002 | M |
| US-REG-004 | Registration | 前台取消挂号 | Not Impl | US-REG-001 | M |
| US-REG-005 | Registration | 状态自动跟随医案完成 | Not Impl | US-REG-001/002 + MC | S |
| US-REG-006 | Registration | 医案取消联动 | Not Impl | US-REG-005 | M |
| US-FORM-002 | Formulas | 查看验方列表 | Partial | CODE-23: HerbCount 始终为 0; 移除 TotalPrice 列 (经验方不涉及价格) | S |
| US-SYNC-008 | Sync | 模式切换 | Partial | 切换前同步检查 | M |

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

### Sprint 3: 核心业务补全 (9 US)

**目标**: 补全 MedicalCase 高级功能 + Printing + Registration 历史 + 关键修复

| US 编号 | 模块 | 名称 | 当前状态 | 差距 | 依赖 | 工作量 |
|---------|------|------|---------|------|------|--------|
| US-MC-016 | MedicalCase | 验方导入到处方 | Partial | CODE-08: 实时价格同步缺失 | Herbs/Formulas Done | M |
| US-MC-018 | MedicalCase | 复制历史处方 | Partial | CODE-08: 同上价格问题 | MC Done | M |
| US-MC-010 | MedicalCase | 跨医案搜索 | Partial | EditModeStateMachine 延期部分 | MC Done | S |
| US-MC-015 | MedicalCase | 打印触发 | Implemented | 已完成 (CODE-02 已修复) | MC Done | - |
| US-PRINT-001 | Printing | 处方打印 | Partial | CODE-24: 空处方校验; CODE-36/37: A4 适配 | MC Done | L |
| US-HERB-008 | Herbs | 批量删除 | Partial | CODE-11: 缺引用检查 | Herbs Done | S |
| US-PAT-013 | Patients | 患者状态管理 | Partial | CODE-22: 缺活跃医案检查+权限限制 | Patients/MC Done | M |
| US-AUTH-013 | Auth | 认证事件体系 | Partial | 4 个事件未实现 | Auth Done | M |
| US-REG-007 | Registration | 挂号历史查询 | Not Impl | Registration Done (Sprint 2) | S |

### Sprint 4: 同步与外设 (5 US)

**目标**: 补全 Sync 高级功能 + CardReader + 编辑模式

**注**: US-AUTH-007 已被设计决策移除 (simplify-auth)，标记为 **Removed**，不占 Sprint 容量。

| US 编号 | 模块 | 名称 | 当前状态 | 差距 | 依赖 | 工作量 |
|---------|------|------|---------|------|------|--------|
| US-SYNC-006 | Sync | 同步删除 | Partial | 基础已实现，需完善 | Sync Done | M |
| US-SYNC-007 | Sync | 完整同步工作流 | Partial | 冲突解决部分需完善 | US-SYNC-006 | L |
| US-CARD-001 | CardReader | 读卡器连接与读取 | Partial | PRD-13/14/15: 配置/加密/去重降级链 | 独立 | L |
| US-CARD-002 | CardReader | 读卡数据填充 | Partial | PRD-16: RealName->Name 映射 | US-CARD-001 | S |
| US-MC-011 | MedicalCase | 编辑模式 | Not Impl | EditModeStateMachine 完整实现 | MC Done | XL |
| US-AUTH-007 | Auth | 登出前警告 | Removed | 设计决策已移除 (simplify-auth) | - | - |

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

### Backlog: Could Have 补全 (3 US)

| US 编号 | 模块 | 名称 | 当前状态 | 差距 |
|---------|------|------|---------|------|
| US-ERR-007 | Error | 错误追踪码 | Partial | CODE-25: TokenExpired 错误码缺失 |
| US-SHELL-007 | Shell | 账户设置 | Partial | CODE-21: 状态栏缺用户名/版本号 |
| (无新增) | | | | |

**Could Have 不分配 Sprint，按优先级在 Sprint 间隙处理。**

---

## 时间线视图

| Sprint | 周期 | 重点模块 | Must | Should | Could | 目标 |
|--------|------|---------|------|--------|-------|------|
| Done | - | 全模块 | 43 | 37 | 30 | 审计确认 |
| Sprint 2 | W1-W2 | Registration + Formulas + Sync | 8 | 0 | 0 | **v1.0-alpha** (Must 100%) |
| Sprint 3 | W3-W4 | MC + Printing + Herbs + Auth + REG | 0 | 9 | 0 | 核心业务补全 |
| Sprint 4 | W5-W6 | Sync + CardReader + MC | 0 | 5 | 0 | **v1.0-beta** (Must+Should 100%) |
| Backlog | 间隙 | Error + Shell | 0 | 0 | 3 | 锦上添花 |

### 汇总

| 指标 | 数值 |
|------|------|
| 总 US | 138 (15 模块) |
| 已完成 US | 110 / 138 (79.7%) |
| 需补全 US | 19 Partial + 8 Not Impl (含 7 REG 新增) = 27 |
| 已移除 US | 1 (US-AUTH-007) |
| 预估 Sprint 数 | 3 (6 周) |
| v1.0-alpha 达成 | Sprint 2 结束 (2 周后) |
| v1.0-beta 达成 | Sprint 4 结束 (6 周后) |

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

### v1.0-rc Exit Criteria

- [ ] 所有 CRITICAL/HIGH 技术债务清零
- [ ] Code-PRD 审计 OPEN 项清零
- [ ] Could Have Backlog 3 项评估处理
- [ ] 用户验收测试 (UAT) 通过
- [ ] 性能指标满足 nfr.md 要求

---

## 开放审计项跟踪

以下审计项需在对应 Sprint 中关闭:

| 审计项 | 严重度 | 对应 US | Sprint |
|--------|--------|---------|--------|
| CODE-03 | CRITICAL | (Auth 安全) | Sprint 2 |
| CODE-04 | CRITICAL | (Auth 安全) | Sprint 2 |
| CODE-08 | HIGH | US-MC-016/018 | Sprint 2 |
| CODE-11 | HIGH | US-HERB-008 | Sprint 2 |
| CODE-22 | MEDIUM | US-PAT-013 | Sprint 2 |
| CODE-23 | MEDIUM | US-FORM-002 | Sprint 1 |
| CODE-24 | MEDIUM | US-PRINT-001 | Sprint 2 |
| CODE-25 | MEDIUM | US-ERR-007 | Backlog |
| CODE-21 | MEDIUM | US-SHELL-007 | Backlog |
| CODE-36/37 | LOW | US-PRINT-001 | Sprint 2 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始版本: 依赖分析 + Sprint 分配 + Release 验收标准 |
| 2026-03-06 | v1.1 | 新增 Registration 模块 (7 US) 纳入 Sprint; Sprint 重编号 (1->2, 2->3, 3->4); 总量 131->138 US |
