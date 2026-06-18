# WPF 性能优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute to implement this plan task-by-task.

**Goal:** 通过启动并行化、模块延迟加载、数据缓存、UI 虚拟化提升 WPF 客户端响应速度。

**Architecture:** 渐进式优化——每个 Task 独立可交付，不影响现有功能。

**Tech Stack:** C# / .NET 8 / WPF / Prism

---

### Task 1: 启动管道并行化

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IStartupPipeline.cs` — IStartupStep 新增 ParallelGroup
- Modify: `src/Client/Desktop/Shell/Services/Startup/StartupPipeline.cs` — 并行执行逻辑
- Modify: 启动步骤类（CoreServicesStartupStep 等）

- [ ] **Step 1: IStartupStep 新增 ParallelGroup 属性**

```csharp
/// <summary>并行组名——相同组的步骤并行执行，null=串行</summary>
string? ParallelGroup => null;
```

- [ ] **Step 2: StartupPipeline.ExecuteAsync 增加并行分组逻辑**

将 `foreach` 循环改为：按 Order 排序后，检查相邻步骤是否有相同 ParallelGroup，有则 `Task.WhenAll` 并行。

- [ ] **Step 3: 给 CoreServicesStartupStep 设置 ParallelGroup**

让 CoreServices 和 ModuleCoordinator 并行（它们无依赖关系）。

- [ ] **Step 4: 构建验证 + Commit**

```bash
dotnet build LYBTZYZS.sln --no-restore
git commit -m "perf: parallelize non-dependent startup pipeline steps"
```

### Task 2: 模块延迟加载

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Modules/ModuleLoadingService.cs`
- Modify: 各 `RoleDefinition` — 区分必须/延迟模块

- [ ] **Step 1: 检查 Prism ModuleCatalog 配置**

确认模块注册方式（`InitializationMode.WhenAvailable` vs `OnDemand`）。

- [ ] **Step 2: 将非首屏模块改为 OnDemand**

Auth + 角色工作区（Clinical/Admin/Receptionist）= WhenAvailable。
Patients/Herbs/Formula/Users/MedicalCase/Registration/Reports = OnDemand。

- [ ] **Step 3: 导航时按需加载**

在 NavigationCoordinator.NavigateTo 中，检查目标模块是否已加载，未加载则先 LoadModuleAsync。

- [ ] **Step 4: 构建验证 + Commit**

### Task 3: 数据缓存

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs`

- [ ] **Step 1: 添加患者列表缓存**

首次加载后缓存到私有字段，导航返回时检查缓存是否有效（5分钟过期），有效则不重新请求。

- [ ] **Step 2: 添加患者历史缓存**

`Dictionary<Guid, (List<HistoryItem> Items, DateTime CachedAt)>`，5分钟过期。

- [ ] **Step 3: 构建验证 + Commit**

### Task 4: HerbListControl UI 虚拟化

**Covers:** [S6]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/HerbList/HerbListControl.xaml`

- [ ] **Step 1: 添加 VirtualizingStackPanel 到 HerbListControl**

```xml
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel VirtualizingPanel.IsVirtualizing="True"
                                     VirtualizingPanel.VirtualizationMode="Recycling" />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

- [ ] **Step 2: 构建验证 + Commit**

### Task 5: 最终构建 + 验证

- [ ] **Step 1: 全量构建**
Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: Commit**
```bash
git commit -m "perf: WPF performance optimization complete"
```
