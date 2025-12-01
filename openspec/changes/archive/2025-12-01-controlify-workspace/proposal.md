# OpenSpec Proposal: controlify-workspace

## 元数据

- **提案ID**: controlify-workspace
- **创建日期**: 2025-12-01
- **状态**: Draft
- **类型**: Refactor
- **影响范围**: MedicalCase模块UI层

## 背景与动机

### 当前问题

医案工作区(MedicalCaseWorkspace)的ViewModel层代码量过大，职责耦合严重：

| ViewModel | 行数 | 问题 |
|-----------|------|------|
| MedicalCaseWorkspaceViewModel | 1481 | 协调逻辑与业务逻辑混杂 |
| PrescriptionPanelViewModel | 1236 | 已拆分Components但仍过大 |
| ConsultationPanelViewModel | 406 | 相对合理 |
| **总计** | **3123** | |

### 根本原因

1. **Panel不是独立控件**: ConsultationPanel和PrescriptionPanel作为嵌入式区域存在，无法独立复用
2. **父子ViewModel强耦合**: Workspace直接持有子ViewModel引用，负责初始化和协调
3. **通信模式不统一**: 混合使用直接调用、事件、属性绑定

## 目标

1. 将诊断和处方拆分为**独立UserControl**，各自拥有完整的MVVM实现
2. 使用Prism **Region + RegionContext**实现松耦合组合
3. 使用**EventAggregator**实现控件间通信
4. 每个ViewModel行数控制在**300行以内**

## 设计方案

### 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                  MedicalCaseWorkspaceView                    │
│  ┌─────────────────────────┐ ┌─────────────────────────────┐ │
│  │   ConsultationRegion    │ │    PrescriptionRegion       │ │
│  │  ┌───────────────────┐  │ │  ┌───────────────────────┐  │ │
│  │  │ ConsultationControl│  │ │  │ PrescriptionControl   │  │ │
│  │  │  (UserControl)     │  │ │  │  (UserControl)        │  │ │
│  │  └───────────────────┘  │ │  └───────────────────────┘  │ │
│  └─────────────────────────┘ └─────────────────────────────┘ │
│                         1 : 1 布局                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ EventAggregator │
                    │  (消息总线)      │
                    └─────────────────┘
```

### 核心组件设计

#### 1. RegionContext 共享上下文

```csharp
/// <summary>
/// 医案工作区共享上下文
/// 通过RegionContext传递给所有子控件
/// </summary>
public class MedicalCaseWorkspaceContext : BindableBase
{
    public Guid MedicalCaseId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public WorkspaceMode Mode { get; init; }
    public bool IsEditing { get; set; }
    public bool IsReadOnly => !IsEditing;
    
    // 审计相关
    public bool IsHistoricalEditMode { get; init; }
    public string? EditReason { get; set; }
}
```

#### 2. 事件定义

```csharp
// 诊断相关事件
public class ConsultationSavedEvent : PubSubEvent<ConsultationSavedPayload> { }
public class ConsultationCompletedEvent : PubSubEvent<Guid> { }

// 处方相关事件
public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedPayload> { }
public class PrescriptionPrintRequestedEvent : PubSubEvent<Guid> { }

// 工作区协调事件
public class WorkspaceModeChangedEvent : PubSubEvent<WorkspaceMode> { }
public class UnsavedChangesDetectedEvent : PubSubEvent<UnsavedChangesPayload> { }
```

#### 3. ConsultationControl (独立控件)

```
LYBT.Desktop.MedicalCase/
├── Controls/
│   ├── ConsultationControl.xaml          # UserControl
│   └── ConsultationControl.xaml.cs       # Code-behind (最小化)
├── ViewModels/
│   └── ConsultationControlViewModel.cs   # 目标 <300行
```

**职责边界**:
- 四诊信息(望闻问切)的展示和编辑
- 辨证论治的编辑
- 诊断完成状态管理
- 自动保存逻辑

**不负责**:
- 导航决策
- 整体工作流控制
- 处方状态感知

#### 4. PrescriptionControl (独立控件)

```
LYBT.Desktop.MedicalCase/
├── Controls/
│   ├── PrescriptionControl.xaml
│   └── PrescriptionControl.xaml.cs
├── ViewModels/
│   └── PrescriptionControlViewModel.cs   # 目标 <300行
│   └── Components/                        # 复用现有Components
│       ├── PrescriptionCalculator.cs
│       ├── PrescriptionValidator.cs
│       ├── PrescriptionItemHandler.cs
│       ├── PrescriptionSaveHandler.cs
│       ├── PrescriptionImportHandler.cs
│       └── PrescriptionDataLoader.cs
```

**职责边界**:
- 药材网格管理
- 价格计算
- 验方导入/历史复制
- 处方保存

**不负责**:
- 患者信息展示
- 诊断状态感知
- 导航决策

#### 5. MedicalCaseWorkspaceViewModel (协调器)

**重构后职责**:
- Region初始化和Context注入
- 导航参数处理
- 按钮可见性和命令路由
- 事件订阅和协调

**目标行数**: <300行

### XAML结构设计

```xml
<!-- MedicalCaseWorkspaceView.xaml -->
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseWorkspaceView"
             xmlns:prism="http://prismlibrary.com/">
    
    <Grid>
        <!-- 顶部: 患者信息栏 -->
        <Border Grid.Row="0">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding PatientName}"/>
                <TextBlock Text="{Binding PatientInfo}"/>
            </StackPanel>
        </Border>
        
        <!-- 中部: 1:1 诊断/处方区域 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>  <!-- 50% -->
                <ColumnDefinition Width="*"/>  <!-- 50% -->
            </Grid.ColumnDefinitions>
            
            <!-- 诊断区域 -->
            <ContentControl Grid.Column="0"
                prism:RegionManager.RegionName="{x:Static local:RegionNames.ConsultationRegion}"
                prism:RegionManager.RegionContext="{Binding WorkspaceContext}"/>
            
            <!-- 处方区域 -->
            <ContentControl Grid.Column="1"
                prism:RegionManager.RegionName="{x:Static local:RegionNames.PrescriptionRegion}"
                prism:RegionManager.RegionContext="{Binding WorkspaceContext}"/>
        </Grid>
        
        <!-- 底部: 操作按钮栏 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal">
            <Button Content="保存草稿" Command="{Binding SaveDraftCommand}"/>
            <Button Content="完成看诊" Command="{Binding CompleteCommand}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

### 子控件获取Context

```csharp
public class ConsultationControlViewModel : BindableBase, IRegionAware
{
    private MedicalCaseWorkspaceContext? _context;
    
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 从RegionContext获取共享上下文
        var region = navigationContext.NavigationService.Region;
        _context = RegionContext.GetObservableContext(region).Value as MedicalCaseWorkspaceContext;
        
        if (_context != null)
        {
            _ = InitializeAsync(_context.MedicalCaseId, _context.PatientId);
        }
    }
    
    // 或者使用IRegionMemberLifetime + RegionContext.GetObservableContext
}
```

### 事件通信示例

```csharp
// ConsultationControlViewModel - 发布诊断完成事件
private async Task ExecuteCompleteConsultation()
{
    var success = await SaveAsync();
    if (success)
    {
        _eventAggregator.GetEvent<ConsultationCompletedEvent>().Publish(_medicalCaseId);
    }
}

// MedicalCaseWorkspaceViewModel - 订阅事件
public MedicalCaseWorkspaceViewModel(IEventAggregator eventAggregator)
{
    eventAggregator.GetEvent<ConsultationCompletedEvent>()
        .Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
    
    eventAggregator.GetEvent<PrescriptionSavedEvent>()
        .Subscribe(OnPrescriptionSaved, ThreadOption.UIThread);
}

private void OnConsultationCompleted(Guid medicalCaseId)
{
    // 更新UI状态
    ConsultationStatusText = "已完成";
    CanComplete = CheckCanComplete();
}
```

## 实施计划

### Phase 1: 基础设施准备

| 任务 | 说明 |
|------|------|
| 1.1 创建Controls目录结构 | `Controls/`, `Controls/Base/` |
| 1.2 定义MedicalCaseWorkspaceContext | 共享上下文类 |
| 1.3 定义Region名称常量 | `RegionNames.cs` |
| 1.4 定义事件类 | `Events/Workspace/` |

### Phase 2: ConsultationControl 控件化

| 任务 | 说明 |
|------|------|
| 2.1 创建ConsultationControl.xaml | 从现有View提取 |
| 2.2 创建ConsultationControlViewModel | 从PanelViewModel迁移 |
| 2.3 实现IRegionAware获取Context | RegionContext集成 |
| 2.4 实现事件发布 | ConsultationSaved/Completed |
| 2.5 注册到Module | RegisterViewWithRegion |

### Phase 3: PrescriptionControl 控件化

| 任务 | 说明 |
|------|------|
| 3.1 创建PrescriptionControl.xaml | 从现有View提取 |
| 3.2 创建PrescriptionControlViewModel | 复用现有Components |
| 3.3 实现IRegionAware获取Context | RegionContext集成 |
| 3.4 实现事件发布 | PrescriptionSaved |
| 3.5 注册到Module | RegisterViewWithRegion |

### Phase 4: Workspace重构

| 任务 | 说明 |
|------|------|
| 4.1 重构MedicalCaseWorkspaceView | 使用Region替代直接嵌入 |
| 4.2 重构MedicalCaseWorkspaceViewModel | 精简为协调器 |
| 4.3 实现事件订阅 | 协调子控件状态 |
| 4.4 清理废弃代码 | 删除直接引用 |

### Phase 5: 验证与清理

| 任务 | 说明 |
|------|------|
| 5.1 单元测试更新 | 适配新结构 |
| 5.2 集成测试 | 端到端验证 |
| 5.3 代码清理 | 删除废弃文件 |
| 5.4 文档更新 | 架构图更新 |

## 预期成果

### 行数对比

| 组件 | 当前 | 目标 | 减少 |
|------|------|------|------|
| MedicalCaseWorkspaceViewModel | 1481 | <300 | -80% |
| ConsultationControlViewModel | 406 | <250 | -38% |
| PrescriptionControlViewModel | 1236 | <300 | -76% |
| **ViewModel总计** | **3123** | **<850** | **-73%** |

### 架构收益

1. **控件可复用**: 诊断/处方控件可在历史查看、打印预览等场景复用
2. **独立测试**: 每个控件可独立进行单元测试
3. **并行开发**: 不同开发者可同时修改不同控件
4. **维护简化**: 职责单一，问题定位更快

### 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| Region初始化时序问题 | 使用IRegionMemberLifetime控制生命周期 |
| Context传递失败 | 添加空检查和日志 |
| 事件内存泄漏 | 在OnNavigatedFrom中取消订阅 |
| 现有功能回归 | 分Phase实施，每Phase验证 |

## 相关文档

- [Prism View Composition](https://prismlibrary.github.io/docs/wpf/view-composition.html)
- [Prism RegionContext](https://docs.prismlibrary.com/docs/platforms/wpf/view-composition.html)
- [cleanup-ui-layer tasks](./cleanup-ui-layer/tasks.md)

## 审批

- [ ] 技术评审通过
- [ ] 工作量确认
- [ ] 开始实施
