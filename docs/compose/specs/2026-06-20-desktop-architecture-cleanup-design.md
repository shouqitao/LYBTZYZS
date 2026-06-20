# Desktop 层基础设施重组 — 设计规格

> 日期: 2026-06-20
> 基于: 4 个并行 CodeGraph 子代理分析（依赖图 + Core 深度分析 + 模块/Shell 分析 + DI/XAML/测试质量审计）
> 方案: A — Infrastructure 拆分 + Models/Utilities 合并 + Contracts 清理

## [S1] 问题

Desktop 层 21 个项目中，`LYBT.Desktop.Infrastructure` 承担了过多职责：ViewModel 基类(590 行) + WPF 控件(29 个 .cs + 21 个 .xaml) + 主题(15 个 .xaml) + 导航服务(570 行) + 转换器(17 个) + 业务服务(35 个 .cs) + 行为 + 常量 + 事件 + 扩展。总代码量 18,663 行，占全部 Core 代码的 48%。

同时 `Models` 项目仅含 5 个文件(1,008 行)，只提供 ViewModel 基类——这些基类与 Infrastructure 的 `MasterDetailViewModelBase` 高度耦合，存在一条没有实际价值的依赖边。

`Contracts` 混入了非接口类型(CommandResult、AuthState、ApiClientOptions 等)，泄漏了实现细节。

**根因：** 历史演变中，所有新功能被持续添加到 Infrastructure（唯一的"基础设施"项目），没有适时拆分。

## [S2] 当前状态

### 21 个 Desktop 项目（CodeGraph 分析确认）

| 项目 | 类型 | LOC | 依赖数 | 被依赖数 |
|------|------|-----|--------|---------|
| Contracts | Core(接口) | 4,316 | 0 | 16 |
| Utilities | Core(工具) | 465 | 0 | 3 |
| Foundation | Core(HTTP/安全) | 7,437 | 1 | 11 |
| Infrastructure | Core(WPF基础设施) | **18,663** | 2 | 15 |
| Models | Core(ViewModel基类) | 1,008 | 2 | 12 |
| Printing | Core(打印) | 2,775 | 1 | 2 |
| LocalData | Core(本地存储) | 1,013 | 1 | 1 |
| CardReader | Core(硬件) | 1,526 | 1 | 4 |
| 8 个业务模块 | Module | ~22K | 4-6 | 1-5 |
| 3 个角色工作区 | Role | ~5.3K | 8-9 | 1 |
| Shell | Shell | 7,857 | 18 | 0 |

### Infrastructure 内部分布

| 子目录 | .cs | .xaml | 说明 |
|--------|-----|-------|------|
| Controls/ | 29 | 21 | 自定义 WPF 控件 |
| Services/ | 35 | 0 | WPF 服务实现 |
| Themes/ | 0 | 15 | 设计令牌 + 样式 |
| Converters/ | 17 | 1 | 值转换器 |
| Navigation/ | 11 | 3 | 导航协调器 |
| ViewModels/ | 8 | 0 | MasterDetailVMBase, BaseStatusHandler 等 |
| Dependencies/ | 1 | 0 | DI 扩展 |
| 其他 | 37 | 2 | Events, Constants, Commands, Behaviors 等 |

## [S3] 目标架构

```
当前:                    目标:
Contracts (4.3K)        Contracts (4.3K) ← 清理非接口类型
Foundation (7.4K)       Foundation (7.9K) ← 合并 Utilities
Infrastructure (18.6K)  Infrastructure (~10K) ← 仅服务 + ViewModel 基类
Models (1K)             Controls (~8.7K) ← NEW: 控件 + 主题 + 转换器
Utilities (0.5K) ← 删除  Shared (~0.5K) ← NEW: 非接口共享类型
```

**关键变更：**
1. `Models` 合并进 `Infrastructure`（消除依赖边）
2. `Infrastructure/Controls` + `Themes` + `Converters` → 新项目 `LYBT.Desktop.Controls`
3. `Contracts` 中的非接口类型 → 新项目 `LYBT.Desktop.Shared`
4. `Utilities` 合并进 `Foundation`
5. `Controls` 被 `Infrastructure` 和各模块可选引用

## [S4] 新项目：LYBT.Desktop.Controls

**从 Infrastructure 提取的展示层资产：**

```
LYBT.Desktop.Controls/
├── Controls/          # 29 个自定义控件 (.cs + .xaml)
│   ├── MasterDetailControlBase/
│   ├── HerbItem/
│   ├── HerbList/
│   ├── DataGridSelectionBehavior/
│   └── ...
├── Converters/        # 17 个值转换器
├── Themes/            # 15 个主题资源字典
│   ├── DesignSystem.xaml
│   ├── PreviewStyles.xaml
│   ├── PanelStyles.xaml
│   ├── MedicalCaseStyles.xaml
│   ├── ValidationStyles.xaml
│   └── ...
└── Behaviors/         # XAML 行为
```

**依赖：** `Controls` → `Contracts`（接口定义）+ `LYBT.Shared.Models`（DTO）

**谁引用 Controls：**
- `Infrastructure`（需要知道控件基类的存在）
- 各模块（MasterDetailControlBase 等）
- Shell（App.xaml 合并主题字典）

**不引用 Controls 的项目：**
- `Foundation`（纯 HTTP/安全，不涉及 WPF）
- `LocalData`（纯数据访问）
- `Printing`（使用 QuestPDF，不依赖 WPF 控件库）

## [S5] Models 合并进 Infrastructure

**5 个文件迁移：**

| 文件 | 迁移到 |
|------|--------|
| CoreViewModelBase.cs | Infrastructure/ViewModels/Base/ |
| NavigableViewModelBase.cs | Infrastructure/ViewModels/Base/ |
| DialogViewModelBase.cs | Infrastructure/ViewModels/Base/ |
| ValidatableModelBase.cs | Infrastructure/ViewModels/Base/ |
| ValidationAccessors.cs | Infrastructure/ViewModels/Base/ |

**影响：** 所有引用 `LYBT.Desktop.Models.ViewModels.Base.*` 的项目需要更新 using 指令。涉及 12 个被依赖项目（主要是各模块的 ViewModel）。

**风险：** 低——仅 using 指令变更，无行为变更。

## [S6] Utilities 合并进 Foundation

**1 个文件迁移：**

| 文件 | 迁移到 |
|------|--------|
| ExcelHelper.cs (465 行) | Foundation/Utilities/ExcelHelper.cs |

**影响：** 3 个引用 Utilities 的项目（Patients, Herbs, Users）需要更新引用。

**风险：** 极低——单一文件，已确认无 WPF 依赖。

## [S7] Contracts 清理

**从 Contracts 移出的非接口类型：**

| 类型 | 类别 | 移到 |
|------|------|------|
| CommandResult / CommandResult\<T\> | 共享 DTO | Shared (新项目) |
| AuthState | 共享 DTO | Shared |
| ApiClientOptions | 配置 | Shared |
| PerformanceReport / Metric | 共享 DTO | Shared |
| BreadcrumbItem | UI 模型 | Controls |
| CacheEvents | 事件定义 | Infrastructure |
| ImportValidationResult | 共享 DTO | Shared |
| UnfinishedCaseChoice | 共享 DTO | Shared |

**注意：** `LYBT.Desktop.Shared` 是一个轻量级项目，仅包含从 Contracts 移出的共享类型 + 枚举。它不包含实现代码，不引入新依赖。

## [S8] 影响范围

### 项目级影响

| 项目 | 变更类型 | 影响程度 |
|------|----------|---------|
| Infrastructure | 删除 Controls/Themes/Converters 子目录 + 接收 Models 文件 | HIGH（项目结构大变） |
| Controls (新) | 新项目创建 | — |
| Shared (新) | 新项目创建 | — |
| Models | 删除（合并进 Infrastructure） | LOW |
| Utilities | 删除（合并进 Foundation） | LOW |
| Foundation | 接收 ExcelHelper | LOW |
| Contracts | 移出非接口类型 | MEDIUM |
| 各业务模块 | 更新 using + ProjectReference | MEDIUM（每个模块 2-5 个 .cs 文件） |
| Shell | 更新 App.xaml MergedDictionaries 路径 + ProjectReference | MEDIUM |
| 3 个角色项目 | 更新 ProjectReference | LOW |
| Desktop 测试项目 | 更新 ProjectReference + using | LOW |

### 需要更新的文件（估计）

| 变更类型 | 估计数量 |
|----------|---------|
| 移动 .cs/.xaml 文件 | ~65（Controls/Themes/Converters/Models/Utilities） |
| 更新 ProjectReference（.csproj） | ~15 |
| 更新 using 指令 | ~30（Models 合并后的命名空间变更） |
| 更新 XAML MergedDictionaries 路径 | ~5（App.xaml + 测试项目） |
| 新建 .csproj（Controls + Shared） | 2 |
| **总计** | ~117 个文件变更 |

## [S9] 迁移顺序与回滚

### 执行顺序（5 步）

**Step 1: 创建新项目骨架**
- 创建 `LYBT.Desktop.Controls.csproj`（引用 Contracts + Shared.Models）
- 创建 `LYBT.Desktop.Shared.csproj`（无依赖）
- 在 `LYBTZYZS.sln` 中注册新项目
- 验证：`dotnet build` 0 错误

**Step 2: 迁移文件**
- Infrastructure/Controls → Controls/Controls/
- Infrastructure/Themes → Controls/Themes/
- Infrastructure/Converters → Controls/Converters/
- Infrastructure/Behaviors → Controls/Behaviors/（如存在）
- Models/* → Infrastructure/ViewModels/Base/
- Utilities/ExcelHelper.cs → Foundation/Utilities/
- Contracts 中的非接口类型 → Shared/

**Step 3: 更新引用**
- 更新 15 个 .csproj 的 ProjectReference
- 更新 ~30 个 .cs 的 using 指令
- 更新 5 个 XAML 的 MergedDictionaries 路径
- 删除旧 Models/ 和 Utilities/ 项目
- 从 .sln 移除 Models/Utilities 的项目条目

**Step 4: 验证**
- `dotnet build LYBTZYZS.sln` — 0 错误
- `dotnet test tests/LYBT.Tests.Architecture/` — 0 失败
- `dotnet test tests/LYBT.Tests.Desktop/` — 不变（基线对照）

**Step 5: 清理**
- 删除 Infrastructure 中已空的 Controls/Themes/Converters 子目录
- 更新 AGENTS.md 中的架构描述
- 更新受影响模块的 AGENTS.md

### 回滚策略

每一步完成后可独立回滚（git revert）。最大的风险在 Step 2（文件移动），可通过 `git checkout` 恢复。Step 3（引用更新）如果出错，可通过全局搜索/替换修复。

## [S10] 验证标准

| 标准 | 测试方法 |
|------|---------|
| 编译通过 | `dotnet build LYBTZYZS.sln` — 0 错误 |
| 架构测试通过 | `dotnet test tests/LYBT.Tests.Architecture/` — 0 失败 |
| Desktop 测试基线不变 | `dotnet test tests/LYBT.Tests.Desktop/` — 通过数 >= 基线（预修复） |
| Infrastructure LOC 减半 | `Get-ChildItem -Recurse -Include *.cs -Path src/Client/Desktop/Core/LYBT.Desktop.Infrastructure \| Measure-Object -Sum` — < 10,000 |
| Controls 项目存在且独立 | `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/` — 0 错误 |
| 无循环依赖 | 依赖图仍为 DAG |
| Models/Utilities 项目不再存在 | glob 确认 |
| Contracts 仅含接口 | grep `class \|struct \|enum \|record` — 0 匹配（除接口定义文件外） |
