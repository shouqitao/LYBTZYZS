# Spec: patient-search-performance

## Purpose

定义患者搜索的性能优化策略，包括客户端缓存、防抖优化和服务端查询优化。

## Context

当前患者搜索存在以下性能问题：
- 防抖时间300ms过短，导致频繁API请求
- 缺乏客户端缓存，重复搜索相同关键字会重新请求
- 服务端返回完整DTO，数据传输量大

---

## ADDED Requirements

### Requirement: PERF-PS-001 客户端搜索缓存

系统 SHALL 实现 `IPatientSearchCache` 服务，使用LRU策略缓存最近搜索结果。缓存规格：最大10条、Key格式 `{keyword}:{page}`、过期时间5分钟。

#### Scenario: 缓存命中
- **GIVEN** 用户搜索关键字
- **AND** 该搜索结果已在缓存中
- **AND** 缓存未过期
- **WHEN** 用户再次搜索相同关键字
- **THEN** 直接返回缓存结果
- **AND** 不发起API请求
- **AND** 响应时间小于50ms

#### Scenario: 缓存失效
- **GIVEN** 缓存中存在搜索结果
- **WHEN** 创建或更新或删除患者
- **THEN** 清空所有缓存条目
- **AND** 下次搜索发起新的API请求

---

### Requirement: PERF-PS-002 优化防抖时间

搜索防抖时间 SHALL 从300ms调整为500ms。

#### Scenario: 防抖生效
- **GIVEN** 用户开始输入搜索关键字
- **WHEN** 用户连续输入
- **THEN** 每次击键重置防抖计时器
- **AND** 最后一次击键后500ms执行搜索
- **AND** 中间不发起任何API请求

---

### Requirement: PERF-PS-003 轻量级搜索DTO

Server端 SHALL 添加专用搜索端点 `GET /api/v1/patients/search`，返回精简DTO。

#### Scenario: 使用轻量级端点
- **GIVEN** 患者搜索请求
- **WHEN** 调用搜索端点
- **THEN** 返回 `PatientSearchResultDto` 列表
- **AND** 响应数据大小减少30%以上
- **AND** 原有端点保持兼容

---

## MODIFIED Requirements

### Requirement: PERF-PS-M01 搜索请求取消

搜索逻辑 SHALL 支持取消正在进行的请求。

#### Scenario: 取消旧请求
- **GIVEN** 搜索某关键字的请求正在进行中
- **WHEN** 用户输入新关键字触发新搜索
- **THEN** 取消旧请求
- **AND** 发起新搜索
- **AND** 只显示新搜索的结果

---

## Performance Metrics

| 指标 | 当前值 | 目标值 |
|------|--------|--------|
| 搜索响应时间（首次） | ~300ms | < 300ms |
| 搜索响应时间（缓存命中） | N/A | < 50ms |
| 搜索响应数据大小 | ~2KB/条 | < 1.4KB/条 |

## Dependencies

- `LYBT.Module.Patients` - Server端患者搜索端点
- `LYBT.Desktop.Presentation` - 客户端缓存服务
- `LYBT.Shared.Models` - PatientSearchResultDto

## Migration Notes

1. 先实现缓存服务（不影响现有功能）
2. 添加Server端搜索端点（保持原端点兼容）
3. 调整防抖时间
4. 集成缓存到ViewModel
