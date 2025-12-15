# OpenSpec Proposal: print-prescription-slip

## Summary

实现"新建医案"中的"打印处方笺"功能，按照诊所处方模板格式打印，支持诊所名称配置、患者信息和处方数据自动填充。

## Motivation

### 现状问题

1. **打印服务存在但数据未集成**：`PrescriptionPrintService` 已实现打印框架，但关键数据填充方法使用硬编码占位符
2. **诊所信息不可配置**：诊所名称、地址、电话等信息硬编码在代码中
3. **患者信息未关联**：打印时无法获取真实患者信息
4. **诊断信息映射缺失**：中医诊断和治疗方案未正确映射到打印字段

### 业务需求

- 打印格式需符合诊所现有处方模板（普通处方笺）
- 诊所名称可通过配置文件修改
- 患者信息、处方内容自动从当前医案获取
- 诊断字段使用"中医诊断"，诊见字段使用"治疗方案"

## Requirements

### 功能需求

#### REQ-001: 诊所信息配置

- 在 `appsettings.json` 中添加诊所配置节
- 配置项包括：诊所名称、地址、电话、科别
- 打印服务从配置读取诊所信息

#### REQ-002: 患者信息自动填充

- 打印时自动获取当前医案关联的患者信息
- 填充字段：姓名、性别、年龄、住址、电话
- 就诊时间使用医案创建时间

#### REQ-003: 处方信息自动填充

- 从当前处方获取药材列表
- 格式：药名+剂量+单位（如"厚朴10g"）
- 填充剂数、用法（水煎服，日X剂，1日X次）

#### REQ-004: 诊断字段映射

- "诊断"字段使用 Consultation 的"中医诊断"(TCMDiagnosis)
- "诊见"字段使用 Consultation 的"治疗方案"(TreatmentPrinciple)

#### REQ-005: 打印格式规范

按模板布局：
```
[诊所名称]普通处方笺
姓名___  性别___  年龄___岁  时间____年__月__日
门诊号___  科别___  电话___  住址：___
诊断：_______________  诊见：_______________
Rp.
  [药材1] [剂量]g  [药材2] [剂量]g  ...
  [剂数]剂，[用法]
医师签字___  审核___  调配___
诊疗费___  药费___  治疗费___  合计___
```

### 非功能需求

- NFR-001: 打印预览响应时间 < 500ms
- NFR-002: 向后兼容 - 不影响现有打印接口签名
- NFR-003: 配置变更后无需重启应用

## Affected Components

| 层 | 组件 | 影响程度 |
|----|------|----------|
| Server/WebAPI | `appsettings.json` | Minor - 添加诊所配置节 |
| Desktop/Prescriptions | `PrescriptionPrintService.cs` | Major - 实现数据填充逻辑 |
| Desktop/Prescriptions | `PrescriptionFlowDocumentBuilder.cs` | Moderate - 调整布局匹配模板 |
| Desktop/Prescriptions | `PrescriptionPrintDto.cs` | Minor - 确认字段完整性 |
| Desktop/MedicalCase | `PrescriptionPanelViewModel.cs` | Minor - 调用打印时传递完整上下文 |
| Desktop/Infrastructure | `IConfigurationService.cs` | Minor - 添加诊所配置读取 |

## Acceptance Criteria

- [ ] AC-001: appsettings.json 包含 ClinicSettings 配置节，可配置诊所名称
- [ ] AC-002: 打印预览显示真实患者姓名、性别、年龄
- [ ] AC-003: 打印预览显示处方药材列表，格式为"药名+剂量g"
- [ ] AC-004: 打印预览"诊断"字段显示中医诊断内容
- [ ] AC-005: 打印预览"诊见"字段显示治疗方案内容
- [ ] AC-006: 修改 appsettings.json 中诊所名称后，打印显示新名称
- [ ] AC-007: 打印布局与处方模板一致

## Out of Scope

- PDF导出功能增强（保持现有XPS导出）
- 打印历史记录
- 多处方模板选择
- 处方笺编号自动生成规则
- 费用自动计算（保持现有逻辑）

## Risks

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 配置服务在Desktop端不可用 | 中 | 高 | 使用本地配置文件或嵌入式默认值 |
| 患者服务跨模块调用复杂 | 低 | 中 | 使用 ICrossModuleQueryService |
| FlowDocument布局与模板差异 | 中 | 低 | 提供预览功能供用户确认 |

## Technical Notes

### 现有代码分析

`PrescriptionPrintService.cs` 已有完整打印框架：
- `PrintPrescriptionAsync()` - 打印入口
- `PreviewPrescriptionAsync()` - 预览功能
- `MapToPrintDtoAsync()` - 数据映射（需修改）
- `BuildFlowDocument()` - 文档构建

需修改的占位符方法：
- `PopulateClinicInfo()` - 从配置读取
- `PopulatePatientInfo()` - 从患者服务获取
- `PopulateDiagnosisInfo()` - 映射中医诊断和治疗方案

### 配置结构

```json
{
  "ClinicSettings": {
    "Name": "凌隐宝堂中医诊所",
    "Address": "",
    "Phone": "",
    "Department": "中医科"
  }
}
```

## References

- 处方模板文件: `普通处方模版-1.docx`
- 现有打印服务: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
- 打印接口定义: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Interfaces/IPrescriptionPrintService.cs`
- 模块通信规范: `openspec/specs/module-communication/spec.md`
