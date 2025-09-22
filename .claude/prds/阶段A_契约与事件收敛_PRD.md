# 阶段 A：契约与事件收敛 PRD

## 目标
- 保持后端不变，严格以 `src/Shared` 为标准源（DTO/枚举/接口）。
- 桌面端统一病例与会诊状态：以 Shared 的 Record‑Only 枚举为准（MedicalCaseStatus: Active/Closed；ConsultationStatus 以 Shared 为准）。
- 收敛桌面事件体系：采用 Prism `PubSubEvent<T>` 的最小事件集，去除重复/历史“New”类事件。

## 范围
- In Scope：Shell、Modules（MedicalCase/Patients/Prescriptions 等）、Core（Events/Services/ViewModels）内对状态与事件的使用与展示；事件发布/订阅路径；状态文本/颜色/排序及统计逻辑。
- Out of Scope：后端 API、数据库与 Shared 内已发布契约；UI 大改动（仅统一文案与映射，不调整布局）。

## 交付物
- 统一后的状态引用与展示文案（仅展示“进行中/已关闭”）。
- 事件白名单（Core/Events 下）与迁移后的发布/订阅代码。
- 兼容层（必要时）：保留最小的旧→新映射/适配，不暴露给新代码。

## 验收标准
- 代码全量编译通过：`dotnet build LYBT.Desktop.sln -c Release --no-restore`。
- 全局搜索无以下遗留：`Registered`/`InConsultation`/`Completed`/`Cancelled` 的业务判定；`*EventNew` 类型。
- 登录成功/退出/快速开始诊疗/处方保存的事件链路与导航正确；今日接诊与病例详情仅展示“进行中/已关闭”。

## 里程碑
1. 建立事件白名单（Core/Events）并补齐缺失事件（LoginSuccess/Logout/QuickStartConsultation 等）。
2. 替换模块内旧事件与“New”事件为白名单事件；保留最小门面（UnifiedEventHandler→仅转发）。
3. 状态统一改造：Shell 与 MedicalCase 模块首批文件替换映射与展示；统计/排序修正。
4. 清理未引用的旧事件与影子状态逻辑；提交变更与构建校验。

## 风险与缓解
- 风险：事件替换造成订阅遗漏。缓解：在门面层临时双发旧/新事件，逐步切换，搜索订阅点对齐后移除旧事件。
- 风险：状态文本/颜色变化影响 UI 预期。缓解：提供前后对比截图与回归清单。

## 依赖
- Shared 中的标准枚举/DTO（不改动）。
- Prism 事件聚合器（EventAggregator）。

## 回滚方案
- 如发现关键路径回归（登录/导航/保存），撤销事件替换提交，恢复至上一个稳定提交，并在门面层补充缺失映射后重提。

## 度量
- 遗留枚举文本/判断引用计数为 0。
- 遗留“New”事件定义与引用计数为 0。

## 测试计划
- 构建与格式化：
  - `dotnet build LYBT.Desktop.sln -c Release --no-restore`
  - `dotnet format LYBT.All.sln`
- 单测与架构测试：
  - `dotnet test tests -c Release --no-build`
  - `dotnet test tests/Architecture/LYBT.ArchTests.csproj`
- 手动回归：
  - 登录→主页→快速开始诊疗（事件驱动导航）。
  - 今日接诊列表状态排序/颜色与计数。
  - 病例详情状态文本与保存后刷新。

## 受影响文件（示例）
- `src/Client/Desktop/Core/Events/*`
- `src/Client/Desktop/Shell/ViewModels/HomeViewModel.cs`
- `src/Client/Desktop/Shell/Models/TodayPatientDto.cs`
- `src/Client/Desktop/Modules/MedicalCase/*`

