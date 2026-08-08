# 任务 A-19：移除 Auto 模式，模式切换用户自主（不自动降级）

> 依据：产品负责人 2026-08-08 决策「远程模式和本地模式的切换需要自主选择，而不是远程不能用的时候自动降级到本地模式」
> 决策：**彻底移除 Auto 模式**，只保留 Local/Remote 显式切换

## 背景（已勘察确认）

`ConnectionModeService`（`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/ConnectionModeService.cs`）当前三模式：

| 模式 | 行为 | 问题 |
|------|------|------|
| Local | 显式切换 | ✅ |
| Remote | 显式切换（要求 `_isRemoteAvailable`） | ✅ |
| **Auto** | `SetMode(Auto)` → `DetectBestModeAsync()`：PreferredMode=Remote 时探测远程，**失败 → LogWarning "falling back to Local mode" → ApplyMode(Local)** | ⚠️ **自动降级**——用户无法保持远程模式，远程恢复也不会自动切回 |

**痛点**：Auto 模式下远程不可用自动降级本地，用户失去自主控制。

## 任务

### 1. 移除 Auto 模式（核心）

- `ConnectionMode` 枚举（`LYBT.Desktop.Contracts/Models/AuthState.cs` 或定义处）移除 `Auto` 值
- `ConnectionModeService`：
  - 删除 `DetectBestModeAsync()`（或改为不存在的内部方法，由显式切换取代）
  - `SetMode` 删除 `case ConnectionMode.Auto` 分支
  - 构造时不再调用自动检测：`_currentMode = connectionSettings.IsLocal ? Local : Remote`（**按已保存的 PreferredMode/URL 恢复，不探测**）
  - 删除 `OnUrlChanged` 自动推导或改为仅更新显示（不自动切换模式？——**注意：URL 变更推导模式是合理的用户配置跟随，保留**，但需确认不触发探测）
- `IConnectionModeService`（`Contracts/Services/IConnectionModeService.cs`）同步删除 `DetectBestModeAsync` 声明

### 2. 调用点清理

- `ConnectionStatusViewModel.cs:104` `DetectBestModeAsync()` → 改为读取当前模式或显式 `SetMode`
- 任何引用 `ConnectionMode.Auto` / `DetectBestModeAsync` 的地方同步清理
- `ConnectionModeDisplay` / UI 绑定检查是否暴露 Auto 选项，移除

### 3. 远程不可用时的用户反馈（替代自动降级）

- 保持 `CheckRemoteAvailableAsync()`（UI 按钮状态用，如「切换远程」按钮禁用）
- 用户显式 `SetMode(Remote)` 时若不可达：保持现行为（LogWarning + 不切换），**必要时**UI 弹提示（Toast）告知「远程不可用，仍处于当前模式」

### 4. 行为验证

- 用户选 Remote + 远程不可用 → **仍保持 Remote 模式**（不降级本地），UI 显示不可用状态
- 用户选 Local → 保持 Local
- 重启应用 → 按保存的 PreferredMode 恢复，不自动探测

## 硬性约束

1. **只移除 Auto 降级机制**，不改双轨实现（SwitchingApiClient/HttpClientApiClient/RefitApiClient 不动）
2. 不引入新机制（不加重试/熔断，那是另一回事）
3. UI 文案保持一致（远程模式/本地模式）

## 验证

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：84/84
3. 相关单测（ConnectionModeService 若有）
4. commit + push

## 不做

- ❌ 不改契约双套统一（A-18 批次2 的另一任务，勿混入）
- ❌ 不做自动重连/健康监控降级策略
- ❌ 不删除 `CheckRemoteAvailableAsync`/`TestRemoteConnectionAsync`/`TestLocalConnectionAsync`（UI 状态用）
