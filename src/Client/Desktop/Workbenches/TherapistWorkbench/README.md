# LYBT.Desktop.Workbench.Therapist

凌隐宝堂中医诊所系统 - 理疗师工作台模块

## 项目概述

理疗师工作台是专为理疗师设计的康复治疗和理疗管理环境，提供理疗计划制定、治疗记录管理、康复进度跟踪、理疗设备管理等专业功能。采用现代化WPF界面和Prism MVVM架构，支持完整的中医理疗和康复治疗流程。

## 目录结构

```
TherapistWorkbench/
├── ViewModels/                        # 视图模型
│   └── TherapistMainViewModel.cs      # 理疗师主工作台视图模型
├── Views/                            # 用户界面
│   ├── TherapistMainView.xaml         # 理疗师主工作台视图
│   └── TherapistMainView.xaml.cs      # 主视图代码后置
└── TherapistWorkbenchModule.cs        # Prism模块定义
```

## 核心功能

### 1. 理疗师主工作台 (TherapistMainView)
- **统一理疗管理界面**: 集成所有理疗相关功能的中央操作台
- **患者理疗状态**: 显示当前理疗患者的治疗进度和状态
- **设备使用情况**: 监控理疗设备的使用状态和预约情况
- **治疗计划概览**: 查看当日的理疗计划和安排

### 2. 预留专业功能模块

#### 理疗计划管理 (TherapyPlanningView - 待实现)
- **个性化理疗方案**: 根据患者病情制定个性化理疗计划
- **治疗周期管理**: 规划治疗周期和频次安排
- **理疗项目选择**: 选择合适的理疗项目和治疗手段
- **康复目标设定**: 设定阶段性康复目标和评估标准

#### 治疗记录管理 (TreatmentRecordView - 待实现)
- **理疗记录录入**: 详细记录每次理疗的具体内容
- **治疗效果评估**: 记录治疗效果和患者反馈
- **治疗参数记录**: 记录治疗设备的参数设置
- **不良反应监测**: 监测和记录治疗过程中的不良反应

#### 康复管理系统 (RehabilitationManagementView - 待实现)
- **康复进度跟踪**: 跟踪患者的康复进展情况
- **功能评定**: 定期进行功能评定和康复评估
- **家庭训练指导**: 制定和指导家庭康复训练计划
- **康复设备管理**: 管理康复训练设备和器械

## 中医理疗特色

### 1. 传统理疗方法
- **针灸治疗**: 传统针灸、电针、耳针等针法治疗
- **推拿按摩**: 中医推拿手法和穴位按摩
- **拔罐疗法**: 火罐、气罐、走罐等拔罐治疗
- **艾灸疗法**: 艾条灸、艾柱灸、温灸器灸等

### 2. 现代理疗技术
- **电疗**: 低频电疗、中频电疗、高频电疗
- **光疗**: 红外线、紫外线、激光治疗
- **磁疗**: 静磁场、脉冲磁场治疗
- **超声波疗法**: 治疗性超声波应用

### 3. 综合康复治疗
- **运动疗法**: 主动运动、被动运动、阻抗运动
- **物理因子治疗**: 热疗、冷疗、水疗等
- **作业治疗**: 日常生活能力训练
- **言语治疗**: 语言功能康复训练

## 技术架构

### 框架技术栈
- **.NET 8.0-windows**: 现代.NET平台
- **WPF**: Windows桌面应用程序框架
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入
- **LYBT.Desktop.Core**: 桌面应用程序核心框架

### 设计模式
- **MVVM模式**: 视图-视图模型-模型分离
- **依赖注入**: 使用DryIoc容器管理依赖关系
- **模块化架构**: Prism模块化应用程序结构
- **治疗流程模式**: 标准化的理疗治疗流程

## 模块注册

### TherapistWorkbenchModule
Prism模块定义，负责理疗师工作台的初始化和服务注册：

```csharp
public class TherapistWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册理疗师工作台主视图
        containerRegistry.RegisterForNavigation<TherapistMainView>();
        
        // 注册占位视图 (暂时注释，待实现)
        // containerRegistry.RegisterForNavigation<TherapyPlanningView>();
        // containerRegistry.RegisterForNavigation<TreatmentRecordView>();
        // containerRegistry.RegisterForNavigation<RehabilitationManagementView>();
        
        // 预留：未来可注册理疗师相关的其他视图和服务
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册自定义的ViewModel映射
        ViewModelLocationProvider.Register<TherapistMainView, TherapistMainViewModel>();
    }
}
```

## 理疗业务流程

### 标准理疗流程
```csharp
// 示例理疗治疗流程
public async Task<TherapyResult> ExecuteTherapySessionAsync(TherapySessionDto session)
{
    // 1. 患者评估
    var assessment = await AssessPatientConditionAsync(session.PatientId);
    
    // 2. 治疗准备
    var preparation = await PrepareTherapyEquipmentAsync(session.TherapyType);
    if (!preparation.IsReady)
    {
        return TherapyResult.Failed("设备准备失败");
    }
    
    // 3. 执行治疗
    var treatment = await ExecuteTreatmentAsync(session);
    
    // 4. 监测过程
    var monitoring = await MonitorTreatmentProgressAsync(session.Id);
    
    // 5. 记录结果
    await RecordTreatmentResultAsync(session.Id, treatment.Result, monitoring.VitalSigns);
    
    // 6. 效果评估
    var evaluation = await EvaluateTreatmentEffectAsync(session.PatientId);
    
    return TherapyResult.Success(evaluation);
}
```

### 康复计划制定流程
```csharp
// 示例康复计划制定流程
public async Task<RehabilitationPlan> CreateRehabilitationPlanAsync(PlanCreateDto dto)
{
    // 1. 功能评定
    var functionalAssessment = await PerformFunctionalAssessmentAsync(dto.PatientId);
    
    // 2. 康复目标设定
    var goals = await SetRehabilitationGoalsAsync(functionalAssessment);
    
    // 3. 治疗方案设计
    var treatmentPlan = await DesignTreatmentPlanAsync(goals, dto.Duration);
    
    // 4. 治疗频次安排
    var schedule = await CreateTreatmentScheduleAsync(treatmentPlan, dto.Frequency);
    
    // 5. 创建康复计划
    var plan = new RehabilitationPlan
    {
        PatientId = dto.PatientId,
        Goals = goals,
        TreatmentPlan = treatmentPlan,
        Schedule = schedule,
        Duration = dto.Duration,
        CreatedBy = dto.TherapistId
    };
    
    await SaveRehabilitationPlanAsync(plan);
    return plan;
}
```

## 设备管理

### 理疗设备类型
- **电疗设备**: 低频治疗仪、中频治疗仪、经皮神经电刺激仪
- **光疗设备**: 红外治疗仪、紫外治疗仪、激光治疗仪
- **机械治疗设备**: 牵引床、运动训练器械
- **热疗设备**: 石蜡治疗仪、超短波治疗仪

### 设备管理功能
```csharp
// 设备管理示例
public class TherapyEquipmentManager
{
    public async Task<EquipmentStatus> CheckEquipmentStatusAsync(string equipmentId)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId);
        
        return new EquipmentStatus
        {
            Id = equipment.Id,
            Name = equipment.Name,
            IsAvailable = equipment.Status == EquipmentStatus.Available,
            LastMaintenanceDate = equipment.LastMaintenanceDate,
            NextMaintenanceDate = equipment.NextMaintenanceDate,
            Usage = await CalculateEquipmentUsageAsync(equipmentId)
        };
    }
    
    public async Task<bool> ReserveEquipmentAsync(string equipmentId, DateTime startTime, TimeSpan duration)
    {
        var reservation = new EquipmentReservation
        {
            EquipmentId = equipmentId,
            StartTime = startTime,
            EndTime = startTime.Add(duration),
            Status = ReservationStatus.Reserved
        };
        
        return await _reservationService.CreateReservationAsync(reservation);
    }
}
```

## 治疗效果评估

### 评估指标体系
- **疼痛评估**: VAS视觉模拟评分、数字疼痛评分
- **功能评估**: 关节活动度、肌力评估、平衡能力
- **生活质量**: 日常生活能力、工作能力评估
- **患者满意度**: 治疗满意度调查

### 评估记录系统
```csharp
// 治疗效果评估示例
public class TreatmentEvaluationSystem
{
    public async Task<EvaluationResult> EvaluateTreatmentEffectAsync(Guid patientId, EvaluationType type)
    {
        var baseline = await GetBaselineAssessmentAsync(patientId);
        var current = await PerformCurrentAssessmentAsync(patientId, type);
        
        var improvement = CalculateImprovement(baseline, current);
        
        return new EvaluationResult
        {
            PatientId = patientId,
            EvaluationType = type,
            BaselineScore = baseline.Score,
            CurrentScore = current.Score,
            ImprovementPercentage = improvement,
            EvaluationDate = DateTime.Now,
            Recommendations = await GenerateRecommendationsAsync(improvement, type)
        };
    }
}
```

## 权限管理

### 理疗师权限级别
- **初级理疗师**: 基础理疗操作，需要高级理疗师指导
- **主治理疗师**: 独立制定治疗计划和执行复杂治疗
- **理疗科主任**: 科室管理权限，质量控制和培训指导

### 操作权限控制
```csharp
// 权限验证示例
public async Task<bool> ValidateTherapistPermissionAsync(string operation, int therapistLevel)
{
    var requiredPermissions = await GetRequiredPermissionsAsync(operation);
    var therapistPermissions = await GetTherapistPermissionsAsync(therapistLevel);
    
    return therapistPermissions.Contains(requiredPermissions.All(p => p));
}
```

## 开发状态

### 已实现功能
- ✅ 基础工作台框架
- ✅ 主工作台视图 (TherapistMainView)
- ✅ Prism模块注册和依赖注入
- ✅ 基础架构搭建

### 待实现功能 (v2.0)
- 🔄 理疗计划管理 (TherapyPlanningView)
- 🔄 治疗记录管理 (TreatmentRecordView)
- 🔄 康复管理系统 (RehabilitationManagementView)
- 🔄 设备管理集成
- 🔄 治疗效果评估系统
- 🔄 康复训练指导模块

### 扩展功能规划 (v3.0)
- 🔄 远程康复指导
- 🔄 AI辅助诊断
- 🔄 虚拟现实康复训练
- 🔄 可穿戴设备集成
- 🔄 家庭康复监测

## 开发指南

### 添加新理疗功能
1. **理疗专业知识**: 确保功能符合理疗专业标准
2. **治疗安全性**: 重点考虑治疗过程的安全控制
3. **设备集成**: 考虑与理疗设备的接口集成
4. **效果追踪**: 建立完整的治疗效果评估体系

### 设备接口集成
```csharp
// 设备接口集成示例
public interface ITherapyDevice
{
    Task<bool> ConnectAsync();
    Task<bool> ConfigureParametersAsync(TherapyParameters parameters);
    Task<bool> StartTreatmentAsync();
    Task<bool> StopTreatmentAsync();
    Task<DeviceStatus> GetStatusAsync();
    Task<TreatmentData> GetTreatmentDataAsync();
}

public class ElectrotherapyDevice : ITherapyDevice
{
    public async Task<bool> ConfigureParametersAsync(TherapyParameters parameters)
    {
        // 配置电疗参数：频率、强度、时间等
        return await ConfigureElectrotherapySettingsAsync(parameters);
    }
}
```

## 质量保证

### 治疗质量控制
- **标准操作规程**: 建立和遵守标准化治疗流程
- **安全防护措施**: 确保治疗过程的患者安全
- **效果监测**: 持续监测治疗效果和患者反应
- **持续改进**: 基于效果评估持续优化治疗方案

### 专业能力保障
- **资质认证**: 理疗师资质和认证管理
- **技能培训**: 定期技能培训和考核
- **经验分享**: 建立治疗经验分享机制

## 测试策略

### 功能测试
- **治疗流程测试**: 验证完整的理疗治疗流程
- **设备集成测试**: 测试与理疗设备的接口集成
- **效果评估测试**: 验证治疗效果评估算法的准确性

### 安全性测试
- **治疗安全测试**: 验证治疗过程的安全控制措施
- **设备安全测试**: 测试设备操作的安全性
- **数据安全测试**: 确保患者治疗数据的安全性

## 相关文档

- [LYBT.Desktop.Workbench.Core](../Core/README.md) - 工作台核心框架
- [LYBT.Desktop.Workbench.Consultation](../ConsultationWorkbench/README.md) - 诊疗工作台
- [理疗师操作指南](../../../docs/guides/therapist-operation-guide.md) - 理疗师专业操作手册
- [理疗设备使用指南](../../../docs/guides/therapy-equipment-guide.md) - 理疗设备操作规范
- [康复评估标准](../../../docs/guides/rehabilitation-assessment-standards.md) - 康复功能评估标准

---

**项目状态**: 🔄 开发中 (v1.0基础框架完成) | **最后更新**: 2025-01-01