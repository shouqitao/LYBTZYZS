# 剩余任务全量清零

## Goal
完成 remaining-tasks.md 中 15 项 OPEN 任务，归档后实现阶段性清零。

## Phases

### Phase A: CRITICAL -- 业务逻辑修复 (4 项) - complete (已在先前会话实现)
- A1. CODE-01: CompleteAsync TcmDiagnosis 验证
- A2. CODE-02: 编辑已打印医案重置 IsPrinted
- A3. CODE-03: LoginAsync 撤销旧 AutoLoginToken
- A4. CODE-04: sysadmin SuperAdminOnly 策略

### Phase B: HIGH -- 数据完整性 (4 项) - complete
- B1. CODE-05/06: MedicalCase FK Fluent API
- B2. CODE-11: Herb BatchDelete 引用检查
- B3. T5-P3-06: Desktop 写后缓存失效
- B4. T5-P2-42: 同步前网络检查

### Phase C: MEDIUM -- 代码修复 (3 项) - complete (已在先前会话实现)
- C1. T5-P3-03: ProblemDetails Severity
- C2. T5-P3-01: 配置验证警告
- C3. T5-P3-19: Email 编辑

### Phase D: MEDIUM -- UI 增强 (1 项) - complete (已在 Session 4 实现)
- D1. T5-P3-21: 状态栏同步标识

### Phase E: 文档 + 清理 (3 项) - complete
- E1. DOC5-04: DataSource 双模式文档
- E2. DOC5-05: Sync 跨模块文档
- E3. T5-P3-20b: MedicalCase Checksum

### Phase F: 归档 - complete

## Decisions
- SuperAdminOnly 策略仅用于 reset-password 和 restore
- Herb BatchDelete 跳过有引用的项，不中断批量操作
- 缓存失效: 写操作成功后调用 Invalidate
