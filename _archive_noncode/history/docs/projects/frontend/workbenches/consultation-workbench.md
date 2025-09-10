# LYBT.Desktop.Workbench.Consultation 诊疗工作台项目文档

## 项目概览

**项目名称**: LYBT.Desktop.Workbench.Consultation  
**项目类型**: WPF医生专用工作台  
**技术框架**: .NET 8.0, WPF, Prism.DryIoc 8.1.97  
**业务领域**: 医生诊疗流程管理专用界面  
**更新时间**: 2025-09-01

## 项目定位

### 核心功能
LYBT.Desktop.Workbench.Consultation是专为医生角色设计的诊疗工作台，提供：

1. **完整诊疗流程**: 从患者接诊到处方开具的全流程管理
2. **专业导航体验**: 基于医生工作习惯的界面布局和功能导航
3. **验方集成**: UltraThink Phase 3.4集成验方管理功能
4. **模块协作**: 与患者、医案、诊疗、处方模块深度集成
5. **工作流优化**: 为中医诊疗流程特别优化的工作界面

### 业务场景
- **日常门诊**: 医生进行患者接诊、诊断、开方的主要工作界面
- **病历管理**: 查看和管理患者历史病历和医疗案例
- **处方开具**: 基于验方模板快速开具个性化处方
- **验方管理**: 管理个人验方库，提升诊疗效率
- **诊疗统计**: 查看个人诊疗数据和统计报表

## 技术架构

### 核心依赖
```xml
<PackageReference Include="Prism.DryIoc" Version="8.1.97" />
<ProjectReference Include="..\Core\LYBT.Desktop.Workbench.Core.csproj" />
<ProjectReference Include="..\..\Core\LYBT.Desktop.Core.csproj" />
<ProjectReference Include="..\..\Services\LYBT.Desktop.Services.csproj" />
<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
<!-- UltraThink Phase 3.4: 集成Formula模块功能 -->
<ProjectReference Include="..\..\Modules\Formula\LYBT.Desktop.Formula.csproj" />
```

### 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <AssemblyName>LYBT.Desktop.Workbench.Consultation</AssemblyName>
    <RootNamespace>LYBT.Desktop.Workbench.Consultation</RootNamespace>
</PropertyGroup>
```

## 核心组件架构

### ConsultationWorkbenchModule - 模块定义

#### Prism模块注册
```csharp
public class ConsultationWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册工作台导航器
        containerRegistry.RegisterSingleton<IConsultationWorkbenchNavigator, ConsultationWorkbenchNavigator>();
        
        // 注册主视图
        containerRegistry.RegisterForNavigation<ConsultationWorkbenchMainView>();
        
        // UltraThink Phase 3.4: 注册集成的验方管理功能
        containerRegistry.RegisterForNavigation<FormulaManagementView>();
    }
}
```

#### ViewModel映射
```csharp
public void OnInitialized(IContainerProvider containerProvider)
{
    ViewModelLocationProvider.Register<ConsultationWorkbenchMainView, ConsultationWorkbenchMainViewModel>();
}
```

### IConsultationWorkbenchNavigator - 专业导航接口

#### 核心导航方法
```csharp
public interface IConsultationWorkbenchNavigator
{
    /// <summary>导航到患者管理（查看和快速注册）</summary>
    void NavigateToPatients();
    
    /// <summary>导航到看诊管理</summary>
    void NavigateToConsultations();
    
    /// <summary>导航到医疗案例管理</summary>
    void NavigateToMedicalCases();
    
    /// <summary>导航到处方管理</summary>
    void NavigateToPrescriptions();
    
    /// <summary>导航到验方模板（供医生参考使用）</summary>
    void NavigateToFormulas();
    
    /// <summary>导航到个人设置</summary>
    void NavigateToPersonalSettings();
    
    /// <summary>通用导航方法</summary>
    void NavigateToView(string viewName);
    
    /// <summary>指定区域导航方法</summary>
    void NavigateToView(string regionName, string viewName);
}
```

### ConsultationWorkbenchNavigator - 导航实现

#### Prism区域导航
```csharp
public class ConsultationWorkbenchNavigator : IConsultationWorkbenchNavigator
{
    private readonly IRegionManager _regionManager;
    private const string ContentRegion = RegionNames.ConsultationWorkbenchContentRegion;
    
    public void NavigateToPatients()
    {
        NavigateToView("PatientManagementView");
    }
    
    public void NavigateToConsultations()
    {
        NavigateToView("ConsultationManagementView");
    }
    
    public void NavigateToMedicalCases()
    {
        NavigateToView("MedicalCaseManagementView");
    }
    
    public void NavigateToPrescriptions()
    {
        NavigateToView("PrescriptionManagementView");
    }
    
    public void NavigateToFormulas()
    {
        NavigateToView("FormulaManagementView");
    }
}
```

#### 导航实现细节
- **ContentRegion**: 使用专用内容区域`ConsultationWorkbenchContentRegion`
- **视图路由**: 直接路由到业务模块提供的管理视图
- **错误处理**: 导航失败时的回退机制
- **参数传递**: 支持导航时传递上下文参数

## 诊疗工作流集成

### 1. 患者管理集成
```csharp
// 快速患者注册和查看
public void NavigateToPatients()
{
    // 路由到患者管理模块的PatientManagementView
    // 支持患者列表查看、快速注册、基本信息编辑
    NavigateToView("PatientManagementView");
}
```

### 2. 诊疗流程管理
```csharp
// 诊疗记录和四诊管理
public void NavigateToConsultations()
{
    // 路由到诊疗模块的ConsultationManagementView
    // 支持四诊记录（望闻问切）、辨证论治
    NavigateToView("ConsultationManagementView");
}
```

### 3. 医疗案例管理
```csharp
// 完整病历和诊疗案例
public void NavigateToMedicalCases()
{
    // 路由到医疗案例模块的MedicalCaseManagementView
    // 支持完整病历查看、历史案例对比
    NavigateToView("MedicalCaseManagementView");
}
```

### 4. 处方开具功能
```csharp
// 智能处方开具
public void NavigateToPrescriptions()
{
    // 路由到处方模块的PrescriptionManagementView
    // 支持智能配伍、价格计算、处方打印
    NavigateToView("PrescriptionManagementView");
}
```

### 5. 验方模板集成 (UltraThink Phase 3.4)
```csharp
// 验方管理和应用
public void NavigateToFormulas()
{
    // 路由到验方模块的FormulaManagementView
    // 支持个人验方库、经典验方应用
    NavigateToView("FormulaManagementView");
}
```

## 项目结构

### 目录组织
```
src/Client/Desktop/Workbenches/ConsultationWorkbench/
├── ConsultationWorkbenchModule.cs          # Prism模块定义
├── Navigation/                             # 导航相关
│   └── IConsultationWorkbenchNavigator.cs # 导航接口
├── Services/                               # 服务实现
│   └── ConsultationWorkbenchNavigator.cs  # 导航服务
├── ViewModels/                             # 视图模型
│   └── ConsultationWorkbenchMainViewModel.cs
└── Views/                                  # 视图文件
    └── ConsultationWorkbenchMainView.xaml
```

### 区域定义
```csharp
public static class ConsultationWorkbenchRegions
{
    public const string ContentRegion = "ConsultationWorkbenchContentRegion";
    public const string NavigationRegion = "ConsultationWorkbenchNavigationRegion"; 
    public const string StatusRegion = "ConsultationWorkbenchStatusRegion";
    public const string QuickActionsRegion = "ConsultationWorkbenchQuickActionsRegion";
}
```

## 用户体验设计

### 1. 医生工作流优化
- **快速导航**: 常用功能放在显眼位置
- **工作流顺序**: 按照诊疗自然流程排列功能
- **快捷操作**: 提供键盘快捷键和右键菜单

### 2. 界面布局
```
┌─────────────────────────────────────────────┐
│ 诊疗工作台 - Dr. 张医生                          │
├─────────────────────────────────────────────┤
│ [患者管理] [看诊管理] [医疗案例] [处方管理] [验方模板] │
├─────────────────────────────────────────────┤
│                                           │
│            主要内容区域                        │
│        (各个模块的管理界面)                      │
│                                           │
├─────────────────────────────────────────────┤
│ 状态栏: 当前患者 | 今日诊疗数 | 时间                │
└─────────────────────────────────────────────┘
```

### 3. 快速操作面板
- **今日预约**: 显示今日预约患者列表
- **快速开方**: 基于常用验方快速开具处方
- **患者搜索**: 快速查找历史患者
- **诊疗统计**: 显示个人诊疗数据概览

## 业务模块集成

### 集成的业务模块
1. **患者模块 (Patients)**: 患者信息管理和快速注册
2. **诊疗模块 (Consultation)**: 四诊记录和辨证论治
3. **医疗案例模块 (MedicalCase)**: 完整病历和案例管理
4. **处方模块 (Prescriptions)**: 智能处方开具和打印
5. **验方模块 (Formula)**: 个人和经典验方管理

### 模块协作模式
```csharp
// 诊疗工作流：患者 → 医案 → 诊断 → 处方
public class ConsultationWorkflow
{
    // 1. 选择或注册患者
    public void StartConsultation(PatientDto patient)
    
    // 2. 创建医疗案例
    public void CreateMedicalCase(Guid patientId)
    
    // 3. 记录诊疗信息
    public void RecordConsultation(Guid medicalCaseId)
    
    // 4. 开具处方
    public void CreatePrescription(Guid consultationId, FormulaDto template)
}
```

## 性能优化

### 1. 模块延迟加载
- 各业务模块按需加载，减少启动时间
- 导航时才初始化对应的视图和服务

### 2. 数据缓存策略
- 患者基础信息缓存
- 常用验方模板缓存
- 个人设置和偏好缓存

### 3. 界面响应优化
- 异步加载大数据量列表
- 虚拟化长列表控件
- 智能刷新，避免全量更新

## 安全和权限

### 1. 角色权限控制
```csharp
// 只有医生角色可以访问诊疗工作台
[RequiredRole("Doctor")]
public class ConsultationWorkbenchMainView
```

### 2. 数据访问控制
- 只能查看本人负责的患者
- 诊疗记录的查看和修改权限
- 处方开具的电子签名验证

### 3. 审计日志
- 记录所有诊疗操作
- 处方开具和修改日志
- 患者信息访问记录

## 扩展和定制

### 1. 自定义导航菜单
- 支持医生个人偏好设置
- 可配置的快捷操作按钮
- 个性化工作台布局

### 2. 插件扩展支持
- 支持第三方插件集成
- 自定义报表和统计功能
- 外部系统数据导入

### 3. 多语言支持
- 界面文本国际化
- 中医术语标准化
- 多地区诊疗习惯适配

## 测试策略

### 1. 单元测试
- 导航逻辑测试
- 业务模块集成测试
- 权限控制测试

### 2. 用户界面测试
- 诊疗工作流测试
- 导航功能测试
- 响应性能测试

### 3. 集成测试
- 与其他模块的协作测试
- 数据一致性测试
- 并发操作测试

## 维护说明

### 版本更新
- 保持与业务模块版本同步
- 向后兼容性保证
- 渐进式功能升级

### 性能监控
- 导航响应时间监控
- 模块加载性能统计
- 用户操作行为分析

### 错误处理
- 模块加载失败处理
- 网络异常恢复机制
- 用户友好的错误提示

---

**版本**: v1.0  
**维护**: UltraThink开发团队  
**更新**: 2025-09-01