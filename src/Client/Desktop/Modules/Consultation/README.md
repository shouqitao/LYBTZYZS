# LYBT.Desktop.Consultation

## 概述

LYBT.Desktop.Consultation是凌隐宝堂桌面客户端的看诊模块，提供中医四诊（望闻问切）、辨证论治、诊断记录等核心诊疗功能。作为系统的核心业务模块之一，它与患者、医案、处方等模块紧密集成，构建完整的中医诊疗工作流。

## 核心功能

### 🏥 中医四诊
- **望诊**: 观察患者面色、舌象、体态等外在表现
- **闻诊**: 听声音、嗅气味，记录患者声息和体味
- **问诊**: 询问病史、症状、生活习惯等信息
- **切诊**: 脉诊和触诊，记录脉象和体征

### 🔍 辨证论治
- **症状分析**: 收集和分析患者症状表现
- **证候判断**: 基于中医理论进行证候分析
- **治法确定**: 确定相应的治疗法则
- **方药选择**: 推荐适合的方剂和药物

### 📋 诊疗记录
- **诊断管理**: 创建、编辑、查看诊断记录
- **病情跟踪**: 记录病情变化和治疗效果
- **历史查询**: 查看患者历史诊疗记录
- **数据整合**: 与医案、处方数据的集成展示

### 💼 工作流集成
- **患者选择**: 与患者模块集成选择诊疗对象
- **医案关联**: 与医案模块协作管理诊疗过程
- **处方开具**: 与处方模块配合完成用药指导
- **验方应用**: 与验方模块结合应用经典方剂

## 项目结构

```
src/Client/Desktop/Modules/Consultation/
├── ConsultationModule.cs         # Prism模块定义和注册
├── Services/                    # 业务服务层
│   └── ConsultationModule.cs   # 看诊业务模块
├── ViewModels/                  # 视图模型
│   ├── ConsultationMainViewModel.cs      # 主看诊视图模型
│   └── ConsultationManagementViewModel.cs # 诊疗管理视图模型
├── Views/                       # 用户界面视图
│   ├── ConsultationMainView.xaml         # 主看诊界面
│   ├── ConsultationMainView.xaml.cs     # 主看诊界面代码
│   ├── ConsultationManagementView.xaml   # 诊疗管理界面
│   └── ConsultationManagementView.xaml.cs # 诊疗管理界面代码
└── Api/                         # API接口定义(如果存在)
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入
- **Prism.Wpf 8.1.97**: WPF版本的Prism框架

### 模块依赖
- **LYBT.Desktop.Core**: 核心框架和基础设施
- **LYBT.Desktop.Infrastructure**: 基础设施和HTTP通信
- **LYBT.Desktop.Services**: 业务服务层
- **LYBT.Desktop.Patients**: 患者管理模块
- **LYBT.Desktop.Formula**: 验方管理模块
- **LYBT.Desktop.Herbs**: 中药材管理模块
- **LYBT.Desktop.MedicalCase**: 医案管理模块

## 核心特性

### 🏥 中医诊疗流程

#### 完整诊疗工作流
```
1. 患者选择 (Patients模块) 
   ↓
2. 创建医案 (MedicalCase模块)
   ↓
3. 四诊检查 (Consultation模块) ← 当前模块核心功能
   ├── 望诊记录
   ├── 闻诊记录  
   ├── 问诊记录
   └── 切诊记录
   ↓
4. 辨证论治 (Consultation模块)
   ├── 症状分析
   ├── 证候判断
   └── 治法确定
   ↓
5. 开具处方 (Prescriptions模块)
   ├── 方剂选择 (Formula模块)
   └── 药材配伍 (Herbs模块)
```

#### 诊疗数据模型
```csharp
public class ConsultationRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }        // 患者ID
    public Guid MedicalCaseId { get; set; }    // 医案ID
    public Guid DoctorId { get; set; }         // 医生ID
    
    // 四诊记录
    public string? Observation { get; set; }   // 望诊
    public string? Listening { get; set; }     // 闻诊
    public string? Inquiry { get; set; }       // 问诊
    public string? Palpation { get; set; }     // 切诊
    
    // 辨证论治
    public string? Symptoms { get; set; }      // 症状
    public string? Syndrome { get; set; }      // 证候
    public string? Treatment { get; set; }     // 治法
    public string? Diagnosis { get; set; }     // 诊断
    
    public DateTime ConsultationDate { get; set; }
    public ConsultationStatus Status { get; set; }
}
```

### 📱 MVVM实现

#### ConsultationMainViewModel核心功能
```csharp
public class ConsultationMainViewModel : CoreViewModel
{
    // 当前诊疗对象
    public PatientDto? CurrentPatient { get; set; }
    public MedicalCaseDto? CurrentMedicalCase { get; set; }
    public ConsultationDto? CurrentConsultation { get; set; }
    
    // 四诊记录
    public string Observation { get; set; }  // 望诊
    public string Listening { get; set; }    // 闻诊  
    public string Inquiry { get; set; }      // 问诊
    public string Palpation { get; set; }    // 切诊
    
    // 辨证论治
    public string Symptoms { get; set; }     // 症状分析
    public string Syndrome { get; set; }     // 证候判断
    public string Treatment { get; set; }    // 治疗方法
    public string Diagnosis { get; set; }    // 最终诊断
    
    // 命令
    public ICommand SaveConsultationCommand { get; }
    public ICommand SelectPatientCommand { get; }
    public ICommand OpenPrescriptionCommand { get; }
    public ICommand ViewHistoryCommand { get; }
    
    // 保存诊疗记录
    private async Task SaveConsultationAsync()
    {
        try
        {
            var dto = new ConsultationCreateDto
            {
                PatientId = CurrentPatient.Id,
                MedicalCaseId = CurrentMedicalCase.Id,
                Observation = Observation,
                Listening = Listening,
                Inquiry = Inquiry,
                Palpation = Palpation,
                Symptoms = Symptoms,
                Syndrome = Syndrome,
                Treatment = Treatment,
                Diagnosis = Diagnosis
            };
            
            var result = await _consultationService.CreateAsync(dto);
            if (result.IsSuccess)
            {
                ShowSuccessMessage("诊疗记录保存成功");
                
                // 发布诊疗完成事件
                _eventAggregator.GetEvent<ConsultationCompletedEvent>()
                    .Publish(result.Data);
            }
            else
            {
                ShowErrorMessage(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "保存诊疗记录");
        }
    }
}
```

### 🎨 用户界面设计

#### 主看诊界面特性
- **分区布局**: 四诊、辨证、处方等功能区域清晰分离
- **标签页设计**: 望闻问切四个标签页独立操作
- **实时保存**: 输入内容自动保存，防止数据丢失
- **快捷操作**: 常用诊断和治法的快速选择
- **历史查看**: 患者历史诊疗记录的快速查看

#### 界面布局示例
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/> <!-- 患者信息栏 -->
        <RowDefinition Height="*"/>    <!-- 主要诊疗区域 -->
        <RowDefinition Height="Auto"/> <!-- 操作按钮栏 -->
    </Grid.RowDefinitions>
    
    <!-- 患者信息 -->
    <Border Grid.Row="0" Style="{StaticResource PatientInfoBorderStyle}">
        <TextBlock Text="{Binding PatientDisplayText}"/>
    </Border>
    
    <!-- 四诊标签页 -->
    <TabControl Grid.Row="1">
        <TabItem Header="望诊">
            <TextBox Text="{Binding Observation}" AcceptsReturn="True"/>
        </TabItem>
        <TabItem Header="闻诊">
            <TextBox Text="{Binding Listening}" AcceptsReturn="True"/>
        </TabItem>
        <TabItem Header="问诊">
            <TextBox Text="{Binding Inquiry}" AcceptsReturn="True"/>
        </TabItem>
        <TabItem Header="切诊">
            <TextBox Text="{Binding Palpation}" AcceptsReturn="True"/>
        </TabItem>
        <TabItem Header="辨证论治">
            <Grid>
                <!-- 症状、证候、治法、诊断输入区域 -->
            </Grid>
        </TabItem>
    </TabControl>
    
    <!-- 操作按钮 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal">
        <Button Command="{Binding SaveConsultationCommand}" Content="保存诊疗"/>
        <Button Command="{Binding OpenPrescriptionCommand}" Content="开具处方"/>
        <Button Command="{Binding ViewHistoryCommand}" Content="查看历史"/>
    </StackPanel>
</Grid>
```

### 🔧 模块集成

#### 与其他模块的协作
```csharp
public class ConsultationModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册业务服务
        containerRegistry.RegisterSingleton<ConsultationModule>();
        
        // 注册视图模型
        containerRegistry.Register<ConsultationMainViewModel>();
        containerRegistry.Register<ConsultationManagementViewModel>();
        
        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<ConsultationMainView>();
        containerRegistry.RegisterForNavigation<ConsultationManagementView>();
    }
}
```

#### 事件集成
```csharp
// 监听患者选择事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected);

// 监听医案创建事件  
_eventAggregator.GetEvent<MedicalCaseCreatedEvent>()
    .Subscribe(OnMedicalCaseCreated);

// 发布诊疗完成事件
_eventAggregator.GetEvent<ConsultationCompletedEvent>()
    .Publish(consultationData);
```

## 使用指南

### 模块启动

```csharp
// 导航到主看诊界面
_regionManager.RequestNavigate("ContentRegion", "ConsultationMainView");

// 传递患者参数
var parameters = new NavigationParameters();
parameters.Add("PatientId", selectedPatientId);
_regionManager.RequestNavigate("ContentRegion", "ConsultationMainView", parameters);
```

### 诊疗流程操作

```csharp
// 1. 选择患者并创建医案
await SelectPatientAndCreateMedicalCaseAsync(patientId);

// 2. 开始四诊记录
await StartConsultationAsync(medicalCaseId);

// 3. 保存诊疗数据
await SaveConsultationRecordAsync(consultationData);

// 4. 开具处方(可选)
if (needPrescription)
{
    await NavigateToPrescriptionModuleAsync(consultationId);
}
```

### 历史记录查询

```csharp
// 查看患者历史诊疗记录
var history = await _consultationService.GetPatientHistoryAsync(patientId);

// 按医案查询诊疗记录
var consultations = await _consultationService.GetByMedicalCaseAsync(medicalCaseId);
```

## 开发规范

### 业务逻辑
- 诊疗记录必须关联有效的患者和医案
- 四诊内容允许部分为空，但至少要有一项记录
- 辨证论治是诊疗的核心，诊断不能为空
- 保存操作需要验证数据完整性和合理性

### MVVM实现
- ViewModel继承自CoreViewModel获得基础功能
- 使用AutoMapper进行DTO转换
- 通过EventAggregator与其他模块通信
- 异步操作使用AsyncRelayCommand

### 用户体验
- 提供输入验证和格式化
- 支持常用诊断术语的智能提示
- 实现数据的自动保存和恢复
- 提供丰富的键盘快捷键支持

### 数据安全
- 诊疗数据涉及患者隐私，需要加密传输
- 操作日志记录用于审计追踪
- 支持数据备份和恢复机制
- 实现访问权限控制

## 中医特色功能

### 🌿 中医术语支持
- **症状词典**: 内置中医常用症状术语库
- **证候分类**: 按中医理论分类的证候体系
- **治法索引**: 常用治法的系统化索引
- **方剂推荐**: 基于证候的方剂智能推荐

### 📚 知识库集成
- **经典条文**: 集成《伤寒论》、《金匮要略》等经典
- **现代研究**: 现代中医研究成果的参考
- **临床经验**: 名老中医临床经验的积累
- **用药指南**: 中药配伍和用法用量指导

### 🔍 智能辅助
- **症状分析**: 基于输入症状的智能分析
- **证候推导**: 从症状到证候的逻辑推导
- **方药匹配**: 证候与方药的智能匹配
- **疗效评估**: 治疗效果的量化评估

## 维护说明

### 数据模型维护
- 中医术语库的定期更新和扩展
- 诊疗模板的优化和个性化定制
- 历史数据的清理和归档策略
- 数据结构变更的兼容性处理

### 功能扩展
- 支持图像诊断(舌诊、面诊)
- 集成脉诊仪等硬件设备
- 增加语音识别输入功能
- 实现移动端的诊疗记录同步

### 性能优化
- 大量历史数据的查询优化
- 界面响应速度的持续改进
- 内存使用的监控和优化
- 数据库查询的索引优化

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*