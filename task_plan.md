# Sprint 6: DataSource 重构 + v2.0 功能提前

## Goal

废除 DataSource 抽象层 (SYNC-D02)，实现运行时模式切换 (SYNC-D03)，同时完成 4 项功能增强 (诊所配置化/PDF导出/照片加密/草稿水印)。

## Decisions

| Decision | Rationale | Source |
|----------|-----------|--------|
| 方案 A+ (Factory + Dual Repository) | 远程走 HTTP API、本地走 EF Core，两条路径本质不同，无法用单一 DbContext 统一 | Planner + Gemini 审核确认 |
| IConnectionModeProvider 抽象 | DryIoc 不支持运行时替换注册，用工厂模式 + Transient 注册绕过 | Gemini 确认 |
| Singleton 禁止直接注入 Repository | 切换模式后 Singleton 会持有旧实例，必须用 Func<T> 工厂 | Gemini 新发现 |
| MenuManager 改注入 IConnectionModeProvider | 当前注入 ConnectionMode 枚举值 (固定)，无法响应运行时切换 | Gemini 新发现 |
| ModeSwitchValidator 查询 ActiveConsultation | 切换时必须检查活跃医案 + 脏数据 | Gemini 新发现 |
| D2 使用 IOptionsMonitor | 支持运行时热更新，与动态模式主题对齐 | Gemini 建议 |

## Phases

### Phase 1: SYNC-D02 DataSource 废除
Status: complete

- [x] 1.1 创建 IConnectionModeProvider 接口
- [x] 1.2 实现 ConnectionModeProvider
- [x] 1.3 Singleton 依赖审计 (grep 所有注入 IDataSource/Repository 的 Singleton)
- [x] 1.4 重构 PatientRepository (试点)
- [x] 1.5 重构其余 5 个 Repository (Herb/Formula/MedicalCase/User/Registration)
- [x] 1.6 重写 DataSourceRegistrationExtensions (工厂注册)
- [x] 1.7 重构 MenuManager 注入 IConnectionModeProvider
- [x] 1.8 更新 ModeSwitchValidator
- [x] 1.9 删除 DataSource 文件 (~24个) + 创建 LocalRegistrationMapper
- [x] 1.10 更新架构测试 (P01 DataSource->Repository, Entity 允许列表, Repository 接口位置验证) + Integration 测试迁移

### Phase 2: SYNC-D03 运行时模式切换
Status: complete

- [x] 2.1a 两套基础设施始终注册 (DataSourceRegistrationExtensions 重写)
- [x] 2.1b Repository 改为工厂注册 (resolve 时根据 CurrentMode 选择实现)
- [x] 2.1c IConnectionModeProvider 添加 SwitchModeAsync + IsSwitching + ModeSwitchResult
- [x] 2.1d ConnectionModeProvider 实现 (验证 -> ActiveConsultation 检查 -> Region 清理 -> 切换 -> 导航首页)
- [x] 2.1e LoggingRegistrationExtensions 始终注册两套 Logger
- [x] 2.2 实现切换 UI (MainWindowViewModel SwitchModeCommand + SidebarControl 模式切换按钮)
- [x] 2.3 切换前用户确认对话框 (ActiveConsultation + ModeSwitchValidator 已覆盖核心阻断场景)
- [x] 2.4 切换遮罩层 UI (MainWindow 半透明遮罩 + IsSwitchingMode 绑定)
- [x] 2.5 CancellationToken 传播 (SwitchModeAsync API 已支持, 命令层传播)
- [x] 2.6 单元测试 (16 tests: 初始化/成功切换/阻断条件/验证器路由/取消/异常处理)

### Phase 3: D2 诊所信息配置化
Status: complete

- [x] 3.1 扩展 ClinicSettingsOptions 模型 (LicenseNumber/Email) + 删除重复的 Infrastructure ClinicSettings POCO
- [x] 3.2 分离 clinic-settings.json + reloadOnChange 热更新
- [x] 3.3 重写 ClinicSettingsService (IConfiguration 热读取 + SaveSettingsAsync 持久化)
- [x] 3.4 修复打印断链 (PrescriptionPrintHandler 注入 IClinicSettingsService)
- [x] 3.5 SystemSettingsView 增加诊所信息配置区域 (ViewModel + XAML)

### Phase 4: D1 PDF 处方导出
Status: complete

- [x] 4.1 评估 QuestPDF vs PdfSharp (QuestPDF 2025.4.0 已选定，上一会话完成)
- [x] 4.2 实现 PDF 导出 Service (PrescriptionPdfExporter.cs 已实现，上一会话完成)
- [x] 4.3 UI 集成 (ExportPdfCommand + 导出按钮 + PrescriptionPrintHandler.ExportPdfAsync)

### Phase 5: C2 照片 DPAPI 加密存储
Status: complete

- [x] 5.1 创建 IPhotoStorageService 接口
- [x] 5.2 DPAPI 加密实现 (11 tests)
- [x] 5.3 集成到读卡流程 + DI 注册

### Phase 6: D3 草稿水印
Status: complete

- [x] 6.1 PrescriptionPrintModel 添加 IsDraft
- [x] 6.2 打印模板添加水印层 (4 个 XAML 模板)
- [x] 6.3 PDF 导出器添加水印 (QuestPDF Foreground)
- [x] 6.4 PrescriptionPrintHandler 设置 IsDraft (非 Completed = 草稿)

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| (无) | - | - |
