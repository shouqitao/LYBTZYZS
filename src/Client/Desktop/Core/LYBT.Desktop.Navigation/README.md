# LYBT.Desktop.Navigation

导航协调模块，统一管理桌面客户端的页面导航、模块懒加载、面包屑和导航历史。

## 项目定位

桌面客户端的导航基础设施，通过 `NavigationCoordinator` 提供统一的页面跳转入口。支持按需加载 Prism 模块、维护导航历史栈、生成面包屑路径，解决 WPF 多模块场景下的导航碎片化问题。

## 目录结构

```
LYBT.Desktop.Navigation/
├── Abstractions/
│   └── INavigationCoordinator.cs     # 导航协调器接口
├── NavigationCoordinator.cs          # 核心实现
├── Models/
│   ├── NavigationContext.cs          # 导航上下文（目标视图/参数）
│   └── BreadcrumbItem.cs            # 面包屑节点
├── Constants/
│   └── ViewToModuleMap.cs            # 视图→模块映射配置
└── NavigationModule.cs               # Prism 模块注册
```

## 核心组件

### NavigationCoordinator — 导航协调器

**设计依据**：Facade + Mediator 模式，统一入口替代散落在各模块的 `IRegionManager.RequestNavigate` 调用，集中管理懒加载、历史、面包屑。

| 方法/属性 | 签名 | 说明 |
|-----------|------|------|
| NavigateTo | `void NavigateTo(string viewName, NavigationParameters? parameters = null)` | 统一导航入口，自动触发模块加载 |
| GoBack | `bool GoBack()` | 后退到上一个导航目标 |
| CanGoBack | `bool CanGoBack { get; }` | 是否有可后退的历史 |
| Breadcrumbs | `ObservableCollection<BreadcrumbItem> Breadcrumbs` | 当前面包屑路径 |
| 历史上限 | 20 条 | 超出时丢弃最早的记录 |
| 模块懒加载 | 通过 `ViewToModuleMap` 查找目标视图所属模块 | 未加载则先 `IModuleManager.LoadModule` |
| 区域监控 | `SubscribeToRegionCollection` | 监听 Region 视图集合变化，同步面包屑 |

### INavigationCoordinator — 接口

**设计依据**：接口抽象便于单元测试 Mock，模块仅依赖接口而非具体实现。

| 方法 | 说明 |
|------|------|
| NavigateTo | 导航到指定视图 |
| GoBack | 后退 |
| CanGoBack | 是否可后退 |
| Breadcrumbs | 面包屑集合 |

### ViewToModuleMap — 视图-模块映射

**设计依据**：静态配置表，声明每个视图归属的 Prism 模块名，协调器据此决定是否需要先加载模块。

| 配置项 | 说明 |
|--------|------|
| Key | 视图名称（如 `"PatientListView"`） |
| Value | 模块名（如 `"PatientModule"`） |

### BreadcrumbItem — 面包屑节点

**设计依据**：值对象，表示导航路径中的一个层级。

| 属性 | 类型 | 说明 |
|------|------|------|
| DisplayName | `string` | 显示名称 |
| ViewName | `string` | 对应视图名称 |
| Parameters | `NavigationParameters?` | 导航参数（支持从面包屑回跳） |

## 依赖关系

| 依赖 | 用途 |
|------|------|
| Prism.Modularity | IModule / IModuleManager 模块懒加载 |
| Prism.Regions | IRegionManager / IRegion 导航与区域管理 |
| Prism.DryIoc | DI 容器 |
| CommunityToolkit.Mvvm | ObservableProperty / ObservableCollection |

## 设计决策

1. **统一入口替代分散调用**：所有导航经由 `NavigationCoordinator`，避免模块间直接耦合 `IRegionManager`
2. **懒加载模块**：导航时按 `ViewToModuleMap` 按需加载模块，减少启动时间
3. **历史上限 20 条**：防止长时间使用后历史栈无限增长占用内存
4. **面包屑实时同步**：通过 `SubscribeToRegionCollection` 监听 Region 变化，无需手动维护
5. **Forward 导航**：支持从面包屑点击直接跳转到历史节点，参数一并恢复

## 已知陷阱

- **ViewToModuleMap 遗漏**：新增视图若未注册到映射表，`NavigateTo` 会因找不到模块而静默失败
- **模块加载时序**：`LoadModule` 是异步操作，若模块未加载完成就导航会导致 Region 找不到视图
- **历史上限截断**：超过 20 条时最早记录被丢弃，`GoBack` 可能无法回到最初的页面
- **Region 激活冲突**：同一 Region 快速连续导航可能产生竞态，需确保前一次导航完成后再发起新导航
- **面包屑参数恢复**：从面包屑回跳时，若参数中包含已释放的对象引用会抛异常
