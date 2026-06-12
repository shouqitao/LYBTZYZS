# 打印 API 参考

> 医案打印相关端点，隶属 MedicalCasePrintController（远程）和 MedicalCasesController（本地）。

---

## 概述

打印功能依附于医案模块，没有独立的打印控制器。所有打印端点路由以 `/medicalcases/{id}/print-` 开头。

| 属性 | 说明 |
|------|------|
| 远程控制器 | `MedicalCasePrintController` |
| 本地控制器 | `MedicalCasesController`（内嵌打印端点） |
| 服务层 | `MedicalCasePrintService` → `IMedicalCasePrintService` |
| 认证 | 远程: `[Authorize(Policy = "DoctorOrAdmin")]` / 本地: JWT |
| 打印类型 | `PrintType` 枚举: `Prescription=1`, `Formula=2` |

---

## PUT /medicalcases/{id}/print-completed

记录一次成功的打印，更新医案打印状态并创建打印日志。

**认证**: DoctorOrAdmin

**请求体**:

```json
{
  "printType": 1,
  "printerName": "HP-LaserJet-01"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `printType` | int (PrintType) | 是 | `1`=处方, `2`=验方 |
| `printerName` | string? | 否 | 打印机名称，最大100字符 |

**响应**: `ApiResponse<MedicalCaseDetailDto>` — 更新后的医案详情

**副作用**:

| 字段 | 变更 |
|------|------|
| `IsPrinted` | → `true` |
| `PrintCount` | `+1` |
| `LastPrintedAt` | → `DateTime.UtcNow` |
| `PrintVersion` | `+1` |

同时创建 `MedicalPrintLog` 记录。

**远程 vs 本地差异**:

| 行为 | 远程 | 本地 |
|------|------|------|
| 更新医案字段 | ✅ | ✅ |
| 创建打印日志 | ✅ | ❌ |

---

## POST /medicalcases/{id}/print-logs

记录打印成功或失败事件。

**认证**: DoctorOrAdmin

**请求体**:

```json
{
  "printType": 1,
  "isSuccess": true,
  "printerName": "HP-LaserJet-01",
  "errorMessage": null
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `printType` | int (PrintType) | 是 | `1`=处方, `2`=验方 |
| `isSuccess` | bool | 是 | 是否成功 |
| `printerName` | string? | 否 | 打印机名称，最大100字符 |
| `errorMessage` | string? | 否 | 失败原因，最大500字符 |

**响应**: `ApiResponse<object>` — `{"success": true, "message": "打印日志记录成功"}`

**副作用** (`isSuccess=true` 时):

| 字段 | 变更 |
|------|------|
| `IsPrinted` | → `true` |
| `PrintCount` | `+1` |
| `LastPrintedAt` | → `DateTime.UtcNow` |
| `PrintVersion` | `+1` |

`isSuccess=false` 时仅记录日志，不更新医案字段。

**远程 vs 本地差异**:

| 行为 | 远程 | 本地 |
|------|------|------|
| 创建打印日志 | ✅ | ❌ (仅返回确认) |
| 更新医案字段 | ✅ (成功时) | ❌ |

---

## 打印保护规则

打印后的医案受以下保护：

| 规则 | 说明 | 错误码 |
|------|------|--------|
| 编辑需原因 | 修改已打印医案必须提供 `editReason` | `ERR-30403` (422) |
| 禁止删除处方项 | 已打印医案不可删除处方药品 | `ERR-30404` (422) |
| 重置打印状态 | 内容变更后 `IsPrinted` → `false`，`PrintVersion++` | — |
| 审计追踪 | 所有打印后编辑记录操作者、时间、变更前后值 | — |

---

## 打印日志实体

`MedicalPrintLog` 存储于 `MedicalCasePrintLogs` 表：

| 字段 | 类型 | 说明 |
|------|------|------|
| `MedicalCaseId` | Guid | 医案FK |
| `PrintType` | PrintType | 处方/验方 |
| `PrintVersion` | int | 打印时版本快照 |
| `PrintedAt` | DateTime | 打印时间 |
| `PrintedBy` | Guid? | 操作者ID |
| `PrintedByName` | string? | 操作者姓名 |
| `PrinterName` | string? | 打印机名称 |
| `IsSuccess` | bool | 是否成功 |
| `ErrorMessage` | string? | 失败原因 |

---

## 医案打印相关字段

`MedicalCaseDetailDto` 中的打印字段：

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `PrintVersion` | int | 1 | 打印版本号 |
| `LastPrintedAt` | DateTime? | null | 最后打印时间 |
| `PrintCount` | int | 0 | 总打印次数 |
| `IsPrinted` | bool | false | 是否已打印 |

---

## 角色权限

| 角色 | 打印权限 |
|------|---------|
| SuperAdmin | 打印所有处方 |
| Admin | 打印所有处方 |
| Doctor | 打印自己的处方 |
| Receptionist | 无打印权限 |

---

## PDF 导出

服务端无 PDF 导出端点。PDF 生成在桌面客户端通过 QuestPDF 完成：
- `PrescriptionPdfExporter` (`LYBT.Desktop.Printing/Services/`)

---

## 变更记录
| 日期 | 变更 |
|------|------|
| 2026-06-12 | 初始版本（从 medical-cases.md 独立出来）; 标题改为路由格式 |
