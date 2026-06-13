# 打印架构

## 1. 概述

打印功能为 Desktop-only 特性，运行在 WPF 客户端进程内。系统采用双引擎渲染架构：WPF `FixedDocument` 用于打印与预览，QuestPDF 用于 PDF 导出。服务端负责记录打印日志并维护打印保护状态（`IsPrinted`、`PrintVersion`），满足医疗合规审计要求。

核心打印组件分布：

| 组件 | 位置 | 职责 |
|------|------|------|
| PrintingModule | `LYBT.Desktop.Printing/` | Prism 模块注册，DI 容器配置 |
| IPrintService\<TModel\> | `LYBT.Desktop.Printing/Interfaces/` | 泛型打印服务接口（打印/预览/导出/批量） |
| PrescriptionPrintService | `LYBT.Desktop.Printing/Services/` | WPF FixedDocument 打印实现，分页引擎 |
| PrescriptionPdfExporter | `LYBT.Desktop.Printing/Services/` | QuestPDF PDF 导出，独立于 WPF 渲染管线 |
| PrescriptionPrintTemplate | `LYBT.Desktop.Printing/Templates/` | A5 首页 XAML 模板 |
| PrescriptionPrintA4Template | `LYBT.Desktop.Printing/Templates/` | A4 首页 XAML 模板 |
| PrescriptionContinuationTemplate | `LYBT.Desktop.Printing/Templates/` | A5 续页 XAML 模板 |
| PrescriptionContinuationA4Template | `LYBT.Desktop.Printing/Templates/` | A4 续页 XAML 模板 |
| PrescriptionPrintHandler | `LYBT.Desktop.MedicalCase/ViewModels/Components/` | ViewModel → PrintService 桥接层，数据准备 |
| PrescriptionPrintModel | `LYBT.Desktop.Printing/Models/` | 打印数据模型，模板绑定源 |
| MedicalCasePrintService | `LYBT.Module.MedicalCase/Services/` | 服务端打印状态回写 |
| MedicalCasePrintController | `LYBT.WebAPI/Controllers/` | 打印 API 端点（2 个） |
| MedicalCasePrintLog | `LYBT.Entities/MedicalCases/` | 打印日志实体 |

## 2. 打印模板体系

系统提供 4 套 XAML 模板，按纸张尺寸（A5/A4）和页面角色（首页/续页）正交组合。所有模板绑定到 `PrescriptionPrintModel` 作为 `DataContext`，使用宋体（SimSun）字体。

### 2.1 A5 模板

#### 2.1.1 首页模板（PrescriptionPrintTemplate）

- **页面尺寸**: 559 × 794 px（148 × 210 mm @ 96 DPI）
- **页边距**: 30 px（≈8 mm）四边
- **药材项宽度**: 95 px
- **标签字号**: 10.5 pt

布局分为 13 行，从上到下依次为：

| 行号 | 内容 | 数据源 |
|------|------|--------|
| 0 | 诊所名称 + 地址/电话 | `ClinicName`、`ClinicAddress`、`ClinicPhone` |
| 1 | 标题「普通处方笺」 | 固定文本 |
| 2 | 患者姓名/性别/年龄/日期 | `PatientName`、`Gender`、`Age`、`ConsultationDate` |
| 3 | 门诊号/科室/电话 | `OutpatientNumber`、`Department`、`PatientPhone` |
| 4 | 地址 | `PatientAddress` |
| 5 | 中医诊断 | `TcmDiagnosis` |
| 6 | 四诊（望诊/闻诊/舌诊/脉诊）2×2 网格 | `InspectionDiagnosis`、`AuscultationDiagnosis`、`TongueDiagnosis`、`PulseDiagnosis` |
| 7 | 症状描述 | `SymptomsText`（合并 `Symptoms` + `PresentIllness`） |
| 8 | Rp. + 药材列表 + 剂数/用法/医嘱 | `Items`、`DosageCount`、`Usage`、`Advice` |
| 9 | 弹性占位空间 | — |
| 10 | 水平分隔线 | — |
| 11 | 签名行（医师/审核/调配） | `DoctorName`、`Reviewer`、`Dispenser` |
| 12 | 费用行（诊查费/药费/折扣/合计） | `ConsultationFee`、`MedicineFee`、`Discount`、`TotalPrice` |

**草稿水印**: `DataTrigger` 绑定 `IsDraft`，显示 72pt「草稿」文字，旋转 -35°，颜色 `#20FF0000`。

#### 2.1.2 续页模板（PrescriptionContinuationTemplate）

- **页面尺寸**: 同首页（559 × 794 px）
- **简化页眉**: 诊所名称 + 「续页」 + 患者姓名 + 日期
- **内容区**: Rp.（续）+ 药材列表
- **仅末页显示**: 用法、医嘱、分隔线、签名行、费用行（由 `SetAsLastPage()` 控制）

### 2.2 A4 模板

#### 2.2.1 首页模板（PrescriptionPrintA4Template）

- **页面尺寸**: 794 × 1123 px（210 × 297 mm @ 96 DPI）
- **页边距**: 57 px（≈15 mm）四边
- **药材项宽度**: 135 px
- **标签字号**: 11 pt
- **首页药材上限**: 20 味（A5 为 12 味）
- **草稿水印**: 96pt（A5 为 72pt）

布局结构同 A5 首页，因页面面积更大，字段间距和字号均有放大。

#### 2.2.2 续页模板（PrescriptionContinuationA4Template）

- **页面尺寸**: 同 A4 首页（794 × 1123 px）
- **续页药材上限**: 20 味
- **结构与 A5 续页一致**，仅尺寸和字号放大

### 2.3 续方（续页）机制

当处方药材数量超过首页容量时，系统自动触发多页文档生成：

| 纸张 | 首页上限 | 续页上限 |
|------|---------|---------|
| A5 | 12 味 | 20 味/页 |
| A4 | 20 味 | 20 味/页 |

分页流程：

```
PrescriptionPrintService.BuildFixedDocument()
  │
  ├─ Items.Count <= limit → 单页文档（首页模板）
  │
  └─ Items.Count > limit → BuildMultiPageDocument()
       │
       ├─ 第 1 页: 首页模板，显示首页数量的药材
       ├─ 第 2~N 页: 续页模板，每页显示续页数量的药材
       └─ 最后一页: 调用 SetAsLastPage() 显示签名/费用
```

每页通过 `CloneModelWithItems()` 深拷贝模型并替换药材列表，保持各页数据独立。

## 3. 数据准备管线

`PrescriptionPrintHandler` 负责将领域 DTO 转换为 `PrescriptionPrintModel`，这是 ViewModel 层与打印服务之间的桥接层。

### 3.1 数据流

```
PrescriptionPrintHandler.PrintPreviewAsync()
  │
  ├─ 1. 数据获取
  │   ├─ 缓存优先: _medicalCaseService.CachedPrescription
  │   └─ 回退: IDataProvider 构建处方明细
  │
  ├─ 2. 验证（CODE-24）
  │   ├─ 处方为空 → PrintResult.Failed
  │   └─ 药材项为空/null → PrintResult.Failed
  │
  ├─ 3. BuildPrintModel() 数据映射
  │   ├─ 医师姓名 ← ISessionManager.CurrentUser.RealName
  │   ├─ 诊所信息 ← IClinicSettingsService.GetSettings()（支持热重载）
  │   ├─ 折扣 ← Prescription.Discount（默认 1.0）
  │   ├─ IsDraft ← CaseStatus != Completed
  │   ├─ MedicineFee = SingleDosePrice × DosageCount
  │   ├─ TotalPrice = MedicineFee × Discount（四舍六入五成双）
  │   └─ 药材项映射: HerbName + Dosage + Unit + DecocteMethod
  │
  ├─ 4. 渲染
  │   └─ _printService.PreviewAsync(printModel)
  │
  └─ 5. 状态回写
      └─ RecordPrintCompletedAsync()（失败不阻塞打印预览）
```

### 3.2 PrescriptionPrintModel 数据结构

| 分组 | 字段 | 来源 |
|------|------|------|
| 诊所信息 | `ClinicName`、`Department`、`ClinicAddress`、`ClinicPhone` | `IClinicSettingsService`（`clinic-settings.json`） |
| 患者信息 | `PatientName`、`Gender`、`Age`、`ConsultationDate`、`OutpatientNumber`、`PatientPhone`、`PatientAddress` | `PatientDetailDto` |
| 诊断 | `TcmDiagnosis`、`Symptoms`、`PresentIllness`、`InspectionDiagnosis`、`AuscultationDiagnosis`、`TongueDiagnosis`、`PulseDiagnosis` | `ConsultationDto` |
| 处方 | `Items`（List\<PrescriptionItemPrintModel\>）、`DosageCount`、`Usage` | `PrescriptionDetailDto` |
| 费用 | `ConsultationFee`、`MedicineFee`、`TreatmentFee`、`SingleDosePrice`、`Discount`、`TotalPrice` | 计算字段 |
| 签名 | `DoctorName`、`PrescriptionDate`、`Reviewer`、`Dispenser` | 会话 + DTO |
| 状态 | `IsDraft`、`PrescriptionNumber`、`Advice`、`FormulaSource` | 状态判断 |

### 3.3 药材项模型（PrescriptionItemPrintModel）

| 属性 | 类型 | 说明 |
|------|------|------|
| `SequenceNumber` | int | 序号 |
| `HerbName` | string | 药材名称（截断至 10 字符，CODE-37） |
| `Dosage` | decimal | 用量 |
| `Unit` | string | 单位（g、片等） |
| `DecocteMethod` | string? | 煎煮方法（先煎、后下等） |
| `DisplayText` | string | 计算属性: `"HerbName DosageUnit(DecocteMethod)"` |

### 3.4 诊所信息注入

诊所信息通过 `IClinicSettingsService` 提供，数据源为 `clinic-settings.json` 配置文件，支持热重载。字段映射：

- `ClinicName` → 诊所名称（打印在页眉左上）
- `ClinicAddress` → 诊所地址（页眉右上）
- `ClinicPhone` → 诊所电话（页眉右上）
- `Department` → 科室名称（患者信息行）

## 4. QuestPDF 集成

### 4.1 PDF 导出管线

```
PrescriptionPrintHandler.ExportPdfAsync()
  │
  ├─ 数据准备（同打印流程）
  ├─ SaveFileDialog（PDF 筛选器）
  └─ PrescriptionPdfExporter.Export(model, filePath)
       │
       ├─ QuestPDF.Settings.License = Community
       ├─ 字体: Microsoft YaHei（微软雅黑）
       ├─ 页面: A5 尺寸
       └─ 生成 PDF → 写入文件
```

### 4.2 QuestPDF 渲染引擎

`PrescriptionPdfExporter` 为静态类，使用 QuestPDF Fluent API 构建文档，布局镜像 XAML 模板结构：

| 方法 | 渲染内容 | 字号 |
|------|---------|------|
| `ComposeClinicHeader` | 诊所名称 + 地址/电话 | 12pt / 9pt |
| `ComposePatientInfoRow1` | 姓名/性别/年龄/日期 | 9pt / 10pt |
| `ComposePatientInfoRow2` | 门诊号/科室/电话 | 9pt / 10pt |
| `ComposeFieldRow` | 通用标签 + 下划线值 | 9pt / 10pt |
| `ComposeFourDiagnosis` | 四诊 2×2 网格 | 9pt / 10pt |
| `ComposePrescription` | Rp. + 药材流式布局 + 剂数/用法 + 医嘱 | 10pt |
| `ComposeSignatureRow` | 医师/审核/调配 | 9pt |
| `ComposeFeeRow` | 诊查费/药费/折扣/合计 | 9pt |

### 4.3 草稿水印

QuestPDF 渲染草稿水印：72pt 粗体「草稿」文字，旋转 -35°，颜色 `#30FF0000`，仅在 `IsDraft = true` 时显示。

### 4.4 依赖配置

- **NuGet**: QuestPDF `2025.12.4`
- **许可证**: Community（开源/小商业免费）
- **字体**: 微软雅黑（系统预装），A5 页面尺寸
- **安全覆盖**: SixLabors.Fonts / SixLabors.ImageSharp 安全补丁

## 5. 打印保护机制

打印保护是医疗合规的核心机制，防止已打印处方被未经授权地修改或删除。

### 5.1 保护字段

`MedicalCaseModel`（聚合根）上的打印保护字段：

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsPrinted` | bool | false | 打印保护标志，首次打印后置 true |
| `PrintVersion` | int | 1 | 版本号，每次打印或修改后递增 |
| `PrintCount` | int | 0 | 累计打印次数 |
| `LastPrintedAt` | DateTime? | null | 最后打印时间 |

### 5.2 状态流转

```
           创建
             │
             ▼
         ┌────────┐
         │ Draft  │  IsPrinted=false, PrintVersion=1
         │(未打印) │
         └────┬───┘
              │ 首次打印成功
              ▼
         ┌────────┐
         │Printed │  IsPrinted=true, PrintVersion=2, PrintCount=1
         │(已打印) │
         └────┬───┘
              │ 修改内容（需填写 EditReason）
              ▼
         ┌────────┐
         │Edited  │  IsPrinted=false, PrintVersion=3
         │(已修改) │  EditReason 必填
         └────┬───┘
              │ 重新打印
              ▼
         ┌────────┐
         │Re-     │  IsPrinted=true, PrintVersion=4, PrintCount=2
         │printed │
         └────────┘
```

### 5.3 保护规则

| 规则 | 描述 | 错误码 |
|------|------|--------|
| **修改需原因** | `IsPrinted=true` 时修改内容必须提供 `EditReason` | MC-D15 |
| **禁止删除** | `IsPrinted=true` 后不可删除医案 | ERR-30404 |
| **版本递增** | 每次打印或内容修改均递增 `PrintVersion` | — |
| **计数累加** | `PrintCount` 只增不减，记录累计打印次数 | — |
| **草稿水印** | `CaseStatus != Completed` 时所有模板显示「草稿」水印 | — |

### 5.4 EditReason 字段

当 `IsPrinted = true` 的医案被修改时，`EditReason`（修改原因）为必填字段。此字段记录在修改操作的审计日志中，确保所有打印后修改可追溯。

## 6. 打印日志架构

### 6.1 实体模型（MedicalCasePrintLog）

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | Guid | PK | 主键 |
| `MedicalCaseId` | Guid | Required, FK | 关联医案 |
| `PrintType` | PrintType | Required | 打印类型（Prescription=1, Formula=2） |
| `PrintVersion` | int | — | 打印时的 PrintVersion 快照 |
| `PrintedAt` | DateTime | — | 打印时间 |
| `PrintedBy` | Guid? | — | 操作人 ID |
| `PrintedByName` | string? | MaxLength(50) | 操作人姓名 |
| `PrinterName` | string? | MaxLength(100) | 打印机名称或 IP |
| `IsSuccess` | bool | 默认 true | 是否成功 |
| `ErrorMessage` | string? | MaxLength(500) | 失败原因 |
| `Remark` | string? | MaxLength(200) | 备注 |
| `CreatedAt` | DateTime | — | 创建时间（继承自 BaseEntity） |

### 6.2 存储模式

| 模式 | 日志存储 | 说明 |
|------|---------|------|
| **远程模式** | SQL Server `MedicalCasePrintLogs` 表 | 完整审计链，服务端记录 |
| **本地模式** | 无服务端日志 | LocalDB 模式下打印状态仅在本地维护 |
| **Desktop 端** | `PrintLogEntry`（内存） | `PrintLogRequested` 事件通知，不持久化 |

### 6.3 日志写入流程

```
Desktop 打印成功
  │
  ├─ PrescriptionPrintHandler.RecordPrintCompletedAsync()
  │   │
  │   └─ IMedicalCaseRepository.RecordPrintCompletedAsync()
  │       │
  │       └─ PUT /api/v1/medicalcases/{id}/print-completed
  │           │
  │           └─ MedicalCasePrintController
  │               │
  │               └─ MedicalCasePrintService.RecordPrintCompletedAsync()
  │                   │
  │                   ├─ GetByIdWithDetailsFreshAsync()（获取最新 RowVersion）
  │                   ├─ IsPrinted = true, PrintCount++, PrintVersion++
  │                   ├─ LastPrintedAt = UtcNow
  │                   ├─ 创建 MedicalCasePrintLog
  │                   └─ AddPrintLogAndSaveAsync()（显式 DbContext.Add）
```

### 6.4 AD-04 并发修复

打印日志写入使用以下模式避免 EF Core 实体状态跟踪问题：

1. **`GetByIdWithDetailsFreshAsync()`**: 从数据库获取最新实体（含 RowVersion），避免并发冲突
2. **显式 `DbContext.Add()`**: 通过 `_context.Set<MedicalCasePrintLog>().Add(printLog)` 显式标记为 `Added`，避免通过导航属性添加时 EF Core 将新实体误标记为 `Modified`（已设置 Guid 的实体）

## 7. 打印 API

### 7.1 端点概览

| 方法 | 路由 | 授权 | 说明 |
|------|------|------|------|
| `PUT` | `/api/v{version}/medicalcases/{id}/print-completed` | DoctorOrAdmin | 记录打印完成，更新打印状态 |
| `POST` | `/api/v{version}/medicalcases/{id}/print-logs` | DoctorOrAdmin | 记录打印事件（成功/失败） |

### 7.2 PrintCompletedRequest

```json
{
  "printType": 1,
  "printerName": "HP LaserJet Pro"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `PrintType` | PrintType (int) | 是 | 默认 Prescription=1 |
| `PrinterName` | string? | 否 | MaxLength(100) |

**副作用**: 设置 `IsPrinted=true`，递增 `PrintCount` 和 `PrintVersion`，设置 `LastPrintedAt`，创建 `MedicalCasePrintLog` 记录。

### 7.3 PrintLogInputDto

```json
{
  "printType": 1,
  "isSuccess": true,
  "printerName": "HP LaserJet Pro",
  "errorMessage": null
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `PrintType` | PrintType (int) | 是 | Prescription 或 Formula |
| `IsSuccess` | bool | 是 | 是否成功 |
| `PrinterName` | string? | 否 | MaxLength(100) |
| `ErrorMessage` | string? | 否 | MaxLength(500) |

**副作用**: 成功时同 `PrintCompletedRequest`；失败时仅创建日志记录，不修改医案状态。

### 7.4 操作人提取

两个端点均从 JWT Claims 提取操作人信息（`GetOperator()`），无需客户端传递用户信息。

### 7.5 PrintType 枚举

| 值 | 名称 | 说明 |
|----|------|------|
| 1 | Prescription | 处方打印（已实现） |
| 2 | Formula | 验方打印（已实现） |

> PRD 中规划了 `Consultation` 和 `CaseSummary` 类型，尚未实现。

## 8. 决策记录

| ID | 决策 | 原因 |
|----|------|------|
| AD-04 | 显式 DbContext.Add 写入打印日志 | 避免通过导航属性添加时 EF Core 将预设 Guid 的新实体误标记为 Modified |
| CODE-37 | 药材名称截断至 10 字符 | A5 模板药材项宽度 95px，超出则布局溢出 |
| CODE-24 | 空处方打印拦截 | 处方为空或药材项为空时阻止打印操作 |
| IPrintService\<TModel\> | 泛型打印服务接口 | 支持未来打印类型扩展（验方、医案总结等），无需修改服务实现 |
| 双引擎渲染 | WPF FixedDocument + QuestPDF | FixedDocument 原生支持 WPF 打印对话框和预览；QuestPDF 提供高质量 PDF 导出，不依赖 WPF 渲染管线 |
| PrintLogRequested 事件 | 打印结果松耦合通知 | 打印服务通过事件通知结果，调用方可自行决定是否记录日志，不耦合服务端 API |
| IClinicSettingsService 热重载 | 诊所信息运行时可修改 | 诊所名称/地址变更后无需重启应用，配置文件实时生效 |
| IPrintService 可选注入 | 优雅降级 | `PrescriptionPrintHandler` 中 `IPrintService?` 为可选依赖，打印服务未配置时返回明确错误信息而非崩溃 |

---

## 变更记录

| 日期 | 版本 | 描述 | 作者 |
|------|------|------|------|
| 2026-06-13 | 1.0 | 初始创建：完整打印架构文档 | AI |
