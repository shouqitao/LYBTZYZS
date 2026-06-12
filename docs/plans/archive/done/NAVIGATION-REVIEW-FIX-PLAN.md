# 导航架构 Review 修复计划

> 生成时间: 2026-04-18 19:35  
> 审查来源: OpenCode 交叉验证 (zai-coding-plan/glm-4.6)  
> 项目: LYBTZYZS 凌隐宝堂中医诊所管理系统

---

## 修复范围

基于 OpenCode code review 结果，修复 Critical / Warning 级别问题。

---

## Phase 1: Critical — IToastService 迁移补全

### 问题
- `IToastService.cs` 和 `ToastType` 枚举从 Infrastructure 迁移到 Contracts
- 需要确认 Contracts 中的接口完整，Infrastructure 中的引用全部更新

### 步骤

#### 1.1 验证 Contracts 中的 IToastService
- 检查 `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IToastService.cs` 是否存在且完整
- 确认包含 `ToastType` 枚举定义
- 确认接口方法签名：`Show(string message, ToastType type, int durationMs = 3000)`

#### 1.2 清理 Infrastructure 中的旧文件
- 删除 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Toast/IToastService.cs`（如果还存在）
- 确保 `ToastService.cs` 引用正确的命名空间 `LYBT.Desktop.Contracts.Services`

#### 1.3 更新所有引用
确保以下文件使用正确的 using：
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Toast/ToastService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/Toast/ToastControl.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/NavigableViewModelBase.cs`

#### 1.4 验证 DI 注册
- 检查 `ViewModelServicesExtensions.cs` 中 `IToastService` 注册是否正确

### 验证
```
dotnet build -p:EnableWindowsTargeting=true 2>&1 | grep -i toast
```

---

## Phase 2: Warning — Toast API 统一

### 问题
- `MedicalCaseCommandsViewModel` 使用 `_toastService.Show(msg, ToastType.Success, 5000)`
- `NavigableViewModelBase` 使用 `ToastService.ShowSuccess(message)`
- 两种调用方式共存

### 步骤

#### 2.1 统一为 Show(msg, type, duration) 签名
- 在 `NavigableViewModelBase` 中将 `ShowSuccess`/`ShowError`/`ShowWarning` 替换为 `Show(msg, ToastType.XXX)`
- 这些便捷方法本质上是语法糖，统一调用方式更清晰

#### 2.2 或保留便捷方法但内部统一实现
- 如果 `ShowSuccess`/`ShowError` 在多处使用，保留为便捷方法
- 但确保它们内部调用 `Show(msg, type, duration)` 而非重复逻辑

### 验证
```
dotnet build -p:EnableWindowsTargeting=true 2>&1 | grep "error CS" | grep -i toast
```

---

## Phase 3: Warning — 服务生命周期审查

### 问题
- `IEnhancedNavigationService` 注册为单例
- 导航历史和状态在 ViewModel 间共享

### 步骤

#### 3.1 确认单例是合理选择
- 导航历史栈应该全局共享（符合预期）
- 前进/后退栈跨 ViewModel 是设计意图
- **结论**: 单例合理，不需要改

#### 3.2 添加线程安全注释
- 在 `EnhancedNavigationService` 中添加注释说明线程模型
- 如果导航操作只在 UI 线程，添加 `Dispatcher` 断言

### 验证
无编译变更，仅文档/注释

---

## Phase 4: Warning — RequestEnterEditMode 空实现

### 问题
- `MedicalCaseMasterDetailViewModel.RequestEnterEditMode()` 是空实现（No-op）
- `MedicalCaseWorkspaceViewModel` 期望调用状态机

### 步骤

#### 4.1 检查接口定义
- 在 `IWorkspaceHost` 中确认 `RequestEnterEditMode` 的契约
- 如果 MasterDetail 场景不需要编辑模式，标记为 `NotImplementedException` 或添加 TODO 注释

#### 4.2 添加防御性注释
- 说明为什么是 No-op（MasterDetail 视图不支持内联编辑）
- 或实现编辑模式切换逻辑

### 验证
```
dotnet build -p:EnableWindowsTargeting=true 2>&1 | grep "error CS"
```

---

## Phase 5: 线程安全 — 导航操作 Dispatcher 断言

### 步骤

#### 5.1 在 EnhancedNavigationService 添加线程检查
- 在 `RequestNavigate` 入口检查是否在 UI 线程
- 添加 `Dispatcher.CheckAccess()` 断言

### 验证
```
dotnet build -p:EnableWindowsTargeting=true 2>&1 | grep "error CS"
```

---

## 执行顺序

| Phase | 内容 | 预计影响文件数 |
|-------|------|---------------|
| 1 | IToastService 迁移补全 | 5-8 |
| 2 | Toast API 统一 | 3-5 |
| 3 | 服务生命周期审查 | 1 (注释) |
| 4 | RequestEnterEditMode | 1-2 |
| 5 | 线程安全 | 1 |

每个 Phase 完成后执行 `dotnet build` 验证，确保不引入新错误。

---

## 最终验证

```bash
export PATH="$HOME/.dotnet:$PATH" && cd ~/repos/LYBTZYZS && \
  dotnet build -p:EnableWindowsTargeting=true --no-incremental 2>&1 | \
  grep -E "error CS|Build succeeded|Error\(s\)|Warning\(s\)"
```

目标：Infrastructure 和 Shell 项目零错误，MedicalCase 模块的 ConsultationItem 问题作为独立 issue 处理。
