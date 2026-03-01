# Progress: Code Simplifier 审查修复

## Session: 2026-03-01

### Phase 1: 独立修复 -- COMPLETE
- [done] Step 1: MasterDetailViewModelBase 移除 11 处 [DEBUG] LogDebug
- [done] Step 2: HerbImportExportHandler ex.Message -> 安全错误消息 (2处)
- [done] Step 3: LoggingRegistrationExtensions 移除 2 个未使用 using

### Phase 2: StatusHandler 泛型基类重构 -- COMPLETE
- [done] Step 4: 创建 BaseStatusHandler<TListDto> (~110行)
- [done] Step 5: HerbStatusHandler 86行 -> 40行
- [done] Step 6: FormulaStatusHandler 87行 -> 40行 (修正 FormulaItem -> FormulaDetailModel)
- [done] Step 7: FormulaModule DI 修正 + Handler 注册 + 移除 unused using
- [done] Step 8: PatientStatusHandler 55行 -> 33行
- [done] Step 9: PatientsModule 新增 Handler DI 注册
- [done] Step 10: UserStatusHandler 102行 -> 70行

### Phase 3: StatusOptions 优化 -- COMPLETE
- [done] Step 11: 创建 CommonOptions.StatusOptions (~15行)
- [done] Step 12: 更新 3 个 ViewModel 引用

### Verification -- PASS
- Build: 0 errors, 0 warnings
- Server unit tests: 370 passed
- Desktop unit tests: 597 passed
- Architecture tests: 74 passed
