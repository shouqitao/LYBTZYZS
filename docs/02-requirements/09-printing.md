# 处方打印 (Printing)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 模块概述

处方打印是医案（MedicalCase）诊疗闭环的最后一步：将结构化电子处方渲染为符合中医处方笺行业标准的纸质载体，交付患者取药。本模块为 Desktop 客户端能力，基于 WPF `FixedDocument` 渲染 XAML 模板，并通过 QuestPDF 提供独立的 PDF 导出管线。打印成功后将状态与日志回写到 MedicalCase 聚合根（`IsPrinted` / `PrintVersion` / `PrintCount` / `LastPrintedAt`）及 `MedicalCasePrintLog`。

打印与医案模块强耦合：一旦打印，聚合根进入编辑保护态，后续编辑需填写原因并使 `PrintVersion` 递增、`IsPrinted` 重置（详见 [07-medical-cases.md](07-medical-cases.md)）。

## 业务规则

1. **空处方守卫（CODE-24）**：`PrescriptionPrintModel.Items` 为 null 或空集合时抛出异常，禁止打印。
2. **默认纸张**：A5（148×210mm，中医处方笺标准）；同时支持 A4（210×297mm）。
3. **多页续打**：药材数超出单页容量时使用续页模板（A5 首页 12 味、续页 20 味；签名区始终位于最后一页底部）。
4. **打印事件**：`PrintLogRequested` 在成功与失败时均触发，驱动日志回写。
5. **打印保护耦合**：编辑已打印医案会重置 `IsPrinted=false` 并递增 `PrintVersion`。
6. **草稿水印**：`CaseStatus != Completed` 的医案允许打印，但模板与 PDF 均叠加"草稿"水印（Sprint 6 已实现）。
7. **PDF 管线**：QuestPDF 2025.4.0（Community 许可证），独立布局引擎，非 XPS 转换。
8. **诊所信息来源**：标题区诊所名称/科别/地址/电话从 `ClinicSettings` 配置读取，缺失时使用默认值。

## 用户故事

### US-PRINT-001: 打印处方（A5/A4，对话框/直打印）

**角色**: 医生 / 管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 将处方内容打印为标准化的纸质处方笺（支持 A5/A4 与对话框/直打印两种模式），**以便** 患者可凭规范处方笺到药房取药，消除手写辨认错误。

**验收标准**:
- [ ] 内容在 148×210mm（A5）或 210×297mm（A4）范围内正确排版
- [ ] 打印成功后 `MedicalCase.PrintCount += 1`、`IsPrinted=true`、`LastPrintedAt=now`
- [ ] 打印后新增一条 `MedicalCasePrintLog`（`PrintType=Prescription`）
- [ ] `ShowDialog=false` 时跳过系统打印对话框直接提交（批量打印默认静默）
- [ ] 处方无药材时拦截并提示，禁止打印

**业务规则**:
1. 默认纸张 A5；`PaperSize` 可选 A5/A4，`Orientation` 默认纵向。
2. 打印模板：`PrescriptionPrintTemplate.xaml`（A5）、`PrescriptionPrintA4Template.xaml`（A4）及各自续页模板。
3. 打印内容含：诊所信息、患者信息、四诊（现病史/舌诊/脉诊/中医辨证）、药材明细（药名/剂量/单位/煎法）、费用（单剂/总价/折扣）、签名区。
4. 煎法 `DecocteMethod=Normal` 不标注，其他在药名后括号标注（如"红参(另炖)"）。
5. `BatchPrintAsync` 默认 `ShowDialog=false`，单个失败不影响后续，返回成功计数。
6. **单联/多联（D14 决策）**：v1.0 仅支持**单联打印**——患者持单付费取药（一张处方笺完成全部流程）；多联分发（药房联 / 存根联 / 收据联等）属 **v2.0**，不在 v1.0 范围。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 本地 OS 打印操作，与数据源模式无关） |

**实现参考**: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs:23`、`Interfaces/IPrintService.cs:9`、模板 `PrescriptionPrintTemplate.xaml` 等 4 个；服务端回写 `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasePrintController.cs:44`

---

### US-PRINT-002: 处方预览

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 在打印前预览处方笺的内容与排版（WYSIWYG），**以便** 确认无误后再执行打印，避免浪费纸张。

**验收标准**:
- [ ] 预览后打印的纸面内容与预览完全一致
- [ ] 设置面板可选择打印机、份数、纸张大小（A4/A5）
- [ ] 切换打印机后预览即时刷新
- [ ] 草稿医案预览叠加"草稿"水印

**业务规则**:
1. 预览窗口布局：左侧设置面板（打印机/份数/纸张）+ 右侧文档预览。
2. 预览基于 WPF `FixedDocument`，与实际打印共用同一渲染管线。
3. 草稿水印：XAML 模板与 PDF 均叠加（72pt / -35° 旋转 / #30FF0000）。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（纯客户端 UI 渲染） |

**实现参考**: `PrescriptionPrintService.cs`（预览与打印共用 `FixedDocument` 构建逻辑）

---

### US-PRINT-003: 导出处方（XPS/PDF）

**角色**: 医生
**优先级**: Should
**状态**: ✅ 已实现

**作为** 医生，**我想要** 将处方导出为 XPS 或 PDF 文件留存/归档，**以便** 在无打印机或需电子归档时仍有可交付的处方副本。

**验收标准**:
- [ ] 导出 XPS 使用 WPF 原生支持
- [ ] 导出 PDF 使用 QuestPDF 独立布局引擎（非 XPS 转换）
- [ ] `filePath` 为空时返回参数错误
- [ ] PDF 同样叠加草稿水印（当医案未完成时）

**业务规则**:
1. `ExportFormat`：XPS（WPF 原生）、PDF（QuestPDF 2025.4.0，Community 许可证）。
2. PDF 由 `PrescriptionPdfExporter` 生成，布局独立于 XAML 模板。
3. 导出入口：`MedicalCaseWorkspaceView` 的导出按钮 + `ExportPdfCommand`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（本地文件写入） |

**实现参考**: `PrescriptionPdfExporter`（QuestPDF）、`ExportPdfCommand`

---

### US-PRINT-004: 打印记录回写服务器（成功/失败）

**角色**: 管理员
**优先级**: Must
**状态**: ⚠️ **v1.0 补回**（D2 决策：恢复打印日志实体 + 回写端点 + PrintVersion，约 3-5 人日）

**作为** 管理员，**我想要** 每次打印操作（含失败）的详细信息回写到服务端日志，**以便** 打印历史可审计、失败可排查、版本可追溯。

**验收标准**:
- [ ] 打印成功 → 新增 `MedicalCasePrintLog`，`IsSuccess=true`
- [ ] 打印失败 → `IsSuccess=false`，`ErrorMessage` 含错误详情
- [ ] 日志含：医案ID、`PrintType`、`PrintVersion` 快照、时间、打印人、打印机名
- [ ] 打印失败不阻断日志写入

**业务规则**:
1. `MedicalCasePrintLog` 与 `MedicalCase` 关联（FK `MedicalCaseId`），继承 `BaseEntity`。
2. `PrintType` 枚举：`Prescription`(0) 已实现；`Consultation`(1)、`CaseSummary`(2) 预留。
3. `PrintVersion` 记录打印时的聚合根版本快照，用于事后溯源。
4. 回写端点：`PUT /print-completed`、`POST /print-log`（`MedicalCasePrintController`）。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 日志回写到服务端 `MedicalCasePrintLog` 表 |
| 本地 | 完全一致（通过统一 Service 层写入本地数据库） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasePrintController.cs:44,72`

---

## 边界条件验收标准

### 无打印机安装

- [ ] 客户端未安装打印机 → `ShowDialog=true` 时系统打印对话框无可用打印机，用户可取消；`ShowDialog=false` 时静默打印失败，记录 `MedicalCasePrintLog.IsSuccess=false`（ErrorMessage 含"无可用打印机"）
- [ ] 导出 PDF/XPS 不依赖打印机安装 → 正常导出

### 过期打印版本

- [ ] 打印后医生修改处方内容（IsPrinted=true → 修改 → IsPrinted=false, PrintVersion++）→ 前台持有的旧打印版本与电子记录不一致，系统提示"处方已修改，请重新打印"
- [ ] 打印版本（PrintVersion）与当前 MedicalCase.PrintVersion 不一致 → 日志记录版本差异（PrintVersionSnapshot vs CurrentPrintVersion）
- [ ] 同一医案多次打印 → PrintCount 递增，每次打印生成独立 MedicalCasePrintLog 记录

## 依赖

| 依赖 | 说明 |
|------|------|
| [07-medical-cases.md](07-medical-cases.md) | `IsPrinted`/`PrintVersion`/`PrintCount`/`LastPrintedAt` 字段；编辑保护规则 |
| [11b-configuration.md](11b-configuration.md) | `ClinicSettings`（诊所名称/科别/地址/电话） |

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-25 | 补充无打印机、过期打印版本边界条件验收标准 | 需求文档验收标准完善 |
| 2026-06-28 | US-PRINT-001 补单联/多联打印业务规则（D14：v1.0 单联，多联 v2.0） | spec S7 弱反映项补全 |
