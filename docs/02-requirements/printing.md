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
  5. 打印后 Prescription.IsPrinted = true
  6. 打印后 Prescription.LastPrintedAt 更新
- **远程模式**: Desktop 客户端打印，日志记录到服务端
- **本地模式**: Desktop 本地打印
- **验收标准**:
  - [ ] A5 纸张正确排版
  - [ ] 打印次数正确递增

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
  - [ ] 预览内容与打印结果一致
  - [ ] 可切换打印设置

### FR-PRINT-003: 打印版本管理

- **描述**: 跟踪处方内容修改后的打印版本
- **业务规则**:
  1. 处方内容修改后，PrintVersion 递增
  2. 每次打印记录当前 PrintVersion
  3. 可追溯打印的是第几个版本的内容
- **远程模式**: 服务端记录版本号
- **本地模式**: 本地记录
- **验收标准**:
  - [ ] 修改处方后版本号递增
  - [ ] 打印日志关联正确版本

### FR-PRINT-004: 打印日志

- **描述**: 记录每次打印操作的详细信息
- **业务规则**:
  1. 记录: 处方ID、打印版本、打印时间、打印人、打印机名称
  2. 记录: 是否成功、失败时的错误信息
  3. 与 Prescription 关联 (PrescriptionId FK)
- **远程模式**: 服务端存储打印日志
- **本地模式**: 本地存储
- **验收标准**:
  - [ ] 每次打印生成日志记录
  - [ ] 失败时记录错误信息

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
| 处方内容 | Items (药材列表), DosageCount, Usage | 药材明细和用法 |
| 费用信息 | SingleDosePrice, TotalPrice, ConsultationFee | 价格汇总 |
| 签名区 | DoctorName, PrescriptionDate, Reviewer, Dispenser | 医生签名 |

---

## 待讨论项

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | PDF 导出功能的优先级和实现方案 | FR-PRINT-002 | 待讨论 |
| 2 | 打印模板的自定义配置 (诊所信息来源) | FR-PRINT-001 | 待讨论 |
| 3 | 批量打印的场景需求 (多个处方连续打印) | FR-PRINT-001 | 待讨论 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 printing-infrastructure spec + Desktop.Printing 模块 + PrescriptionPrintLog 实体提取 |
