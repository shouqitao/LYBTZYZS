# 医疗案例模块 (MedicalCase Module)

**最后更新**: 2025-09-01  
**模块状态**: ✅ 生产就绪 (已简化)  
**对应后端**: LYBT.Module.MedicalCase  
**需求参考**: [功能需求-医疗案例模块](../../../../../docs/requirements/functional-requirements.md#3️⃣-医疗案例模块-medicalcase)

---

## 📋 模块概览

### 业务定位
**诊疗流程容器模块** - 管理完整的诊疗案例，作为患者每次就诊的聚合根，统一管理诊疗过程。

### 核心功能 (简化版)
- ✅ **医案基础管理**: 创建、查询、修改、完成医案
- ✅ **状态流转管理**: Registered → InProgress → Completed
- ✅ **医案与诊断关联**: 1:1关联Consultation模块
- ✅ **医案搜索筛选**: 按患者、医生、状态、时间筛选
- ❌ **已移除功能**: 统计分析、归档功能、复杂历史记录 (2025-09-01)

### 业务关系
```
Patient (患者) 
    ↓ 1:N
MedicalCase (医案) ←→ 1:1 ←→ Consultation (诊断)
    ↓ 1:N
Prescription (处方)
```

---

## 🏗️ 模块结构

### 目录组织
```
src/Client/Desktop/Modules/MedicalCase/
├── Services/
│   └── MedicalCaseModule.cs       # 模块注册 (简化版)
├── ViewModels/
│   ├── MedicalCaseListViewModel.cs        # 医案列表管理
│   ├── MedicalCaseDetailViewModel.cs      # 医案详情编辑
│   └── CreateMedicalCaseViewModel.cs      # 创建医案流程
├── Views/
│   ├── MedicalCaseManagementView.xaml     # 医案管理界面
│   ├── MedicalCaseListView.xaml           # 医案列表
│   ├── MedicalCaseDetailView.xaml         # 医案详情
│   └── CreateMedicalCaseDialog.xaml       # 创建医案对话框
└── README.md                      # 本文档
```

### 核心功能说明
- **医案容器**: 作为完整诊疗流程的聚合根
- **状态管理**: 三状态流转 (Registered/InProgress/Completed)
- **数据聚合**: 统一展示患者信息、诊断记录、处方信息
- **流程控制**: 控制诊疗过程的开始、进行、完成

---

## 🔌 API接口集成

### 后端API对接
```csharp
// 主要API端点 (对应LYBT.Module.MedicalCase)
GET    /api/v1/medicalcases              // 获取医案列表(分页)
GET    /api/v1/medicalcases/{id}         // 获取医案详情
POST   /api/v1/medicalcases              // 创建新医案
PUT    /api/v1/medicalcases/{id}         // 更新医案信息
DELETE /api/v1/medicalcases/{id}         // 删除医案(软删除)
PUT    /api/v1/medicalcases/{id}/status  // 更新医案状态
GET    /api/v1/medicalcases/search       // 医案搜索

// 已移除的API (2025-09-01简化)
// GET /api/v1/medicalcases/statistics   // 统计分析 (过度设计)
// POST /api/v1/medicalcases/{id}/archive // 归档功能 (简单诊所不需要)
```

### 数据传输对象
```csharp
// 主要DTO类 (来自LYBT.Shared.Models)
- MedicalCaseDto: 医案基础信息
- MedicalCaseCreateDto: 创建医案请求
- MedicalCaseUpdateDto: 更新医案请求
- MedicalCaseSearchDto: 搜索条件
- MedicalCaseStatus: 状态枚举 (Registered/InProgress/Completed)
```

---

## 💻 开发指南

### 状态流转逻辑
```csharp
public enum MedicalCaseStatus
{
    Registered = 1,    // 已登记 - 患者登记，待开始诊疗
    InProgress = 2,    // 诊疗中 - 正在进行诊断和处方
    Completed = 3      // 已完成 - 诊疗结束
}

// 状态转换规则
public static class MedicalCaseStatusTransition
{
    public static bool CanTransition(MedicalCaseStatus from, MedicalCaseStatus to)
    {
        return (from, to) switch
        {
            (Registered, InProgress) => true,
            (InProgress, Completed) => true,
            (InProgress, Registered) => true,  // 允许回退到登记状态
            _ => false
        };
    }
}
```

### 业务流程集成
```csharp
// 典型的医案处理流程
public class MedicalCaseWorkflow
{
    // 1. 创建医案
    public async Task<MedicalCaseDto> CreateMedicalCaseAsync(Guid patientId, string chiefComplaint)
    {
        var createDto = new MedicalCaseCreateDto
        {
            PatientId = patientId,
            DoctorId = CurrentUser.Id,
            ChiefComplaint = chiefComplaint,
            Status = MedicalCaseStatus.Registered
        };
        
        return await _medicalCaseService.CreateAsync(createDto);
    }
    
    // 2. 开始诊疗 (自动创建关联的Consultation)
    public async Task<bool> StartConsultationAsync(Guid medicalCaseId)
    {
        // 更新医案状态
        var success = await _medicalCaseService.UpdateStatusAsync(
            medicalCaseId, MedicalCaseStatus.InProgress);
            
        if (success)
        {
            // 导航到诊断模块
            _regionManager.NavigateToConsultation(medicalCaseId);
        }
        
        return success;
    }
    
    // 3. 完成医案
    public async Task<bool> CompleteMedicalCaseAsync(Guid medicalCaseId)
    {
        return await _medicalCaseService.UpdateStatusAsync(
            medicalCaseId, MedicalCaseStatus.Completed);
    }
}
```

---

## 🧪 测试指南

### 手动测试清单
- [ ] **医案创建**: 选择患者，输入主诉，创建新医案
- [ ] **状态流转**: 测试 Registered → InProgress → Completed 流转
- [ ] **医案搜索**: 按患者姓名、医生、状态、时间范围搜索
- [ ] **详情查看**: 查看医案详情，包括患者信息、诊断记录
- [ ] **关联功能**: 从医案跳转到诊断模块、处方模块
- [ ] **权限控制**: 验证医生只能管理自己的医案
- [ ] **数据完整性**: 删除医案时检查关联数据处理

### 业务场景测试
```csharp
// 完整诊疗流程测试
[TestMethod]
public async Task CompleteConsultationWorkflow_ShouldSuccess()
{
    // 1. 创建医案
    var medicalCase = await CreateMedicalCaseAsync(patientId, "头痛三天");
    Assert.AreEqual(MedicalCaseStatus.Registered, medicalCase.Status);
    
    // 2. 开始诊疗
    var started = await StartConsultationAsync(medicalCase.Id);
    Assert.IsTrue(started);
    
    // 3. 完成诊疗 (在Consultation模块中)
    // ... 诊断过程 ...
    
    // 4. 完成医案
    var completed = await CompleteMedicalCaseAsync(medicalCase.Id);
    Assert.IsTrue(completed);
}
```

---

## 📊 简化说明 (2025-09-01)

### 已移除的复杂功能
```csharp
// ❌ 已移除: 复杂统计分析
// public async Task<MedicalCaseStatisticsDto> GetStatisticsAsync()
// 移除原因: 小诊所不需要复杂的数据分析功能

// ❌ 已移除: 医案归档功能  
// public async Task<bool> ArchiveAsync(Guid id, string archiveReason)
// 移除原因: 简单诊所直接完成医案即可，无需复杂归档流程

// ❌ 已移除: 复杂历史记录
// public async Task<List<MedicalCaseHistoryDto>> GetHistoryAsync(Guid patientId)
// 移除原因: 通过患者ID直接查询医案列表更简单直接
```

### Stub实现保持兼容性
```csharp
// 为保持接口兼容性提供的stub实现
public Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
{
    return Task.FromResult(ServiceResult<bool>.Failure(
        "简单诊所版本不支持归档功能，请直接完成医案"));
}

public Task<ServiceResult<MedicalCaseStatisticsDto>> GetStatisticsAsync()
{
    return Task.FromResult(ServiceResult<MedicalCaseStatisticsDto>.Failure(
        "简单诊所版本不提供复杂统计功能"));
}
```

---

## 🔧 配置说明

### 模块注册 (简化版)
```csharp
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 只注册核心服务，移除复杂的统计和归档服务
        containerRegistry.Register<IMedicalCaseService, MedicalCaseService>();
        
        // ViewModel注册
        containerRegistry.Register<MedicalCaseListViewModel>();
        containerRegistry.Register<MedicalCaseDetailViewModel>();
        containerRegistry.Register<CreateMedicalCaseViewModel>();
        
        // Navigation注册
        containerRegistry.RegisterForNavigation<MedicalCaseManagementView>();
        containerRegistry.RegisterForNavigation<MedicalCaseDetailView>();
    }
}
```

---

## 🐛 故障排除

### 常见问题
1. **医案状态转换失败**
   - 检查状态转换规则是否符合业务逻辑
   - 验证用户权限 (医生只能管理自己的医案)

2. **关联数据丢失**
   - 检查MedicalCase与Consultation的1:1关系
   - 验证外键约束和数据完整性

3. **搜索结果异常**
   - 确认搜索条件格式正确
   - 检查日期范围和状态筛选参数

### 调试技巧
- 启用详细的状态转换日志
- 使用断点调试业务流程
- 检查数据库外键约束状态

---

## 📚 相关文档

### 需求文档
- [功能需求-医疗案例](../../../../../docs/requirements/functional-requirements.md#3️⃣-医疗案例模块-medicalcase)
- [系统业务流程](../../../../../docs/requirements/system-overview.md#🔄-核心业务流程)

### 关联模块
- [Consultation模块](../Consultation/README.md) - 1:1关联的诊断模块
- [Patients模块](../Patients/README.md) - N:1关联的患者模块
- [Prescriptions模块](../Prescriptions/README.md) - 1:N关联的处方模块

### 过程文档
- [MedicalCase简化过程](../../../../../docs/process/refactoring/medicalcase-refactoring-plan-20250901.md)

---

**维护说明**: 本文档反映MedicalCase模块简化后的当前状态。该模块已移除过度设计的功能，专注于简单诊所的核心诊疗流程管理。