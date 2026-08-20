# Formulas 模块设计

> 复杂度: 3/5 | 状态: 草稿

## 概述

Formulas 管理验方（经典方/经验方），支持延迟绑定（导入时药材名未匹配药典→手动绑定）。

**职责**: 验方 CRUD/搜索/批量操作、延迟绑定工作流、Excel 导入导出、与 MedicalCase 集成。

**依赖**: 上游 Herbs（药材匹配），下游 MedicalCase（导入验方到处方）。

## API 端点

| HTTP | 路由 | 权限 |
|------|------|------|
| GET | `/api/v1/formulas` | Doctor/Admin |
| POST | `/api/v1/formulas` | Admin/Doctor（Doctor 仅自己） |
| PUT | `/api/v1/formulas/{id}` | Admin/Doctor |
| POST | `/api/v1/formulas/batch-import` | Admin+ |
| GET | `/api/v1/formulas/pending-validation` | Doctor/Admin |
| POST | `/api/v1/formulas/{id}/herbs/{id}/validate` | Doctor/Admin |

## 状态机（延迟绑定）

```
Draft (待验证) ←── 编辑后有未绑定药材
     │ 所有药材绑定完成
     ▼
Validated (已验证)
```

**流程**: 导入→TryMatchHerbAsync→匹配成功设 HerbId/失败设 null→手动绑定 `/validate`→全部绑定→升级 Validated

## 业务规则

| 规则 | 描述 |
|------|------|
| 延迟绑定 | 导入时药材名未匹配→Draft→手动绑定→Validated |
| 编辑重评估 | Validated 编辑后有未绑定药材→回退 Draft |
| 所有权 | 非共享验方仅所有者可编辑/删除 |
| 药材非空 | 创建/更新时 Herbs 列表必须非空 |

## 已知问题

- `RestoreAsync` 是空操作（返回 null）
- 批量删除有所有权检查，单删无
