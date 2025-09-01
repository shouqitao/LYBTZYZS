# LYBT.Desktop.Formula

## 概述

LYBT.Desktop.Formula是凌隐宝堂桌面客户端的验方管理模块，提供中医经典验方、个人验方的管理、组合和应用功能。该模块是中医诊疗系统的重要组成部分，为医生提供系统化的方剂管理和智能处方辅助功能。

## 核心功能

### 📚 验方库管理
- **经典验方**: 收录《伤寒论》、《金匮要略》等经典方剂
- **个人验方**: 医生个人临床经验方剂的记录
- **验方分类**: 按功效、证候、病症等维度分类管理
- **验方搜索**: 多维度检索和快速查找验方

### ✏️ 验方编辑
- **新增验方**: 创建新的验方记录
- **编辑验方**: 修改现有验方信息和组成
- **验方详情**: 查看完整的验方信息和配伍
- **验方模板**: 基于已有验方创建新的处方模板

### 🔍 智能应用
- **配伍分析**: 验方组成的配伍合理性分析
- **功效说明**: 详细的方剂功效和主治说明
- **用法用量**: 标准化的煎服方法和剂量指导
- **适应证**: 方剂的适应症和禁忌症管理

### 🔗 处方集成
- **处方引用**: 在开具处方时快速引用验方
- **方剂组合**: 多个验方的合理组合应用
- **个性化调整**: 基于患者情况调整验方组成
- **疗效跟踪**: 验方使用效果的记录和分析

## 🚨 UltraThink架构重构方案

### 当前架构问题

**🔴 严重架构问题**：
- **FormulaModule.cs**: **662行巨无霸单体类**
- **职责严重混乱**: 验方管理、药材组合、配伍检查、智能推荐等多个职责混合
- **违背UltraThink原则**: 与后端Formula模块三层架构完全不一致
- **维护困难**: 验方相关功能修改风险高，影响面广

### 重构目标架构

**🎯 UltraThink三层架构重构**：
```csharp
FormulaModule (纯委托层 - 约50行)
    ├── FormulaCoreService (核心操作层 - 约150行)
    │   ├── API通信: CallCreateFormulaApi, CallUpdateFormulaApi
    │   ├── 基础CRUD: GetFormulaById, GetAllFormulas
    │   └── 数据验证: ValidateFormulaData, ValidateIngredients
    ├── FormulaQueryService (查询专业层 - 约120行)
    │   ├── 搜索功能: SearchFormulas, FilterByCategory
    │   ├── 智能推荐: RecommendFormulas, FindSimilar
    │   └── 统计分析: GetFormulaStatistics, GetUsageStats
    └── FormulaBusinessService (业务逻辑层 - 约160行)
        ├── 验方管理: CreateFormula, UpdateFormula, DeleteFormula
        ├── 配伍检查: CheckCompatibility, ValidateIngredients
        ├── 组合管理: ManageHerbCombinations, OptimizeDosage
        └── 应用集成: ApplyToPresciption, GenerateVariation
```

#### 🎯 代码质量目标
- **重构前**: 662行单体类，多个职责混合
- **重构后**: 4个文件，职责清晰分离 (总计约480行，减少28%)

### 重构优先级
**🟡 中优先级**: 验方是中医核心知识，重构后便于知识管理和功能扩展

## 项目结构

### 当前结构
```
src/Client/Desktop/Modules/Formula/
├── FormulaModule.cs              # Prism模块定义和注册
├── Services/                     # 业务服务层
│   └── FormulaModule.cs         # 🔴 662行巨无霸 (需要重构)
├── ViewModels/                  # 视图模型
│   ├── FormulaManagementViewModel.cs    # 验方管理主视图模型
│   ├── FormulaDetailViewModel.cs        # 验方详情视图模型
│   ├── AddFormulaDialogViewModel.cs     # 新增验方对话框视图模型
│   ├── EditFormulaDialogViewModel.cs    # 编辑验方对话框视图模型
│   └── ViewFormulaDialogViewModel.cs    # 查看验方对话框视图模型
├── Views/                       # 用户界面视图
│   ├── FormulaManagementView.xaml       # 验方管理主界面
│   ├── FormulaManagementView.xaml.cs   # 验方管理主界面代码
│   ├── FormulaDetailView.xaml          # 验方详情界面
│   ├── FormulaDetailView.xaml.cs       # 验方详情界面代码
│   ├── AddFormulaDialog.xaml           # 新增验方对话框
│   ├── AddFormulaDialog.xaml.cs        # 新增验方对话框代码
│   ├── EditFormulaDialog.xaml          # 编辑验方对话框
│   ├── EditFormulaDialog.xaml.cs       # 编辑验方对话框代码
│   ├── ViewFormulaDialog.xaml          # 查看验方对话框
│   └── ViewFormulaDialog.xaml.cs       # 查看验方对话框代码
├── Coordinators/                # 协调器(如果存在)
└── Api/                         # API接口定义(如果存在)
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入
- **Prism.Wpf 8.1.97**: WPF版本的Prism框架
- **Microsoft.Extensions.Logging.Abstractions 9.0.0**: 日志抽象层

### 项目引用
- **LYBT.Desktop.Core**: 核心框架和基础设施
- **LYBT.Desktop.Services**: 业务服务层
- **LYBT.Desktop.Infrastructure**: 基础设施和HTTP通信

## 核心特性

### 📚 验方数据模型

#### 验方基础信息
```csharp
public class FormulaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }           // 验方名称
    public string? Source { get; set; }        // 方剂来源(经典/个人)
    public string? Category { get; set; }      // 方剂分类
    public string? Effect { get; set; }        // 主要功效
    public string? Indication { get; set; }    // 主治病症
    public string? Composition { get; set; }   // 方剂组成
    public string? Usage { get; set; }         // 用法用量
    public string? Contraindication { get; set; } // 禁忌症
    public bool IsTemplate { get; set; }       // 是否为模板
    public bool IsShared { get; set; }         // 是否共享
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}
```

#### 方剂组成详情
```csharp
public class FormulaCompositionDto
{
    public Guid FormulaId { get; set; }
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }       // 药材名称
    public decimal Dosage { get; set; }        // 用量
    public string Unit { get; set; }           // 单位
    public string? ProcessMethod { get; set; }  // 炮制方法
    public string? Role { get; set; }          // 在方中的作用(君臣佐使)
    public int SortOrder { get; set; }         // 排序
}
```

### 📱 MVVM实现

#### FormulaManagementViewModel核心功能
```csharp
public class FormulaManagementViewModel : CoreViewModel
{
    // 验方列表
    public ObservableCollection<FormulaDto> Formulas { get; set; }
    public FormulaDto? SelectedFormula { get; set; }
    
    // 搜索和筛选
    public string SearchKeyword { get; set; }
    public string? SelectedCategory { get; set; }
    public string? SelectedSource { get; set; }
    
    // 分页
    public int CurrentPage { get; set; }
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    
    // 命令
    public ICommand LoadFormulasCommand { get; }
    public ICommand SearchFormulasCommand { get; }
    public ICommand AddFormulaCommand { get; }
    public ICommand EditFormulaCommand { get; }
    public ICommand ViewFormulaCommand { get; }
    public ICommand DeleteFormulaCommand { get; }
    public ICommand CopyFormulaCommand { get; }
    public ICommand ShareFormulaCommand { get; }
    
    // 搜索验方
    private async Task SearchFormulasAsync()
    {
        try
        {
            var query = new FormulaQueryDto
            {
                PageIndex = CurrentPage,
                PageSize = PageSize,
                Keyword = SearchKeyword,
                Category = SelectedCategory,
                Source = SelectedSource
            };
            
            var result = await _formulaService.GetPagedAsync(query);
            if (result.IsSuccess)
            {
                Formulas.Clear();
                foreach (var formula in result.Data.Items)
                {
                    Formulas.Add(formula);
                }
                TotalCount = result.Data.TotalCount;
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "搜索验方");
        }
    }
    
    // 新增验方
    private async Task AddFormulaAsync()
    {
        var parameters = new DialogParameters();
        var result = await _dialogService.ShowDialogAsync("AddFormulaDialog", parameters);
        
        if (result.Result == ButtonResult.OK)
        {
            await LoadFormulasAsync();
            ShowSuccessMessage("验方添加成功");
        }
    }
}
```

#### AddFormulaDialogViewModel验方创建
```csharp
public class AddFormulaDialogViewModel : DialogViewModelBase
{
    // 验方基本信息
    public string Name { get; set; }
    public string? Source { get; set; }
    public string? Category { get; set; }
    public string? Effect { get; set; }
    public string? Indication { get; set; }
    public string? Usage { get; set; }
    public string? Contraindication { get; set; }
    
    // 方剂组成
    public ObservableCollection<FormulaCompositionDto> Compositions { get; set; }
    
    // 命令
    public ICommand AddHerbCommand { get; }
    public ICommand RemoveHerbCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    // 保存验方
    private async Task SaveFormulaAsync()
    {
        try
        {
            var dto = new FormulaCreateDto
            {
                Name = Name,
                Source = Source,
                Category = Category,
                Effect = Effect,
                Indication = Indication,
                Usage = Usage,
                Contraindication = Contraindication,
                Compositions = Compositions.ToList()
            };
            
            var result = await _formulaService.CreateAsync(dto);
            if (result.IsSuccess)
            {
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            else
            {
                ShowErrorMessage(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "保存验方");
        }
    }
}
```

### 🎨 用户界面设计

#### 验方管理主界面
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/> <!-- 搜索栏 -->
        <RowDefinition Height="*"/>    <!-- 验方列表 -->
        <RowDefinition Height="Auto"/> <!-- 操作按钮栏 -->
    </Grid.RowDefinitions>
    
    <!-- 搜索和筛选 -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
        <TextBox PlaceholderText="搜索验方..." Text="{Binding SearchKeyword}" Width="200"/>
        <ComboBox ItemsSource="{Binding Categories}" SelectedItem="{Binding SelectedCategory}" 
                  PlaceholderText="选择分类" Width="120"/>
        <ComboBox ItemsSource="{Binding Sources}" SelectedItem="{Binding SelectedSource}" 
                  PlaceholderText="选择来源" Width="120"/>
        <Button Command="{Binding SearchFormulasCommand}" Content="搜索"/>
    </StackPanel>
    
    <!-- 验方列表 -->
    <DataGrid Grid.Row="1" ItemsSource="{Binding Formulas}" 
              SelectedItem="{Binding SelectedFormula}" AutoGenerateColumns="False">
        <DataGrid.Columns>
            <DataGridTextColumn Header="验方名称" Binding="{Binding Name}" Width="200"/>
            <DataGridTextColumn Header="来源" Binding="{Binding Source}" Width="100"/>
            <DataGridTextColumn Header="分类" Binding="{Binding Category}" Width="120"/>
            <DataGridTextColumn Header="主要功效" Binding="{Binding Effect}" Width="200"/>
            <DataGridTextColumn Header="主治" Binding="{Binding Indication}" Width="200"/>
            <DataGridTextColumn Header="创建时间" Binding="{Binding CreateTime, StringFormat=yyyy-MM-dd}" Width="120"/>
        </DataGrid.Columns>
    </DataGrid>
    
    <!-- 操作按钮 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="10">
        <Button Command="{Binding AddFormulaCommand}" Content="新增验方"/>
        <Button Command="{Binding EditFormulaCommand}" Content="编辑验方"/>
        <Button Command="{Binding ViewFormulaCommand}" Content="查看详情"/>
        <Button Command="{Binding DeleteFormulaCommand}" Content="删除验方"/>
        <Button Command="{Binding CopyFormulaCommand}" Content="复制验方"/>
        <Button Command="{Binding ShareFormulaCommand}" Content="共享验方"/>
    </StackPanel>
</Grid>
```

#### 验方编辑对话框
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/> <!-- 基本信息 -->
        <RowDefinition Height="*"/>    <!-- 方剂组成 -->
        <RowDefinition Height="Auto"/> <!-- 按钮 -->
    </Grid.RowDefinitions>
    
    <!-- 基本信息 -->
    <GroupBox Grid.Row="0" Header="基本信息">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <Label Grid.Row="0" Grid.Column="0" Content="验方名称:"/>
            <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding Name}"/>
            
            <Label Grid.Row="0" Grid.Column="2" Content="来源:"/>
            <TextBox Grid.Row="0" Grid.Column="3" Text="{Binding Source}"/>
            
            <!-- 更多字段... -->
        </Grid>
    </GroupBox>
    
    <!-- 方剂组成 -->
    <GroupBox Grid.Row="1" Header="方剂组成">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            
            <Button Grid.Row="0" Command="{Binding AddHerbCommand}" Content="添加药材"/>
            
            <DataGrid Grid.Row="1" ItemsSource="{Binding Compositions}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}"/>
                    <DataGridTextColumn Header="用量" Binding="{Binding Dosage}"/>
                    <DataGridTextColumn Header="单位" Binding="{Binding Unit}"/>
                    <DataGridTextColumn Header="作用" Binding="{Binding Role}"/>
                </DataGrid.Columns>
            </DataGrid>
        </Grid>
    </GroupBox>
    
    <!-- 操作按钮 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Command="{Binding SaveCommand}" Content="保存" IsDefault="True"/>
        <Button Command="{Binding CancelCommand}" Content="取消"/>
    </StackPanel>
</Grid>
```

### 🔧 模块集成

#### 与处方模块的集成
```csharp
// 在处方模块中引用验方
public async Task SelectFormulaForPrescriptionAsync()
{
    var parameters = new DialogParameters();
    parameters.Add("Mode", "Selection");
    
    var result = await _dialogService.ShowDialogAsync("FormulaSelectionDialog", parameters);
    
    if (result.Result == ButtonResult.OK && result.Parameters.TryGetValue("SelectedFormula", out var selectedFormula))
    {
        var formula = selectedFormula as FormulaDto;
        await ApplyFormulaToCurrentPrescriptionAsync(formula);
    }
}
```

#### 验方智能推荐
```csharp
public async Task<List<FormulaRecommendationDto>> GetRecommendationsAsync(string symptoms, string syndrome)
{
    try
    {
        var result = await _formulaService.GetRecommendationsAsync(symptoms, syndrome);
        return result.IsSuccess ? result.Data : new List<FormulaRecommendationDto>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取验方推荐失败");
        return new List<FormulaRecommendationDto>();
    }
}
```

## 使用指南

### 模块注册和启动

```csharp
// 在App.xaml.cs中注册Formula模块
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<FormulaModule>();
}

// 导航到验方管理界面
_regionManager.RequestNavigate("ContentRegion", "FormulaManagementView");
```

### 验方操作流程

```csharp
// 1. 加载验方列表
await LoadFormulasAsync();

// 2. 搜索特定验方
await SearchFormulasAsync("逍遥散");

// 3. 新增验方
await ShowAddFormulaDialogAsync();

// 4. 编辑验方
await ShowEditFormulaDialogAsync(selectedFormulaId);

// 5. 在处方中应用验方
await ApplyFormulaToCurrentPrescriptionAsync(formulaId);
```

### 验方分类管理

```csharp
// 常用验方分类
public static class FormulaCategories
{
    public const string JieBiao = "解表剂";        // 解表剂
    public const string QingRe = "清热剂";         // 清热剂
    public const string XieXia = "泻下剂";         // 泻下剂
    public const string QingBu = "清补剂";         // 清补剂
    public const string WenBu = "温补剂";         // 温补剂
    public const string LiQi = "理气剂";          // 理气剂
    public const string HuaXue = "化血剂";        // 化血剂
    public const string QingShi = "清湿剂";       // 清湿剂
    public const string AnShen = "安神剂";        // 安神剂
}
```

## 开发规范

### 数据验证
- 验方名称不能为空且不能重复
- 方剂组成至少要有一味药材
- 用法用量必须符合中医用药规范
- 禁忌症和注意事项要完整填写

### MVVM实现
- 所有ViewModel继承CoreViewModel或DialogViewModelBase
- 使用ObservableCollection管理动态集合
- 命令操作使用AsyncRelayCommand处理异步逻辑
- 通过EventAggregator发布验方相关事件

### 用户体验
- 提供验方的智能搜索和筛选
- 支持验方的快速复制和模板化
- 提供丰富的验方信息展示
- 实现验方的导入导出功能

### 中医专业性
- 方剂组成遵循君臣佐使理论
- 用量单位使用传统中医计量
- 炮制方法符合中药炮制规范
- 功效主治使用标准中医术语

## 中医验方知识库

### 📚 经典方剂收录
- **伤寒论方**: 桂枝汤、麻黄汤、小柴胡汤等112方
- **金匮要略方**: 肾气丸、逍遥散、当归芍药散等
- **温病条辨**: 银翘散、桑菊饮、清营汤等
- **医方集解**: 补中益气汤、六味地黄丸等

### 🔍 智能功能
- **症状匹配**: 根据症状智能推荐相关验方
- **证候分析**: 基于中医理论的证候-方剂匹配
- **配伍禁忌**: 自动检测方剂配伍的安全性
- **剂量计算**: 根据患者情况调整药物剂量

### 📖 临床应用
- **加减变化**: 记录方剂的临床加减经验
- **案例关联**: 关联成功案例提高临床参考价值
- **疗效统计**: 跟踪方剂的临床使用效果
- **经验分享**: 医生间的验方经验交流

## 维护说明

### 数据维护
- **方剂库更新**: 定期更新和扩充验方数据库
- **质量控制**: 确保方剂信息的准确性和专业性
- **版本管理**: 维护验方的历史版本和变更记录
- **备份策略**: 定期备份重要的验方数据

### 功能扩展
- **多媒体支持**: 支持验方相关的图片和视频资料
- **国际化**: 支持验方信息的多语言展示
- **移动端**: 开发移动端的验方查询和应用
- **AI集成**: 集成人工智能的方剂推荐算法

### 性能优化
- **搜索优化**: 优化大量验方数据的搜索性能
- **缓存策略**: 常用验方的智能缓存机制
- **界面响应**: 提升验方界面的加载和响应速度
- **数据同步**: 优化验方数据的同步和更新机制

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*