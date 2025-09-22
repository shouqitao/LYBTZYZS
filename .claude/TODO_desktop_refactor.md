# 桌面端重构 TODO（分批）

- [ ] 批次0：病历为主业务流纠偏（Hotfix）
  - [ ] 修复开始就诊调用：MedicalCaseDetailViewModel.StartConsultationAsync -> 使用 _medicalCaseService.StartAsync(id)
  - [ ] 提交与构建验证：`dotnet build LYBT.Desktop.sln -c Release --no-restore`

- [ ] 阶段A：契约与事件收敛
  - [ ] 统一 Active/Closed 状态引用与展示（Shell/MedicalCase）
  - [ ] 建立事件白名单并替换“New”/重复事件
  - [ ] 移除未用旧事件类型

- [ ] 阶段B：通知与加载统一
  - [ ] 注册 ISmartLoadingManager（Singleton）
  - [ ] 关键 VM 替换为 ExecuteWithLoadingAsync 使用
  - [ ] 统一通知服务出口，去除直接 MessageBox 调用

- [ ] 阶段C：配置与热更新精简
  - [ ] 引入 NullFeatureToggleService 并切换默认实现
  - [ ] HotReloadService 仅依赖 IConfigurationManagerService

- [ ] 阶段D：安全与告警收敛
  - [ ] SecureConfigurationService 迁移 .NET 8 AES‑GCM 新构造器
  - [ ] 新增加解密单测覆盖

- [ ] 阶段E：文本与本地化/编码
  - [ ] 修复乱码与统一 UTF‑8
  - [ ] 资源化首批常用文案

- [ ] 阶段F：构建、测试与门禁
  - [ ] 单元与架构测试增强；覆盖率采集
  - [ ] 分析器警告收敛（XML 注释、CS1998）

- [ ] 阶段G：清理与文档
  - [ ] 删除未引用旧代码/控件
  - [ ] 完善开发指引/事件与状态规范

