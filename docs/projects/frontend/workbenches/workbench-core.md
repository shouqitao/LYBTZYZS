# LYBT.Desktop.Workbench.Core 工作台核心项目文档

## 项目概览

**项目名称**: LYBT.Desktop.Workbench.Core  
**项目类型**: WPF工作台基础设施库  
**技术框架**: .NET 8.0, WPF, Prism.DryIoc 8.1.97  
**业务领域**: 基于角色的工作台导航和路由系统  
**更新时间**: 2025-09-01

## 项目定位

### 核心功能
LYBT.Desktop.Workbench.Core是整个工作台系统的基础设施，负责：

1. **角色到工作台映射**: 根据用户角色自动路由到对应工作台
2. **导航基础设施**: 提供工作台内部导航和模块访问控制
3. **权限管理**: 基于角色的模块和功能访问控制
4. **导航项模型**: 统一的导航菜单数据结构
5. **工作台注册**: 动态工作台注册和管理机制

### 架构角色
- **路由中心**: 统一管理角色与工作台的映射关系
- **导航基础**: 为所有工作台提供导航功能基类和接口
- **权限控制**: 实现基于角色的功能访问控制
- **扩展支持**: 支持动态注册新角色和工作台

## 技术架构

### 核心依赖
```xml
<PackageReference Include="Prism.DryIoc" Version="8.1.97" />
<ProjectReference Include="..\..\Core\LYBT.Desktop.Core.csproj" />
```

### 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <AssemblyName>LYBT.Desktop.Workbench.Core</AssemblyName>
</PropertyGroup>
```

## 核心组件设计

### IWorkbenchRouter - 工作台路由器接口

#### 核心职责
```csharp
public interface IWorkbenchRouter
{
    /// <summary>根据用户角色获取对应的工作台视图名称</summary>
    string GetWorkbenchForRole(string role);
    
    /// <summary>检查角色是否可以访问指定模块</summary>
    bool CanAccessModule(string role, string module);
    
    /// <summary>获取角色对应的导航项列表</summary>
    IEnumerable<NavigationItem> GetNavigationItems(string role);
    
    /// <summary>获取角色可访问的模块列表</summary>
    IEnumerable<string> GetAccessibleModules(string role);
    
    /// <summary>注册新的工作台</summary>
    void RegisterWorkbench(string role, string workbench, List<string> modules);
    
    /// <summary>获取角色的欢迎消息</summary>
    string GetWelcomeMessage(string role, string userName);
}
```

#### 角色映射标准
基于项目README.md的映射关系：
- **Administrator** → SystemWorkbench (系统管理工作台)
- **Doctor** → ConsultationWorkbench (诊疗工作台)
- **Reception** → ReceptionWorkbench (接待工作台)
- **Cashier** → CashierWorkbench (收银工作台)
- **Pharmacist** → PharmacistWorkbench (药师工作台)
- **Therapist** → TherapistWorkbench (治疗师工作台)

### NavigationItem - 导航项模型

#### 完整数据结构
```csharp
public class NavigationItem
{
    /// <summary>导航项ID</summary>
    public string Id { get; set; }
    
    /// <summary>显示名称</summary>
    public string DisplayName { get; set; }
    
    /// <summary>图标名称或路径</summary>
    public string Icon { get; set; }
    
    /// <summary>导航目标视图名称</summary>
    public string ViewName { get; set; }
    
    /// <summary>所属模块</summary>
    public string Module { get; set; }
    
    /// <summary>排序顺序</summary>
    public int Order { get; set; }
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>子导航项</summary>
    public List<NavigationItem> Children { get; set; }
    
    /// <summary>必需的权限</summary>
    public List<string> RequiredPermissions { get; set; }
    
    /// <summary>徽章文本（状态提示）</summary>
    public string BadgeText { get; set; }
    
    /// <summary>徽章类型</summary>
    public string BadgeType { get; set; }
    
    /// <summary>工具提示</summary>
    public string ToolTip { get; set; }
    
    /// <summary>导航参数</summary>
    public Dictionary<string, object> Parameters { get; set; }
    
    /// <summary>是否为分隔符</summary>
    public bool IsSeparator { get; set; }
}
```

#### 特殊功能
```csharp
/// <summary>创建分隔符</summary>
public static NavigationItem CreateSeparator()
{
    return new NavigationItem
    {
        IsSeparator = true,
        Id = Guid.NewGuid().ToString()
    };
}

/// <summary>检查是否有子项</summary>
public bool HasChildren => Children != null && Children.Count > 0;
```

### IWorkbenchNavigator - 工作台导航器基接口

#### 核心导航方法
```csharp
public interface IWorkbenchNavigator
{
    /// <summary>导航到指定视图</summary>
    void NavigateToView(string viewName);
    
    /// <summary>导航到指定区域的视图</summary>
    void NavigateToView(string regionName, string viewName);
}
```

## 架构设计模式

### 1. 角色驱动导航模式
```csharp
// 用户登录后，系统根据角色自动选择工作台
var workbench = workbenchRouter.GetWorkbenchForRole(user.Role);
regionManager.RequestNavigate("MainRegion", workbench);
```

### 2. 权限控制模式
```csharp
// 导航前检查权限
if (workbenchRouter.CanAccessModule(user.Role, "Patients"))
{
    navigator.NavigateToPatients();
}
```

### 3. 动态工作台注册模式
```csharp
// 支持运行时动态添加新工作台
workbenchRouter.RegisterWorkbench(
    role: "Nurse", 
    workbench: "NursingWorkbench",
    modules: ["Patients", "Consultations"]
);
```

### 4. 层次化导航模式
```csharp
// 支持多级导航菜单
var navigation = new NavigationItem 
{
    DisplayName = "患者管理",
    Children = new List<NavigationItem> 
    {
        new() { DisplayName = "患者列表", ViewName = "PatientListView" },
        new() { DisplayName = "患者统计", ViewName = "PatientStatsView" }
    }
};
```

## 工作台系统集成

### 具体工作台实现
系统包含以下具体工作台实现：

1. **ConsultationWorkbench** - 诊疗工作台 (医生角色)
   - 患者管理、看诊管理、医疗案例、处方开具、验方管理
   
2. **SystemWorkbench** - 系统管理工作台 (管理员角色)
   - 用户管理、系统配置、数据统计、系统监控

3. **CashierWorkbench** - 收银工作台 (收银员角色)
   - 费用结算、收费管理、财务统计

4. **PharmacistWorkbench** - 药师工作台 (药师角色)
   - 药材管理、处方审核、库存监控

5. **ReceptionistWorkbench** - 接待工作台 (接待员角色)
   - 患者接待、预约管理、基础登记

6. **TherapistWorkbench** - 治疗师工作台 (治疗师角色)
   - 治疗计划、康复管理

### 区域管理
每个工作台使用标准区域结构：
```csharp
public static class WorkbenchRegions
{
    public const string MainContentRegion = "{WorkbenchName}MainContentRegion";
    public const string NavigationRegion = "{WorkbenchName}NavigationRegion";
    public const string StatusRegion = "{WorkbenchName}StatusRegion";
}
```

## 使用模式

### 1. 工作台模块注册
```csharp
public class CustomWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册工作台导航器
        containerRegistry.RegisterSingleton<ICustomWorkbenchNavigator, CustomWorkbenchNavigator>();
        
        // 注册主视图
        containerRegistry.RegisterForNavigation<CustomWorkbenchMainView>();
    }
}
```

### 2. 自定义导航器实现
```csharp
public class CustomWorkbenchNavigator : IWorkbenchNavigator
{
    private readonly IRegionManager _regionManager;
    
    public void NavigateToCustomView()
    {
        _regionManager.RequestNavigate("ContentRegion", "CustomView");
    }
}
```

### 3. 权限检查集成
```csharp
public class SecureNavigationService
{
    public void SecureNavigate(string role, string module, string view)
    {
        if (_workbenchRouter.CanAccessModule(role, module))
        {
            _navigator.NavigateToView(view);
        }
        else
        {
            ShowAccessDeniedMessage();
        }
    }
}
```

## 性能优化

### 1. 导航项缓存
- 角色导航项在首次加载后缓存
- 权限检查结果缓存，避免重复计算

### 2. 延迟加载
- 工作台模块按需加载
- 导航项树按需展开

### 3. 内存管理
- 使用弱引用管理导航事件
- 及时清理未使用的导航项

## 扩展指南

### 添加新工作台
1. 创建工作台项目，继承 Workbench.Core
2. 实现 IWorkbenchNavigator 接口
3. 在 WorkbenchRouter 中注册角色映射
4. 创建工作台主视图和 ViewModel

### 添加新角色
1. 在 WorkbenchRouter 中注册新角色
2. 定义角色可访问的模块列表
3. 创建角色专用的导航项配置
4. 实现角色特定的欢迎消息

### 自定义导航项
1. 扩展 NavigationItem 类添加新属性
2. 更新导航模板支持新功能
3. 在具体工作台中配置导航项

## 质量保证

### 单元测试覆盖
- 角色到工作台映射测试
- 权限检查逻辑测试
- 导航项创建和管理测试
- 动态注册功能测试

### 集成测试
- 完整工作台切换流程测试
- 跨模块导航测试
- 权限控制集成测试

## 维护说明

### 版本兼容性
- 新增工作台保持向后兼容
- 导航项结构变更需要迁移策略
- 角色权限变更需要数据库同步

### 错误处理
- 无效角色处理：返回默认工作台
- 权限不足处理：显示友好提示
- 导航失败处理：回退到安全视图

---

**版本**: v1.0  
**维护**: UltraThink开发团队  
**更新**: 2025-09-01