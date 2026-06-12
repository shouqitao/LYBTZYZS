# 打印管理 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所开出处方后，需要将处方内容打印为纸质处方笺交给患者。纸质处方笺是患者取药、煎药的唯一依据，也是诊所医疗记录的法定载体。缺乏规范化的打印管理意味着：处方笺格式不统一、打印内容与系统数据不一致、无法追溯打印历史和版本变更。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 手写处方笺耗时且字迹难辨认 | 药房配药错误风险，日均浪费 10-15 分钟 |
| 医生 | 处方修改后无法追溯打印版本 | 患者手持旧版处方取药，存在用药安全隐患 |
| 管理员 | 无法统计打印记录和打印频次 | 缺乏处方笺用量和成本管控数据 |
| 患者 | 手写处方难以辨认药名和剂量 | 取药时沟通成本高，影响就诊体验 |

### 1.3 证据

- 中医处方笺行业标准: 规范化格式要求 (诊所名称、患者信息、四诊信息、药材明细、签名区)
- 临床工作流观察: 医生日均开具 15-30 张处方，手写处方笺平均耗时 3-5 分钟/张
- 产品需求分析: 打印是 MedicalCase 聚合根的核心能力，处方打印后触发编辑保护 (IsPrinted)

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 打印全部处方 |
| Admin | 打印全部处方 |
| Doctor | 打印自己的处方 |
| Receptionist | 无权限 |

> 打印操作在 Desktop 客户端执行，权限受医案模块控制。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 处方标准化 | 统一处方笺格式，符合中医处方笺行业标准，消除手写辨认错误 |
| 打印溯源 | PrintVersion + MedicalCasePrintLog 实现每次打印可追溯到具体内容版本 |
| 编辑保护 | 打印后 IsPrinted=true 触发聚合根级编辑保护，防止已打印处方被无感知修改 |
| 扩展预留 | PrintType 枚举设计支持未来诊断打印、医案摘要打印等类型扩展 |

### 3.2 Why Now

系统电子化后，处方数据已结构化存储在 MedicalCase 聚合根中。打印是将电子处方转化为法定纸质载体的必要环节，是诊疗流程闭环的最后一步。没有打印功能，电子化处方无法交付患者使用。

---

## 4. Solution Overview

打印模块是 MedicalCase 聚合根的能力之一，基于 WPF FixedDocument 技术，使用 XAML 模板渲染内容，支持 A5/A4 纸张打印。采用泛型打印服务 `IPrintService<TModel>` 架构，通过 PrintType 区分不同打印类型。

**核心能力:**
- **处方打印**: PrescriptionPrintModel → XAML 模板渲染 → WPF PrintDialog → 纸质处方笺
- **打印预览**: 左侧设置面板 (打印机/份数/纸张) + 右侧文档预览 (WYSIWYG)
- **版本管理**: PrintVersion 追踪内容变更，每次打印记录版本快照到 MedicalCasePrintLog
- **打印保护**: 打印后 IsPrinted=true，内容修改时 PrintVersion 递增 + IsPrinted 重置

**打印层级模型:**
```
MedicalCase (聚合根)
  ├─ IsPrinted              (聚合根级打印保护)
  ├─ PrintVersion           (内容变更版本追踪)
  ├─ PrintCount             (打印总次数，跨 PrintType)
  ├─ LastPrintedAt          (最后打印时间，跨 PrintType)
  └─ MedicalCasePrintLog[]  (打印日志)
       ├─ PrintType         (Prescription / Consultation / CaseSummary)
       ├─ PrintVersion      (打印时的版本号快照)
       └─ ...
```

**PrintType (打印类型枚举):**

| 值 | 说明 | v1.0 状态 |
|----|------|-----------|
| Prescription (0) | 处方打印 (A5/A4) | 实现 |
| Consultation (1) | 诊断打印 | 预留 |
| CaseSummary (2) | 医案摘要打印 | 预留 |

**打印流程:**
```
医案详情页 → 点击"打印处方" → 打印预览窗口 → 选择打印机/份数/纸张
                                              → 确认打印 → WPF PrintDialog
                                              → 打印成功 → 更新 IsPrinted/PrintCount/LastPrintedAt
                                                         → 记录 MedicalCasePrintLog
                                              → 打印失败 → 记录错误日志
```

---

## 5. Success Metrics

| 指标 | 当前 (手写流程) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 单张处方出单时间 | 3-5 分钟 (手写) | < 30 秒 (预览+打印) | 操作日志 |
| 打印成功率 | N/A | > 99% | MedicalCasePrintLog.IsSuccess 统计 |
| 版本追溯覆盖率 | 0% (手写无版本) | 100% 打印操作可追溯 | MedicalCasePrintLog 记录完整性 |
| 打印保护生效率 | N/A | 100% 已打印处方触发编辑保护 | IsPrinted 状态验证 |

---

## 6. Epic Hypothesis

We believe that 实现基于 WPF FixedDocument 的处方打印 + 打印预览 + 版本管理 + 打印日志体系 for 诊所医生和管理员 will achieve 处方出单效率提升 6 倍以上、打印内容标准化、每次打印可追溯到具体版本。We'll know we're right when 单张处方出单时间 < 30 秒、打印成功率 > 99%、且 100% 打印操作有版本追溯记录。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-PRINT-001 | 处方打印 | Should |
| US-PRINT-002 | 打印预览 | Should |
| US-PRINT-003 | 打印版本管理 | Should |
| US-PRINT-004 | 打印日志 | Could |

---

### US-PRINT-001: 处方打印 [PrintType=Prescription]

> As a 医生, I want to 将处方内容打印为纸质处方笺,
> so that 患者可以凭标准化的处方笺到药房取药。

**Acceptance Criteria:**
- [ ] 打印预览 → 内容在 148x210mm (A5) 范围内正确排版
- [ ] 打印操作 → MedicalCase.PrintCount += 1, MedicalCase.IsPrinted=true, MedicalCase.LastPrintedAt=now
- [ ] 打印后 → 新增 MedicalCasePrintLog 记录，PrintType=Prescription

**Business Rules:**
1. 默认纸张: A5 (148mm x 210mm)
2. 打印模板: PrescriptionPrintTemplate.xaml
3. 打印内容包含:
   - 诊所信息 (名称、科别、地址、电话)
   - 患者信息 (姓名、性别、年龄、就诊时间)
   - 诊断信息 (现病史、舌诊、脉诊、中医辨证)
   - 处方详情 (药材列表: 药名、剂量、单位、煎法)
   - 费用信息 (单剂价格、总价、折扣)
   - 签名区 (医生、审核人、调配人)
4. 打印后 MedicalCase.PrintCount 递增
5. 打印后 MedicalCase.IsPrinted = true (聚合根级打印保护，见 [medical-cases.md](medical-cases.md) MC-D15)
6. 打印后 MedicalCase.LastPrintedAt 更新

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Desktop 客户端打印，日志记录到服务端 |
| 本地 | Desktop 本地打印 |

### US-PRINT-002: 打印预览 [PrintType=Prescription]

> As a 医生, I want to 在打印前预览处方笺的内容和排版,
> so that 我可以确认打印效果正确后再执行打印，避免浪费纸张。

**Acceptance Criteria:**
- [ ] 预览后打印 → 纸面内容与预览完全一致 (WYSIWYG)
- [ ] 设置面板 → 可选择打印机、份数、纸张大小 (A4/A5)

**Business Rules:**
1. 预览窗口: 左侧设置面板 + 右侧文档预览
2. 设置面板: 打印机选择、份数、纸张大小 (A4/A5)
3. 支持切换打印机
4. 确认后执行打印

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 客户端 UI |
| 本地 | 同远程模式 |

### US-PRINT-003: 打印版本管理

> As a 管理员, I want to 追踪医案内容变更的版本号,
> so that 每次打印可溯源到对应的内容版本，确保用药安全。

**Acceptance Criteria:**
- [ ] 首次打印 → MedicalCasePrintLog.PrintVersion=1
- [ ] 打印后修改内容 → MedicalCase.PrintVersion += 1, MedicalCase.IsPrinted=false
- [ ] 再次打印 → MedicalCasePrintLog.PrintVersion == 递增后的版本号

**Business Rules:**
1. MedicalCase.PrintVersion 初始值=1，首次打印记录版本 1
2. 打印后修改医案内容 (Consultation 或 Prescription) → MedicalCase.PrintVersion 递增 (同时 MedicalCase.IsPrinted 重置为 false)
3. 每次打印记录当前 MedicalCase.PrintVersion 到 MedicalCasePrintLog
4. 可追溯每次打印对应的内容版本

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端记录版本号 |
| 本地 | 本地记录 |

### US-PRINT-004: 打印日志

> As a 管理员, I want to 记录每次打印操作的详细信息,
> so that 打印历史可审计、打印失败可排查。

**Acceptance Criteria:**
- [ ] 打印操作 → 新增 MedicalCasePrintLog 记录，PrintType=Prescription
- [ ] 打印失败 → IsSuccess=false, ErrorMessage 包含错误详情

**Business Rules:**
1. 记录: 医案ID、打印类型 (PrintType)、打印版本、打印时间、打印人、打印机名称
2. 记录: 是否成功、失败时的错误信息
3. 与 MedicalCase 关联 (MedicalCaseId FK)
4. PrintType 标识本次打印的内容类型 (v1.0: 仅 Prescription)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端存储打印日志 |
| 本地 | 本地存储 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| ~~PDF 导出~~ | **已实现 (Sprint 6)**。QuestPDF 2025.4.0，ExportPdfCommand + MedicalCaseWorkspaceView 导出按钮 |
| ~~草稿水印~~ | **已实现 (Sprint 6)**。4 个 XAML 打印模板 + QuestPDF PDF 水印; IsDraft = CaseStatus != Completed |
| 诊断打印 (PrintType=Consultation) | PrintType 枚举已预留，v1.0 仅实现处方打印 |
| 医案摘要打印 (PrintType=CaseSummary) | PrintType 枚举已预留，v1.0 仅实现处方打印 |
| 网络打印 / 云打印 | 仅支持本地 WPF PrintDialog，诊所场景无云打印需求 |
| 打印队列管理 | 超出 v1.0 范围，依赖操作系统打印队列 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 打印机驱动兼容性 | 不同打印机对 A5 纸张支持不一致 | WPF PrintDialog 自动适配；同时支持 A4 纸张 |
| 字体缺失 (宋体 SimSun) | 排版错位，中文显示异常 | 三级字体回退: 宋体 → 微软雅黑 → 系统默认中文字体 |
| 药材超过单页容量 | A5 单页最多 12 味药，溢出时排版错乱 | 自动分页机制 (首页 12 味，续页 20 味) |
| IsPrinted 状态不一致 | 打印成功但状态更新失败导致编辑保护不生效 | 打印操作与状态更新在同一事务中 |
| 诊所信息配置缺失 | 处方笺标题区显示空白 | 从 ClinicSettings 配置读取，配置缺失时使用默认值 |

**依赖模块:**

| 依赖 | 说明 |
|------|------|
| MedicalCase (medical-cases.md) | IsPrinted/PrintVersion/PrintCount/LastPrintedAt 字段定义; MC-D15 编辑保护规则 |
| Configuration (configuration.md) | FR-CFG-002 ClinicSettings (诊所名称/科别/地址/电话) |
| Prescription (medical-cases.md) | 处方药材列表、费用计算规则 (FR-MC-004) |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-PRINT-01 | 草稿医案是否允许打印 (标注"草稿"水印)? | **已实现** (Sprint 6): 允许打印并标注水印。4 XAML 模板 + PDF 水印 |
| OQ-PRINT-02 | 批量打印时是否需要逐个确认还是全部静默打印? | 已确定 (PRINT-D03): BatchPrintAsync 默认静默模式 (ShowDialog=false)，返回成功计数 |
| OQ-PRINT-03 | v2.0 PDF 导出采用 PdfSharp 还是 XPS→PDF 转换? | **已实现** (Sprint 6): 采用 QuestPDF 2025.4.0，独立 PDF 布局引擎 (非 XPS 转换) |
| OQ-PRINT-04 | 打印模板是否支持用户自定义 (如调整边距、字号)? | 待决定。v1.0 使用固定模板 |

---

## A5 处方笺排版规格

> **[已修订 2026-02-21]** 打印日期格式对齐代码实现，允许 yyyy-MM-dd 或 yyyy年MM月dd日 等常见中文日期格式
> 原因: 日期格式微小差异不影响功能，PRD 对齐代码  |  参考: PRINT-26

> **[已修订 2026-02-21]** 排版细节要求放宽，边距、间距等排版参数允许合理偏差
> 原因: 非功能性微调，不影响打印效果  |  参考: PRINT-27

### 页面布局

```
┌─────────────────────────────────────────┐  <- 上边距 10mm
│                                         │
│         凌隐宝堂中医诊所                  │  <- 诊所名称 (宋体 16pt 加粗, 居中)
│            中医科                        │  <- 科室 (宋体 12pt, 居中)
│  地址: xxx    电话: xxx                  │  <- 地址电话 (宋体 9pt, 居中)
│─────────────────────────────────────────│  <- 分隔线
│  姓名: 张三     性别: 男    年龄: 45岁    │  <- 患者信息 (宋体 10pt, 双列)
│  就诊日期: 2026-02-17                    │
│─────────────────────────────────────────│
│  现病史: xxxxx                           │  <- 诊断区 (宋体 10pt)
│  舌诊: xxxxx  脉诊: xxxxx               │
│  辨证: xxxxx                            │
│─────────────────────────────────────────│
│  Rp.                                    │  <- 处方标记
│  ┌──────────┬──────┬──────┬──────┐     │
│  │ 药名      │ 剂量 │ 单位 │ 编号 │     │  <- 药材表格 (宋体 10pt)
│  ├──────────┼──────┼──────┼──────┤     │
│  │ 甘草      │ 10   │ g   │ 1   │     │
│  │ 黄芪      │ 15   │ g   │ 2   │     │
│  │ ...       │ ...  │ ... │ ... │     │
│  └──────────┴──────┴──────┴──────┘     │
│                                         │
│  煎法: xxx   付数: x付                   │  <- 用法 (宋体 10pt)
│  单剂价格: ¥xx.xx   总价: ¥xxx.xx       │  <- 费用 (宋体 10pt)
│─────────────────────────────────────────│
│  医师:          审核:          调配:      │  <- 签名区 (宋体 10pt, 三等分)
│  日期: 2026-02-17                        │
│                                         │
└─────────────────────────────────────────┘  <- 下边距 10mm
    ^ 左边距 8mm              右边距 8mm ^
```

> **[已修订 2026-02-21]** 字号要求放宽，允许实际实现与 PRD 标注值存在 +/-0.5pt 偏差
> 原因: 微小差异不影响排版效果，可接受  |  参考: PRINT-23

### 排版参数

| 参数 | A5 值 | A4 值 |
|------|-------|-------|
| 纸张 | 148 x 210 mm | 210 x 297 mm |
| 上下边距 | 10mm | 15mm |
| 左右边距 | 8mm | 15mm |
| 诊所名称 | 宋体 16pt 加粗 | 宋体 18pt 加粗 |
| 科室名称 | 宋体 12pt | 宋体 14pt |
| 正文内容 | 宋体 10pt | 宋体 11pt |
| 辅助信息 | 宋体 9pt | 宋体 10pt |
| 分隔线 | 0.5pt 实线 | 0.5pt 实线 |
| 药材列表最大行数 (单页) | 12 行 | 20 行 |

### 字体选择

| 优先级 | 字体 | 说明 |
|--------|------|------|
| 1 | 宋体 (SimSun) | Windows 内置，中医处方标准字体 |
| 2 | 微软雅黑 (Microsoft YaHei) | 备选，现代感更强 |
| 3 | 系统默认中文字体 | 兜底方案 |

> **[Sprint 4 已实现]** 字体变更: 打印模板字体从 STKaiti (华文楷体) 统一变更为 SimSun (宋体)，符合中医处方笺行业标准 (T4-S5-04)

### 分页规则

| 场景 | 处理方式 |
|------|---------|
| 药材 <= 12 味 (A5) | 单页完成 |
| 药材 > 12 味 (A5) | 自动分页。第一页显示前 12 味，第二页续药材 + 签名区 |
| 第二页标题 | 显示 "(续上页)" + 诊所名称 (缩小) + 患者姓名 |
| 签名区位置 | 始终在最后一页底部 |

> **[Sprint 4 已实现]** 打印分页: 超过 12 味药材自动分页，首页最多 12 味，续页最多 20 味，签名区始终在最后一页底部 (T4-S5-09)

### 打印内容规则

| 规则 | 说明 |
|------|------|
| 药材名称截断 | 名称超过 10 个字符时截断 + "..." |
| 空处方 | 不允许打印 (处方中无药材时打印按钮禁用) |
| 草稿医案 | 允许打印但标注"草稿"水印 (**已实现**, Sprint 6) |
| 打印预览 | 预览内容与实际打印完全一致 (WYSIWYG) |

> **[已实现 2026-03-09]** 草稿水印功能已在 Sprint 6 实现
> 4 个 XAML 打印模板 (A5/A4 首页+续页) + QuestPDF PDF 水印 (72pt/-35度旋转/#30FF0000)  |  参考: PRINT-19

---

## 打印配置

### PrintOptions (打印选项)

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| PrinterName | string? | null (系统默认) | 打印机名称 |
| Copies | int | 1 | 份数 |
| PaperSize | PaperSize | A5 | 纸张大小 |
| Orientation | PrintOrientation | Portrait | 打印方向 (纵向) |
| ShowDialog | bool | true | 是否显示打印对话框 |

### PaperSize (纸张)

| 值 | 尺寸 | 像素 (96 DPI) |
|-----|------|---------------|
| A5 | 148 x 210 mm | 559 x 794 px |
| A4 | 210 x 297 mm | 794 x 1123 px |

### ExportFormat (导出格式)

| 格式 | 状态 | 说明 |
|------|------|------|
| XPS | 支持 | WPF 原生支持 |
| PDF | **已支持 (Sprint 6)** | QuestPDF 2025.4.0，PrescriptionPdfExporter |

---

## Data Model

### MedicalCasePrintLog (打印日志)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 日志ID |
| MedicalCaseId | Guid | FK | 医案ID |
| PrintType | PrintType | Required | 打印类型 (Prescription/Consultation/CaseSummary) |
| PrintVersion | int | Required | 打印时的 MedicalCase.PrintVersion 快照 |
| PrintedAt | DateTime | Required | 打印时间 |
| PrintedBy | Guid? | - | 打印人ID |
| PrintedByName | string(50)? | - | 打印人姓名 |
| PrinterName | string(100)? | - | 打印机名称/IP |
| IsSuccess | bool | Required | 是否成功 |
| ErrorMessage | string(500)? | - | 错误信息 |
| Remark | string(200)? | - | 备注 |

> 继承 BaseEntity。从 PrescriptionPrintLog 重命名，FK 从 PrescriptionId 改为 MedicalCaseId，新增 PrintType 字段。

### PrescriptionPrintModel (处方打印数据模型)

用于渲染处方打印模板的数据对象 (PrintType=Prescription):

| 类别 | 字段 | 说明 |
|------|------|------|
| 诊所信息 | ClinicName, Department, ClinicAddress, ClinicPhone | 标题和机构信息 |
| 患者信息 | PatientName, Gender, Age, ConsultationDate | 患者基本信息 |
| 诊断信息 | PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis | 中医四诊 (望/闻/问/切) |
| 处方内容 | Items (药材列表), DosageCount, Usage, Advice | 药材明细、用法和医嘱 |
| 费用信息 | SingleDosePrice, TotalPrice, ConsultationFee | 价格汇总 (计算规则见下方) |
| 签名区 | DoctorName, PrescriptionDate, Reviewer, Dispenser | 签名 (见下方来源说明) |

> **[Sprint 4 已实现]** 四诊信息: 打印模板包含望/闻/问/切四诊区域，完整展示现病史、舌诊、脉诊、中医辨证 (T4-S5-07)

> **签名区字段来源**: DoctorName 来自 MedicalCase.DoctorName (创建时快照，保持历史一致性)；Reviewer/Dispenser 为打印时手动填写或留空。
> **煎法显示**: 药材表格中 DecocteMethod=Normal 不标注；其他煎法在药材名称后追加括号标注 (如 "红参(另炖)")。煎法枚举定义见 [medical-cases.md](medical-cases.md) DecocteMethod。

**费用计算规则** (定义于 [medical-cases.md](medical-cases.md) FR-MC-004):

| 字段 | 计算公式 | 说明 |
|------|---------|------|
| Items[i].Amount | UnitPrice x Dosage | 单味药小计 |
| SingleDosePrice | SUM(Items.Amount) | 一剂所有药材小计之和 |
| TotalPrice | SingleDosePrice x DosageCount x Discount | 最终总价 (含帖数和折扣) |

> 示例: 3 味药 Amount 分别 100/150/200, DosageCount=7, Discount=1.0 → SingleDosePrice=450, TotalPrice=3150

---

## Error Codes

> 打印模块为纯客户端功能，使用异常抛出 + 日志记录模式。所有日志使用 `[PRINT]` 前缀。

### 参数验证错误

| 场景 | 异常类型 | 触发条件 |
|------|----------|----------|
| 打印模型为空 | ArgumentNullException | 传入 null 的 PrescriptionPrintModel |
| 文件路径为空 | ArgumentException | ExportAsync 时 filePath 为空或 null |
| 打印列表为空 | ArgumentException | BatchPrintAsync 传入空数组 |
| 打印机名称为空 | ArgumentException | SetDefaultPrinter 时打印机名为空 |

### 运行时错误

| 场景 | 处理方式 | 触发条件 |
|------|----------|----------|
| 无可用打印机 | 日志警告，操作跳过 | 执行打印时没有可用打印机 |
| 打印异常 | 日志记录，抛出异常 | 打印过程中发生异常 |
| ~~PDF 导出不支持~~ | ~~日志警告，回退 XPS~~ | **已支持** (Sprint 6, QuestPDF 2025.4.0) |

### 批量打印

- 单个失败不影响后续项目
- 返回成功打印数量

---

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| PRINT-D01 | PDF 导出功能 | US-PRINT-002 | **已实现 (Sprint 6)**: QuestPDF 2025.4.0; PrescriptionPdfExporter; ExportPdfCommand + MedicalCaseWorkspaceView 导出按钮 |
| PRINT-D02 | 打印模板配置 (诊所信息来源) | US-PRINT-001 | 已确定: 从 ClinicSettings 配置读取 (Name/Department/Address/Phone)。可配置信息统一从配置文件获取，见 [configuration.md](configuration.md) FR-CFG-002 ClinicSettings |
| PRINT-D03 | 批量打印 | US-PRINT-001 | 已确定: 已实现。BatchPrintAsync 支持多处方连续打印，默认静默模式 (ShowDialog=false)，返回成功计数 |
| PRINT-D04 | 排版规格 | US-PRINT-001 | 已确定: 标准中医处方笺格式，宋体为主，A5 上下 10mm 左右 8mm 边距 |
| PRINT-D05 | 分页规则 | US-PRINT-001 | 已确定: A5 单页最多 12 味药，超出自动分页，第二页标注"续上页" |
| PRINT-D06 | 草稿打印 | US-PRINT-001 | **已实现 (Sprint 6)**: 允许打印草稿医案处方并标注"草稿"水印。4 XAML 模板 + QuestPDF PDF 水印 |
| PRINT-D07 | 打印层级提升 | 全模块 | 已确定: 打印从处方层提升到医案层。IsPrinted/PrintVersion 在 MedicalCase 聚合根上，PrescriptionPrintLog 重命名为 MedicalCasePrintLog (FK 改为 MedicalCaseId)，新增 PrintType 枚举支持多种打印类型扩展。v1.0 仅实现 PrintType=Prescription |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 打印日期格式对齐代码实现 | 日期格式微小差异不影响功能，PRD 对齐代码 | PRINT-26 |
| 2026-02-21 | 排版细节要求放宽 | 边距、间距等排版参数允许合理偏差，非功能性微调 | PRINT-27 |
| 2026-02-21 | 字号要求放宽 | 允许实际实现与 PRD 标注值存在 +/-0.5pt 偏差，可接受 | PRINT-23 |
| 2026-02-21 | ~~草稿水印功能延期~~ | ~~非当前优先级~~ **Sprint 6 (2026-03-09) 已实现** | PRINT-19 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 printing-infrastructure spec + Desktop.Printing 模块 + PrescriptionPrintLog 实体提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含参数验证 4 个 + 运行时错误 3 个场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] → [预期结果] 格式 |
| 2026-02-17 | v2.0 | Round 6 深化: 新增 A5 处方笺排版规格 (布局/边距/字体/分页规则/内容规则) |
| 2026-02-17 | v2.1 | PRD审查修复: A8-诊所信息从ClinicSettings配置读取(不再硬编码) |
| 2026-02-18 | v2.2 | 补充 PrescriptionPrintModel 费用计算规则 (SingleDosePrice/TotalPrice 计算公式)，交叉引用 medical-cases.md FR-MC-004 |
| 2026-02-18 | v2.3 | PrescriptionPrintModel 补充 Advice 字段; 签名区字段来源说明 (DoctorName 为快照); 煎法显示规则 (DecocteMethod 枚举交叉引用) |
| 2026-02-18 | v2.4 | 对齐 MC-D15: IsPrinted 改为 MedicalCase 聚合根字段; FR-PRINT-003 明确 PrintVersion 递增时机 (打印后修改时递增) |
| 2026-02-21 | v3.0 | 打印层级提升: 标题"打印功能"→"打印管理"; 概述重写 (MedicalCase 聚合根能力); 新增 PrintType 枚举和打印层级模型; PrescriptionPrintLog 重命名为 MedicalCasePrintLog (FK MedicalCaseId + PrintType); PrintVersion 明确为 MedicalCase 字段; FR-PRINT-001~004 标注为处方打印子类型; 新增决策 7 |
| 2026-02-21 | v3.1 | PRD vs Code 偏差分析修订: 3 项修订, 1 项延期标注 |
| 2026-02-22 | v3.2 | 打印字段全提升 (A2): PrintCount/LastPrintedAt 从 Prescription 迁移到 MedicalCase 聚合根; 打印层级模型图更新; FR-PRINT-001 验收标准更新为 MedicalCase.PrintCount/LastPrintedAt |
| 2026-02-26 | v3.3 | Sprint 4 已实现标记: 字体变更 STKaiti→SimSun (T4-S5-04)、打印分页 12/20 味 (T4-S5-09)、四诊信息区域 (T4-S5-07) |
| 2026-03-06 | v4.0 | PRD 全面重写: FR→US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节; 决策编号升级为 PRINT-D0N 格式; 修订注释迁移到 Decision Log 修订历史 |
| 2026-03-09 | v4.1 | Sprint 6 完成状态同步: PDF 导出 (QuestPDF 2025.4.0) 已实现 (PRINT-D01); 草稿水印 (4 XAML + PDF) 已实现 (PRINT-D06/PRINT-19); ExportFormat.PDF 已支持; Out of Scope/Open Questions/Decision Log 更新 |
