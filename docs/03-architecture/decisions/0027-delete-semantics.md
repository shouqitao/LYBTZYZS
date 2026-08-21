# ADR-0027: 删除语义与批量归一

**日期**: 2026-08-21  
**状态**: Accepted  
**相关**: R56-05/R56-06/R58-06, Sprint2 T2.2

## 背景

- `IRepository.DeleteAsync` 承载 3 种语义：软删（`IsDeleted=true`）、物理删（`HardDelete`）、校验（`ValidateBeforeDelete`），同一动词在 20 处混用，`RemoveAsync` 在 `IConfigurationStore` 为物理删加剧混淆。
- 批量启用/禁用/删除/恢复在 5 模块各一套：`ToggleStatusAsync` vs `BatchEnableAsync/BatchDisableAsync/RestoreAsync`，`CommonStatus` 与 `UserRole` 三态混用，`MasterDetailCommandGroup` 同时暴露 `BatchEnable/BatchDisable/Restore`。
- 仅真 Breaking：`Delete` 重命名涉及 API 路由 `/delete` 与 DB 脚本，需兼容。

## 决策

1. **显式四态命名**（`IRepository<T>`）：
   - `SoftDeleteAsync(id)` — 软删（置 `IsDeleted=true`，默认）
   - `RestoreAsync(id)` — 恢复（`IsDeleted=false`）
   - `HardDeleteAsync(entity)` — 物理删（仅 `MedicalCase` 取消=物理删等显式场景，`Registration` 通过 `MedicalCase` 联动）
   - `BatchSoftDeleteAsync(ids)` — 批量软删（与 `BatchOperationHandlerBase` 统一走 `BatchOptions.DefaultMaxBatchSize`）
   - 旧 `DeleteAsync(id)` 标记 `[Obsolete("Use SoftDeleteAsync")]` 保留 1 版本，双存期 `SoftDeleteAsync` 默认委托旧实现，待 v2 移除。

2. **批量状态归一**：
   - 统一为 `SetStatusAsync(id, CommonStatus)` + `BatchSetStatusAsync(ids, status)`，`ToggleStatusAsync` 保留为 UI 快捷入口（内部取反后调 `SetStatus`），`BatchEnable/BatchDisable` 标记 `[Obsolete]` 转发至 `BatchSetStatus`。

3. **物理删显式**：
   - 全库 `grep RemoveAsync` 仅保留 `IConfigurationStore.RemoveAsync`（配置键物理删语义正确）与 `DbSet.Remove`（EF 物理删），其余 `RemoveAsync` 改名 `HardDeleteAsync`。

4. **兼容三阶段**：
   - ① 新增四态方法 + Obsolete 旧名双存，旧路由 `/delete` 保留并新增 `/soft-delete`（308 转发），② 客户端双适配 1 版本，③ 下版本移除旧名与旧路由。

## 后果

- **正向**：删除语义显式化，`grep Delete` 可按四态精确区分；批量状态 2 方法替代 3，调用方仅需 `SetStatus`；`IRepository` 约束收紧为 `where T:BaseEntity`（见 R54-02）后 `SoftDelete` 可依赖 `IsDeleted`。
- **负向**：1 版本双存期增加 1 接口方法与 1 标记；全库改名约 20 行 + 5 控制器 `BatchEnable/Disable` 转发；`MedicalCase.HardDelete` 与 `Registration.CancelFromMedicalCase` 物理删语义需文档化于 `04-data-model`。
- **迁移**：`AddRegistrationSingleWaitingUniqueIndex` 等迁移中已用 `SoftDelete` 语义，无需数据迁移；`MasterDetailCommandGroup` 的 `BatchEnable/Disable` 转发需同步 `CanExecute` 语义。
