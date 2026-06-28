# 报表管理 (Reports)

> 版本: v1.1 | 日期: 2026-06-28 | 状态: 新建（A7 报表清单落地）

## 模块概述

报表模块为诊所经营提供数据统计能力，基于现有业务数据（`MedicalCase` / `Registration` / `Prescription` / `Herb`）聚合计算，不引入独立数据采集链路。v1.0 范围聚焦三大主题：

1. **收入汇总**（挂号费 / 药费 / 总收入）—— 经营对账
2. **就诊统计**（总问诊数 + 按医生分组）—— 工作量与流量
3. **药材使用排行**（使用次数 / 总用量）—— 药材消耗盘点

**时间维度**：每个端点接受可选 `startDate` / `endDate`（ISO 日期，默认当日），向后兼容现有调用，支持日 / 周 / 月 / 任意区间查询。复用同一聚合逻辑，不新增端点、不新增主题。

**医生工作量**：由 US-REPORT-002 的 `byDoctor`（医生姓名 + 问诊数）覆盖，不单列「医生工作量报表」。

**v1.0 克制范围（不做）**：趋势分析、可视化仪表盘、库存周转、跨期对比、导出报表文件。这些属后续版本演进项，v1.0 维持 3 端点 + 时间范围参数的最简形态。

> **决策来源**：现有 3 个当日端点（`daily/income`、`daily/consultations`、`daily/herbs`）已覆盖收入 / 就诊 / 药材三大主题，唯一缺口是时间维度（仅当日，无月度对账）。方案为 3 端点各加可选 `startDate` / `endDate`，不加新端点、不加新主题。

## 用户故事

### US-REPORT-001: 查询收入报表（按时间范围，默认当日）

**角色**: 管理员 / 医生
**优先级**: Must
**状态**: 🚧 v1.0 待实现（时间范围参数 startDate/endDate 代码未实现）

**作为** 管理员，**我想要** 按时间范围查询收入汇总（挂号费 / 药费 / 总计），**以便** 进行日 / 月经营对账。

**验收标准**:
- [ ] 支持可选查询参数 `startDate`、`endDate`（ISO 日期，如 `2026-06-01`），缺省时默认当日
- [ ] 仅 `startDate` 传入时，`endDate` 默认等于 `startDate`（单日查询）
- [ ] 返回 `totalIncome`、`registrationFeeTotal`、`medicineFeeTotal` 三个汇总值
- [ ] `totalIncome = registrationFeeTotal + medicineFeeTotal`
- [ ] `startDate > endDate` 时返回 400 参数错误
- [ ] 权限策略 `DoctorOrAdmin`（Doctor / Admin / SuperAdmin 可查）
- [ ] 不传任何参数时行为与历史完全一致（向后兼容）

**业务规则**:
1. 收入来源：挂号费取自 `Registration`，药费取自 `Prescription` 聚合。
2. 区间为闭区间 `[startDate, endDate]`，按业务日期（非时间戳）对齐。
3. 时间范围参数为待代码扩展项，当前实现固定为当日；API 契约已定义为 v1.1 目标。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | GET `/reports/daily/income?startDate=&endDate=` |
| 本地 | 完全一致（LocalWebAPI 同端点） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs:30`（`GetDailyIncome`）

---

### US-REPORT-002: 查询就诊统计报表（按时间范围）

**角色**: 管理员 / 医生
**优先级**: Must
**状态**: 🚧 v1.0 待实现（时间范围参数 startDate/endDate 代码未实现）

**作为** 管理员，**我想要** 按时间范围查询就诊统计（总问诊数 + 各医生问诊数），**以便** 掌握诊所流量与医生工作量。

**验收标准**:
- [ ] 支持可选查询参数 `startDate`、`endDate`（ISO 日期，缺省默认当日）
- [ ] 返回 `totalCount`（区间内问诊总数）
- [ ] 返回 `byDoctor` 数组：每项含 `doctorName`（医生姓名）+ `count`（问诊数）
- [ ] `byDoctor` 按 `count` 降序排列
- [ ] 权限策略 `DoctorOrAdmin`
- [ ] 不传参数时行为与历史完全一致（向后兼容）

**业务规则**:
1. 问诊数来源：`Registration` / `MedicalCase` 就诊记录聚合。
2. **医生工作量覆盖**：本 US 的 `byDoctor` 即满足医生工作量统计需求，不单列独立报表。
3. 时间范围参数为待代码扩展项，当前实现固定为当日。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | GET `/reports/daily/consultations?startDate=&endDate=` |
| 本地 | 完全一致（LocalWebAPI 同端点） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs:38`（`GetDailyConsultations`）

---

### US-REPORT-003: 查询药材使用排行（按时间范围）

**角色**: 管理员 / 医生
**优先级**: Should
**状态**: 🚧 v1.0 待实现（时间范围参数 startDate/endDate 代码未实现）

**作为** 管理员，**我想要** 按时间范围查询药材使用排行，**以便** 盘点高频药材与消耗量，指导采购与库存。

**验收标准**:
- [ ] 支持可选查询参数 `startDate`、`endDate`（ISO 日期，缺省默认当日）
- [ ] 返回 `items` 数组：每项含 `herbName`（药材名称）、`usageCount`（使用次数）、`totalDosage`（总用量）
- [ ] `items` 按 `usageCount` 降序排列
- [ ] 权限策略 `DoctorOrAdmin`
- [ ] 不传参数时行为与历史完全一致（向后兼容）

**业务规则**:
1. 使用次数 = 出现在区间内处方中的次数；总用量 = 该药材在区间内所有处方剂量之和。
2. 数据来源：`PrescriptionItem` 关联 `Herb`。
3. 时间范围参数为待代码扩展项，当前实现固定为当日。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | GET `/reports/daily/herbs?startDate=&endDate=` |
| 本地 | 完全一致（LocalWebAPI 同端点） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs:46`（`GetDailyHerbs`）

---

## 边界条件验收标准

### 时间范围参数

- [ ] `startDate` / `endDate` 均为 ISO 日期格式（`YYYY-MM-DD`），非日期格式返回 400
- [ ] `startDate > endDate` 返回 400 参数错误
- [ ] 仅传 `startDate`：`endDate` 默认等于 `startDate`（单日）
- [ ] 仅传 `endDate`：`startDate` 默认等于 `endDate`（单日）
- [ ] 均不传：默认当日（向后兼容）
- [ ] 区间跨月 / 跨年正常返回聚合结果

### 空数据

- [ ] 区间内无任何就诊 / 处方记录 → 收入返回全 0，就诊返回 `totalCount=0` + 空 `byDoctor`，药材返回空 `items`

## 依赖

| 依赖 | 说明 |
|------|------|
| [07-medical-cases.md](07-medical-cases.md) | `MedicalCase` / `Prescription` / `PrescriptionItem` 数据源 |
| [08-registration.md](08-registration.md) | `Registration` 挂号费、就诊记录数据源 |
| [05-herbs.md](05-herbs.md) | `Herb` 药材名称数据源 |

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | 新建报表模块需求文档，落地 US-REPORT-001~003（A7 报表清单：3 端点 + 时间范围参数） | A7 报表清单设计落地 |
| 2026-06-28 | US-REPORT-001/002/003 状态从 ✅已实现 降级为 🚧 v1.0 待实现 | 审计 S1：ReportsController 当前无 startDate/endDate 参数，US AC 要求时间范围未达 |
