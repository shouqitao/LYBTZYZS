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

## 查询参数

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|:---:|--------|------|
| startDate | DateTime | 否 | 当日 | 查询起始日期(含) |
| endDate | DateTime | 否 | 当日 | 查询结束日期(含) |

参数校验：startDate ≤ endDate；日期范围不超过 1 年（防滥用）；格式 ISO 8601。

## 响应约定

| 场景 | 响应 |
|------|------|
| 正常查询 | 200 + 数据列表 |
| 空数据集 | 200 + 空列表 `{"items": []}`（非 404） |
| 参数无效 | 400 + ValidationException |
| 权限不足 | 403 + ForbiddenException |

## 缓存策略

只读查询启用 `MemoryCache`，缓存粒度按「日期范围+报表类型」组合键，TTL 5 分钟。写操作（如有）后主动清除。

## 业务规则

| 规则 | 约束 | US | 状态 |
|------|------|-----|:---:|
| 时间范围 | 支持 startDate/endDate 参数(默认当日) | US-REPORT-001/002/003 | ✅ |
| 收入计算 | 挂号费+药费汇总 | US-REPORT-001 | ✅ |
| 医生工作量 | 按医生分组统计 | US-REPORT-002 | ✅ |
| 药材排行 | 按使用次数降序 | US-REPORT-003 | ✅ |
| 最大日期范围 | ≤365天，防滥用 | NFR | ✅ |
