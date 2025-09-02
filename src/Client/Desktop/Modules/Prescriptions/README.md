# 处方管理模块 (Prescriptions Module)

**最后更新**: 2025-09-01  
**模块状态**: ✅ 生产就绪  
**对应后端**: LYBT.Module.Prescriptions  
**需求参考**: [功能需求-处方管理模块](../../../../../docs/requirements/functional-requirements.md#5️⃣-处方管理模块-prescriptions)

---

## 📋 模块概览

### 业务定位
**中医处方核心模块** - 支持中医特色的处方开具、药材配伍、验方应用和处方输出功能。

### 核心功能
- ✅ **处方基础管理**: 创建、编辑、删除、查询处方
- ✅ **智能药材配伍**: 十八反十九畏安全检查，配伍冲突警告
- ✅ **验方模板应用**: 经典验方快速应用，个性化调整
- ✅ **费用计算**: 自动计算处方总价、药材用量、服药剂数
- ✅ **处方输出**: 标准格式打印、复制、导出功能
- ✅ **历史管理**: 患者处方历史、复用历史处方

### 中医特色
- **十八反十九畏**: 自动检测配伍禁忌，保障用药安全
- **君臣佐使**: 支持药材主次关系管理
- **验方传承**: 经典验方与临床应用结合
- **个性化调整**: 基于患者症状的药材加减

---

## 🏗️ 模块结构

### 目录组织
```
src/Client/Desktop/Modules/Prescriptions/
├── Services/
│   └── PrescriptionModule.cs           # 模块注册和配置
├── ViewModels/
│   ├── PrescriptionManagementViewModel.cs     # 处方列表管理
│   ├── PrescriptionComposerViewModel.cs       # 处方编辑器
│   ├── FormulaSelectionViewModel.cs           # 验方选择
│   └── PrescriptionEditorViewModel.cs         # 处方编辑对话框
├── Views/
│   ├── PrescriptionsMainView.xaml             # 处方主界面
│   ├── PrescriptionManagementView.xaml        # 处方管理
│   ├── PrescriptionComposerView.xaml          # 处方编辑器
│   ├── PrescriptionView.xaml                  # 处方查看
│   ├── PrescriptionEditorDialog.xaml          # 编辑对话框
│   ├── FormulaTemplateDialog.xaml             # 验方模板
│   ├── HerbSelectionDialog.xaml               # 药材选择
│   └── SelectFormulaDialog.xaml               # 验方选择
└── README.md                          # 本文档
```

### 核心组件说明
- **PrescriptionComposer**: 处方编辑器，核心功能组件
- **FormulaTemplate**: 验方模板应用
- **HerbSelection**: 药材选择和配伍检查
- **SafetyChecker**: 十八反十九畏安全检查
- **Calculator**: 费用和用量计算器

---

## 🔌 API接口集成

### 后端API对接
```csharp
// 处方管理API端点
GET    /api/v1/prescriptions              // 获取处方列表(分页)
GET    /api/v1/prescriptions/{id}         // 获取处方详情  
POST   /api/v1/prescriptions              // 创建新处方
PUT    /api/v1/prescriptions/{id}         // 更新处方
DELETE /api/v1/prescriptions/{id}         // 删除处方
GET    /api/v1/prescriptions/patient/{patientId}  // 患者处方历史

// 处方项目管理
POST   /api/v1/prescriptions/{id}/items   // 添加药材
PUT    /api/v1/prescriptions/{id}/items/{itemId}  // 更新药材
DELETE /api/v1/prescriptions/{id}/items/{itemId}  // 删除药材

// 验方应用
POST   /api/v1/prescriptions/{id}/apply-formula/{formulaId}  // 应用验方

// 安全检查
POST   /api/v1/prescriptions/check-safety // 配伍安全检查

// 计算功能
POST   /api/v1/prescriptions/calculate    // 费用计算
```

### 数据传输对象
```csharp
// 主要DTO类
- PrescriptionDto: 处方完整信息
- PrescriptionCreateDto: 创建处方请求
- PrescriptionItemDto: 处方药材项目
- PrescriptionCalculationDto: 费用计算结果
- SafetyCheckDto: 安全检查结果
- FormulaApplicationDto: 验方应用请求
```

---

## 💻 开发指南

### 处方编辑器使用
```csharp
public class PrescriptionComposerViewModel : ViewModelBase
{
    // 当前处方
    public ObservableCollection<PrescriptionItemDto> PrescriptionItems { get; set; }
    
    // 添加药材
    public async Task<bool> AddHerbAsync(Guid herbId, decimal dosage, string usage)
    {
        var newItem = new PrescriptionItemDto
        {
            HerbId = herbId,
            Dosage = dosage,
            Usage = usage
        };
        
        // 安全检查
        var safetyResult = await CheckSafetyAsync(newItem);
        if (!safetyResult.IsSafe)
        {
            await ShowWarningAsync($"配伍禁忌警告: {safetyResult.Warning}");
            return false;
        }
        
        // 添加到处方
        PrescriptionItems.Add(newItem);
        
        // 重新计算费用
        await RecalculateAsync();
        
        return true;
    }
    
    // 应用验方
    public async Task<bool> ApplyFormulaAsync(Guid formulaId)
    {
        var formula = await _formulaService.GetByIdAsync(formulaId);
        if (formula?.Items == null) return false;
        
        foreach (var item in formula.Items)
        {
            await AddHerbAsync(item.HerbId, item.Dosage, item.Usage);
        }
        
        return true;
    }
}
```

### 安全检查实现
```csharp
public class PrescriptionSafetyChecker
{
    // 十八反检查
    private static readonly Dictionary<string, List<string>> EighteenAntagonisms = new()
    {
        ["甘草"] = new List<string> { "甘遂", "大戟", "海藻", "芫花" },
        ["乌头"] = new List<string> { "贝母", "瓜蒌", "半夏", "白蔹", "白及" },
        // ... 更多配伍禁忌
    };
    
    // 十九畏检查  
    private static readonly Dictionary<string, List<string>> NineteenFears = new()
    {
        ["硫磺"] = new List<string> { "朴硝" },
        ["水银"] = new List<string> { "砒霜" },
        // ... 更多畏恶关系
    };
    
    public SafetyCheckResult CheckSafety(List<PrescriptionItemDto> items)
    {
        var warnings = new List<string>();
        
        // 检查十八反
        foreach (var item1 in items)
        {
            foreach (var item2 in items)
            {
                if (item1.HerbId == item2.HerbId) continue;
                
                if (IsAntagonistic(item1.HerbName, item2.HerbName))
                {
                    warnings.Add($"十八反: {item1.HerbName} 与 {item2.HerbName} 相反");
                }
                
                if (IsFearful(item1.HerbName, item2.HerbName))
                {
                    warnings.Add($"十九畏: {item1.HerbName} 畏 {item2.HerbName}");
                }
            }
        }
        
        return new SafetyCheckResult
        {
            IsSafe = warnings.Count == 0,
            Warnings = warnings
        };
    }
}
```

---

## 🧪 测试指南

### 功能测试清单
- [ ] **处方创建**: 新建处方，添加基础信息
- [ ] **药材添加**: 选择药材，设置用量和用法
- [ ] **配伍检查**: 添加相反药材，验证警告提示
- [ ] **验方应用**: 选择验方模板，自动添加药材
- [ ] **费用计算**: 修改用量，验证费用自动更新
- [ ] **处方保存**: 保存处方并验证数据完整性
- [ ] **处方打印**: 生成标准格式处方并打印预览
- [ ] **历史复用**: 查看患者历史处方，复用到新处方
- [ ] **权限验证**: 验证医生只能管理自己开具的处方

### 安全测试场景
```csharp
[TestMethod]
public void SafetyCheck_ShouldDetectAntagonism()
{
    // Arrange
    var items = new List<PrescriptionItemDto>
    {
        new() { HerbName = "甘草", Dosage = 10 },
        new() { HerbName = "甘遂", Dosage = 5 }  // 十八反
    };
    
    // Act
    var result = _safetyChecker.CheckSafety(items);
    
    // Assert
    Assert.IsFalse(result.IsSafe);
    Assert.IsTrue(result.Warnings.Any(w => w.Contains("十八反")));
}
```

### 验方应用测试
```csharp
[TestMethod]
public async Task ApplyFormula_ShouldAddAllItems()
{
    // Arrange
    var formulaId = Guid.NewGuid();
    var viewModel = new PrescriptionComposerViewModel();
    
    // Act
    var success = await viewModel.ApplyFormulaAsync(formulaId);
    
    // Assert
    Assert.IsTrue(success);
    Assert.IsTrue(viewModel.PrescriptionItems.Count > 0);
}
```

---

## 🎨 界面设计

### 处方编辑器界面
```xml
<!-- PrescriptionComposerView.xaml 核心布局 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>    <!-- 工具栏 -->
        <RowDefinition Height="*"/>       <!-- 药材列表 -->
        <RowDefinition Height="Auto"/>    <!-- 费用总计 -->
    </Grid.RowDefinitions>
    
    <!-- 工具栏 -->
    <StackPanel Grid.Row="0" Orientation="Horizontal">
        <Button Content="添加药材" Command="{Binding AddHerbCommand}"/>
        <Button Content="应用验方" Command="{Binding ApplyFormulaCommand}"/>
        <Button Content="安全检查" Command="{Binding CheckSafetyCommand}"/>
        <Button Content="打印处方" Command="{Binding PrintCommand}"/>
    </StackPanel>
    
    <!-- 药材列表 -->
    <DataGrid Grid.Row="1" ItemsSource="{Binding PrescriptionItems}">
        <DataGrid.Columns>
            <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}"/>
            <DataGridTextColumn Header="用量(g)" Binding="{Binding Dosage}"/>
            <DataGridTextColumn Header="用法" Binding="{Binding Usage}"/>
            <DataGridTextColumn Header="单价" Binding="{Binding UnitPrice}"/>
            <DataGridTextColumn Header="小计" Binding="{Binding SubTotal}"/>
        </DataGrid.Columns>
    </DataGrid>
    
    <!-- 费用总计 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
        <TextBlock Text="处方总价: "/>
        <TextBlock Text="{Binding TotalAmount}" FontWeight="Bold"/>
        <TextBlock Text=" 元"/>
    </StackPanel>
</Grid>
```

### 验方选择对话框
```xml
<!-- FormulaSelectionDialog.xaml -->
<Window Title="选择验方" Width="800" Height="600">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- 搜索条件 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <TextBox x:Name="SearchText" Width="200" 
                     Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"/>
            <Button Content="搜索" Command="{Binding SearchCommand}"/>
        </StackPanel>
        
        <!-- 验方列表 -->
        <ListBox Grid.Row="1" ItemsSource="{Binding Formulas}" 
                 SelectedItem="{Binding SelectedFormula}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel>
                        <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                        <TextBlock Text="{Binding Indications}" FontStyle="Italic"/>
                        <TextBlock Text="{Binding Composition}" TextWrapping="Wrap"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
        
        <!-- 操作按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="应用" Command="{Binding ApplyCommand}"/>
            <Button Content="取消" Command="{Binding CancelCommand}"/>
        </StackPanel>
    </Grid>
</Window>
```

---

## 🔧 配置说明

### 模块注册
```csharp
public class PrescriptionModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 核心服务
        containerRegistry.Register<IPrescriptionService, PrescriptionService>();
        containerRegistry.Register<IPrescriptionSafetyChecker, PrescriptionSafetyChecker>();
        containerRegistry.Register<IPrescriptionCalculator, PrescriptionCalculator>();
        
        // ViewModels
        containerRegistry.Register<PrescriptionManagementViewModel>();
        containerRegistry.Register<PrescriptionComposerViewModel>();
        containerRegistry.Register<FormulaSelectionViewModel>();
        
        // Views
        containerRegistry.RegisterForNavigation<PrescriptionsMainView>();
        containerRegistry.RegisterForNavigation<PrescriptionComposerView>();
        
        // Dialogs
        containerRegistry.RegisterDialog<PrescriptionEditorDialog>();
        containerRegistry.RegisterDialog<FormulaSelectionDialog>();
    }
}
```

### 安全检查配置
```csharp
// 可通过配置文件管理十八反十九畏数据
public class SafetyConfiguration
{
    public Dictionary<string, List<string>> EighteenAntagonisms { get; set; }
    public Dictionary<string, List<string>> NineteenFears { get; set; }
    public bool EnableStrictMode { get; set; } = true;  // 严格模式禁止保存
    public bool ShowWarningsOnly { get; set; } = false; // 仅警告模式
}
```

---

## 🐛 故障排除

### 常见问题
1. **配伍检查不生效**
   - 检查药材名称匹配是否准确
   - 验证十八反十九畏数据配置

2. **费用计算错误**
   - 检查药材单价数据
   - 验证用量格式和数值类型

3. **验方应用失败**
   - 确认验方数据完整性
   - 检查验方与药材的关联关系

4. **处方打印格式异常**
   - 检查打印模板配置
   - 验证处方数据完整性

### 调试技巧
```csharp
// 启用处方编辑器调试日志
public class PrescriptionComposerViewModel
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    private async Task DebugPrescriptionState()
    {
        Logger.Info("当前处方状态:");
        Logger.Info($"药材数量: {PrescriptionItems.Count}");
        Logger.Info($"总金额: {TotalAmount:C}");
        
        foreach (var item in PrescriptionItems)
        {
            Logger.Info($"- {item.HerbName}: {item.Dosage}g, {item.SubTotal:C}");
        }
    }
}
```

---

## 📚 相关文档

### 需求文档
- [功能需求-处方管理](../../../../../docs/requirements/functional-requirements.md#5️⃣-处方管理模块-prescriptions)
- [中医特色需求](../../../../../docs/requirements/system-overview.md#核心业务模块)

### 关联模块
- [Formula模块](../Formula/README.md) - 验方模板管理
- [Herbs模块](../Herbs/README.md) - 药材信息管理
- [MedicalCase模块](../MedicalCase/README.md) - 关联的医案管理

### 技术文档
- [十八反十九畏配置](../../../../../docs/guides/tcm-safety-rules.md)
- [处方打印格式规范](../../../../../docs/guides/prescription-format.md)

---

**维护说明**: 本文档反映Prescriptions模块的当前功能状态。中医配伍规则和验方数据更新时，需同步更新相关配置和文档。