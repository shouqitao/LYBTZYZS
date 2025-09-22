# 批次 0：病历（MedicalCase）主业务流纠偏 PRD

## 背景与问题
- 后端以病历 MedicalCase 为一等业务对象（Active/Closed），而桌面端当前以诊疗 Consultation/诊断为主线（默认工作台、导航与事件）。
- 发现严重问题：前端“开始就诊”调用了“完成病例（Closed）”API，导致状态错乱。

## 目标
- 保持后端不变，严格以 Shared 为标准（DTO/枚举/接口）。
- 以 MedicalCase 为业务主线；Consultation/四诊作为病例内部步骤。
- 先完成最小风险纠偏（Hotfix），后续再做导航/事件全面收敛（见阶段 A~G PRD）。

## 范围（本批次）
- In Scope：
  - 修复开始/结束就诊的 API 调用语义。
  - 明确后续改造点（仅文档化）：默认工作台、事件白名单、状态/统计统一。
- Out of Scope：
  - 大规模导航/工作台重构（将纳入后续批次）。

## 交付物
- 代码修复：
  - 将“开始就诊”改为调用 `IMedicalCaseService.StartAsync(id)`（置为 Active）。
  - 保持“结束就诊”调用 `CompleteAsync(id)`（置为 Closed）。
- 文档：
  - 本 PRD 与后续批次 PRD（A~G）已生成（.claude/prds）。

## 详细修改点（代码）
- 文件：`src/Client/Desktop/Modules/MedicalCase/ViewModels/MedicalCaseDetailViewModel.cs`
  - 方法：`StartConsultationAsync()`（约 L334 处）
    - 当前：`var result = await _medicalCaseService.CompleteAsync(MedicalCase.Id, "开始就诊");`
    - 目标：`var result = await _medicalCaseService.StartAsync(MedicalCase.Id);`
  - 方法：`CompleteCaseAsync()` 保持 `CompleteAsync(MedicalCase.Id, ...)` 不变（结束=Closed）。

## 验收标准
- 构建通过：`dotnet build LYBT.Desktop.sln -c Release --no-restore`。
- 进入病例详情点击“开始就诊”后，后端状态变为 Active；“结束就诊”后变为 Closed。
- 今日接诊/统计未出现明显回归（增量变更）。

## 风险与缓解
- 风险：VM 其他地方仍有 Consultation 术语导致误导。
  - 缓解：后续批次逐步切换导航与文案；本次仅修语义错误。

## 回滚方案
- 若修复后影响现有流程，可回滚本次提交；同时保留文档用于后续重新实施。

## 备注
- 后续批次将：
  - 调整默认工作台与导航为“病历中心优先”；
  - 建立病例主事件白名单（Started/Updated/Closed），收敛 Consultation 内部事件；
  - 统一状态展示与统计逻辑（Active/Closed）。

