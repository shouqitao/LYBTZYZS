# 模块对接分析报告：药材管理 (Herbs)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 11 |
| Desktop 已对接点数 | 7 |
| **对接完成情况** | **64%** |

---

### WebApi 总点数列表 (11 个)

- `GetHerbs` (GET /api/v1/herbs - 分页)
- `GetHerbById` (GET /api/v1/herbs/{id})
- `CreateHerb` (POST /api/v1/herbs)
- `UpdateHerb` (PUT /api/v1/herbs/{id})
- `UpdateStatus` (PATCH /api/v1/herbs/status)
- `ToggleStatus` (PATCH /api/v1/herbs/{id}/toggle-status)
- `GetAvailableHerbs` (GET /api/v1/herbs/available)
- `GetStatistics` (GET /api/v1/herbs/statistics)
- `ImportHerbs` (POST /api/v1/herbs/import)
- `ExportHerbs` (GET /api/v1/herbs/export)
- `GetImportTemplate` (GET /api/v1/herbs/import-template)

---

### Desktop 已对接点数列表 (7 个)

- `GetHerbByIdAsync`
- `CreateHerbAsync`
- `UpdateHerbAsync`
- `UpdateStatusAsync`
- `ToggleStatusAsync`
- `ImportHerbsAsync`
- `ExportHerbsAsync`

---

### 未对接点分析

- **`GetHerbs`**: 分页获取药材列表。前端的药材列表展示功能尚未对接。
- **`GetAvailableHerbs`**: 获取所有可用的药材。
- **`GetStatistics`**: 获取药材相关的统计数据。
- **`GetImportTemplate`**: 下载药材导入的Excel模板文件。

---

### 状态总结

**核心管理功能已对接。**

前端实现了药材的增、删、改和手动的导入导出。但药材列表的展示、筛选，以及统计和模板下载等辅助功能尚未与后端真实数据打通。
