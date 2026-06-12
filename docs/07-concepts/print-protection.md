---
type: concept
title: 打印保护机制
created: 2026-06-10
updated: 2026-06-10
tags: [business-rule, security, data-integrity, compliance, audit, medical-case]
related: [medical-case, business-rules, medical-case-locking-rules, medical-case-module, medical-case-api, wpf-printing-architecture]
sources: ["docs/02-requirements/printing.md", "docs/04-api-reference/medical-cases.md", "docs/02-requirements/medical-cases.md"]
---

# 打印保护机制

**打印保护 (Print Protection)** 是凌隐宝堂系统中确保医疗数据一致性、用药安全及纸质/电子记录同步的核心合规性业务规则（规则编号：MC-D15）。

## 核心逻辑与业务规则

1.  **状态标记**: 当医案 (`MedicalCase`) 成功打印后，聚合根字段 `IsPrinted` 被设置为 `true`。
2.  **编辑限制与版本重置**: 系统不强制禁止编辑已打印的医案，但任何对诊断 (`Consultation`) 或处方 (`Prescription`) 内容的修改都会触发以下连锁反应：
    *   `IsPrinted` 自动重置为 `false`。
    *   `PrintVersion` 递增（例如从 1 变为 2）。
    *   这标志着当前电子记录已与上一版纸质凭证不一致，医生必须重新打印以生成新的、与系统同步的纸质处方。
3.  **禁止删除处方项**: 医案打印后，严禁删除处方中的任何药材项。若尝试删除，系统将直接拦截。
4.  **一致性保障**: 该机制有效防止了“患者手持旧版处方取药”的安全隐患，确保诊疗数据的可追溯性与完整性。

## API 实现与校验细节

在 `PUT /api/v1/medicalcases/{id}` 端点中，后端服务会严格校验打印状态与修改请求：

* **修改原因必填**: 当 `MedicalCase.IsPrinted = true` 且检测到临床数据变更时，API 请求体必须包含 `editReason` 字段。若缺失，将返回 `422 Unprocessable Entity` 及错误码 `ERR-30403` ("医案已打印，修改需要提供修改原因")。
* **删除拦截**: 若尝试删除已打印医案的处方项，API 将返回 `422` 及错误码 `ERR-30404` ("医案已打印，不允许删除处方")。

```csharp
if (medicalCase.IsPrinted && HasClinicalChanges(input))
{
    if (string.IsNullOrWhiteSpace(input.EditReason))
    {
        throw new AppException("ERR-30403", "医案已打印，修改需要提供修改原因");
    }
    
    medicalCase.PrintVersion++;
    medicalCase.IsPrinted = false;
    // 记录审计日志，包含 editReason
}
```

## 版本溯源与审计追踪

* **PrintVersion**: 持续追踪医案核心内容的变更次数。
* **MedicalCasePrintLog**: 每次成功打印时，系统会将当前的 `PrintVersion` 及处方快照记录在日志中。
* **医案审计日志**: 所有打印后的修改行为均会被完整记录在 [[medical-case-audit-log|医案审计日志]] 中，包含：
  * 操作人 ID 与角色
  * 修改时间戳
  * 修改前后的具体值
  * **修改原因 (Edit Reason)**
* **合规价值**: 结合版本日志与审计记录，管理员或审计人员可精准追溯患者在特定时间点拿到的处方具体包含哪些药材和剂量，满足医疗行业对数据变更可追溯性的严格要求。

## 前端交互建议

* **状态感知**: 当检测到 `IsPrinted = true` 时，UI 应自动禁用“删除处方项”按钮。
* **强制输入原因**: 在编辑诊断或处方字段时，若检测到内容变更，应强制弹出对话框要求用户输入“修改原因”，否则禁止提交保存。
* **操作反馈**: 保存成功后，UI 应明确提示用户“医案已更新，请重新打印”，以引导医生完成凭证同步。

## 相关规则与参考

* 参见 [[business-rules]] 中的 BR-001 和 BR-003。
* 与 [[medical-case-locking-rules]] 共同构成医案数据完整性的防线。
* 相关架构与接口：[[medical-case-api|医案 API 参考]]、[[wpf-printing-architecture|WPF 打印架构]]、[[medical-case-audit-log|医案审计日志]]。