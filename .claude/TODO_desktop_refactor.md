# 桌面端重构 TODO（迭代中）

- [ ] 批次0（病历为业务流纠偏 Hotfix）
  - [ ] 修正“开始就诊”语义（Start → Active）
  - [ ] 提交并构建验证 `dotnet build LYBT.Desktop.sln -c Release --no-restore`

- [ ] 批次1（桌面端状态统一与编译修复）
  - [ ] 修复 `MedicalCaseThemeViewModel` 语法错误与重复分支，补齐 `GetBadgeTheme()`
  - [ ] `HomeViewModel`：按 Active/Closed 统一排序、统计、文案与颜色
  - [ ] `PrescriptionPrintService`：状态文本统一为 Active/Closed
  - [ ] 构建与格式化：`dotnet build` / `dotnet format`

- [ ] 阶段A（契约/事件收敛）
  - [ ] 统一 Active/Closed 状态引用于 Shell/MedicalCase
  - [ ] 事件清单整理与替换（去除未使用事件）
  - [ ] 移除未使用旧事件

- [ ] 阶段B（通知与加载统一）
  - [ ] 注册 `ISmartLoadingManager`（Singleton）
  - [ ] 关键 VM 改造为 `ExecuteWithLoadingAsync` 统一入口
  - [ ] 统一通知口径，移除直接 `MessageBox`

- [ ] 阶段C（配置与特性开关）
  - [ ] 补齐 `NullFeatureToggleService` 与可替换默认实现
  - [ ] `HotReloadService` 接入 `IConfigurationManagerService`

- [ ] 阶段D（安全与告警治理）
  - [ ] `SecureConfigurationService` 迁移到 .NET 8 AES-GCM
  - [ ] 增加加密单元测试与回归用例

- [ ] 阶段E（文本与本地化/编码）
  - [ ] 修复乱码并统一 UTF-8
  - [ ] 常用文案资源化

- [ ] 阶段F（工程化与质量门禁）
  - [ ] 单元与架构测试增强，覆盖率采集
  - [ ] 清理非功能性警告（XML 注释/CS1998 等）

- [ ] 阶段G（重构归档与文档）
  - [ ] 清理未使用代码/控件
  - [ ] 规范开发指引/事件与状态约定
