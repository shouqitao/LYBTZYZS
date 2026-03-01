# Progress - 审计遗留项处理

## Session: 2026-03-01

### Phase 0: 快速清理 [complete]
- 0A: LocalData.csproj added to LYBT.All.sln
- 0B: AuthErrorCode 枚举删除 (AuthEnums.cs + 3 comment refs)
- 0C: ICrossModuleService 接口文件删除 + 8 文件注释更新到 ISP 接口名

### Phase 1: Draft -> Suspended 全量迁移 [complete]
- 1A: MedicalCaseEnums.cs (Draft alias removed), CaseStatus.cs, MedicalCaseModel.cs, MedicalCaseBusinessRules.cs
- 1B: 6 Server Service 文件 (SaveDraftAsync -> SuspendAsync)
- 1C: MedicalCaseController.cs (PUT /draft -> /suspend)
- 1D: 14 Desktop 文件 (Command/ViewModel/Service/Repository/XAML)
- 1E: 11 test 文件
- Verification: Build 0 errors, Tests 1438 passed / 0 failed
- Grep确认: MedicalCaseStatus.Draft / SaveDraft / /draft 零残留

### Phase 2: Server 缓存失效 [complete]
- 2A: ICacheInvalidationService + CacheInvalidationService (OutputCache Tag + MemoryCache Prefix)
- 2B: DI 注册为 Singleton
- 2C: 6 Service 集成 (HerbService, FormulaService, FormulaImportExportService, PatientService, MedicalCaseCommandService, MedicalCaseStateService)
- 2D: 5 test files updated (constructor mock params)
- 2E: Architecture test whitelist updated
- Verification: Build 0 errors, Tests 1453 passed / 0 failed

### Phase 3: Desktop 缓存失效 [complete]
- 3A: CacheEvents.cs (Contracts/Events), IDesktopCacheManager (Contracts), DesktopCacheManager (Foundation/Caching)
- 3B: PatientSearchCache 订阅 PatientEvents.Created/Updated + CacheEvents.Invalidated
- 3C: SyncService.ExecuteSyncAsync 完成后调用 _cacheManager.InvalidateAll()
- 3D: DI 注册为 Singleton
- Verification: Build 0 errors/0 warnings, Tests 1476 passed / 0 failed

### Phase 4: A4 打印模板 [complete]
- 4A: PrescriptionPrintA4Template.xaml + .xaml.cs (794x1123px, Margin=57px, 字号: 18/17/11/10pt, HerbItem Width=135px)
- 4B: PrescriptionContinuationA4Template.xaml + .xaml.cs (A4续页, SetAsLastPage() 控制尾部区域可见性)
- 4C: PrescriptionPrintService 更新:
  - A5FirstPageHerbLimit=12, A4FirstPageHerbLimit=20
  - IsA4(Size) / GetFirstPageHerbLimit(Size) 辅助方法
  - CreateFixedPage: 根据 IsA4 选择 PrescriptionPrintA4Template / PrescriptionPrintTemplate
  - CreateContinuationFixedPage: 根据 IsA4 选择 A4/A5 续页模板
- Verification: Build 0 errors, Tests 1438 passed / 0 failed
