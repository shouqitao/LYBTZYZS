# Server 层深度分析报告

## [S1] 分析方法
使用 Codegraph 三路并行扫描：
- Entities 层完整性 (DbSet 覆盖 + BaseEntity 继承)
- Handler-Controller 覆盖率 (87 handler → controller dispatch)
- DI 注册完整性 (8个模块 + 基础设施)

## [S2] Entities 层
- 15 个实体全部有 DbSet
- FormulaHerbItem 无显式 DbSet (通过导航属性工作)
- BaseEntity 继承链完整

## [S3] Handler 覆盖
- 87 个 handler 中 86 个有 controller dispatch
- 1 个缺口: SearchPatientByIdNumberQuery (已修复)

## [S4] DI 注册
- 全部完整，无缺失注册
- 2 个服务仅通过 MediatR 内部使用 (OK*)

## [S5] 死代码清理
- 四轮清理共删除 34 个文件，2,157 行代码
- 覆盖: Server/Domain/Application/Infrastructure/ErrorCode

## [S6] 剩余项
- IHerbRepositoryLegacy/IFormulaRepositoryLegacy 仍有 1 个消费者 (HerbImportExportService/FormulaImportExportService)
- 领域事件系统已发布但无订阅者 (设计预留)
