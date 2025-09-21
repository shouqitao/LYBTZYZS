# LYBT.Desktop.Workbench.Admin

凌隐宝堂中医诊所系统 - 系统管理工作台模块

## 🎯 项目概述

系统管理工作台是专为系统管理员设计的综合性管理环境，提供用户管理、系统配置、数据维护、监控报表等管理功能。采用现代化WPF界面和Prism MVVM架构，支持对整个诊所管理系统的全面管理。

## 目录结构

```
SystemWorkbench/
├── Services/                          # 服务层
│   ├── ISystemWorkbenchNavigator.cs   # 系统工作台导航接口
│   └── SystemWorkbenchNavigator.cs    # 导航服务实现
├── ViewModels/                        # 视图模型
│   └── SystemWorkbenchMainViewModel.cs # 主工作台视图模型
├── Views/                            # 用户界面
│   ├── SystemWorkbenchMainView.xaml     # 主工作台视图
│   └── SystemWorkbenchMainView.xaml.cs  # 视图代码后置
└── SystemWorkbenchModule.cs          # Prism模块定义
```

## 核心功能

### 1. 系统管理主界面
- **统一管理面板**: 集成所有系统管理功能的中央控制台
- **快速操作**: 提供系统管理常用操作的快捷入口
- **状态监控**: 实时显示系统运行状态和关键指标

### 2. 用户与权限管理
- **用户账户管理**: 创建、编辑、禁用用户账户
- **角色权限配置**: 管理医生、管理员等角色权限
- **访问控制**: 控制用户对系统功能的访问权限
- **密码策略**: 设置和管理系统密码安全策略

### 3. 业务数据管理
整合8个核心业务模块的管理功能：

#### 患者数据管理
- **患者档案维护**: 批量导入、导出患者信息
- **数据清理**: 清理重复或无效的患者记录
- **隐私保护**: 敏感信息脱敏和权限控制

#### 医疗数据管理
- **医疗案例审核**: 审核和管理医疗案例记录
- **诊断记录维护**: 管理诊断数据的完整性和准确性
- **处方数据分析**: 统计分析处方用药情况

#### 基础数据管理
- **中药材库维护**: 管理药材信息、价格、供应商等
- **验方模板管理**: 维护经典验方和个人验方库
- **配伍规则配置**: 设置药物配伍禁忌和注意事项

### 4. 系统监控与维护
- **性能监控**: 监控系统运行性能和资源使用情况
- **日志管理**: 查看和管理系统操作日志
- **数据备份**: 配置和执行数据库备份策略
- **系统更新**: 管理系统版本更新和补丁安装

## 技术架构

### 框架技术栈
- **.NET 8.0-windows**: 现代.NET平台
- **WPF**: Windows桌面应用程序框架 
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入（DI）
- **LYBT.Desktop.Core**: 桌面应用程序核心框架

### 设计模式
- **MVVM模式**: 视图-视图模型-模型分离
- **依赖注入（DI）**: 使用DryIoc容器管理依赖关系
- **模块化架构**: Prism模块化应用程序结构
- **权限模式**: 基于角色的访问控制(RBAC)

## 导航系统

### ISystemWorkbenchNavigator
系统管理工作台专用导航接口：

```csharp
public interface ISystemWorkbenchNavigator
{
    // 导航到用户管理
    Task NavigateToUserManagementAsync();
    
    // 导航到患者管理  
    Task NavigateToPatientManagementAsync();
    
    // 导航到系统配置
    Task NavigateToSystemConfigurationAsync();
    
    // 导航到数据维护
    Task NavigateToDataMaintenanceAsync();
    
    // 导航到监控报表
    Task NavigateToMonitoringReportsAsync();
}
```

### 集成的业务模块视图
系统工作台集成以下业务模块的管理视图：

- **UserManagementView**: 用户管理界面
- **PatientManagementView**: 患者管理界面 
- **MedicalCaseListView**: 医疗案例列表管理
- **ConsultationMainView**: 诊断记录管理
- **HerbManagementView**: 中药材管理界面
- **FormulaManagementView**: 验方模板管理
- **PrescriptionManagementView**: 处方记录管理

## 模块注册

### SystemWorkbenchModule
Prism模块定义，负责工作台的初始化和服务注册：

```csharp
public class SystemWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册工作台导航器
        containerRegistry.RegisterSingleton<ISystemWorkbenchNavigator, SystemWorkbenchNavigator>();
        
        // 注册主视图
        containerRegistry.RegisterForNavigation<SystemWorkbenchMainView>("SystemWorkbenchMainView");
        
        // UltraThink v2.0: 显式注册业务模块视图用于SystemWorkbench导航
        var viewRegistrations = new Dictionary<string, string>
        {
            ["UserManagementView"] = "LYBT.Desktop.Users.Views.UserManagementView, LYBT.Desktop.Users",
            ["PatientManagementView"] = "LYBT.Desktop.Patients.Views.PatientManagementView, LYBT.Desktop.Patients",
            ["MedicalCaseListView"] = "LYBT.Desktop.MedicalCase.Views.MedicalCaseListView, LYBT.Desktop.MedicalCase",
            ["ConsultationMainView"] = "LYBT.Desktop.Consultation.Views.ConsultationMainView, LYBT.Desktop.Consultation",
            ["HerbManagementView"] = "LYBT.Desktop.Herbs.Views.HerbManagementView, LYBT.Desktop.Herbs",
            ["FormulaManagementView"] = "LYBT.Desktop.Formula.Views.FormulaManagementView, LYBT.Desktop.Formula",
            ["PrescriptionManagementView"] = "LYBT.Desktop.Prescriptions.Views.PrescriptionManagementView, LYBT.Desktop.Prescriptions"
        };
        
        // 动态注册业务模块视图
        foreach (var kvp in viewRegistrations)
        {
            var viewType = Type.GetType(kvp.Value);
            if (viewType != null)
            {
                containerRegistry.RegisterForNavigation(viewType, kvp.Key);
            }
        }
    }
}
```

## 权限管理

### 管理员权限级别
- **超级管理员**: 拥有所有系统管理权限，包括用户创建和系统配置
- **数据管理员**: 负责业务数据的维护和管理
- **系统维护员**: 负责系统监控、备份和日常维护

### 功能权限控制
```csharp
// 基于角色的功能访问控制
if (await _authService.HasRoleAsync(UserRole.Admin))
{
    // 允许访问系统管理功能
    await NavigateToSystemManagementAsync();
}

// 基于具体权限的访问控制
if (await _authService.HasPermissionAsync("system:user:manage"))
{
    // 允许用户管理操作
    await ShowUserManagementAsync();
}
```

### 操作审计
- **操作记录**: 记录所有管理操作的详细日志
- **审计追踪**: 追踪敏感操作的执行者和时间
- **权限变更**: 记录权限和角色变更历史

## 数据管理功能

### 数据导入导出
```csharp
// 患者数据批量导入
public async Task<ImportResult> ImportPatientsAsync(string filePath)
{
    var patients = await _excelService.ReadPatientsFromFileAsync(filePath);
    return await _patientService.BatchCreateAsync(patients);
}

// 处方数据导出
public async Task<string> ExportPrescriptionsAsync(ExportCriteria criteria)
{
    var prescriptions = await _prescriptionService.GetByCriteriaAsync(criteria);
    return await _excelService.ExportPrescriptionsAsync(prescriptions);
}
```

### 数据统计分析
- **用户活跃度**: 统计用户登录和操作频率
- **业务数据量**: 统计患者、诊断、处方等数据增长趋势
- **系统性能**: 分析响应时间、错误率等性能指标

## 系统配置管理

### 配置项管理
- **业务参数配置**: 诊所基本信息、营业时间等
- **系统参数配置**: 缓存策略、连接池设置等
- **安全参数配置**: JWT过期时间、密码策略等

### 配置热更新
```csharp
// 配置动态更新，无需重启系统
public async Task UpdateSystemConfigAsync(string key, string value)
{
    await _configService.UpdateAsync(key, value);
    await _eventBus.PublishAsync(new ConfigChangedEvent(key, value));
}
```

## 监控与报表

### 系统监控
- **实时状态**: 显示系统运行状态和关键指标
- **性能图表**: 可视化显示CPU、内存、数据库等资源使用情况
- **异常告警**: 自动检测和报告系统异常

### 业务报表
- **诊疗统计报表**: 按时间段统计诊疗量和收入
- **用药统计报表**: 统计常用药材和处方模式
- **患者分析报表**: 分析患者来源和疾病分布

## 用户界面

### 主控制台界面
- **仪表盘**: 显示系统关键指标和快捷操作
- **导航菜单**: 左侧树形菜单，分类显示管理功能
- **内容区域**: 右侧主要工作区域，显示具体管理界面
- **状态栏**: 底部显示当前用户、系统时间、连接状态等

### 响应式设计
- **多屏适配**: 支持不同分辨率的显示器
- **主题切换**: 支持明暗主题和高对比度主题
- **可访问性**: 支持键盘操作和屏幕阅读器

## 安全特性

### 访问控制
- **身份验证**: 强制管理员身份验证
- **会话管理**: 管理员会话超时和自动登出
- **IP限制**: 限制管理员访问的IP地址范围

### 数据安全
- **敏感信息加密**: 对敏感配置信息进行加密存储
- **操作审计**: 完整记录所有管理操作
- **备份加密**: 数据备份文件加密保护

## 部署与维护

### 部署要求
- **管理员权限**: 需要Windows管理员权限运行
- **数据库权限**: 需要数据库管理员权限
- **网络访问**: 需要访问所有业务服务的网络权限

### 日常维护
- **定期备份**: 自动执行数据备份任务
- **日志清理**: 定期清理过期的系统日志
- **性能优化**: 监控和优化系统性能瓶颈

## 开发指南

### 添加新管理功能
1. **创建管理视图**: 创建对应的XAML视图和ViewModel
2. **注册导航**: 在SystemWorkbenchModule中注册视图
3. **添加权限检查**: 为新功能添加适当的权限验证
4. **更新导航器**: 在导航器中添加导航方法

### 集成外部系统
```csharp
// 集成外部系统的示例
public async Task SyncWithHISAsync()
{
    var externalData = await _hisConnector.GetPatientsAsync();
    await _patientService.SyncExternalDataAsync(externalData);
}
```

## 测试策略

### 管理功能测试
- **权限测试**: 验证各种权限场景下的功能访问
- **数据操作测试**: 测试批量数据操作的正确性
- **配置管理测试**: 测试配置项的增删改查功能

### 安全测试
- **权限绕过测试**: 测试是否存在权限绕过漏洞
- **数据泄露测试**: 验证敏感数据的保护机制
- **会话安全测试**: 测试会话管理的安全性

## 相关文档

- [LYBT.Desktop.Workbench.Core](../Core/README.md) - 工作台核心框架
- [LYBT.Desktop.Users](../../Modules/Users/README.md) - 用户管理模块
- [系统管理员手册](../../../docs/guides/system-admin-manual.md) - 系统管理操作指南
- [权限管理指南](../../../docs/guides/permission-management-guide.md) - 权限配置说明
- [数据备份恢复指南](../../../docs/guides/backup-restore-guide.md) - 备份恢复操作

---

> 项目状态: ✅ 生产就绪 | **最后更新**: 2025-01-01

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。
