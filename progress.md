# Progress: 剩余任务全量清零

## Session: 2026-03-01 (Session 3)

### Phase B: HIGH -- 数据完整性 (4 项)

#### B1. CODE-05/06: MedicalCase FK Fluent API -- SKIPPED (已完成)
- 调研确认 PatientId + UserId FK 已在 MedicalCaseConfiguration.cs 显式配置
- HasOne<Patient/User>().WithMany().HasForeignKey().IsRequired().OnDelete(Restrict)
- 无需额外修改

#### B2. CODE-11: Herb BatchDelete 引用检查 -- DONE
修改文件:
- `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
  - CheckReferenceAsync: 补充 FormulaHerbItems 引用计数 (Set<FormulaHerbItem>().CountAsync)
  - BatchDeleteAsync: 成功后调用 _cacheInvalidation.InvalidateAsync("herbs")
  - DeleteAsync: 使用 refCheck.Data.DeleteWarning 代替硬编码消息
  - 新增 BuildReferenceWarning() 辅助方法，分别显示处方/验方引用数
- `src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbReferenceCheckDto.cs`
  - ReferenceCount 注释更新为"处方+验方引用总数"
- 编译通过: 0 错误 0 警告
- 测试通过: 45 个 Herb 相关测试全部通过

#### B3. T5-P3-06: Desktop 写后缓存失效 -- DONE
修改文件 (5 个 ViewModel):
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
  - 注入 IDesktopCacheManager，Save/Delete/ToggleStatus/Restore/Import 后调用 InvalidateHerbCaches()
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
  - 注入 IDesktopCacheManager，Save/Delete/ToggleStatus/CopyFormula/Restore 后调用 InvalidateFormulaCaches()
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
  - 注入 IDesktopCacheManager，Save/Delete/Restore/Import/CardReader 后调用 InvalidatePatientCaches()
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`
  - 注入 IDesktopCacheManager，Save/Delete/ToggleStatus/Restore/Import 后调用 InvalidateUserCaches()
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
  - 注入 IDesktopCacheManager，Save/Delete 后调用 InvalidateMedicalCaseCaches()
- 编译通过: 5 个模块全部 0 错误 0 警告

#### B4. T5-P2-42: 同步前网络检查 -- DONE
修改文件:
- `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs`
  - 注入 IApiHealthCheckService
  - 新增 ValidatePreConditionsAsync() -- 检查认证状态 + 网络连接 (5s 超时)
  - CheckDifferencesAsync: 使用 ValidatePreConditionsAsync 替代内联认证检查
  - ExecuteSyncAsync: 入口添加 ValidatePreConditionsAsync 二次验证
  - 网络不可用时显示具体错误信息 (LastErrorMessage)
- 编译通过: 0 错误 0 警告

### 验证结果
- 全量编译: 0 错误，2 个预存在 Mapper 警告 (LocalUserMapper.cs)
- LYBT.Tests.Unit (Herb): 45 通过
- LYBT.Tests.Desktop.Unit: 600 通过

## Session: 2026-03-01 (Session 4)

### Phase A: CRITICAL -- 业务逻辑修复 (4 项) -- SKIPPED (均已实现)
- CODE-01: TcmDiagnosis 验证 - MedicalCaseStateService.cs:138-143
- CODE-02: IsPrinted 重置 - 3 个编辑入口均有重置逻辑
- CODE-03: AutoLoginToken 撤销 - AuthService.cs:227-247
- CODE-04: SuperAdminOnly 策略 - Controller + 策略定义均已到位

## Session: 2026-03-01 (Session 5)

### Phase C: MEDIUM -- 代码修复 (3 项) -- SKIPPED (均已实现)
- T5-P3-03 (C1): ProblemDetailsFactory.cs Severity 关联 -- 代码注释 T5-P3-03 已存在
- T5-P3-01 (C2): ProductionConfigurationValidator.cs Warning 级别 -- 代码注释 T5-P3-01 已存在
- T5-P3-19 (C3): AccountSettingsViewModel.cs Email 编辑 -- 代码注释 T5-P3-19 已存在

### Phase D: MEDIUM -- UI 增强 (1 项) -- SKIPPED (已在 Session 4 实现)
- T5-P3-21 (D1): MainWindowViewModel.cs 状态栏同步标识 -- 已在 Session 4 完成

### Phase E: 文档 + 清理 (3 项) -- DONE
- E1 (DOC5-04): dual-mode.md 补充 DataSource 接口架构 (~35行)
  - IDataSourceBase<TDetail, TInput> 泛型基接口 + 5 个实体接口
  - Remote vs Local 实现对应表 (5 对)
  - DI 注册切换详情 (Remote/Local/共享)
- E2 (DOC5-05): dual-mode.md 补充 Sync 跨模块调用链 (~50行)
  - 端到端调用链 (Desktop SyncViewModel -> Server SyncService)
  - 跨模块依赖表 (CrossModuleService ISP 四接口)
  - 同步依赖顺序 (Herb -> Patient -> Formula)
  - 基础数据 vs 聚合同步对比表
- E3 (T5-P3-20b): MedicalCase Checksum -- 已在 Session 4 完成

### Phase F: 归档 -- DONE
- task_plan.md: 所有 Phase 标记 complete
- progress.md: 追加 Session 5 完成记录
- findings.md: 追加调研结果
- remaining-tasks.md: 全部 15 项标记完成，0 OPEN

### 修改文件列表
- `docs/03-architecture/dual-mode.md` (E1 + E2 文档补全)
- `task_plan.md` (状态更新)
- `progress.md` (Session 5 记录)
- `findings.md` (调研结果)
- `docs/plans/2026-03-01-remaining-tasks.md` (完成标记)

### 最终结果
- 全部 15 项 OPEN 任务清零
- 本次仅文档变更，无代码修改
