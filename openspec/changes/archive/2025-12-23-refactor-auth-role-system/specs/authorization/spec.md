# Authorization Spec Delta

## ADDED Requirements

### Requirement: 可扩展角色注册机制
系统 SHALL 支持通过配置和接口定义新角色，无需修改核心代码。

#### Scenario: 角色注册
- **WHEN** 应用程序启动
- **THEN** 系统从配置和代码中加载所有 `IRoleDefinition` 实现
- **AND** 注册到 `RoleRegistry` 服务

#### Scenario: 角色定义查询
- **WHEN** 系统需要获取角色的模块列表或首页视图
- **THEN** 从 `RoleRegistry` 查询对应的 `IRoleDefinition`
- **AND** 返回角色配置信息

#### Scenario: 配置驱动的角色属性
- **WHEN** 角色的显示名称、启用状态或模块列表需要调整
- **THEN** 通过修改配置文件完成
- **AND** 无需重新编译代码

### Requirement: Receptionist角色
系统 SHALL 支持前台/挂号角色（Receptionist），作为可扩展角色的模板实现。

#### Scenario: Receptionist登录
- **WHEN** Receptionist角色用户登录
- **THEN** 系统加载 `ReceptionistModule`
- **AND** 导航到 `ReceptionistHomeView`

#### Scenario: Receptionist工作台
- **WHEN** Receptionist用户进入工作台
- **THEN** 显示"功能开发中"的占位界面
- **AND** 提供基本的导航和退出功能

#### Scenario: Receptionist权限
- **WHEN** Receptionist用户尝试访问功能
- **THEN** 仅允许访问已授权的只读功能
- **AND** 拒绝未授权的操作

### Requirement: 动态模块加载
系统 MUST 根据用户角色动态加载对应的功能模块，而非硬编码。

#### Scenario: 基于角色的模块加载
- **WHEN** 用户登录成功
- **THEN** 系统从 `RoleRegistry` 获取该角色的 `RequiredModules`
- **AND** 按配置顺序加载模块

#### Scenario: 模块加载失败处理
- **WHEN** 某个模块加载失败
- **THEN** 记录错误日志
- **AND** 继续加载其他模块
- **AND** 用户仍可使用已加载的功能

### Requirement: 统一权限网关
系统 MUST 通过 `IPermissionGateway` 集中处理所有权限检查。

#### Scenario: 权限检查
- **WHEN** ViewModel 需要检查用户是否有特定权限
- **THEN** 调用 `IPermissionGateway.HasPermission(permission)`
- **AND** 返回布尔结果

#### Scenario: 角色权限检查
- **WHEN** 需要检查用户是否属于特定角色
- **THEN** 调用 `IPermissionGateway.IsInRole(role)`
- **AND** 返回布尔结果

#### Scenario: 权限驱动的UI
- **WHEN** UI 需要根据权限显示或隐藏元素
- **THEN** 绑定到 `PermissionGateway` 的检查结果
- **AND** 无权限时自动隐藏或禁用

## MODIFIED Requirements

### Requirement: UserRole枚举
系统的用户角色枚举 MUST 包含所有支持的角色类型。

#### Scenario: 角色枚举定义
- **WHEN** 系统定义 UserRole 枚举
- **THEN** 包含以下值:
  - `Receptionist = 0`（前台/挂号）
  - `Doctor = 1`（医生）
  - `Admin = 10`（管理员）
  - `SuperAdmin = 100`（超级管理员）

#### Scenario: 角色权限层级
- **WHEN** 比较角色权限
- **THEN** 数值越大权限越高
- **AND** 高权限角色包含低权限角色的所有功能

### Requirement: 角色导航服务
`RoleNavigationService` MUST 从 `RoleRegistry` 获取角色的首页视图。

#### Scenario: 获取角色首页
- **WHEN** 用户登录后需要导航到首页
- **THEN** 从 `RoleRegistry` 查询当前角色的 `HomeViewName`
- **AND** 导航到对应视图

#### Scenario: 未知角色处理
- **WHEN** 角色未在 `RoleRegistry` 中注册
- **THEN** 返回默认首页视图
- **AND** 记录警告日志

### Requirement: 模块加载配置
`ApplicationBootstrapper` MUST 支持配置驱动的模块加载，替代硬编码。

#### Scenario: 配置文件格式
- **WHEN** 系统读取角色配置
- **THEN** 支持以下配置格式:
```json
{
  "Roles": {
    "Receptionist": {
      "Enabled": true,
      "DisplayName": "前台",
      "HomeView": "ReceptionistHomeView",
      "Modules": ["PatientsModule"]
    }
  }
}
```

#### Scenario: 模块加载顺序
- **WHEN** 加载角色模块
- **THEN** 按配置中定义的顺序加载
- **AND** 确保依赖模块先于依赖者加载
