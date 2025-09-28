# 客户端Consultation模块设计文档

> ⚠️ **当前状态警告**：本文档描述的是设计目标，实际代码仅完成基础框架。大部分功能标记为"设计完成📋"或"待实现⚠️"，不应误解为已完成功能。

## 文档信息
- **创建时间**: 2025-09-27
- **模块名称**: LYBT.Desktop.Consultation
- **模块版本**: 1.0.0-基础框架
- **技术栈**: WPF + Prism.DryIoc + MVVM
- **实际状态**: 基础框架已搭建，核心业务逻辑待实现

## 1. 模块概述

### 1.1 模块定位
客户端Consultation模块是凌隐宝堂中医诊所管理系统中的核心诊疗功能模块，负责提供中医诊疗过程的前端界面和业务逻辑。该模块采用简化设计理念，专为小型中医诊所的实际需求而设计，避免过度工程化。

### 1.2 核心功能状态

#### 1.2.1 设计完成📋
- **诊疗记录管理设计**：新建、编辑、查看诊疗记录的功能规划
- **中医四诊录入设计**：望、闻、问、切四诊的结构化录入界面设计
- **患者历史查询设计**：患者诊疗历史查看功能设计
- **四诊模板设计**：内置常用中医症候的录入模板设计

#### 1.2.2 基础实现✅
- **ConsultationModule**: Prism模块注册框架
- **基础服务注册**: IConsultationService接口和实现类框架

#### 1.2.3 待实现⚠️
- **诊疗记录创建与管理**：所有CRUD操作待实现
- **中医四诊录入**：四诊的结构化录入界面待实现
- **患者历史查询**：患者诊疗历史查看功能待实现
- **四诊模板应用**：录入模板功能待实现
- **处方开具界面**：集成处方创建功能待实现

### 1.3 技术特点（已实现）
- 基于WPF + Prism.DryIoc架构框架
- 遵循MVVM设计模式
- 采用依赖注入和服务分离

## 2. 当前架构实现

### 2.1 实际文件结构

```
LYBT.Desktop.Consultation/
├── ConsultationModule.cs           ✅ 已实现（简化版）
├── Interfaces/                     📋 设计完成
├── Models/                         📋 设计完成
├── Services/                       ⚠️ 框架已搭建，业务逻辑待实现
├── ViewModels/                     ⚠️ 待实现
└── Views/                          ⚠️ 待实现
```

### 2.2 当前模块注册（实际代码）

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册简化的服务
    containerRegistry.RegisterSingleton<IConsultationService, ConsultationService>();

    // TODO: 注册简化后的视图和视图模型
}
```

### 2.3 实现状态评估
- **架构设计**: 80% 完成 ✅
- **基础框架**: 20% 完成 ⚠️
- **业务逻辑**: 0% 完成 ⚠️
- **UI实现**: 0% 完成 ⚠️
- **测试覆盖**: 0% 完成 ⚠️

## 3. 待实现功能规划

### 3.1 高优先级⚠️

1. **基础服务实现**
   - ConsultationService业务逻辑
   - 诊疗记录CRUD操作
   - API集成和错误处理

2. **核心ViewModel实现**
   - ConsultationMainViewModel
   - 数据绑定和命令处理
   - 状态管理

3. **基础UI界面**
   - ConsultationMainView
   - 诊疗记录列表界面
   - 基础数据展示

### 3.2 中优先级📋

1. **诊疗功能实现**
   - 中医四诊录入界面
   - 诊疗记录详情页面
   - 数据验证和保存

2. **患者历史功能**
   - 历史记录查询
   - 数据筛选和排序
   - 记录详情查看

3. **模板系统**
   - 四诊模板管理
   - 模板应用功能
   - 自定义模板支持

### 3.3 低优先级📋

1. **高级功能**
   - 处方开具集成
   - 数据导出功能
   - 打印支持

2. **性能优化**
   - 数据分页加载
   - 缓存机制
   - 响应速度优化

## 4. 设计规划（参考实现）

### 4.1 MVVM架构映射（设计目标）
- **Model**: ConsultationDto（共享层）+ ConsultationItem（UI模型）
- **View**: ConsultationMainView + ConsultationManagementView
- **ViewModel**: ConsultationMainViewModel + ConsultationManagementViewModel

### 4.2 服务层设计（参考）
```csharp
// 待实现的服务接口
public interface IConsultationService
{
    Task<ServiceResult<List<ConsultationDto>>> GetConsultationsAsync();
    Task<ServiceResult<ConsultationDto>> GetConsultationByIdAsync(int id);
    Task<ServiceResult<ConsultationDto>> CreateConsultationAsync(CreateConsultationRequest request);
    Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(UpdateConsultationRequest request);
    Task<ServiceResult> DeleteConsultationAsync(int id);
}
```

### 4.3 ViewModel设计（参考）
```csharp
// 待实现的ViewModel基础结构
public class ConsultationMainViewModel : ModernViewModelBase
{
    // 属性绑定
    public ObservableCollection<ConsultationItem> Consultations { get; set; }
    public ConsultationItem SelectedConsultation { get; set; }
    public bool IsLoading { get; set; }

    // 命令绑定
    public ICommand LoadConsultationsCommand { get; }
    public ICommand CreateConsultationCommand { get; }
    public ICommand EditConsultationCommand { get; }
    public ICommand DeleteConsultationCommand { get; }
}
```

## 5. 技术债务

### 5.1 当前问题
- **模块注册不完整**: 缺少ViewModels和Views的注册
- **服务层空实现**: ConsultationService可能只有接口没有实现
- **缺少View和ViewModel**: 核心UI组件完全缺失
- **没有测试**: 整个模块没有任何测试覆盖

### 5.2 实现计划

#### 5.2.1 第一阶段（基础框架）
1. 完成ConsultationService基础实现
2. 创建ConsultationMainViewModel框架
3. 创建ConsultationMainView基础界面

#### 5.2.2 第二阶段（核心功能）
1. 实现诊疗记录CRUD操作
2. 添加数据绑定和命令处理
3. 完善错误处理和状态管理

#### 5.2.3 第三阶段（功能完善）
1. 实现中医四诊录入功能
2. 添加患者历史查询功能
3. 实现模板系统和高级功能

## 6. 依赖关系

### 6.1 项目依赖
```xml
<ItemGroup>
  <PackageReference Include="Prism.DryIoc" Version="8.1.97" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\..\Shared\..." />
  <ProjectReference Include="..\..\Core\..." />
</ItemGroup>
```

### 6.2 共享层依赖
- LYBT.Shared.Models.Contracts.Consultation.*
- LYBT.Shared.Interfaces.Services.IConsultationService
- LYBT.Shared.Interfaces.Api.IConsultationApi

## 7. 状态总结

### 7.1 完成度评估
- **模块设计**: 70% 完成 ✅
- **基础框架**: 15% 完成 ⚠️
- **业务逻辑**: 0% 完成 ⚠️
- **UI实现**: 0% 完成 ⚠️
- **测试覆盖**: 0% 完成 ⚠️

### 7.2 后续工作重点
1. **立即需要**: 完成基础服务实现和模块注册
2. **短期目标**: 创建核心ViewModel和View，实现基础诊疗记录功能
3. **中期目标**: 完善诊疗功能，添加四诊录入和历史查询
4. **长期目标**: 实现高级功能、性能优化和测试覆盖

---

*文档版本: 2.0 - 真实状态反映版*  
*最后更新: 2025-09-28*  
*状态: 基础框架已搭建，核心功能完全待实现*