---
type: module
title: 打印管理模块
tags: [module, printing, medical-case]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/printing.md
---

## 概述

打印管理模块是 MedicalCase 聚合根的核心能力之一，基于 WPF FixedDocument 技术将电子处方渲染为标准化纸质处方笺。该模块实现打印溯源与编辑保护机制，是诊疗流程闭环的最后一步，确保电子化处方能够正确交付患者使用。

## 核心能力

| 能力 | 说明 |
|------|------|
| WPF FixedDocument 渲染 | 使用 XAML 模板渲染处方内容，支持 A5/A4 纸张规格，实现所见即所得 (WYSIWYG) 打印预览 |
| 打印预览 | 左侧设置面板 (打印机/份数/纸张) + 右侧文档预览，打印前可确认最终效果 |
| PrintVersion 版本追踪 | 每次打印记录内容版本快照到 MedicalCasePrintLog，支持追溯历史打印版本 |
| 编辑保护机制 | 打印后 IsPrinted=true 触发聚合根级编辑保护，内容修改时 PrintVersion 递增 + IsPrinted 重置 |
| PrintType 类型扩展 | 枚举设计支持 Prescription (处方)、Consultation (诊断)、CaseSummary (医案摘要) 等类型扩展 |

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

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 打印全部处方 |
| Admin | 打印全部处方 |
| Doctor | 打印自己的处方 |
| Receptionist | 无权限 |

> 打印操作在 Desktop 客户端执行，权限受医案模块控制。

## 关键业务规则

1. **PrintVersion 版本快照**: 每次打印时将当前 PrintVersion 记录到 MedicalCasePrintLog，支持追溯某次打印的具体内容版本
2. **编辑保护触发**: 打印后 IsPrinted=true，若处方内容被修改则 PrintVersion 递增且 IsPrinted 重置为 false，防止已打印处方被无感知修改
3. **打印次数统计**: PrintCount 累计所有类型的打印总次数，LastPrintedAt 记录最后一次打印时间 (跨 PrintType)
4. **A5/A4 纸张支持**: 处方笺模板支持 A5 和 A4 两种纸张规格，符合中医处方笺行业标准格式
5. **泛型打印服务架构**: 采用 `IPrintService<TModel>` 接口设计，支持未来扩展其他类型打印 (如诊断单、医案摘要)
6. **WYSIWYG 预览**: 右侧预览区域与实际打印输出完全一致，消除手写处方辨认错误

## 相关链接

- [[medical-case]] - 医案聚合根，包含 IsPrinted/PrintVersion 属性及打印入口
- [[print-log]] - 打印日志记录，追溯每次打印的版本和内容
- [[prescription]] - 处方明细数据，打印内容的核心组成部分
