# Desktop 基础框架全面重构 Design

## [S1] 问题

Desktop 层 21 个项目 ~93K LOC 存在 5 个架构问题：
1. Infrastructure 项目过载（9.4K LOC，7 个职责混合）
2. 双导航系统并存（NavigationCoordinator vs EnhancedNavigationService）
3. MasterDetailVM 继承债（无法继承 CoreViewModelBase）
4. Contracts 泄漏具体类型（7 个 DTO/Enum）
5. 20 个 VM 在继承体系外

## [S2] 目标

**第一目标：架构清晰**——每个项目一个关注点，职责边界明确。

## [S3] 约束

- 渐进式：每阶段可独立编译测试
- 仅 Desktop 层：Server 不动
- **LocalWebAPI 保持 unified service layer**（远程和本地共享同一套 Service 代码，逻辑不偏离）
- Prism 模式不变

### LocalWebAPI 决策：Option D — 正式化例外

**不 解耦**。理由：远程和本地 API 逻辑上不应偏离，UI 始终兼容两种模式。解耦会创造两套实现，违反"逻辑不偏离"原则。

措施：
1. 写 ADR（架构决策记录）记录 unified service layer 的选择理由
2. P21 架构测试从"跳过"改为"例外审计"（验证引用列表是否匹配 ADR）
3. LocalWebAPI AGENTS.md 增加依赖图 + 变更协议

## [S4] Phase 1 — Infrastructure 拆分分析

### 现状：Infrastructure 7 个职责

| 职责 | LOC | 文件数 | 依赖方向 |
|------|-----|--------|---------|
| VM 基类 | ~1,200 | 5 | ← 所有 Modules |
| WPF 服务 | ~3,200 | 35 | ← Modules + Shell |
| 导航系统 | ~1,500 | 8 | ← Shell + 12 VMs |
| 主题样式 | ~2,200 | 15 XAML | ← Controls + 所有 Views |
| HTTP/仓库 | ~400 | 5 | ← Foundation |
| 角色定义 | ~250 | 6 | ← Shell |
| 行为/控件 | ~700 | 5 | ← Controls |

### 拆分方案对比

#### 方案 A：3 项目（推荐）

```
Infrastructure (瘦身)     → VM 基类 + WPF 服务 + 角色定义 + HTTP + 行为
Navigation (新项目)       → NavigationCoordinator + 导航模型 + 历史管理
（主题移入 Controls）      → Themes/ 目录移到 Controls 项目
```

- Infrastructure 从 9.4K → ~5.8K LOC（减少 38%）
- Controls 从 8K → ~10.2K LOC（吸收主题）
- Navigation 新项目 ~1.5K LOC
- 拆分逻辑：Infrastructure 保留"基础设施服务"，Navigation 独立为"导航框架"，主题回归"表现层"

#### 方案 B：4 项目

```
Infrastructure.Core       → VM 基类 + WPF 服务
Navigation                → 导航系统
Themes (新项目)            → 所有 XAML 主题
Controls (已有)            → 控件 + 转换器
```

- 更细粒度但 Themes 项目太小（2.2K LOC 不值得独立项目）
- 增加项目数（21 → 22），增加编译时间

#### 方案 C：最小拆分（只拆导航）

```
Infrastructure (不变)     → 保持现状
Navigation (新项目)       → 只提取导航相关代码
```

- 最小改动，但 Infrastructure 仍然 8K LOC 混合 6 个职责
- 只解决问题 #2（双导航系统），不解决问题 #1

### 推荐：方案 A

理由：
- Infrastructure 瘦身后职责清晰（服务+基类+角色）
- 主题回归 Controls 符合 WPF 惯例（控件+样式同层）
- Navigation 独立解决双系统问题
- 项目数不变（21 → 21，Themes 不新增项目而是合并到 Controls）

## [S5] Phase 2 — 统一导航系统

### 现状：3 套导航

| 系统 | 位置 | 消费者 | 状态 |
|------|------|--------|------|
| NavigationCoordinator | Shell | 12+ VMs | 活跃 |
| EnhancedNavigationService | Infrastructure | 5 文件 | 半成品（3 TODO） |
| Prism IRegionManager | 全局 | 底层 | 正常 |

### 方案

1. **删除 EnhancedNavigationService** + NavigationHistoryManager + BreadcrumbManager + NavigationModels（共 ~900 LOC）
2. **将 NavigationCoordinator 从 Shell 迁移到新 Navigation 项目**
3. NavigationCoordinator 成为唯一的高层导航 API
4. 面包屑和历史面板 VM 直接绑定 NavigationCoordinator

## [S6] Phase 3 — VM 继承体系修复

### 现状

```
ObservableObject
  ├── CoreViewModelBase (abstract, IDisposable)
  │     ├── NavigableViewModelBase (363 LOC)
  │     └── DialogViewModelBase (204 LOC)
  ├── MasterDetailViewModelBase (590 LOC) ← 不继承 CoreViewModelBase
  └── 20 个散落的 ObservableObject 继承者
```

### 方案

1. **MasterDetailViewModelBase 改为继承 NavigableViewModelBase**
   - 将 IMasterDetailServices 通过构造函数注入
   - 解决 ARCH-REFACTOR 注释标记的依赖问题
   - 5 个 MasterDetail VM 获得完整的基类功能

2. **收编 20 个散落 VM**
   - 服务类（PaginationService, SearchService, LoadingStateManager）→ 改为非 VM 服务
   - 真正的 VM → 继承合适的基类

## [S7] Phase 4 — Contracts 清理 + 死代码

### Contracts 清理

7 个 DTO/Enum 迁移到 `LYBT.Desktop.Shared` 或 `LYBT.Shared.Models`：
- ApiClientOptions → Shared.Configuration
- AuthState → Shared.Models.Enums
- PerformanceMetric/Report → Desktop.Shared
- ImportValidationResult → Shared.Models.Contracts
- CacheEvents/UnfinishedCaseChoice → Desktop.Shared

### 死代码删除

- EnhancedNavigationService 3 个 TODO 标记的未实现方法
- PatientViewState（Patients 模块，无消费者）
- FormulaCommandHandler（Formula 模块，未注册）
- 6 个 _wpftmp.csproj 构建产物

## [S8] 实施顺序

```
Phase 1 (Infrastructure 拆分)
  └→ Phase 2 (统一导航)
       └→ Phase 3 (VM 继承)
            └→ Phase 4 (清理)
```

每阶段结束：`dotnet build` + `dotnet test` + commit。
