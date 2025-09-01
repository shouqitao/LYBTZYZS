# LYBT.Desktop.Workbench.Receptionist

凌隐宝堂中医诊所系统 - 前台接待工作台模块

## 项目概述

前台接待工作台是专为前台接待员设计的患者服务和管理环境，提供患者接待、预约管理、基础登记、排队叫号等核心功能。采用现代化WPF界面和Prism MVVM架构，支持完整的前台服务流程，并配有专门的前台主题样式。

## 目录结构

```
ReceptionistWorkbench/
├── Resources/                         # 资源文件
│   └── ReceptionistTheme.xaml         # 前台专用主题样式
├── ViewModels/                        # 视图模型
│   └── ReceptionistMainViewModel.cs   # 前台主工作台视图模型
├── Views/                            # 用户界面
│   ├── AppointmentManagementView.xaml     # 预约管理视图
│   ├── AppointmentManagementView.xaml.cs  # 预约管理视图代码后置
│   ├── BasicRegistrationView.xaml         # 基础登记视图
│   ├── BasicRegistrationView.xaml.cs      # 基础登记视图代码后置
│   ├── PatientReceptionView.xaml          # 患者接待视图
│   ├── PatientReceptionView.xaml.cs       # 患者接待视图代码后置
│   ├── ReceptionistMainView.xaml          # 前台主工作台视图
│   └── ReceptionistMainView.xaml.cs       # 前台主视图代码后置
└── ReceptionistWorkbenchModule.cs     # Prism模块定义
```

## 核心功能

### 1. 前台主工作台 (ReceptionistMainView)
- **统一服务界面**: 集成所有前台服务功能的中央操作台
- **患者信息快速查询**: 支持多种方式快速检索患者信息
- **当日预约总览**: 显示当日预约情况和就诊进度
- **排队叫号**: 实时显示排队状态和叫号信息

### 2. 患者接待服务 (PatientReceptionView)
- **患者签到**: 处理患者到诊所后的签到登记
- **身份验证**: 核实患者身份和预约信息
- **就诊引导**: 指导患者到相应诊室或等候区
- **问诊准备**: 协助患者完成就诊前的必要准备工作

### 3. 预约管理系统 (AppointmentManagementView)
- **预约登记**: 为患者安排就诊预约时间
- **预约查询**: 查询和修改现有预约信息
- **时间安排**: 管理医生的排班和可预约时段
- **预约提醒**: 发送预约提醒短信或电话通知
- **预约统计**: 统计预约情况和到诊率

### 4. 基础登记服务 (BasicRegistrationView)
- **新患者登记**: 为首次就诊患者建立档案
- **基础信息录入**: 录入患者姓名、联系方式、身份证等基础信息
- **病史简录**: 记录患者主要病史和过敏史
- **医保信息**: 录入医保卡号和报销比例信息
- **紧急联系人**: 登记患者紧急联系人信息

## 前台服务流程

### 标准接待流程
1. **患者到诊**: 患者到达诊所
2. **身份确认**: 确认患者身份和预约信息
3. **签到登记**: 完成电子签到或手工登记
4. **候诊安排**: 安排患者到相应等候区域
5. **叫号提醒**: 根据医生排班情况安排就诊顺序
6. **就诊引导**: 引导患者到相应诊室

### 新患者登记流程
```csharp
// 示例新患者登记流程
public async Task<PatientRegistrationResult> RegisterNewPatientAsync(PatientRegistrationDto dto)
{
    // 1. 验证患者信息
    var validationResult = await ValidatePatientInfoAsync(dto);
    if (!validationResult.IsValid)
    {
        return PatientRegistrationResult.Failed(validationResult.ErrorMessage);
    }
    
    // 2. 检查重复登记
    var duplicateCheck = await CheckDuplicatePatientAsync(dto.IdNumber, dto.PhoneNumber);
    if (duplicateCheck.HasDuplicate)
    {
        return await HandleDuplicatePatientAsync(duplicateCheck.ExistingPatient, dto);
    }
    
    // 3. 创建患者档案
    var patient = await CreatePatientRecordAsync(dto);
    
    // 4. 分配患者编号
    patient.PatientCode = await GeneratePatientCodeAsync();
    
    // 5. 建立就诊卡
    var medicalCard = await CreateMedicalCardAsync(patient.Id);
    
    // 6. 录入系统
    await SavePatientRecordAsync(patient);
    
    return PatientRegistrationResult.Success(patient);
}
```

### 预约管理流程
```csharp
// 示例预约管理流程
public async Task<AppointmentResult> CreateAppointmentAsync(AppointmentCreateDto dto)
{
    // 1. 检查医生可用性
    var doctorAvailability = await CheckDoctorAvailabilityAsync(dto.DoctorId, dto.AppointmentTime);
    if (!doctorAvailability.IsAvailable)
    {
        return AppointmentResult.Failed("医生在该时段不可预约");
    }
    
    // 2. 检查时段容量
    var timeSlotCapacity = await CheckTimeSlotCapacityAsync(dto.AppointmentTime);
    if (timeSlotCapacity.IsFull)
    {
        return AppointmentResult.Failed("该时段预约已满");
    }
    
    // 3. 创建预约记录
    var appointment = await CreateAppointmentRecordAsync(dto);
    
    // 4. 发送确认通知
    await SendAppointmentConfirmationAsync(appointment);
    
    // 5. 更新医生排班
    await UpdateDoctorScheduleAsync(dto.DoctorId, dto.AppointmentTime);
    
    return AppointmentResult.Success(appointment);
}
```

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
- **服务流程模式**: 标准化的前台服务流程

### 主题样式系统
- **ReceptionistTheme.xaml**: 前台专用主题样式资源
- **用户友好设计**: 针对前台工作特点优化的界面设计
- **快速操作**: 大按钮、清晰标识的快速操作界面
- **信息展示**: 优化的患者信息和预约信息展示样式

## 模块注册

### ReceptionistWorkbenchModule
Prism模块定义，负责前台工作台的初始化和服务注册：

```csharp
public class ReceptionistWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册前台工作台主视图
        containerRegistry.RegisterForNavigation<ReceptionistMainView>();
        
        // 注册功能视图
        containerRegistry.RegisterForNavigation<PatientReceptionView>();
        containerRegistry.RegisterForNavigation<AppointmentManagementView>();
        containerRegistry.RegisterForNavigation<BasicRegistrationView>();
        
        // 预留：未来可注册前台接待相关的其他视图和服务
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册自定义的ViewModel映射
        ViewModelLocationProvider.Register<ReceptionistMainView, ReceptionistMainViewModel>();
    }
}
```

## 用户界面设计

### 前台主工作台界面
- **快速导航区**: 大图标按钮，快速访问常用功能
- **当日概览**: 显示当日预约数量、到诊人数等关键指标
- **患者搜索**: 强大的患者信息搜索功能
- **排队显示**: 实时显示当前排队和叫号情况

### 患者接待界面
- **患者信息卡**: 清晰显示患者基本信息
- **就诊历史**: 显示患者近期就诊记录
- **操作按钮**: 大按钮设计，支持快速签到和引导操作
- **状态指示**: 清晰的视觉状态指示器

### 预约管理界面
- **日历视图**: 直观的日历选择和时段显示
- **医生排班**: 实时显示各医生的排班状况
- **预约列表**: 可筛选和排序的预约列表
- **快速操作**: 支持预约的快速创建、修改和取消

### 基础登记界面
- **分步录入**: 将登记信息分组，提高录入效率
- **智能提示**: 提供常见信息的智能提示和自动完成
- **格式验证**: 实时验证信息格式的正确性
- **快速保存**: 支持信息的快速保存和打印

## 集成接口

### 与业务模块的集成
- **患者模块**: 患者档案的创建、查询和更新
- **用户模块**: 医生排班信息和可用性查询
- **诊疗模块**: 就诊状态查询和流程协调
- **财务模块**: 挂号费收取和医保信息验证

### 外部系统集成
```csharp
// 医保系统集成示例
public async Task<InsuranceVerificationResult> VerifyInsuranceAsync(string insuranceCardNumber)
{
    try
    {
        var insuranceInfo = await _insuranceSystemApi.VerifyCardAsync(insuranceCardNumber);
        
        return new InsuranceVerificationResult
        {
            IsValid = insuranceInfo.IsActive,
            PatientName = insuranceInfo.PatientName,
            CoverageRatio = insuranceInfo.CoverageRatio,
            RemainingBalance = insuranceInfo.RemainingBalance
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "医保验证失败：{CardNumber}", insuranceCardNumber);
        return InsuranceVerificationResult.Failed("医保系统暂时不可用");
    }
}

// 短信通知系统集成
public async Task SendAppointmentReminderAsync(Appointment appointment)
{
    var message = $"尊敬的{appointment.PatientName}，您预约的{appointment.AppointmentTime:MM月dd日 HH:mm}就诊时间即将到达，请准时到诊。";
    
    await _smsService.SendMessageAsync(appointment.PatientPhone, message);
}
```

## 服务质量管理

### 服务标准
- **接待时间**: 患者到诊后3分钟内完成接待
- **登记准确性**: 患者信息录入准确率达99%以上
- **预约及时性**: 电话预约在2分钟内处理完成
- **服务态度**: 保持专业、耐心、热情的服务态度

### 质量监控
```csharp
// 服务质量监控示例
public class ReceptionistPerformanceMonitor
{
    public async Task<PerformanceReport> GenerateDailyReportAsync(Guid receptionistId, DateTime date)
    {
        var metrics = new PerformanceMetrics();
        
        // 接待患者数量
        metrics.PatientsReceived = await CountPatientsReceivedAsync(receptionistId, date);
        
        // 平均接待时间
        metrics.AverageReceptionTime = await CalculateAverageReceptionTimeAsync(receptionistId, date);
        
        // 预约成功率
        metrics.AppointmentSuccessRate = await CalculateAppointmentSuccessRateAsync(receptionistId, date);
        
        // 信息录入准确率
        metrics.DataAccuracyRate = await CalculateDataAccuracyRateAsync(receptionistId, date);
        
        return new PerformanceReport(metrics);
    }
}
```

## 权限管理

### 前台接待员权限
- **患者信息**: 查看、创建、更新患者基础信息
- **预约管理**: 创建、查询、修改、取消预约
- **签到操作**: 处理患者签到和引导
- **基础统计**: 查看当日接待统计数据

### 敏感操作控制
- **信息修改**: 重要信息修改需要主管审核
- **预约取消**: 当日预约取消需要记录原因
- **数据导出**: 患者信息导出需要特殊权限

## 开发状态

### 已实现功能
- ✅ 基础工作台框架
- ✅ 四个核心功能视图 (PatientReception、AppointmentManagement、BasicRegistration)
- ✅ 专门的前台主题样式 (ReceptionistTheme.xaml)
- ✅ Prism模块注册和依赖注入

### 规划功能 (v2.0)
- 🔄 排队叫号系统
- 🔄 短信通知集成
- 🔄 医保系统集成
- 🔄 自助服务终端支持
- 🔄 多语言界面支持
- 🔄 语音播报功能

## 开发指南

### 添加新前台功能
1. **分析服务需求**: 确定新功能的服务流程
2. **设计用户界面**: 遵循前台工作的界面设计原则
3. **实现业务逻辑**: 编写符合前台工作规范的业务逻辑
4. **集成现有系统**: 与患者、预约等系统模块集成

### 自定义前台主题
```xaml
<!-- 前台主题样式示例 -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 前台主色调：温暖的绿色 -->
    <SolidColorBrush x:Key="ReceptionistPrimaryBrush" Color="#4CAF50"/>
    
    <!-- 大按钮样式 -->
    <Style x:Key="ReceptionistLargeButtonStyle" TargetType="Button">
        <Setter Property="MinHeight" Value="60"/>
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="Background" Value="{StaticResource ReceptionistPrimaryBrush}"/>
    </Style>
    
</ResourceDictionary>
```

## 测试策略

### 用户体验测试
- **操作效率测试**: 测试常见操作的完成时间
- **界面易用性**: 验证界面对前台工作人员的友好程度
- **流程完整性**: 测试完整的患者接待和预约流程

### 数据准确性测试
- **信息录入测试**: 验证患者信息录入的准确性
- **预约逻辑测试**: 测试预约冲突检测和时段管理
- **集成数据测试**: 验证与其他系统的数据同步

## 相关文档

- [LYBT.Desktop.Workbench.Core](../Core/README.md) - 工作台核心框架
- [LYBT.Desktop.Patients](../../Modules/Patients/README.md) - 患者管理模块
- [前台接待操作指南](../../../docs/guides/receptionist-operation-guide.md) - 前台人员操作手册
- [预约管理规范](../../../docs/guides/appointment-management-guide.md) - 预约管理标准流程
- [患者隐私保护指南](../../../docs/guides/patient-privacy-guide.md) - 患者信息保护规范

---

**项目状态**: ✅ 生产就绪 (v1.0核心功能完整) | **最后更新**: 2025-01-01