# 打印功能 需求规格

## 概述

打印模块负责处方笺的打印、预览和版本管理。基于 WPF FixedDocument 技术，使用 XAML 模板渲染处方内容，支持 A5 纸张打印。采用泛型打印服务 `IPrintService<TModel>` 架构，可扩展支持其他打印类型。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 打印全部处方 |
| Admin | 打印全部处方 |
| Doctor | 打印自己的处方 |
| Receptionist | 无权限 |

> 打印操作在 Desktop 客户端执行，权限受医案模块控制。

---

## 功能清单

### FR-PRINT-001: 处方打印

- **描述**: 将处方内容打印到纸质处方笺
- **业务规则**:
  1. 默认纸张: A5 (148mm x 210mm)
  2. 打印模板: PrescriptionPrintTemplate.xaml
  3. 打印内容包含:
     - 诊所信息 (名称、科别、地址、电话)
     - 患者信息 (姓名、性别、年龄、就诊时间)
     - 诊断信息 (现病史、舌诊、脉诊、中医辨证)
     - 处方详情 (药材列表: 药名、剂量、单位、煎法)
     - 费用信息 (单剂价格、总价、折扣)
     - 签名区 (医生、审核人、调配人)
  4. 打印后 Prescription.PrintCount 递增
  5. 打印后 MedicalCase.IsPrinted = true (打印保护标记，见 [medical-cases.md](medical-cases.md) MC-D15)
  6. 打印后 Prescription.LastPrintedAt 更新
- **远程模式**: Desktop 客户端打印，日志记录到服务端
- **本地模式**: Desktop 本地打印
- **验收标准**:
  - [ ] 打印预览 -> 内容在 148x210mm (A5) 范围内正确排版
  - [ ] 打印操作 -> Prescription.PrintCount += 1, MedicalCase.IsPrinted=true

### FR-PRINT-002: 打印预览

- **描述**: 打印前预览处方笺内容和排版
- **业务规则**:
  1. 预览窗口: 左侧设置面板 + 右侧文档预览
  2. 设置面板: 打印机选择、份数、纸张大小 (A4/A5)
  3. 支持切换打印机
  4. 确认后执行打印
- **远程模式**: 客户端 UI
- **本地模式**: 同远程模式
- **验收标准**:
  - [ ] 预览后打印 -> 纸面内容与预览完全一致
  - [ ] 设置面板 -> 可选择打印机、份数、纸张大小(A4/A5)

### FR-PRINT-003: 打印版本管理

- **描述**: 跟踪处方笺内容变更的版本号，支持打印溯源
- **业务规则**:
  1. 初始 PrintVersion=1，首次打印记录版本 1
  2. 打印后修改医案内容 (Consultation 或 Prescription) -> PrintVersion 递增 (同时 MedicalCase.IsPrinted 重置为 false)
  3. 每次打印记录当前 PrintVersion 到 PrescriptionPrintLog
  4. 可追溯每次打印对应的内容版本
- **远程模式**: 服务端记录版本号
- **本地模式**: 本地记录
- **验收标准**:
  - [ ] 首次打印 -> PrescriptionPrintLog.PrintVersion=1
  - [ ] 打印后修改内容 -> PrintVersion += 1, MedicalCase.IsPrinted=false
  - [ ] 再次打印 -> PrescriptionPrintLog.PrintVersion == 递增后的版本号

### FR-PRINT-004: 打印日志

- **描述**: 记录每次打印操作的详细信息
- **业务规则**:
  1. 记录: 处方ID、打印版本、打印时间、打印人、打印机名称
  2. 记录: 是否成功、失败时的错误信息
  3. 与 Prescription 关联 (PrescriptionId FK)
- **远程模式**: 服务端存储打印日志
- **本地模式**: 本地存储
- **验收标准**:
  - [ ] 打印操作 -> 新增 PrescriptionPrintLog 记录
  - [ ] 打印失败 -> IsSuccess=false, ErrorMessage 包含错误详情

---

## A5 处方笺排版规格

### 页面布局

```
┌─────────────────────────────────────────┐  ← 上边距 10mm
│                                         │
│         凌隐宝堂中医诊所                  │  ← 诊所名称 (宋体 16pt 加粗, 居中)
│            中医科                        │  ← 科室 (宋体 12pt, 居中)
│  地址: xxx    电话: xxx                  │  ← 地址电话 (宋体 9pt, 居中)
│─────────────────────────────────────────│  ← 分隔线
│  姓名: 张三     性别: 男    年龄: 45岁    │  ← 患者信息 (宋体 10pt, 双列)
│  就诊日期: 2026-02-17                    │
│─────────────────────────────────────────│
│  现病史: xxxxx                           │  ← 诊断区 (宋体 10pt)
│  舌诊: xxxxx  脉诊: xxxxx               │
│  辨证: xxxxx                            │
│─────────────────────────────────────────│
│  Rp.                                    │  ← 处方标记
│  ┌──────────┬──────┬──────┬──────┐     │
│  │ 药名      │ 剂量 │ 单位 │ 编号 │     │  ← 药材表格 (宋体 10pt)
│  ├──────────┼──────┼──────┼──────┤     │
│  │ 甘草      │ 10   │ g   │ 1   │     │
│  │ 黄芪      │ 15   │ g   │ 2   │     │
│  │ ...       │ ...  │ ... │ ... │     │
│  └──────────┴──────┴──────┴──────┘     │
│                                         │
│  煎法: xxx   付数: x付                   │  ← 用法 (宋体 10pt)
│  单剂价格: ¥xx.xx   总价: ¥xxx.xx       │  ← 费用 (宋体 10pt)
│─────────────────────────────────────────│
│  医师:          审核:          调配:      │  ← 签名区 (宋体 10pt, 三等分)
│  日期: 2026-02-17                        │
│                                         │
└─────────────────────────────────────────┘  ← 下边距 10mm
    ↑ 左边距 8mm              右边距 8mm ↑
```

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

### 分页规则

| 场景 | 处理方式 |
|------|---------|
| 药材 <= 12 味 (A5) | 单页完成 |
| 药材 > 12 味 (A5) | 自动分页。第一页显示前 12 味，第二页续药材 + 签名区 |
| 第二页标题 | 显示 "(续上页)" + 诊所名称 (缩小) + 患者姓名 |
| 签名区位置 | 始终在最后一页底部 |

### 打印内容规则

| 规则 | 说明 |
|------|------|
| 药材名称截断 | 名称超过 10 个字符时截断 + "..." |
| 空处方 | 不允许打印 (处方中无药材时打印按钮禁用) |
| 草稿医案 | 允许打印但标注"草稿" 水印 |
| 打印预览 | 预览内容与实际打印完全一致 (WYSIWYG) |

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
| PDF | 暂不支持 | MVP 阶段后续考虑 |

---

## 数据模型

### PrescriptionPrintLog (打印日志)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 日志ID |
| PrescriptionId | Guid | FK | 处方ID |
| PrintVersion | int | Required | 打印版本号 |
| PrintedAt | DateTime | Required | 打印时间 |
| PrintedBy | Guid? | - | 打印人ID |
| PrintedByName | string(50)? | - | 打印人姓名 |
| PrinterName | string(100)? | - | 打印机名称/IP |
| IsSuccess | bool | Required | 是否成功 |
| ErrorMessage | string(500)? | - | 错误信息 |
| Remark | string(200)? | - | 备注 |

> 继承 BaseEntity

### PrescriptionPrintModel (打印数据模型)

用于渲染打印模板的数据对象:

| 类别 | 字段 | 说明 |
|------|------|------|
| 诊所信息 | ClinicName, Department, ClinicAddress, ClinicPhone | 标题和机构信息 |
| 患者信息 | PatientName, Gender, Age, ConsultationDate | 患者基本信息 |
| 诊断信息 | PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis | 中医四诊 |
| 处方内容 | Items (药材列表), DosageCount, Usage, Advice | 药材明细、用法和医嘱 |
| 费用信息 | SingleDosePrice, TotalPrice, ConsultationFee | 价格汇总 (计算规则见下方) |
| 签名区 | DoctorName, PrescriptionDate, Reviewer, Dispenser | 签名 (见下方来源说明) |

> **签名区字段来源**: DoctorName 来自 MedicalCase.DoctorName (创建时快照，保持历史一致性)；Reviewer/Dispenser 为打印时手动填写或留空。
> **煎法显示**: 药材表格中 DecocteMethod=Normal 不标注；其他煎法在药材名称后追加括号标注 (如 "红参(另炖)")。煎法枚举定义见 [medical-cases.md](medical-cases.md) DecocteMethod。

**费用计算规则** (定义于 [medical-cases.md](medical-cases.md) FR-MC-004):

| 字段 | 计算公式 | 说明 |
|------|---------|------|
| Items[i].Amount | UnitPrice x Dosage | 单味药小计 |
| SingleDosePrice | SUM(Items.Amount) | 一剂所有药材小计之和 |
| TotalPrice | SingleDosePrice x DosageCount x Discount | 最终总价 (含帖数和折扣) |

> 示例: 3 味药 Amount 分别 100/150/200, DosageCount=7, Discount=1.0 -> SingleDosePrice=450, TotalPrice=3150

---

## 错误码

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
| PDF 导出不支持 | 日志警告，回退 XPS | 请求 PDF 导出 (v1.0 不支持) |

### 批量打印

- 单个失败不影响后续项目
- 返回成功打印数量

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | PDF 导出功能 | FR-PRINT-002 | 已确定: v1.0 不支持，使用 XPS 格式导出。v2.0 评估 PdfSharp 或 XPS->PDF 转换方案 |
| 2 | 打印模板配置 (诊所信息来源) | FR-PRINT-001 | 已确定: 从 ClinicSettings 配置读取 (Name/Department/Address/Phone)。可配置信息统一从配置文件获取，见 [configuration.md](configuration.md) FR-CFG-002 ClinicSettings |
| 3 | 批量打印 | FR-PRINT-001 | 已确定: 已实现。BatchPrintAsync 支持多处方连续打印，默认静默模式 (ShowDialog=false)，返回成功计数 |
| 4 | 排版规格 | FR-PRINT-001 | 已确定: 标准中医处方笺格式，宋体为主，A5 上下 10mm 左右 8mm 边距 |
| 5 | 分页规则 | FR-PRINT-001 | 已确定: A5 单页最多 12 味药，超出自动分页，第二页标注"续上页" |
| 6 | 草稿打印 | FR-PRINT-001 | 已确定: 允许打印草稿医案处方，但标注"草稿"水印 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 printing-infrastructure spec + Desktop.Printing 模块 + PrescriptionPrintLog 实体提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含参数验证 4 个 + 运行时错误 3 个场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果] 格式 |
| 2026-02-17 | v2.0 | Round 6 深化: 新增 A5 处方笺排版规格 (布局/边距/字体/分页规则/内容规则) |
| 2026-02-17 | v2.1 | PRD审查修复: A8-诊所信息从ClinicSettings配置读取(不再硬编码) |
| 2026-02-18 | v2.2 | 补充 PrescriptionPrintModel 费用计算规则 (SingleDosePrice/TotalPrice 计算公式)，交叉引用 medical-cases.md FR-MC-004 |
| 2026-02-18 | v2.3 | PrescriptionPrintModel 补充 Advice 字段; 签名区字段来源说明 (DoctorName 为快照); 煎法显示规则 (DecocteMethod 枚举交叉引用) |
| 2026-02-18 | v2.4 | 对齐 MC-D15: IsPrinted 改为 MedicalCase 聚合根字段; FR-PRINT-003 明确 PrintVersion 递增时机 (打印后修改时递增) |
