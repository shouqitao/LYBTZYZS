# Delta: client-layer-architecture

## MODIFIED Requirements

### Requirement: CLI-001 Core层职责

Core层(6个项目) SHALL 提供客户端基础设施支持。

**项目职责**:

| 项目 | 职责 | 主要内容 |
|------|------|----------|
| LYBT.Desktop.Contracts | 接口定义 | IApi接口(Refit)、IService接口、IRepository接口 |
| LYBT.Desktop.Foundation | 基础设施 | HTTP客户端、Token管理、缓存、配置 |
| LYBT.Desktop.Infrastructure | 服务实现 | 业务服务、事件、DI配置、本地化 |
| LYBT.Desktop.Controls | UI组件库 | 控件、转换器、模板、主题 |
| LYBT.Desktop.Models | ViewModel | ViewModelBase、UnifiedViewModelBase |
| LYBT.Desktop.Presentation | UI基类 | DialogViewModelBase、BaseApiRepository |

**变更说明**:
- 新增LYBT.Desktop.Controls项目，从Infrastructure提取UI组件
- Infrastructure重新定位为服务实现层，移除UI组件职责
- Foundation整合所有HTTP和Token管理

#### Scenario: 使用UI控件
- **WHEN** 需要使用通用控件（SearchBox、LoadingOverlay等）
- **THEN** SHALL 从LYBT.Desktop.Controls引用
- **AND** SHALL NOT 从Infrastructure引用控件

#### Scenario: Token管理
- **WHEN** 需要进行Token存储或生命周期管理
- **THEN** SHALL 使用Foundation层的ITokenService
- **AND** SHALL NOT 使用已废弃的ITokenStorage或ITokenStorageService

---

## ADDED Requirements

### Requirement: CLI-007 Controls项目规范

LYBT.Desktop.Controls SHALL 作为独立的UI组件库。

**目录结构**:
```
LYBT.Desktop.Controls/
├── Controls/              # XAML控件
│   ├── DataGridToolbar.xaml
│   ├── SearchBox.xaml
│   ├── LoadingOverlay.xaml
│   └── ...
├── Converters/            # 值转换器
│   ├── BooleanToVisibilityConverter.cs
│   ├── NullToVisibilityConverter.cs
│   └── ...
├── Templates/             # 控件模板
├── Themes/                # 主题资源
└── Behaviors/             # 行为
```

**依赖规则**:
- 仅依赖WPF基础库
- 无业务逻辑依赖
- 无Shared层依赖

#### Scenario: 创建通用控件
- **WHEN** 需要创建可复用的UI控件
- **THEN** SHALL 在Controls/Controls/目录创建
- **AND** SHALL 遵循MVVM模式
- **AND** SHALL NOT 包含业务逻辑

#### Scenario: 创建值转换器
- **WHEN** 需要数据绑定转换
- **THEN** SHALL 在Controls/Converters/目录创建
- **AND** SHALL 实现IValueConverter或IMultiValueConverter

#### Scenario: 引用控件库
- **WHEN** 业务模块需要使用通用控件
- **THEN** SHALL 添加对LYBT.Desktop.Controls的项目引用
- **AND** SHALL 在XAML中声明命名空间

---

### Requirement: CLI-008 依赖方向规范

Models层 SHALL 仅依赖Contracts接口层。

**依赖规则**:
```
Contracts (接口定义)
    ↑
Foundation (基础设施)
    ↑
Infrastructure (服务实现)

Controls (UI组件) ← 独立，无业务依赖

Models (ViewModel) → Contracts (仅接口)
```

**禁止的依赖**:
- Models → Infrastructure (直接依赖)
- Controls → Foundation/Infrastructure (业务依赖)

#### Scenario: ViewModel获取服务
- **WHEN** ViewModel需要使用服务
- **THEN** SHALL 通过Contracts中定义的接口注入
- **AND** SHALL NOT 直接引用Infrastructure中的实现类

#### Scenario: 验证依赖关系
- **WHEN** 编译项目
- **THEN** Models.csproj SHALL NOT 包含对Infrastructure的ProjectReference
- **AND** Controls.csproj SHALL NOT 包含对Foundation/Infrastructure的ProjectReference
