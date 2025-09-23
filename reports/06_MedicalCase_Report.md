# 模块对接分析报告：医案管理 (MedicalCase)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 16 |
| Desktop 已对接点数 | 8 |
| **对接完成情况** | **50%** |

---

### WebApi 总点数列表 (16 个)

- `GetPaged` (分页列表)
- `GetById` (获取详情)
- `Create` (创建)
- `Update` (更新)
- `GetByPatientId` (按患者查)
- `GetTodayByUserId` (查今日)
- `UpdateStatus` (更新状态)
- `Delete` (删除)
- `GetActiveByPatientId` (查患者当前医案)
- `Complete` (完成医案)
- `Suspend` (挂起医案)
- `Resume` (恢复医案)
- `Archive` (归档医案)
- `Search` (搜索)
- `GetStatistics` (统计)
- `GetHistory` (历史记录)

---

### Desktop 已对接点数列表 (8 个)

- `GetByIdAsync`
- `CreateAsync`
- `UpdateAsync`
- `UpdateStatusAsync`
- `SuspendAsync`
- `ResumeAsync`
- `ArchiveAsync`
- `SearchAsync`

---

### 未对接点分析

- **查询类**: `GetPaged`, `GetByPatientId`, `GetTodayByUserId`, `GetActiveByPatientId`
- **生命周期类**: `Delete`, `Complete`
- **统计历史类**: `GetStatistics`, `GetHistory`

---

### 状态总结

**对接完成一半，核心流程部分打通。**

前端实现了医案的创建、更新、搜索和部分状态管理（挂起、恢复、归档）。但同样缺少列表展示和多维度查询功能。此外，`删除` 和 `完成医案` 这两个关键的生命周期操作也尚未对接。
