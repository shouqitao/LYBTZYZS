# 备份/恢复 + FeatureToggle 设计方案（T6 需求深化，暂不实现）

> 日期：2026-08-11 | 状态：设计稿（待用户审批后进入实现）
> 依据：需求先行门禁——backlog 剩余 #5（US-SHELL-013/NFR-AVAIL-001）+ #6（US-CFG-004）为全新功能，先深化需求再设计
> 关联：R1 矩阵 §一 P0 #5/#6（🔴 全无）；13-traceability-matrix v1.1 对应行 🔴

---

## 〇、总览

| 功能 | 需求源 | 现状 | 本设计 |
|---|---|---|---|
| 备份/恢复 | US-SHELL-013 + NFR-AVAIL-001 | 🔴 全无（SystemSettingsViewModel 仅 BackupPath/AutoBackupEnabled 设置项占位） | `ILocalDbBackupService` 服务 + sysadmin 备份管理 UI + 登录后自动备份 + 恢复工作流 |
| FeatureToggle | US-CFG-004 | 🔴 全库消失（ClinicSettingsService 热更新为唯一先例） | `feature-toggles.json` + `FeatureToggleOptions`（IOptions reloadOnChange 热更新）+ VM 可见性消费 + 行为策略 |

**共同点**：本地 JSON/文件驱动 + Desktop 层实现（不涉及远程 Server 数据库）；沿用 ClinicSettingsService 热更新先例（`clinic-settings.json` + `IOptionsMonitor`）。

---

## 一、备份/恢复（US-SHELL-013 + NFR-AVAIL-001）

### 1.1 需求深化（从需求文本到可验收业务规则）

| # | 规则 | 来源 | 设计决策 |
|---|---|---|---|
| B1 | 本地模式**登录成功后自动备份**（fire-and-forget，不阻塞） | NFR-AVAIL-001 | 登录成功事件 → `LocalDbBackupService.BackupAsync()` 后台触发 |
| B2 | 备份文件保留 **7 天/最多 7 个**（需求）vs 代码常量 `BackupRetentionDays=30`（SystemConstants.cs:75 已有） | US-SHELL-013 + NFR | **以 NFR 为准**：7 天；SystemConstants 常量改为 7（现有 30 是遗留） |
| B3 | 备份目录 `%AppData%/LYBTZYZS/Backup/`，文件命名 `LYBTDB_{yyyyMMddHHmmss}.bak` | NFR-AVAIL-001 | 目录常量已存在（SystemConstants.BackupDirectory="Backup"） |
| B4 | 备份状态展示：上次备份时间、文件数量、总大小 | US-SHELL-013 | 管理 UI 只读卡片 |
| B5 | 手动备份按钮 + 进度指示 + 失败原因 | US-SHELL-013 | AsyncRelayCommand + IsBackingUp 状态 + 错误 Toast |
| B6 | 恢复前确认弹框「将覆盖当前数据库」 | US-SHELL-013 | Dialog 三选确认 |
| B7 | 恢复用 `RESTORE DATABASE`（T-SQL） | US-SHELL-013 + NFR | 需**先断开 LocalWebAPI 连接**（恢复期间 DB 独占）→ 恢复 → 提示重启应用 |
| B8 | 恢复后提示重启 | US-SHELL-013 | 完成对话框 |
| B9 | 远程模式：备份由 SQL Server Agent 承担（Desktop 不参与） | NFR-AVAIL-001 | 管理 UI 仅本地模式显示；远程模式显示说明 |

### 1.2 架构设计

**服务层（Desktop Infrastructure）**：

```
LYBT.Desktop.Infrastructure/Services/Backup/
├── ILocalDbBackupService        # 契约（NFR 已设计接口名）
│   ├── Task<BackupResult> BackupAsync(CancellationToken)        # 手动/自动备份
│   ├── Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync()   # 列表（日期/大小）
│   ├── Task<BackupStatus> GetStatusAsync()                      # 上次时间/数量/总大小
│   ├── Task<RestoreResult> RestoreAsync(Guid backupId, CancellationToken)
│   └── Task CleanupOldBackupsAsync()                            # 7 天清理
└── LocalDbBackupService          # 实现：T-SQL BACKUP DATABASE / RESTORE DATABASE
```

**实现要点**：
1. **连接串注入**：复用 `EmbeddedLocalWebApiService` 的 LocalDB 连接串（`(localdb)\MSSQLLocalDB;Database=LYBTDesktop`）——服务注入 `IOptions<LocalJwtOptions>` 或直接读取 Embedded 配置；数据库名从连接串解析（`LYBTDesktop`）
2. **备份 T-SQL**：`BACKUP DATABASE [LYBTDesktop] TO DISK = N'{path}\LYBTDB_{ts}.bak' WITH INIT, FORMAT`
3. **恢复 T-SQL**：
   ```
   ALTER DATABASE [LYBTDesktop] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   RESTORE DATABASE [LYBTDesktop] FROM DISK = N'{path}' WITH REPLACE;
   ALTER DATABASE [LYBTDesktop] SET MULTI_USER;
   ```
   **关键前置**：恢复前必须停止 LocalWebAPI（Kestrel 持有 DB 连接）——通过 `IEmbeddedLocalWebApiService.StopAsync()`；恢复后 `StartAsync()` + 提示重启
4. **登录后自动备份**：`LoginCoordinator` 登录成功事件 → `SafeFireAndForget(BackupAsync + CleanupOldBackupsAsync)`（NFR fire-and-forget）
5. **失败降级**：自动备份失败仅记日志（不阻塞登录）；手动备份失败显示原因

**UI 层（Roles/LYBT.Desktop.Admin Sysadmin）**：
- `SysadminHomeViewModel` 增加备份状态卡片（B4）——或独立 `BackupManagementView`（Sysadmin 区域导航）
- 建议：**独立 BackupManagementView**（sysadmin 运维面板之一），避免 SysadminHome 再膨胀（已 596 行）
  - 备份文件列表（DataGrid：文件名/日期/大小）
  - 手动备份按钮（IsBackingUp 进度）
  - 恢复按钮 → 确认弹框 → 停止 LocalWebAPI → 恢复 → 提示重启

**DI 注册**：SysadminModule `Register<ILocalDbBackupService, LocalDbBackupService>()`

### 1.3 数据流（恢复场景时序）

```
sysadmin 点恢复 → 确认弹框（覆盖警告）→ IsRestoring=true
→ IEmbeddedLocalWebApiService.Stop()（释放 DB 连接）
→ LocalDbBackupService.RestoreAsync(id)
  → ALTER SINGLE_USER → RESTORE WITH REPLACE → ALTER MULTI_USER
→ 结果提示「恢复完成，请重启应用」
→ IEmbeddedLocalWebApiService.Start()（可选：自动重启内嵌服务）
```

### 1.4 边界与风险

| 风险 | 缓解 |
|---|---|
| 恢复期间 LocalWebAPI 占用 DB → RESTORE 失败 | 必须先 Stop 内嵌服务（B7 前置）；Stop 失败则中止恢复 |
| 备份文件损坏 → 恢复失败 | RESTORE 前验证文件存在+大小>0；失败回滚提示 |
| 7 天 vs 30 天常量冲突 | 改 SystemConstants.BackupRetentionDays=7（对齐 NFR） |
| 手动备份与自动备份并发 | `SemaphoreSlim(1)` 串行化 |
| 远程模式误操作 | UI 仅本地模式显示备份管理；远程模式显示「远程备份由服务器维护」 |

### 1.5 验收映射（US-SHELL-013 六项 AC）

| AC | 设计落点 |
|---|---|
| 备份文件列表（7 天/日期/大小） | `ListBackupsAsync` + DataGrid |
| 备份状态（上次时间/数量/总大小） | `GetStatusAsync` + 卡片 |
| 手动备份（进度/失败原因） | `BackupAsync` + IsBackingUp + Toast |
| 选择文件恢复（RESTORE DATABASE） | `RestoreAsync`（T-SQL） |
| 恢复前确认弹框 | Dialog 覆盖警告 |
| 恢复后提示重启 | 完成对话框 |

---

## 二、FeatureToggle（US-CFG-004）

### 2.1 需求深化

| # | 规则 | 来源 | 设计决策 |
|---|---|---|---|
| F1 | `FeatureToggle=false` → 功能按钮/菜单完全隐藏（Collapsed） | US-CFG-004 | VM 消费 `IFeatureToggleService.IsEnabled(key)` → 可见性属性 |
| F2 | `OverwriteConflicts=false` → 同步冲突不自动覆盖 | US-CFG-004 | 同步流程读开关（冲突时提示而非覆盖） |
| F3 | 热更新（文件变更自动刷新，无需重启） | US-CFG-004（[x] 已验收标记） | `feature-toggles.json` + `IOptionsMonitor<FeatureToggleOptions>` reloadOnChange（ClinicSettingsService 同款） |
| F4 | v1.0 仅 `OverwriteConflicts` + `DuplicateHerbMergeStrategy`（"Max"） | US-CFG-004 业务规则 1 | Options 仅两字段；不引入废弃的 18 个布尔开关 |
| F5 | UI 可见性由各模块 VM 按角色/业务状态自管 | US-CFG-004 业务规则 2 | FeatureToggle 是**叠加层**（角色权限之上再按开关隐藏），不替代权限 |

**关键澄清**（设计决策）：需求 2.2「早期 18 个布尔开关已废弃，UI 可见性由 VM 自管」——FeatureToggle 不用于逐按钮开关（避免回到 18 开关反模式），**只服务跨模块策略**：
- `OverwriteConflicts`：同步/导入冲突处理策略
- `DuplicateHerbMergeStrategy`：处方合并重复药材策略（现状：Desktop `DuplicateDosageStrategy` 硬编码 Max/Min/Sum/Average/First——R1 发现与需求 FeatureToggle 机制不一致 → **迁移到开关驱动**）

### 2.2 架构设计

```
LYBT.Desktop.Infrastructure/Services/FeatureToggle/
├── IFeatureToggleService                    # 契约
│   ├── bool IsEnabled(string key)           # 布尔开关
│   ├── string? GetValue(string key)         # 枚举/字符串策略值
│   └── event EventHandler? TogglesChanged   # 热更新通知
├── FeatureToggleService                     # 实现：IOptionsMonitor<FeatureToggleOptions> 订阅
├── FeatureToggleOptions                     # Options 类（F4：两字段）
└── feature-toggles.json                     # 配置文件（应用根目录）
```

**feature-toggles.json 样例**：
```json
{
  "FeatureToggles": {
    "OverwriteConflicts": false,
    "DuplicateHerbMergeStrategy": "Max"
  }
}
```

**实现要点**：
1. **Options 绑定**：`FeatureToggleOptions` + `IOptionsMonitor<FeatureToggleOptions>`（reloadOnChange: true——同 ClinicSettingsService 配置注册方式）；配置文件放应用根目录（`AppContext.BaseDirectory/feature-toggles.json`）
2. **服务封装**：`FeatureToggleService` 订阅 monitor（`OnChange` → 触发 `TogglesChanged` 事件 + 缓存刷新）——VM 绑定 `IsEnabled`/`GetValue`，事件驱动可见性属性刷新（PropertyChanged）
3. **消费点**（v1.0 两个）：
   - 同步冲突（`OverwriteConflicts`）：找到实际同步流程（R1 未深挖——LocalWebAPI 配置同步/数据同步），冲突时读开关决定覆盖或提示
   - 处方合并（`DuplicateHerbMergeStrategy`）：`PrescriptionImportExtensions`/`DuplicateDosageStrategy` 改读开关（替换硬编码）
4. **DI**：Infrastructure 扩展注册 `RegisterSingleton<IFeatureToggleService, FeatureToggleService>()` + `Configure<FeatureToggleOptions>(...)`

### 2.3 边界与风险

| 风险 | 缓解 |
|---|---|
| 配置文件缺失 → 默认值 | Options 默认值（OverwriteConflicts=true 保守、DuplicateHerbMergeStrategy=Max）；缺失不抛异常 |
| 热更新竞态（读时变更） | IOptionsMonitor 原子快照；服务缓存 + OnChange 刷新 |
| 枚举值非法（如 MergeStrategy=Unknown） | Options 校验（未知值回退 Max） |
| 与 18 开关反模式混淆 | 文档明确：FeatureToggle 仅策略级（2 键），不逐按钮 |

### 2.4 验收映射（US-CFG-004）

| AC | 设计落点 |
|---|---|
| false → 功能隐藏（Collapsed） | VM `IsEnabled` → 可见性（仅对策略级功能） |
| OverwriteConflicts=false → 不自动覆盖 | 同步冲突路径读开关 |
| ConfigurationOptionsMonitor 热更新 | `IOptionsMonitor` reloadOnChange + `TogglesChanged` 事件 |

---

## 三、实施批次建议（审批后）

| 批次 | 内容 | 预估改动面 |
|---|---|---|
| T7-1 | 备份服务（ILocalDbBackupService + T-SQL + 自动备份 + 清理） | Infrastructure + Shell（登录事件）+ 单测 |
| T7-2 | 备份管理 UI（BackupManagementView + Sysadmin 注册 + 恢复工作流） | Roles/Sysadmin + XAML |
| T8-1 | FeatureToggle 基建（Options + Service + json + DI） | Infrastructure + 单测 |
| T8-2 | 两消费点接线（OverwriteConflicts 同步冲突 + DuplicateHerbMergeStrategy 处方合并） | 同步流程 + PrescriptionImport |

**依赖**：T7-1 → T7-2（UI 依赖服务）；T8-1 → T8-2。两项独立可并行。

---

## 附：需求追溯

| 功能 | US | NFR | 现状态 | 设计后目标 |
|---|---|---|---|---|
| 备份/恢复 | US-SHELL-013（Should） | NFR-AVAIL-001 | 🔴 全无 | T7 完成后 ✅ |
| FeatureToggle | US-CFG-004（Should） | — | 🔴 全库消失 | T8 完成后 ✅ |

**待用户确认项**：
1. 备份保留期：NFR 7 天（改 SystemConstants 30→7）确认
2. 恢复后是否自动重启内嵌 LocalWebAPI（设计倾向：是，免手动）
3. FeatureToggle 配置文件位置（应用根目录 vs `%AppData%`——前者随部署分发、后者运维可改，倾向**应用根目录**随包分发默认值）
4. T7/T8 实施顺序或并行
