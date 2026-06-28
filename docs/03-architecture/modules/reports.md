# Reports 模块设计

> v1.0 | 2026-06-28

## 模块概述

Reports 提供经营数据统计查询：收入汇总、问诊统计、药材使用排行。支持按时间范围查询。

**职责边界**：只读统计查询（无写操作）

## 接口契约

| 端点 | Service | 权限 | 关键功能 |
|------|---------|------|----------|
| GET /reports/daily/income | GetDailyIncomeAsync | DoctorOrAdmin | 当日收入汇总 |
| GET /reports/daily/consultations | GetDailyConsultationsAsync | DoctorOrAdmin | 问诊统计(按医生) |
| GET /reports/daily/herbs | GetDailyHerbsAsync | DoctorOrAdmin | 药材使用排行 |

## 关键业务规则

| 规则 | 约束 | US |
|------|------|-----|
| 时间范围 | 支持startDate/endDate参数(默认当日) | US-REPORT-001/002/003 |
| 收入计算 | 挂号费+药费汇总 | US-REPORT-001 |
| 医生工作量 | 按医生分组统计 | US-REPORT-002 |
| 药材排行 | 按使用次数降序 | US-REPORT-003 |
