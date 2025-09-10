# LYBT.Desktop.Workbench.Core 类与方法文档

**生成日期**: 2025-09-10  
**文档版本**: v1.0  
**项目路径**: src/Client/Desktop/Workbenches/Core/LYBT.Desktop.Workbench.Core.csproj  
**项目版本**: v2.1.0

## 项目概述

LYBT.Desktop.Workbench.Core 是凌隐宝堂中医诊所系统的工作台核心框架，提供基于角色的工作台导航和路由管理功能。该项目采用 UltraThink 架构设计，使用 C# 12 现代化特性，为不同用户角色提供个性化的工作台体验。

### 技术栈
- **.NET 8**: 目标框架 net8.0-windows
- **WPF**: Windows Presentation Foundation UI框架
- **Prism.DryIoc**: MVVM框架和依赖注入
- **C# 12**: 使用现代化语言特性（集合表达式、主构造函数等）

### 架构特点
- **角色导向设计**: 基于用户角色（管理员、医生、前台）提供差异化工作台
- **缓存优化**: 导航项智能缓存，提升性能
- **扩展性**: 支持动态注册新工作台和权限配置
- **UltraThink标准**: 遵循项目统一架构规范

## 目录结构

```
src/Client/Desktop/Workbenches/Core/
├── IWorkbenchNavigator.cs          # 工作台导航器接口
├── NavigationItem.cs               # 导航项模型类
├── IWorkbenchRouter.cs             # 工作台路由器接口
├── WorkbenchRouter.cs              # 工作台路由器实现
└── LYBT.Desktop.Workbench.Core.csproj  # 项目配置文件
```

## 详细类分析

### IWorkbenchNavigator

**位置**: IWorkbenchNavigator.cs:10-62  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: 接口类型  
**用途**: 定义工作台导航器的标准接口，每个工作台实现自己的导航逻辑

#### 方法列表
- **NavigateToAsync(string viewName, NavigationParameters? parameters)**: Task
  - **用途**: 异步导航到指定视图
  - **参数**: viewName - 目标视图名称, parameters - 可选导航参数
  - **返回值**: 返回导航任务
  
- **NavigateToDefaultAsync()**: Task
  - **用途**: 异步导航到默认视图
  - **返回值**: 返回导航任务
  
- **GoBackAsync()**: Task
  - **用途**: 异步返回上一个视图
  - **返回值**: 返回导航任务
  
- **CanNavigateTo(string viewName)**: bool
  - **用途**: 检查是否可以导航到指定视图
  - **参数**: viewName - 目标视图名称
  - **返回值**: 是否可以导航的布尔值
  
- **GetCurrentView()**: string
  - **用途**: 获取当前活动视图名称
  - **返回值**: 当前视图的名称字符串
  
- **ClearHistory()**: void
  - **用途**: 清除导航历史记录
  
- **SetRegion(string regionName)**: void
  - **用途**: 设置导航使用的Prism区域
  - **参数**: regionName - 目标区域名称
  
- **GetRegionName()**: string
  - **用途**: 获取当前导航区域名称
  - **返回值**: 区域名称字符串

### NavigationItem

**位置**: NavigationItem.cs:7-108  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: 普通类  
**用途**: 导航项数据模型，封装导航菜单项的完整信息，支持层次化导航和权限控制

#### 构造函数
- **NavigationItem()**: 初始化导航项，创建空的子项列表、权限列表和参数字典

#### 属性列表
- **Id**: string - 导航项唯一标识符
- **DisplayName**: string - 显示名称
- **Icon**: string - 图标名称或路径
- **ViewName**: string - 导航目标视图名称
- **Module**: string - 所属业务模块
- **Order**: int - 排序顺序
- **IsEnabled**: bool - 是否启用，默认true
- **IsVisible**: bool - 是否可见，默认true
- **Children**: List<NavigationItem> - 子导航项列表
- **RequiredPermissions**: List<string> - 必需的权限列表
- **ToolTip**: string - 工具提示信息
- **Parameters**: Dictionary<string, object> - 导航参数字典
- **IsSeparator**: bool - 是否为分隔符
- **BadgeText**: string - 徽章文本（用于显示数字或状态）
- **BadgeType**: string - 徽章类型（info, warning, error, success）
- **HasChildren**: bool - 只读属性，检查是否有子项

#### 方法列表
- **CreateSeparator()**: NavigationItem [静态]
  - **用途**: 创建导航分隔符项
  - **返回值**: 标记为分隔符的导航项实例
  - **调用关系**: 在WorkbenchRouter中用于创建菜单分隔线

### IWorkbenchRouter

**位置**: IWorkbenchRouter.cs:8-83  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: 接口类型  
**用途**: 工作台路由器接口，管理角色到工作台的映射和导航权限控制

#### 方法列表
- **GetWorkbenchForRole(string role)**: string
  - **用途**: 根据用户角色获取对应的工作台视图名称
  - **参数**: role - 用户角色字符串
  - **返回值**: 工作台视图名称
  
- **CanAccessModule(string role, string module)**: bool
  - **用途**: 检查角色是否可以访问指定模块
  - **参数**: role - 用户角色, module - 模块名称
  - **返回值**: 是否有访问权限
  
- **GetNavigationItems(string role)**: IEnumerable<NavigationItem>
  - **用途**: 获取角色对应的导航项列表
  - **参数**: role - 用户角色
  - **返回值**: 导航项集合
  
- **GetAccessibleModules(string role)**: IEnumerable<string>
  - **用途**: 获取角色可访问的模块列表
  - **参数**: role - 用户角色
  - **返回值**: 可访问的模块名称列表
  
- **GetDefaultView(string workbench)**: string
  - **用途**: 获取工作台的默认视图
  - **参数**: workbench - 工作台名称
  - **返回值**: 默认视图名称
  
- **RegisterWorkbench(string role, string workbench, List<string> modules)**: void
  - **用途**: 动态注册新的工作台配置
  - **参数**: role - 角色名称, workbench - 工作台名称, modules - 可访问模块列表
  
- **GetAllWorkbenches()**: Dictionary<string, string>
  - **用途**: 获取所有已注册的工作台映射
  - **返回值**: 角色到工作台视图的映射字典
  
- **IsWorkbenchRegistered(string workbench)**: bool
  - **用途**: 检查工作台是否已注册
  - **参数**: workbench - 工作台名称
  - **返回值**: 是否已注册
  
- **GetWelcomeMessage(string role, string userName)**: string
  - **用途**: 获取角色的个性化欢迎消息
  - **参数**: role - 用户角色, userName - 用户姓名
  - **返回值**: 欢迎消息字符串
  
- **GetRoleDisplayName(string role)**: string
  - **用途**: 获取角色的中文显示名称
  - **参数**: role - 角色标识
  - **返回值**: 角色显示名称

### WorkbenchRouter

**位置**: WorkbenchRouter.cs:12-481  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: 实现 IWorkbenchRouter 接口  
**用途**: 工作台路由器核心实现，采用UltraThink架构标准，提供基于角色的工作台视图路由、模块权限控制和导航项生成，支持UserRole枚举映射和字符串角色向后兼容

#### 私有字段
- **_workbenchConfigs**: Dictionary<string, WorkbenchConfig> - 工作台配置存储
- **_navigationCache**: Dictionary<string, List<NavigationItem>> - 导航项缓存

#### 构造函数
- **WorkbenchRouter()**: 初始化工作台路由器，配置默认工作台和角色权限映射

#### 公共方法列表
- **GetWorkbenchForRole(string role)**: string
  - **用途**: 根据用户角色获取对应的工作台视图，优先使用UserRole枚举映射
  - **参数**: role - 用户角色字符串
  - **返回值**: 工作台视图名称，默认为诊疗工作台
  - **调用关系**: 调用 WorkbenchPermissionMapper.GetWorkbenchForRole()
  
- **CanAccessModule(string role, string module)**: bool
  - **用途**: 检查用户角色是否可以访问指定模块，支持UserRole枚举和字符串角色双重验证
  - **参数**: role - 用户角色字符串, module - 业务模块名称
  - **返回值**: 是否具有访问权限
  - **调用关系**: 调用 WorkbenchPermissionMapper.CanAccessModule()
  
- **GetNavigationItems(string role)**: IEnumerable<NavigationItem>
  - **用途**: 获取用户角色对应的导航项集合，智能缓存机制提升性能
  - **参数**: role - 用户角色字符串
  - **返回值**: 导航项集合，包含图标、徽章、排序信息
  - **调用关系**: 调用 GenerateNavigationItems() 私有方法
  
- **GetAccessibleModules(string role)**: IEnumerable<string>
  - **用途**: 获取用户角色可访问的模块列表
  - **参数**: role - 用户角色字符串
  - **返回值**: 可访问模块名称集合
  - **调用关系**: 调用 WorkbenchPermissionMapper.GetAccessibleModules()
  
- **GetDefaultView(string workbench)**: string
  - **用途**: 获取工作台的默认视图
  - **参数**: workbench - 工作台名称
  - **返回值**: 默认视图名称
  
- **RegisterWorkbench(string role, string workbench, List<string> modules)**: void
  - **用途**: 注册工作台配置，支持动态工作台管理
  - **参数**: role - 用户角色名称, workbench - 工作台视图名称, modules - 可访问模块列表
  - **调用关系**: 清除导航缓存以确保配置更新
  
- **GetAllWorkbenches()**: Dictionary<string, string>
  - **用途**: 获取所有已注册的工作台映射
  - **返回值**: 角色名称到工作台视图的映射字典
  
- **IsWorkbenchRegistered(string workbench)**: bool
  - **用途**: 检查工作台是否已注册
  - **参数**: workbench - 工作台视图名称
  - **返回值**: 是否已注册
  
- **GetWelcomeMessage(string role, string userName)**: string
  - **用途**: 获取用户欢迎消息
  - **参数**: role - 用户角色字符串, userName - 用户姓名
  - **返回值**: 个性化欢迎消息
  - **调用关系**: 调用 WorkbenchPermissionMapper.GetWelcomeMessage()
  
- **GetRoleDisplayName(string role)**: string
  - **用途**: 获取角色显示名称
  - **参数**: role - 用户角色字符串
  - **返回值**: 角色的中文显示名称
  - **调用关系**: 调用 WorkbenchPermissionMapper.GetRoleDisplayName()

#### 私有方法列表
- **InitializeDefaultWorkbenches()**: void
  - **用途**: 初始化默认工作台配置，使用C# 12集合表达式
  - **调用关系**: 在构造函数中调用
  
- **GenerateNavigationItems(string role)**: List<NavigationItem>
  - **用途**: 根据角色生成对应的导航项列表，使用C# 12模式匹配
  - **参数**: role - 用户角色字符串
  - **返回值**: 导航项列表
  - **调用关系**: 调用角色特定的导航项生成方法
  
- **GetAdminNavigationItems()**: IEnumerable<NavigationItem>
  - **用途**: 获取管理员角色导航项，包含8个核心业务模块完整权限
  - **返回值**: 管理员导航项集合
  
- **GetDoctorNavigationItems()**: IEnumerable<NavigationItem>
  - **用途**: 获取医生角色导航项，核心诊疗功能导航
  - **返回值**: 医生导航项集合
  
- **GetReceptionNavigationItems()**: IEnumerable<NavigationItem>
  - **用途**: 获取前台接待角色导航项，基础接待功能
  - **返回值**: 前台接待导航项集合

### WorkbenchRouter.WorkbenchConfig

**位置**: WorkbenchRouter.cs:463-480  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: sealed class  
**用途**: 工作台配置内部类，封装角色-工作台-模块的映射配置信息

#### 属性列表
- **Role**: string - 用户角色名称（init-only）
- **WorkbenchView**: string - 工作台视图名称（init-only）
- **AccessibleModules**: List<string> - 可访问的业务模块列表（init-only）

### WorkbenchPermissionMapper

**位置**: WorkbenchRouter.cs:487-669  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: 静态类  
**用途**: UserRole到工作台权限映射器，支持UserRole枚举到工作台的正确映射

#### 静态字段
- **UserRoleWorkbenchMap**: Dictionary<UserRole, WorkbenchPermission> - UserRole到工作台的映射关系

#### 静态方法列表
- **GetWorkbenchForRole(UserRole role)**: string [静态]
  - **用途**: 根据UserRole获取工作台视图名称
  - **参数**: role - 用户角色枚举
  - **返回值**: 工作台视图名称，默认为诊疗工作台
  
- **CanAccessModule(UserRole role, string module)**: bool [静态]
  - **用途**: 检查用户角色是否可以访问指定模块
  - **参数**: role - 用户角色枚举, module - 模块名称
  - **返回值**: 是否有访问权限
  
- **GetAccessibleModules(UserRole role)**: IEnumerable<string> [静态]
  - **用途**: 获取用户角色可访问的所有模块
  - **参数**: role - 用户角色枚举
  - **返回值**: 可访问的模块列表
  
- **GetRoleDisplayName(UserRole role)**: string [静态]
  - **用途**: 获取角色显示名称
  - **参数**: role - 用户角色枚举
  - **返回值**: 角色显示名称
  
- **GetWelcomeMessage(UserRole role, string userName)**: string [静态]
  - **用途**: 获取个性化欢迎消息
  - **参数**: role - 用户角色枚举, userName - 用户姓名
  - **返回值**: 欢迎消息字符串
  
- **GetAllWorkbenchMappings()**: Dictionary<UserRole, string> [静态]
  - **用途**: 获取所有支持的角色工作台映射
  - **返回值**: 角色到工作台的映射字典
  
- **HasManagementAccess(UserRole role)**: bool [静态]
  - **用途**: 检查角色是否有管理权限
  - **参数**: role - 用户角色枚举
  - **返回值**: 是否有管理权限（仅Admin）
  
- **HasMedicalAccess(UserRole role)**: bool [静态]
  - **用途**: 检查角色是否有医疗权限
  - **参数**: role - 用户角色枚举
  - **返回值**: 是否有医疗权限（Doctor或Admin）
  
- **ConvertToLegacyRoleString(UserRole role)**: string [静态]
  - **用途**: 从UserRole枚举转换为旧版字符串角色（向后兼容）
  - **参数**: role - UserRole枚举
  - **返回值**: 字符串角色名称
  
- **ConvertFromLegacyRoleString(string roleString)**: UserRole [静态]
  - **用途**: 从字符串角色转换为UserRole枚举
  - **参数**: roleString - 字符串角色名称
  - **返回值**: UserRole枚举

### WorkbenchPermission

**位置**: WorkbenchRouter.cs:675-698  
**命名空间**: LYBT.Desktop.Workbench.Core  
**继承关系**: sealed class  
**用途**: 工作台权限配置类，封装角色权限和工作台配置信息

#### 属性列表
- **WorkbenchView**: string - 工作台视图名称（init-only）
- **AccessibleModules**: List<string> - 可访问的模块列表（init-only）
- **DisplayName**: string - 角色显示名称（init-only）
- **WelcomeTemplate**: string - 欢迎消息模板，使用{0}占位符表示用户名（init-only）

## 架构特点

### UltraThink架构标准
1. **现代化C# 12特性**: 使用集合表达式、模式匹配、主构造函数等现代语言特性
2. **企业级权限管理**: 基于角色的访问控制，支持动态权限配置
3. **缓存优化**: 导航项智能缓存机制，提升系统性能
4. **向后兼容**: 支持UserRole枚举和字符串角色的双重映射

### 设计模式
1. **策略模式**: 不同角色采用不同的导航项生成策略
2. **工厂模式**: NavigationItem.CreateSeparator()静态工厂方法
3. **映射器模式**: WorkbenchPermissionMapper提供角色到权限的映射
4. **缓存模式**: 导航项缓存减少重复计算

### 角色权限体系
1. **管理员**: 完整的系统管理权限，可访问所有8个核心业务模块
2. **医生**: 核心诊疗功能，包括看诊、患者档案、处方管理等
3. **前台**: 基础接待功能，主要负责患者建档和就诊记录查看

## 技术要点

### 关键技术实现
1. **枚举映射**: UserRole枚举与字符串角色的双向转换
2. **缓存策略**: TryGetValue模式优化缓存访问性能
3. **模式匹配**: C# 12 switch表达式简化角色判断逻辑
4. **集合表达式**: 使用[...]语法简化集合初始化
5. **空值安全**: 使用ArgumentException.ThrowIfNullOrWhiteSpace进行参数验证

### 扩展性设计
1. **动态注册**: 支持运行时注册新的工作台配置
2. **插件化**: 通过IWorkbenchNavigator接口支持多种导航实现
3. **配置化**: 角色权限配置与业务逻辑分离，便于维护

### 性能优化
1. **缓存机制**: 导航项首次生成后缓存，避免重复计算
2. **延迟计算**: 导航项按需生成，不预先加载所有角色配置
3. **内存优化**: 使用值类型属性和只读集合减少内存分配