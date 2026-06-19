# WPF 性能优化 — 设计规格

> 日期: 2026-06-18
> 子项目: D (性能优化)

## [S1] 问题

代码层面识别出 4 个性能瓶颈：
1. 启动管道非依赖步骤串行执行
2. 登录后全量加载角色模块，阻塞首屏
3. 患者/医案数据无缓存，每次导航重新请求
4. 处方药材 ItemsControl 无虚拟化

## [S2] 目标

- 启动时间减少 30%+（并行化非依赖步骤）
- 首屏显示后延迟加载非必要模块
- 常用数据缓存（患者列表、今日医案）
- 大列表 UI 虚拟化

## [S3] 启动管道并行化

### 当前串行流程
```
ErrorHandling(100) → ModuleCoordinator(200) → CoreServices(300) → ApiHealthCheck(400) → Warmup(500)
```

### 优化后
```
ErrorHandling(100) → [ModuleCoordinator(200) + CoreServices(300)] 并行 → ApiHealthCheck(400) → Warmup(500)
```

策略：在 StartupPipeline 中增加 `ParallelGroup` 属性。相同 Group 的步骤并行执行，不同 Group 顺序执行。

### 改动范围
- `IStartupStep` — 新增 `string? ParallelGroup { get; }` 可选属性（默认 null = 串行）
- `StartupPipeline.ExecuteAsync` — 相同 ParallelGroup 的步骤用 `Task.WhenAll` 并行
- `CoreServicesStartupStep` — 设置 `ParallelGroup = "PostError"`

## [S4] 模块延迟加载

### 当前行为
`LoginCoordinator.LoadModulesForUserAsync` 登录后一次性加载角色所有模块。

### 优化策略
- **必须模块**（登录后立即需要）：Auth + Clinical/Admin/Receptionist 角色 → 同步加载
- **延迟模块**（首次导航时加载）：Patients, Herbs, Formula, Users, MedicalCase, Registration, Reports → 标记 `InitializationMode.OnDemand`

### 改动范围
- `ModuleLoadingService` — 区分必须 vs 延迟模块
- 各 `RoleDefinition` — 新增 `RequiredModules` 和 `LazyModules` 属性
- Prism `ModuleCatalog` — 延迟模块设为 `OnDemand`

## [S5] 数据缓存

### 患者-医案缓存

在 `ClinicalWorkspaceViewModel` 中：
- 首次加载患者列表后缓存到内存
- 选中患者时，缓存其历史医案（5分钟过期）
- 导航返回时不重新请求，直接用缓存

### 改动范围
- `ClinicalWorkspaceViewModel` — 增加 `_patientListCache` 和 `_historyCache` 字段
- 使用简单的 `Dictionary<Guid, (List, DateTime)>` 带过期时间戳

## [S6] UI 虚拟化

### 处方药材列表虚拟化

`HerbListControl.xaml` 使用 `ItemsControl` 无虚拟化。改为：
```xml
<ItemsControl>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

或者更好的方案——改用 `ListBox`（内置虚拟化）。

### 改动范围
- `HerbListControl.xaml` — 增加 VirtualizingStackPanel

## [S7] 不在范围内

- 数据库查询优化（Server 端，不在 WPF 范围）
- 网络层优化（HTTP 连接池已有 Refit 管理）
- 打印性能（打印是低频操作）
