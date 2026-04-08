# View层完善完整计划 - RC就绪

**目标**: 完成View层所有缺失项，达到RC就绪条件  
**预计总工时**: 40小时  
**优先级**: P0 (高) - RC阻塞项

---

## 📋 执行策略

### 核心原则
1. **Control优先**: 先补充缺失的View文件 (13个)
2. **统一Dialog**: 创建BaseDialogWindow，统一3种实现
3. **导航守卫**: 实现编辑保护，防止数据丢失
4. **并行执行**: 独立模块可并行开发
5. **测试先行**: 每个View补充对应单元测试

---

## 🎯 Phase 1: 补充缺失Views (20小时)

### 1.1 MasterDetail Views (8小时) - P0

这些Views作为Role Management Views的独立导航目标

#### 1.1.1 UserMasterDetailView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/UserMasterDetailView.xaml`
**结构**:
```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserMasterDetailView"
             xmlns:local="clr-namespace:LYBT.Desktop.Users.Controls">
    <Grid>
        <local:UserMasterDetailControl DataContext="{Binding}" />
    </Grid>
</UserControl>
```
**任务**:
- [ ] 创建XAML文件
- [ ] 创建Code-behind
- [ ] 在UsersModule.cs中注册
- [ ] 添加单元测试

#### 1.1.2 PatientMasterDetailView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientMasterDetailView.xaml`
**结构**: 同上，引用PatientMasterDetailControl

#### 1.1.3 FormulaMasterDetailView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaMasterDetailView.xaml`
**结构**: 引用FormulaMasterDetailControl

#### 1.1.4 HerbMasterDetailView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Views/HerbMasterDetailView.xaml`
**结构**: 引用HerbMasterDetailControl

#### 1.1.5 MedicalCaseMasterDetailView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseMasterDetailView.xaml`
**结构**: 引用MedicalCaseMasterDetailControl

### 1.2 Workspace Views (8小时) - P1

#### 1.2.1 PrescriptionEditorView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PrescriptionEditorView.xaml`
**结构**:
```xml
<UserControl>
    <Grid>
        <!-- 处方编辑界面 -->
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 工具栏 -->
            <RowDefinition Height="*"/>    <!-- 药材列表 -->
            <RowDefinition Height="Auto"/> <!-- 统计信息 -->
        </Grid.RowDefinitions>
    </Grid>
</UserControl>
```

#### 1.2.2 ConsultationEditorView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/ConsultationEditorView.xaml`
**结构**: 四诊信息编辑界面

#### 1.2.3 MedicalCaseCommandsView (2小时)
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseCommandsView.xaml`
**结构**: 命令按钮面板

#### 1.2.4 PendingQueueView (2小时)
**文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PendingQueueView.xaml`
**结构**: 候诊队列视图

### 1.3 辅助Views (4小时) - P2

#### 1.3.1 CardReaderView (1小时)
#### 1.3.2 PatientCardReaderView (1小时)
#### 1.3.3 PatientImportExportView (1小时)
#### 1.3.4 FormulaHerbItemView (1小时)

---

## 🎯 Phase 2: Dialog统一 (10小时)

### 2.1 创建BaseDialogWindow (3小时)

**文件**: `src/Client/Desktop/Shell/Dialogs/BaseDialogWindow.xaml`

**设计规范**:
```xml
<Window x:Class="LYBT.Desktop.Shell.Dialogs.BaseDialogWindow"
        WindowStartupLocation="CenterOwner"
        SizeToContent="WidthAndHeight"
        MinWidth="400"
        MaxWidth="800">
    <Window.Resources>
        <!-- 统一按钮样式 -->
        <Style x:Key="DialogButton" TargetType="Button">
            <Setter Property="MinWidth" Value="80"/>
            <Setter Property="Margin" Value="8,0"/>
        </Style>
    </Window.Resources>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 标题 -->
            <RowDefinition Height="*"/>    <!-- 内容区 -->
            <RowDefinition Height="Auto"/> <!-- 按钮区 -->
        </Grid.RowDefinitions>
        
        <!-- 标题栏 -->
        <Border Grid.Row="0" Background="{StaticResource PrimaryBrush}">
            <TextBlock Text="{Binding Title}" FontSize="16" Margin="16,12"/>
        </Border>
        
        <!-- 内容区 -->
        <ContentControl Grid.Row="1" Content="{Binding Content}"/>
        
        <!-- 按钮区 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" 
                    HorizontalAlignment="Right" Margin="16">
            <Button Content="确定" IsDefault="True" Style="{StaticResource DialogButton}"/>
            <Button Content="取消" IsCancel="True" Style="{StaticResource DialogButton}"/>
        </StackPanel>
    </Grid>
</Window>
```

### 2.2 创建DialogService扩展 (2小时)

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/DialogServiceExtensions.cs`

```csharp
public static class DialogServiceExtensions
{
    public static void ShowConfirm(this IDialogService dialogService, 
        string message, string title, Action<bool> callback)
    {
        var parameters = new DialogParameters
        {
            { "message", message },
            { "title", title }
        };
        dialogService.ShowDialog("ConfirmDialog", parameters, 
            r => callback(r.Result == ButtonResult.OK));
    }
    
    public static void ShowInput(this IDialogService dialogService,
        string message, string title, Action<string?> callback)
    {
        // 实现...
    }
}
```

### 2.3 迁移现有Dialog (5小时)

| Dialog | 当前实现 | 目标实现 | 工时 |
|--------|----------|----------|------|
| InputDialog | Custom Window | BaseDialogWindow | 1h |
| MessageDialog | Custom Window | BaseDialogWindow | 1h |
| ConfirmationDialog | Custom Window | BaseDialogWindow | 1h |
| SyncConflictDialog | Control | BaseDialogWindow | 1h |
| UnfinishedCaseDialog | Control | BaseDialogWindow | 1h |

---

## 🎯 Phase 3: 导航守卫 (6小时)

### 3.1 实现IConfirmNavigationRequest (4小时)

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationGuard.cs`

```csharp
public class NavigationGuard : IConfirmNavigationRequest
{
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;
    
    public NavigationGuard(IDialogService dialogService, 
        IEventAggregator eventAggregator)
    {
        _dialogService = dialogService;
        _eventAggregator = eventAggregator;
    }
    
    public void ConfirmNavigationRequest(NavigationContext navigationContext, 
        Action<bool> continuationCallback)
    {
        // 检查当前ViewModel是否有未保存更改
        var currentVM = GetCurrentViewModel();
        if (currentVM is IEditable editable && editable.HasChanges)
        {
            _dialogService.ShowConfirm(
                "有未保存的更改，是否保存？",
                "确认导航",
                result =>
                {
                    if (result)
                    {
                        editable.SaveAsync().ContinueWith(_ => 
                            continuationCallback(true));
                    }
                    else
                    {
                        continuationCallback(true);
                    }
                });
        }
        else
        {
            continuationCallback(true);
        }
    }
}
```

### 3.2 添加IEditable接口 (1小时)

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Interfaces/IEditable.cs`

```csharp
public interface IEditable
{
    bool HasChanges { get; }
    Task<bool> SaveAsync();
    Task<bool> CanNavigateAwayAsync();
}
```

### 3.3 在ViewModels中实现 (1小时)

在MasterDetailViewModels中实现IEditable接口

---

## 🎯 Phase 4: ViewModel注册 (2小时)

### 4.1 Module注册更新

在每个Module的Module.cs中添加:

```csharp
// UsersModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // Views
    containerRegistry.RegisterForNavigation<UserMasterDetailView>("UserMasterDetailView");
    containerRegistry.RegisterForNavigation<UserManagementView>("UserManagementView");
    
    // ... 其他注册
}
```

### 4.2 Region配置

在Role Module中配置导航映射:

```csharp
// AdminModule.cs
public void OnInitialized(IContainerProvider containerProvider)
{
    var regionManager = containerProvider.Resolve<IRegionManager>();
    
    regionManager.RegisterViewWithRegion("MainRegion", typeof(AdminHomeView));
    regionManager.RegisterViewWithRegion("SidebarRegion", typeof(AdminSidebarView));
}
```

---

## 🎯 Phase 5: 响应式布局优化 (2小时)

### 5.1 更新MasterDetailLayout

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/MasterDetailLayout.xaml`

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" MinWidth="300" MaxWidth="600"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="2*" MinWidth="400"/>
    </Grid.ColumnDefinitions>
    
    <!-- 响应式触发器 -->
    <Grid.Style>
        <Style TargetType="Grid">
            <Style.Triggers>
                <DataTrigger Binding="{Binding ActualWidth, RelativeSource={RelativeSource Self}}" 
                             Value="800">
                    <Setter Property="Grid.ColumnDefinitions">
                        <Setter.Value>
                            <ColumnDefinitionCollection>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="0"/>
                                <ColumnDefinition Width="0"/>
                            </ColumnDefinitionCollection>
                        </Setter.Value>
                    </Setter>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Grid.Style>
</Grid>
```

---

## 🧪 Phase 6: 测试补充 (4小时)

### 6.1 View单元测试

为每个新View创建基础测试:

```csharp
[Fact]
public void UserMasterDetailView_InitializesCorrectly()
{
    // Arrange
    var view = new UserMasterDetailView();
    
    // Assert
    view.Should().NotBeNull();
    view.DataContext.Should().BeAssignableTo<UserMasterDetailViewModel>();
}
```

### 6.2 导航测试

```csharp
[Fact]
public void Navigation_UserMasterDetailView_RegistersCorrectly()
{
    // 测试导航注册
}
```

---

## 📅 执行时间表

### Week 1: Views补充 (20小时)
- Day 1-2: MasterDetail Views (8h)
- Day 3-4: Workspace Views (8h)
- Day 5: 辅助Views + 测试 (4h)

### Week 2: Dialog统一 + 导航守卫 (16小时)
- Day 6-7: BaseDialogWindow + 迁移 (10h)
- Day 8: 导航守卫实现 (6h)

### Week 3: 收尾优化 (4小时)
- Day 9: 注册配置 + 响应式布局 (2h)
- Day 10: 测试补充 + Bug修复 (2h)

---

## 🔗 依赖关系

```
Phase 1 (Views)
    ↓
Phase 4 (注册) - 依赖Phase 1
    ↓
Phase 3 (导航守卫) - 依赖Phase 4
    ↓
Phase 2 (Dialog) - 可并行
    ↓
Phase 5 (布局优化)
    ↓
Phase 6 (测试)
```

**并行机会**:
- Phase 2 (Dialog) 可与 Phase 1 并行
- Phase 5 (布局) 可与 Phase 3 并行

---

## 🎯 成功标准

### 验收标准
- [ ] 13个View文件全部创建完成
- [ ] 所有View能在Module中正确注册
- [ ] 导航可直接跳转到任意View
- [ ] Dialog样式统一，无3种实现并存
- [ ] 编辑状态离开时有确认提示
- [ ] 响应式布局适配800px-4K分辨率
- [ ] 新增Views单元测试覆盖率>80%

### RC就绪标准
- [ ] View文件完整性: 100% (当前70%)
- [ ] 导航逻辑完善度: 100% (当前80%)
- [ ] Dialog统一度: 100% (当前60%)
- [ ] **综合RC就绪度: 95%+**

---

## 📁 输出物清单

### 文件输出
1. 13个新View文件
2. BaseDialogWindow.xaml
3. NavigationGuard.cs
4. IEditable.cs
5. DialogServiceExtensions.cs
6. 13个View测试文件

### 文档输出
1. View注册配置更新
2. 导航路由映射文档
3. Dialog使用指南

---

## ⚠️ 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Control依赖复杂 | 高 | 先梳理Control依赖关系 |
| Dialog迁移破坏现有功能 | 中 | 保留旧实现，渐进迁移 |
| 导航守卫影响用户体验 | 低 | 添加"不再提示"选项 |
| 响应式布局测试不足 | 中 | 多分辨率测试清单 |

---

## 🚀 启动检查清单

启动前确认:
- [ ] 所有依赖NuGet包已安装
- [ ] 现有代码已提交到Git
- [ ] 开发环境配置正确
- [ ] 测试环境可用
- [ ] 40小时工时分配确认

---

**计划生成时间**: 2026-04-08  
**计划版本**: v1.0  
**下次评审**: 每Phase完成后
