# Code-vs-PRD 审计遗留项全量处理

## Goal
处理剩余 6 个遗留项: DEAD-13 / CODE-41 / CODE-38 / CODE-36 / T5-P3-05/06 缓存失效

## Phases

### Phase 0: 快速清理 (0A/0B/0C 并行) - complete
- [x] 0A: DEAD-13 LocalData 加入 sln
- [x] 0B: AuthErrorCode 枚举删除
- [x] 0C: ICrossModuleService 废弃接口清理

### Phase 1: Draft -> Suspended 全量迁移 - complete
- [x] 1A: 枚举与实体
- [x] 1B: Server Service
- [x] 1C: API Controller
- [x] 1D: Desktop Client (~15 文件)
- [x] 1E: 测试 (~11 文件)

### Phase 2: Server 缓存失效 - complete
- [x] 2A: ICacheInvalidationService 接口 + CacheInvalidationService 实现
- [x] 2B-2E: 6 Service 集成 (Herb/Formula/FormulaImport/Patient/MCCommand/MCState)
- [x] 2F: 5 测试文件更新构造函数参数 + 架构测试白名单

### Phase 3: Desktop 缓存失效 - complete
- [x] 3A: CacheEvents + IDesktopCacheManager + DesktopCacheManager
- [x] 3B: PatientSearchCache 订阅 PatientEvents + CacheEvents
- [x] 3C: SyncService 集成 IDesktopCacheManager.InvalidateAll()
- [x] 3D: DI 注册 + 构建验证

### Phase 4: A4 打印模板 - complete
- [x] 4A: PrescriptionPrintA4Template.xaml (794x1123px, 57px边距, 18/17/11/10pt字号)
- [x] 4B: PrescriptionContinuationA4Template.xaml (A4续页, SetAsLastPage支持)
- [x] 4C: PrescriptionPrintService 模板选择逻辑 (IsA4/GetFirstPageHerbLimit, A4=20味/A5=12味)

## Decisions
- FormulaValidationStatus.Draft 保持不变 (独立业务概念)
- API `/draft` -> `/suspend` 直接重命名
- 缓存失效复用已有 CacheExtensions
- A4 模板采用独立 XAML 文件 (KISS，不与 A5 模板耦合)

## Errors Encountered
(none yet)
