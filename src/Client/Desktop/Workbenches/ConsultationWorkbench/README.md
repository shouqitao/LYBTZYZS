# LYBT.Desktop.Workbench.Consultation

凌隐宝堂中医诊所系统 - 诊疗工作台模块

## 🎯 项目概述

诊疗工作台是专为医生设计的综合性诊疗环境，提供中医诊疗流程的完整支持，包括患者管理、四诊记录、辨证论治、处方开具等核心功能。采用现代化WPF界面和Prism MVVM架构。

## 目录结构

```
ConsultationWorkbench/
├── Navigation/                         # 导航服务
│   └── IConsultationWorkbenchNavigator.cs  # 工作台导航接口
├── Services/                          # 业务服务
│   └── ConsultationWorkbenchNavigator.cs   # 导航实现
├── ViewModels/                        # 视图模型
│   └── ConsultationWorkbenchMainViewModel.cs # 主工作台视图模型
├── Views/                            # 用户界面
│   ├── ConsultationWorkbenchMainView.xaml     # 主工作台视图
│   └── ConsultationWorkbenchMainView.xaml.cs  # 视图代码后置
└── ConsultationWorkbenchModule.cs    # Prism模块定义
```

## 核心功能

### 1. 诊疗工作台主界面
- **统一导航**: 集成患者管理、诊断记录、处方开具等功能模块
- **快速操作**: 提供常用诊疗操作的快捷入口
- **状态显示**: 实时显示当前诊疗状态和进度

### 2. 患者诊疗流程
整合8个核心业务模块，提供完整的中医诊疗流程：

#### 患者接待阶段
- **患者档案管理**: 基础信息录入、历史病历查看
- **医疗案例创建**: 为新患者创建诊疗案例容器
- **预约管理**: 处理患者预约和排队信息

#### 中医四诊阶段 
- **望诊记录**: 观察患者面色、舌象、神态等外在表现
- **闻诊记录**: 记录声音、气味等信息
- **问诊记录**: 主诉、现病史、既往史、家族史等详细问诊
- **切诊记录**: 脉象、腹诊等手法检查结果

#### 辨证论治阶段
- **症状分析**: 基于四诊信息进行中医辨证分析
- **证候诊断**: 确定中医证候和治疗原则
- **治疗方案**: 制定个性化的中医治疗方案

#### 处方开具阶段
- **验方选择**: 从经典验方库中选择合适的基础方剂
- **个性化调整**: 根据患者具体情况调整药物组合
- **配伍检查**: 自动检查药物配伍禁忌
- **价格计算**: 实时计算处方总价和单味药价格

### 3. 集成业务模块
诊疗工作台无缝集成以下业务模块：

- **患者管理** (LYBT.Desktop.Patients): 患者档案、联系信息管理
- **诊断记录** (LYBT.Desktop.Consultation): 中医四诊数据录入
- **医疗案例** (LYBT.Desktop.MedicalCase): 诊疗流程管理容器 
- **处方管理** (LYBT.Desktop.Prescriptions): 处方开具和管理
- **验方模板** (LYBT.Desktop.Formula): 经典验方和个人验方
- **中药材库** (LYBT.Desktop.Herbs): 药材信息和价格管理

## 技术架构

### 框架技术栈
- **.NET 8.0-windows**: 现代.NET平台
- **WPF**: Windows桌面应用程序框架
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入（DI）
- **LYBT.Desktop.Core**: 桌面应用程序核心框架

### 设计模式
- **MVVM模式**: 视图-视图模型-模型分离
- **依赖注入（DI）**: 使用DryIoc容器管理依赖关系
- **导航模式**: 统一的页面导航和状态管理
- **模块化架构**: Prism模块化应用程序结构

## 导航系统

### IConsultationWorkbenchNavigator
诊疗工作台专用导航接口，提供工作台内部的页面导航功能：

```csharp
public interface IConsultationWorkbenchNavigator
{
    // 导航到患者管理
    Task NavigateToPatientManagementAsync();
    
    // 导航到诊断记录
    Task NavigateToConsultationAsync(Guid patientId);
    
    // 导航到处方开具
    Task NavigateToPrescriptionAsync(Guid consultationId);
    
    // 导航到验方管理
    Task NavigateToFormulaManagementAsync();
}
```

### 导航实现特性
- **上下文传递**: 支持在页面间传递诊疗上下文信息
- **状态保持**: 保持页面状态，支持快速切换
- **权限控制**: 基于用户角色控制可访问的功能模块

## 模块注册

### ConsultationWorkbenchModule
Prism模块定义，负责工作台的初始化和服务注册：

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
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册ViewModel映射
        ViewModelLocationProvider.Register<ConsultationWorkbenchMainView, ConsultationWorkbenchMainViewModel>();
    }
}
```

## 用户界面

### 主工作台界面
- **导航面板**: 左侧功能模块导航菜单
- **内容区域**: 中央主要工作区域，显示当前活动的功能模块
- **状态栏**: 底部状态信息显示，包括当前用户、时间等
- **快捷操作**: 顶部工具栏提供常用操作快捷方式

### 响应式设计
- **自适应布局**: 根据窗口大小自动调整界面布局
- **主题支持**: 支持明暗主题切换
- **可访问性**: 支持键盘导航和屏幕阅读器

## 业务流程

### 典型诊疗流程
1. **患者签到**: 患者到诊所后首先在系统中签到
2. **创建案例**: 医生为患者创建新的医疗案例
3. **四诊记录**: 按照中医传统进行望、闻、问、切四诊
4. **辨证分析**: 基于四诊结果进行中医辨证分析
5. **处方开具**: 选择验方模板并个性化调整
6. **配伍检查**: 系统自动检查药物配伍安全性
7. **案例完成**: 完成诊疗流程，生成完整病历

### 复诊流程
1. **历史查看**: 查看患者以往的诊疗记录
2. **对比分析**: 对比前后诊疗结果和用药效果
3. **调整方案**: 根据疗效调整诊断和处方
4. **记录变化**: 详细记录病情变化和处理方案

## 集成接口

### 与业务模块的集成
```csharp
// 患者管理集成
public async Task<Patient> GetCurrentPatientAsync()
{
    return await _patientService.GetByIdAsync(CurrentPatientId);
}

// 诊断记录集成  
public async Task<Consultation> CreateConsultationAsync(ConsultationCreateDto dto)
{
    return await _consultationService.CreateAsync(dto);
}

// 处方管理集成
public async Task<Prescription> CreatePrescriptionAsync(PrescriptionCreateDto dto)
{
    return await _prescriptionService.CreateAsync(dto);
}
```

### 数据同步
- **实时同步**: 各模块数据实时同步，确保信息一致性
- **离线支持**: 支持离线操作，网络恢复后自动同步
- **冲突解决**: 处理多用户同时操作的数据冲突

## 权限管理

### 角色权限
- **医生权限**: 完整的诊疗功能访问权限
- **管理员权限**: 所有功能访问权限，包括系统配置
- **访客权限**: 只读权限，用于查看和学习

### 功能权限
```csharp
// 基于角色的功能权限检查
if (await _authService.HasPermissionAsync("consultation:create"))
{
    // 允许创建诊断记录
    await CreateConsultationAsync();
}
```

## 性能优化

### 界面性能
- **虚拟化**: 大数据列表使用虚拟化技术
- **延迟加载**: 非核心数据按需加载
- **缓存机制**: 常用数据本地缓存

### 业务性能 
- **异步操作**: 所有IO操作使用异步模式
- **批量处理**: 支持批量数据操作
- **索引优化**: 关键查询使用数据库索引

## 开发指南

### 添加新功能模块
1. **创建视图**: 在Views目录创建XAML视图文件
2. **创建视图模型**: 在ViewModels目录创建对应的ViewModel
3. **注册导航**: 在Module中注册视图用于导航
4. **更新导航器**: 在导航器中添加新的导航方法

### 自定义样式
1. **主题文件**: 在Themes目录创建样式资源文件
2. **控件模板**: 定义自定义控件的外观模板
3. **资源引用**: 在App.xaml中引用样式资源

## 测试策略

### 单元测试
- **ViewModel测试**: 测试业务逻辑和数据绑定
- **导航测试**: 测试页面导航功能
- **服务测试**: 测试注入的服务功能

### 集成测试
- **模块集成**: 测试与业务模块的集成
- **UI测试**: 测试用户界面交互功能
- **端到端测试**: 测试完整的诊疗流程

## 相关文档

- [LYBT.Desktop.Workbench.Core](../Core/README.md) - 工作台核心框架
- [LYBT.Desktop.Consultation](../../Modules/Consultation/README.md) - 诊断功能模块
- [LYBT.Desktop.Patients](../../Modules/Patients/README.md) - 患者管理模块
- [LYBT.Desktop.Prescriptions](../../Modules/Prescriptions/README.md) - 处方管理模块
- [诊疗工作流程指南](../../../docs/guides/consultation-workflow-guide.md) - 完整诊疗流程说明

---

> 项目状态: ✅ 生产就绪 | **最后更新**: 2025-01-01

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。
