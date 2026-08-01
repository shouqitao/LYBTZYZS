---
feature: offline-sync-evaluation
status: complete
date: 2026-07-31
scope: rebase/offline-sync 分支架构评估
---

# Offline-Sync 分支架构评估

> 评估时间: 2026-07-31 | 分支: `rebase/offline-sync` (9 commits vs master)

## 结论

**放弃该分支，标记为 v2.0 规划。** 分支存在编译阻塞、与 master 冲突、架构缺陷三重问题，修复成本高于重写。

---

## 1. 编译阻塞（无法构建）

分支引用了多个不存在的类型：

| 缺失类型 | 引用方 | 预期位置 |
|---------|--------|---------|
| `ISyncService` (Server) | `SyncService.cs` | `LYBT.Module.Sync.Interfaces` |
| `ISyncService` (Desktop) | `SyncService.cs` | `LYBT.Desktop.Contracts.Services` |
| `ISyncApi` (Refit) | Desktop `SyncService.cs` | `LYBT.Desktop.Contracts.Api` |
| `ChecksumHelper` (Server) | Server `SyncService.cs` | `LYBT.Module.Sync.Services` |
| `ChecksumHelper` (Desktop) | Desktop `SyncService.cs` | `LYBT.Desktop.LocalData.Helpers` |
| 所有 `Sync*Dto` | 两侧 | `LYBT.Shared.Models.Contracts.Sync`（命名空间不存在） |
| `SyncController` | 无（Server 端 API 端点缺失） | `LYBT.WebAPI.Controllers` |

**10+ 个文件缺失**，分支无法通过 `dotnet build`。

---

## 2. 与 Master 冲突

分支 diff (`master..rebase/offline-sync`) 显示删除了以下 master 新增文件：

| 被删除文件 | 来源 | 影响 |
|-----------|------|------|
| `BaseCrudController.cs` | T8 (`a23fd0ead`) | 6 个 Controller 的共享基类 |
| `BaseMedicalCasesController.cs` | T8 (`c144489db`) | MedicalCase 专用基类 |
| `BaseRegistrationsController.cs` | T8 (`3775a7ffd`) | Registration 专用基类 |
| `BatchEnableDisableHerbsCommandHandlers.cs` | T3 (`f299989d9`) | 草药批量启禁 |
| `BatchEnableDisableFormulasCommandHandlers.cs` | T3 (`f299989d9`) | 方剂批量启禁 |

分支基于旧版 master rebase，冲突解决时丢失了这些文件。

---

## 3. 架构问题

### 3.1 ISyncRepository 接口过宽（32 方法）

接口同时承担元数据查询、上传（查找+更新+新增）、下载、软删除、持久化五种职责。应拆分为：
- `ISyncMetadataReader` — 元数据查询
- `ISyncUploadWriter` — 上传写入
- `ISyncDownloadReader` — 下载读取
- `ISyncDeleter` — 软删除

### 3.2 MedicalCase 同步逻辑重复

`SyncRepository.UpdateMedicalCaseValues()` 和 Desktop `SyncService.SaveMedicalCasesAsync()` 包含相同的 ~30 行聚合根处理分支逻辑。应抽取为共享工具方法。

### 3.3 CompareAsync 是死代码

Server 端 `SyncService.CompareAsync()` 已实现，但文档明确说"客户端未使用"。客户端在 `CheckDifferencesAsync()` 中自行做对比。浪费代码。

### 3.4 SyncRepository 继承语义错误

`SyncRepository` 继承 `BaseRepository<Herb>`，但操作 Patient/Formula/MedicalCase 等多种实体。虽然功能正确（BaseRepository 只是持有 DbContext），但类型参数误导。

### 3.5 端口配置不一致

`OfflineModeOptions` 默认端口 `5100`，但系统文档和实际配置均为 `5300`。

---

## 4. 分支新增内容汇总

### Server 端（LYBT.Module.Sync）

| 文件 | 行数 | 功能 |
|------|------|------|
| `ISyncRepository.cs` | ~60 | 同步数据访问接口 |
| `SyncRepository.cs` | ~181 | EF Core 实现，4 实体 CRUD + 软删除 |
| `SyncService.cs` | ~649 | 同步编排：元数据/比较/上传/下载/删除 |

### Desktop 端

| 文件 | 行数 | 功能 |
|------|------|------|
| `SyncService.cs` | ~651 | 客户端同步：差异检测/上传/下载/缓存失效 |
| `ILocal*Api.cs` (7 个) | 各 30-66 | LocalWebAPI Refit 接口 |
| `BoolToOfflineColorConverter.cs` | ~27 | UI 离线状态颜色转换 |

### 配置

| 文件 | 功能 |
|------|------|
| `OfflineModeOptions.cs` | 离线模式配置（启用/自动切换/健康检查间隔/熔断阈值） |

---

## 5. 同步协议设计（可复用）

尽管分支代码不可用，以下设计决策可作为 v2.0 重写的参考：

- **支持实体类型**: Herb → Patient → Formula → MedicalCase（按依赖顺序）
- **差异检测**: 客户端本地对比（非服务端），基于 Checksum 校验和
- **冲突策略**: `OverwriteConflicts` 标志控制，默认不覆盖
- **同步流程**: 元数据对比 → 用户选择冲突 → 上传本地新增 → 下载服务端新增 → 持久化 → 缓存失效
- **软删除**: 使用 `IgnoreQueryFilters()` 确保已删除记录参与校验

---

## 6. 建议的 v2.0 重写方向

1. **重新设计 SyncRepository 接口** — 拆分为 4 个窄接口
2. **抽取 MedicalCase 同步逻辑** — 共享工具类消除重复
3. **删除 CompareAsync** — 客户端自行对比，服务端不需要
4. **添加 SyncController** — 基于 BaseApiController，非 BaseCrudController
5. **统一 ChecksumHelper** — 放在 Shared 项目，两侧共用
6. **端口对齐** — 使用 5300 而非 5100
7. **测试策略** — 先写集成测试（SyncService + 真实 DB），再实现

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-07-31 | 初始评估 |
