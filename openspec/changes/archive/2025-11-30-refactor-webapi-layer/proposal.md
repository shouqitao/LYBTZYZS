# OpenSpec Proposal: refactor-webapi-layer

## 元数据

- **提案ID**: refactor-webapi-layer
- **创建日期**: 2025-11-30
- **状态**: Completed
- **Epic**: N/A (独立重构)

## Why

当前WebAPI层存在未使用的API端点(Dead Endpoints)、批量操作模式不统一、以及Controller职责不清晰等问题，需要系统性清理和规范化。

## What Changes

1. **Phase 1 - Dead Endpoints清理**: 标记6个未使用端点为`[Obsolete]`
2. **Phase 2 - API规范化评估**: 确认批量操作使用Client循环模式，评估Controller拆分需求
3. **Phase 3 - Health端点评估**: 确认现有Health Controller分离设计合理

## 问题陈述

当前WebAPI层存在以下问题：

### 1. 未被调用的API端点 (Dead Endpoints)

通过系统分析Server端Controllers与Client端API调用的对比，发现以下端点从未被Client端调用：

#### UsersController
| 端点 | HTTP方法 | Client调用 | 分析 |
|------|---------|-----------|------|
| `/api/v1/users/batch-delete` | DELETE | 无 | Client端循环调用单个DeleteUser |
| `/api/v1/users/{id}/toggle-status` | PUT | 无 | Client无此功能UI |
| `/api/v1/users/me` (GetCurrentUser) | GET | 无 | Client使用Session缓存 |

#### HerbsController
| 端点 | HTTP方法 | Client调用 | 分析 |
|------|---------|-----------|------|
| `/api/v1/herbs/batch-delete` | DELETE | 无 | Client端循环调用单个Delete |
| `/api/v1/herbs/{id}/check-reference` | GET | 无 | 引用检查未实现UI |
| `/api/v1/herbs/batch-check-reference` | POST | 无 | 批量引用检查未实现UI |
| `/api/v1/herbs/all-for-export` | GET | 无 | Client使用分页Export |

#### FormulasController
| 端点 | HTTP方法 | Client调用 | 分析 |
|------|---------|-----------|------|
| `/api/v1/formulas/batch-delete` | DELETE | 无 | Client端循环调用单个Delete |

#### PrescriptionsController
| 端点 | HTTP方法 | Client调用 | 分析 |
|------|---------|-----------|------|
| `/api/v1/prescriptions/search` | GET | 无 | 与列表查询重复 |

#### MedicalCaseController
| 端点 | HTTP方法 | Client调用 | 分析 |
|------|---------|-----------|------|
| `/api/v1/medicalcases/{id}/complete` | PUT | 无 | 使用UpdateStatus替代 |

#### CacheHealthController (全部)
| 端点 | HTTP方法 | Client调用 | 分析 |
|------|---------|-----------|------|
| `/api/v1/cache/statistics` | GET | 无 | 运维功能，无UI |
| `/api/v1/cache/clear` | DELETE | 无 | 运维功能，无UI |
| `/api/v1/cache/clear-pattern` | DELETE | 无 | 运维功能，无UI |

### 2. API设计一致性问题

- **批量操作模式不统一**：Server端有batch-delete端点，但Client端使用循环单删
- **状态更新端点冗余**：`toggle-status`/`update-status`/`close`/`complete`等多种状态变更端点
- **Controller职责不清晰**：MedicalCaseController包含Prescription/Consultation子资源操作(1192行)

### 3. 健康检查端点分散

存在3个Health相关Controller:
- `RootHealthController` - 根路径健康检查
- `HealthController` - API版本健康检查
- `CacheHealthController` - 缓存健康检查

## 建议的解决方案

### Phase 1: 清理Dead Endpoints (低风险)

1. **标记废弃端点**：为确认不需要的端点添加`[Obsolete]`特性
2. **删除无用端点**：移除从未使用且无未来计划的端点
3. **统一批量操作**：决定使用Server端batch或Client端循环模式

### Phase 2: API规范化 (中等风险)

1. **统一状态变更API**：合并为单一`UpdateStatus`端点
2. **拆分MedicalCaseController**：考虑分离Prescription/Consultation操作
3. **合并Health端点**：统一健康检查入口

### Phase 3: 新增运维API (可选)

评估是否需要在Admin模块添加:
- 缓存管理UI
- 用户状态切换UI
- 引用检查功能

## 受影响的组件

### Server端
- `LYBT.WebAPI/Controllers/*.cs` - 所有Controller
- 相关Service层 (如需删除端点)

### Client端
- 无直接影响 (删除的端点本就未调用)
- 可选：添加新功能UI

## 成功标准

1. 所有API端点都有对应的Client调用或明确的运维用途
2. 批量操作模式统一
3. Controller职责清晰，符合单一职责原则
4. API文档与实际实现一致

## 风险评估

- **Phase 1风险**: 低 - 删除的端点从未被调用
- **Phase 2风险**: 中 - 可能需要Client端配合修改
- **Phase 3风险**: 低 - 纯新增功能

## 时间线

不提供具体时间估算，由团队根据实际情况安排。

## 开放问题

1. CacheHealthController是否保留用于未来运维需求？
2. 批量删除是否统一使用Server端batch-delete提高性能？
3. MedicalCaseController是否需要拆分？目前1192行代码量较大。

---

**提案状态**: 已完成 (2025-11-30)
