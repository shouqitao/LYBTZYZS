# 客户端Prescriptions模块设计文档

> ⚠️ **当前状态警告**：本文档描述的是设计目标，实际代码仅完成基础框架。大部分功能标记为"设计完成📋"或"待实现⚠️"，不应误解为已完成功能。

## 文档信息
- **创建时间**: 2025-09-27
- **模块名称**: LYBT.Desktop.Prescriptions
- **模块版本**: 1.0.0-基础框架
- **技术栈**: WPF + Prism.DryIoc + MVVM
- **实际状态**: 基础框架已搭建，核心业务逻辑待实现

## 1. 模块概述

### 1.1 模块定位
Prescriptions模块是WPF桌面客户端的核心业务模块之一，负责处方管理功能。该模块采用简化设计理念，专为小型中医诊所的实际需求而设计，避免过度工程化。

### 1.2 核心功能状态

#### 1.2.1 设计完成📋
- **处方管理设计**：创建、编辑、查询处方等功能规划
- **处方列表设计**：分页查询、搜索、筛选功能设计
- **处方详情设计**：完整处方信息展示、药材配伍设计
- **处方开具设计**：处方开具流程、药材选择设计

#### 1.2.2 基础实现✅
- **PrescriptionsModule**: Prism模块注册框架
- **基础服务注册**: IPrescriptionService接口和实现类框架
- **额外组件目录**: Components、Constants目录已创建

#### 1.2.3 待实现⚠️
- **处方列表管理**：分页查询、搜索、筛选功能待实现
- **处方详情查看**：完整信息展示、药材配伍显示待实现
- **处方编辑功能**：基本信息、药材配伍编辑待实现
- **处方开具功能**：处方创建流程、药材选择待实现
- **处方打印功能**：处方打印和导出功能待实现

### 1.3 技术特点（已实现）
- 基于WPF + Prism.DryIoc架构框架
- 遵循MVVM设计模式
- 采用依赖注入和服务分离
- 具备扩展组件支持（Components目录）

## 2. 当前架构实现

### 2.1 实际文件结构

```
LYBT.Desktop.Prescriptions/
├── PrescriptionsModule.cs          ✅ 已实现（简化版）
├── Components/                     📋 扩展组件目录已创建
├── Constants/                      📋 常量定义目录已创建
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
    containerRegistry.RegisterSingleton<IPrescriptionService, PrescriptionService>();

    // TODO: 注册简化后的视图和视图模型
}
```

### 2.3 实现状态评估
- **架构设计**: 75% 完成 ✅（相比其他模块有更多目录结构）
- **基础框架**: 20% 完成 ⚠️
- **业务逻辑**: 0% 完成 ⚠️
- **UI实现**: 0% 完成 ⚠️
- **测试覆盖**: 0% 完成 ⚠️

## 3. 待实现功能规划

### 3.1 高优先级⚠️

1. **基础服务实现**
   - PrescriptionService业务逻辑
   - 处方记录CRUD操作
   - API集成和错误处理

2. **核心ViewModel实现**
   - PrescriptionsMainViewModel
   - PrescriptionsListViewModel
   - 数据绑定和命令处理

3. **基础UI界面**
   - PrescriptionsMainView
   - 处方列表界面
   - 基础数据展示

### 3.2 中优先级📋

1. **处方管理功能**
   - 处方详情查看界面
   - 处方编辑功能
   - 处方开具流程

2. **搜索和筛选**
   - 处方编号搜索
   - 按患者筛选
   - 数据分页加载

3. **药材配伍管理**
   - 药材选择界面
   - 配伍验证
   - 剂量计算

### 3.3 低优先级📋

1. **高级功能**
   - 处方模板功能
   - 处方打印支持
   - 数据导入导出

2. **性能优化**
   - 数据缓存机制
   - 界面响应优化
   - 内存管理

## 4. 设计规划（参考实现）

### 4.1 MVVM架构映射（设计目标）
- **Model**: PrescriptionDto（共享层）+ PrescriptionItem（UI模型）
- **View**: PrescriptionsMainView + PrescriptionsListView + PrescriptionEditView
- **ViewModel**: PrescriptionsMainViewModel + PrescriptionsListViewModel + PrescriptionEditViewModel

### 4.2 服务层设计（参考）
```csharp
// 待实现的服务接口
public interface IPrescriptionService
{
    Task<ServiceResult<List<PrescriptionDto>>> GetPrescriptionsAsync();
    Task<ServiceResult<PrescriptionDto>> GetPrescriptionByIdAsync(int id);
    Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionRequest request);
    Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(UpdatePrescriptionRequest request);
    Task<ServiceResult> DeletePrescriptionAsync(int id);
    Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();
}
```

### 4.3 ViewModel设计（参考）
```csharp
// 待实现的ViewModel基础结构
public class PrescriptionsMainViewModel : ModernViewModelBase
{
    // 属性绑定
    public ObservableCollection<PrescriptionItem> Prescriptions { get; set; }
    public PrescriptionItem SelectedPrescription { get; set; }
    public string SearchText { get; set; }
    public bool IsLoading { get; set; }

    // 命令绑定
    public ICommand LoadPrescriptionsCommand { get; }
    public ICommand SearchPrescriptionsCommand { get; }
    public ICommand CreatePrescriptionCommand { get; }
    public ICommand EditPrescriptionCommand { get; }
    public ICommand DeletePrescriptionCommand { get; }
    public ICommand PrintPrescriptionCommand { get; }
}
```

### 4.4 组件设计（扩展功能）
```csharp
// Components目录中可能包含的自定义组件
// HerbSelectionControl - 药材选择控件
// DosageCalculator - 剂量计算器
// PrescriptionPreview - 处方预览控件
```

## 5. 技术债务

### 5.1 当前问题
- **模块注册不完整**: 缺少ViewModels和Views的注册
- **服务层空实现**: PrescriptionService可能只有接口没有实现
- **缺少View和ViewModel**: 核心UI组件完全缺失
- **组件未实现**: Components和Constants目录为空
- **没有测试**: 整个模块没有任何测试覆盖

### 5.2 实现计划

#### 5.2.1 第一阶段（基础框架）
1. 完成PrescriptionService基础实现
2. 创建PrescriptionsMainViewModel框架
3. 创建PrescriptionsMainView基础界面

#### 5.2.2 第二阶段（核心功能）
1. 实现处方记录CRUD操作
2. 添加数据绑定和命令处理
3. 完善搜索和筛选功能

#### 5.2.3 第三阶段（功能完善）
1. 实现处方详情查看功能
2. 添加处方开具功能
3. 实现自定义组件和常量

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
- LYBT.Shared.Models.Contracts.Prescriptions.*
- LYBT.Shared.Interfaces.Services.IPrescriptionService
- LYBT.Shared.Interfaces.Api.IPrescriptionApi

### 6.3 跨模块依赖
- 依赖Herbs模块提供药材信息
- 依赖Patients模块提供患者信息
- 依赖Formula模块提供验方模板

## 7. 状态总结

### 7.1 完成度评估
- **模块设计**: 75% 完成 ✅
- **基础框架**: 20% 完成 ⚠️
- **业务逻辑**: 0% 完成 ⚠️
- **UI实现**: 0% 完成 ⚠️
- **测试覆盖**: 0% 完成 ⚠️

### 7.2 后续工作重点
1. **立即需要**: 完成基础服务实现和模块注册
2. **短期目标**: 创建核心ViewModel和View，实现基础处方管理功能
3. **中期目标**: 完善处方功能，添加搜索筛选和详情查看
4. **长期目标**: 实现高级功能、自定义组件和测试覆盖

---

*文档版本: 2.0 - 真实状态反映版*  
*最后更新: 2025-09-28*  
*状态: 基础框架已搭建，核心功能完全待实现*