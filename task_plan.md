# Code Simplifier 审查修复计划

## Goal
修复 code-simplifier 审查发现的 7 个问题，通过泛型基类消除 4 个 StatusHandler 重复代码，净减少 ~150 行。

## Phases

### Phase 1: 独立修复 - complete
- Step 1: H-02 清除 MasterDetailViewModelBase DEBUG 日志 (11处)
- Step 2: H-03 修复 HerbImportExportHandler ex.Message 泄露 (2处)
- Step 3: M-06 清理 LoggingRegistrationExtensions 未使用 using (2个)

### Phase 2: StatusHandler 泛型基类重构 - complete
- Step 4: 创建 BaseStatusHandler<TListDto>
- Step 5-6: 重构 Herb/Formula StatusHandler + M-01 修复
- Step 7: 修复 FormulaModule DI 注册
- Step 8-9: 重构 Patient StatusHandler + DI 注册
- Step 10: 重构 UserStatusHandler

### Phase 3: StatusOptions 优化 - complete
- Step 11: 创建共享 CommonOptions 常量
- Step 12: 更新 3 个 ViewModel 引用

## Decisions
- 异常策略: 仅捕获 HttpRequestException，其他异常冒泡
- UserStatusHandler.ToggleUserStatusAsync 独立实现 (走 UserService 元组模式)
