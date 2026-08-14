# Desktop 死代码/废弃代码 审计报告

**日期**: 2026-08-14  
**范围**: `src/Client/Desktop/` (15 个项目, 553 个 .cs 文件, 77 个 XAML 文件, ~60,466 行 C# 代码)  
**扫描类别**: 空类/死代码/废弃标记/未使用引用/重复定义/空目录/TODO标记/NuGet包/配置

---

## 📊 审计摘要

| 类别 | 发现数 | 严重度 |
|------|--------|--------|
| 重复资源文件 | 1 | 🔴 高 |
| 未使用的事件类 | 1 | 🟡 中 |
| 重复类型定义（同名不同类） | 1 | 🟡 中 |
| 未使用的资源文件 | 1 | 🟡 中 |
| 空目录/孤儿目录 | 4 | 🟢 低 |
| 未完成功能标记 (TODO/FIXME) | 7 | 🟢 低 |
| 潜在未使用 using 指令 | ~646 | 🟢 低 |
| 空标记类（Prism Event 模式） | 18 | ✅ 正常 |
| NuGet 包引用 | ✅ 全部在用 | ✅ 正常 |
| LocalWebAPI UsersController | ✅ 正常 | ✅ 正常 |

---

## 🔴 P1 — 重复资源文件

### DC-001: StringResources.resx 完全重复

| 项目 | 内容 |
|------|------|
| **文件 1** | `Resources/Strings/StringResources.resx` (7,885 bytes) |
| **文件 2** | `Shell/Resources/Strings/StringResources.resx` (7,885 bytes) |
| **MD5** | 两者完全一致 (`1e536bb1c1537485e88275264ecbeab1`) |
| **问题** | 同一资源文件存在于两个位置，任一文件修改不会同步到另一处，极易造成字符串不一致 |
| **建议** | 🔧 **删除** `Resources/Strings/StringResources.resx`，保留 `Shell/Resources/Strings/` 下的版本（Shell 是生成 .Designer.cs 的位置）。更新 `.csproj` 如果有对旧路径的嵌入资源引用。 |

---

## 🟡 P2 — 未使用的代码

### DC-002: PatientEvents.CreatedEvent 未使用

| 项目 | 内容 |
|------|------|
| **文件** | `Core/LYBT.Desktop.Infrastructure/Events/PatientEvents.cs:21` |
| **定义** | `public class CreatedEvent : PubSubEvent<PatientCreatedPayload> { }` |
| **问题** | 全项目无任何代码发布或订阅此事件。同文件的 `UpdatedEvent` 被正常引用（3 处），但 `CreatedEvent` 零引用。`PatientCreatedPayload` record 定义也仅被 `CreatedEvent` 使用。 |
| **建议** | 🔧 **删除** `CreatedEvent` 类和 `PatientCreatedPayload` record。或在患者创建流程中实际发布此事件（如 `PatientService.CreateAsync` 成功后）。 |

### DC-003: LoginStrings.resx 未使用

| 项目 | 内容 |
|------|------|
| **文件** | `Resources/Strings/LoginStrings.resx` (5,351 bytes) |
| **问题** | 全项目无任何 `.cs` 或 `.xaml` 文件引用 `LoginStrings`。该资源文件已被 `Shell/Resources/Strings/StringResources.resx` 中的登录相关字符串替代。 |
| **建议** | 🔧 **删除** `Resources/Strings/LoginStrings.resx`。确认登录相关字符串已迁移至 StringResources。 |

### DC-004: Resources/Dictionaries/ 目录不存在（文档引用了它）

| 项目 | 内容 |
|------|------|
| **文件** | `Resources/AGENTS.md` — 声明 `Dictionaries/` 子目录包含"styles, colors, control templates, themes" |
| **问题** | `Resources/Dictionaries/` 目录实际不存在。XAML 字典目前分散在 `Core/LYBT.Desktop.Infrastructure/Themes/` 和 `Shell/Controls/` 等位置。AGENTS.md 文档与实际结构不一致。 |
| **建议** | 🔧 更新 `Resources/AGENTS.md` 移除对不存在的 `Dictionaries/` 的引用，反映实际的主题资源位置。 |

---

## 🟡 P3 — 重复类型定义

### DC-005: BreadcrumbItem 两个不同定义

| 项目 | 内容 |
|------|------|
| **定义 1** | `Core/LYBT.Desktop.Contracts/UI/BreadcrumbItem.cs` — `public record BreadcrumbItem(string Title, string ViewName, bool IsCurrent)` |
| **定义 2** | `Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml.cs:100` — `public class BreadcrumbItem { Label, Level, IsCurrent, IsLast, NavigateCommand }` |
| **问题** | 两个同名但结构不同的类型。Contracts 版本（record）用于 `INavigationCoordinator` 和 `NavigationHistoryService`；Controls 版本（class）用于 `BreadcrumbBar` 控件内部。Controls 版本有 `NavigateCommand` 和 `Level` 等额外属性。 |
| **使用情况** | Contracts 版本：6 个文件引用（Contracts + Infrastructure + Shell）；Controls 版本：仅 BreadcrumbBar 自身使用 |
| **建议** | 🔧 **统一为一个类型**。建议以 Contracts 版本为主（record 语义更清晰），扩展 Controls 所需属性。或在 Controls 内部重命名避免命名冲突。当前不影响编译（不同命名空间），但易引起混淆。 |

### DC-006: NavigableViewModelBase 部分文件（partial class）

| 项目 | 内容 |
|------|------|
| **文件** | `NavigableViewModelBase.cs` + `.Editable.cs` + `.Navigation.cs` |
| **问题** | 这是 partial class 的正常拆分模式，**非重复定义**。 |
| **建议** | ✅ **无需操作** — 这是合理的 partial class 设计。 |

---

## 🟢 P4 — TODO/未完成功能标记

### DC-007: 代码中的 TODO/XXX/TEMP 标记

| 文件 | 行 | 标签 | 内容 |
|------|-----|------|------|
| `Modules/LYBT.Desktop.MedicalCase/Reports/ReportsModule.cs` | 12 | TODO | 后续迭代完善报表功能 |
| `Modules/LYBT.Desktop.MedicalCase/ViewModels/Items/PrescriptionItemViewModel.cs` | 26 | TODO | 静态 mapper 与 DI 风格不一致，未来改为构造注入 |
| `Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs` | 198 | TODO | US-SHELL-005 - 从服务获取今日统计数据 |
| `Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs` | 36 | TODO | 超大类型，建议拆分 |

**注**: 扫描器发现的以下标记为**误报**（实际是文档中的占位符文字，非 TODO 标记）:
- `WorkspaceMode.cs:12,19` — `XXX` 是枚举文档中的示例占位符（`"患者：XXX"`）
- `ClientErrorMessageMapper.cs:293` — `xxx` 是错误消息模式说明
- `SystemConstants.cs:104` — `TempDirectory` 是常量值名称
- `StringResources.Designer.cs:42-43` — `TEMP` 是资源管理器生成代码的一部分

| **建议** | 🟢 对 DC-007 建立 Issue 追踪，在后续迭代中逐步清理。优先处理 `MedicalCaseWorkspaceViewModel` 的拆分建议（大型类）。 |

---

## 🟢 P5 — 空目录/孤儿目录

### DC-008: 空目录

| 目录 | 说明 |
|------|------|
| `Resources/Strings/` | 仅含 `LoginStrings.resx`（未使用）和重复的 `StringResources.resx`。无 .cs 文件。 |
| `Shell/Assets/Fonts/` | 含 `simli.ttf` 字体文件，有实际用途（UI 字体）。 |
| `Shell/Assets/Icons/App/` | 含 `app.ico`，有实际用途（应用图标）。 |
| `Shell/Assets/Images/Backgrounds/` | **空目录**，无任何文件。 |

| **建议** | 🔧 删除空目录 `Shell/Assets/Images/Backgrounds/`。`Resources/Strings/` 的内容清理后该目录也可删除。其余目录有实际资源文件。 |

---

## ✅ 正常/无需操作

### UsersController（CQ-02）

`LocalWebAPI/Controllers/UsersController.cs` — **非空控制器**。

```
20 行代码，继承 BaseUsersController
2 个 HTTP 属性（[ApiController], [Route]）
通过基类获得完整 CRUD + 密码重置功能
注入 IUserService，委托给统一服务层
```

**结论**: 这是薄控制器模式的正确实现，通过继承 `BaseUsersController` 获得所有端点。**无需清理。**

### 空标记类（Prism Event 模式）

发现 18 个空类（仅有 `class Xxx : PubSubEvent<T> { }`），**全部被正常使用**：
- 13 个 AuthEvent 类：被 Foundation/Security 服务发布/订阅
- 2 个 CaseEvent 类：被 MedicalCase 模块使用
- 2 个 PatientEvent 类：`UpdatedEvent` 在用，`CreatedEvent` 未用（见 DC-002）
- 1 个 RegistrationEvent 类：被 Registrations 模块使用

这是 Prism 事件聚合器的标准模式，空类体是正常的。✅

### NuGet 包引用

所有 15 个项目的 NuGet 包均被实际代码使用：
- `System.Reactive`：被 13 个 Infrastructure 服务引用（Observable 模式）
- `System.ComponentModel.Annotations`：被 13 个 Model/ViewModel 引用（`[Required]`, `[StringLength]`）
- `Microsoft.AspNetCore.SignalR.Client`：被 Registrations 模块的 4 个文件引用
- `Velopack`：被 Shell 和 Foundation 引用（更新功能）

### 注释掉的代码块

仅发现 1 个注释块（`Core/LYBT.Desktop.Foundation/Http/RefitApiClient.cs:7-10`），实际是**类级文档注释**而非注释掉的代码。✅

### 未使用的 [Obsolete] 标记

仅 1 处提及"已废弃"（`FeatureToggleOptions.cs` 文档注释），是记录设计决策的正常文档。✅

### 未使用 Using 指令

扫描发现 ~646 个潜在未使用的 using 指令。由于以下原因不建议大规模自动清理：
1. 很多 namespace 通过隐式全局 using 引入，静态分析工具可精确判断
2. 手动清理风险高（可能遗漏运行时依赖）
3. IDE 的 "Remove Unused Usings" 功能更安全

| **建议** | 🟢 在 IDE 中批量运行 "Remove and Sort Usings"（ReSharper/Rider 或 VS 2022），比手动更安全可靠。 |

---

## 🗑️ 推荐清理动作汇总

| 优先级 | ID | 动作 | 工作量 |
|--------|-----|------|--------|
| 🔴 P1 | DC-001 | 删除重复的 `Resources/Strings/StringResources.resx` | 5 min |
| 🟡 P2 | DC-002 | 删除 `PatientEvents.CreatedEvent` + `PatientCreatedPayload` | 10 min |
| 🟡 P2 | DC-003 | 删除 `Resources/Strings/LoginStrings.resx` | 5 min |
| 🟡 P3 | DC-005 | 统一 `BreadcrumbItem` 为单一定义 | 30 min |
| 🟢 P4 | DC-007 | 建 Issue 追踪 TODO 项 | 5 min |
| 🟢 P5 | DC-008 | 删除空目录 `Shell/Assets/Images/Backgrounds/` | 1 min |
| 🟢 Doc | DC-004 | 更新 `Resources/AGENTS.md` 移除对不存在 `Dictionaries/` 的引用 | 5 min |

**预估总工作量**: ~1 小时（不含 DC-005 的统一工作）

---

*报告由 Hermes Agent 自动生成，基于静态代码扫描。未使用 using 指令的判断基于文本匹配，可能有误报。*
