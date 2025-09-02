# LYBT.Desktop.Herbs

## 概述

LYBT.Desktop.Herbs是凌隐宝堂桌面客户端的中药材管理模块，提供中药材信息管理、价格维护、用法指导等功能。该模块专注于处方用药的药材选择，不涉及库存管理，为医生开具处方提供准确的药材信息和价格参考。

## 核心功能

### 🌿 药材信息管理
- **药材档案**: 完整的中药材基础信息管理
- **药材分类**: 按功效、性味、归经等维度分类
- **别名管理**: 药材的别名和异名管理
- **药材搜索**: 多维度快速检索药材信息

### 💰 价格管理
- **价格维护**: 药材单价的录入和更新
- **价格历史**: 药材价格变动的历史记录
- **价格分析**: 药材价格趋势分析和预测
- **成本计算**: 处方总价格的自动计算

### 📋 用法指导
- **用量规范**: 标准用量范围和推荐用量
- **炮制方法**: 药材的标准炮制工艺
- **配伍禁忌**: 药材间的配伍宜忌关系
- **使用注意**: 特殊用法和注意事项

### 🔗 处方集成
- **处方选药**: 为处方提供药材选择支持
- **智能推荐**: 根据症状和证候推荐合适药材
- **配伍检查**: 处方配伍的安全性检查
- **用量计算**: 基于患者情况的用量调整

## 🚨 UltraThink架构重构方案

### 当前架构问题

**🔴 严重架构问题**：
- **HerbModule.cs**: **771行巨无霸单体类** (项目中最大的单体类)
- **职责严重混乱**: 一个类承担8+个不同职责
- **违背UltraThink原则**: 与后端三层架构完全不一致
- **维护困难**: 任何修改都可能影响整个类的稳定性

### 重构目标架构

**🎯 UltraThink三层架构重构**：
```csharp
HerbModule (纯委托层 - 约50行)
    ├── HerbCoreService (核心操作层 - 约180行)
    │   ├── API通信: CallCreateHerbApi, CallUpdateHerbApi
    │   ├── 基础CRUD: GetHerbById, GetAllHerbs
    │   └── 数据验证: ValidateHerbData
    ├── HerbQueryService (查询专业层 - 约150行)  
    │   ├── 搜索功能: SearchHerbs, FilterByCategory
    │   ├── 统计分析: GetHerbStatistics, GetPriceTrends
    │   └── 报表查询: GenerateHerbReports
    └── HerbBusinessService (业务逻辑层 - 约200行)
        ├── 业务流程: CreateHerb, UpdateHerb, DeleteHerb
        ├── 配伍检查: CheckCompatibility, ValidateFormula
        └── 价格计算: CalculateFormulaPrice, GetPriceHistory
```

### 重构详细方案

#### 📋 重构任务清单
- [ ] **第一阶段**: 创建三层服务接口 (4个接口文件)
- [ ] **第二阶段**: 实现HerbCoreService (基础操作)
- [ ] **第三阶段**: 实现HerbQueryService (查询功能)
- [ ] **第四阶段**: 实现HerbBusinessService (业务逻辑)  
- [ ] **第五阶段**: 重构HerbModule为纯委托层
- [ ] **第六阶段**: 更新依赖注入配置
- [ ] **第七阶段**: 功能测试和验证

#### 🎯 代码质量目标
- **重构前**: 771行单体类，8个职责混合
- **重构后**: 4个文件，职责清晰分离
  - HerbModule: ≤50行 (纯委托)
  - HerbCoreService: ≤180行 (核心操作)
  - HerbQueryService: ≤150行 (查询功能)  
  - HerbBusinessService: ≤200行 (业务逻辑)

#### ⚡ 预期效果
- ✅ **可维护性提升**: 每层职责单一，易于修改
- ✅ **可测试性提升**: 每层可独立编写单元测试
- ✅ **团队协作改善**: 多人可并行开发不同层
- ✅ **架构一致性**: 与后端UltraThink架构完全统一

### 重构优先级

**🔴 最高优先级**: HerbModule是8个业务模块中最大的巨无霸(771行)，必须优先重构

## 项目结构

### 当前结构
```
src/Client/Desktop/Modules/Herbs/
├── HerbsModule.cs               # Prism模块定义和注册
├── Services/                    # 业务服务层
│   └── HerbModule.cs           # 🔴 771行巨无霸 (需要重构)
├── ViewModels/                  # 视图模型
│   ├── HerbManagementViewModel.cs     # 药材管理主视图模型
│   ├── HerbDetailViewModel.cs         # 药材详情视图模型
│   └── HerbAddEditDialogViewModel.cs  # 药材新增编辑对话框视图模型
├── Views/                       # 用户界面视图
│   ├── HerbManagementView.xaml        # 药材管理主界面
│   ├── HerbManagementView.xaml.cs     # 药材管理主界面代码
│   ├── HerbDetailView.xaml            # 药材详情界面
│   ├── HerbDetailView.xaml.cs         # 药材详情界面代码
│   ├── HerbAddEditDialog.xaml         # 药材新增编辑对话框
│   └── HerbAddEditDialog.xaml.cs      # 药材新增编辑对话框代码
├── Mappings/                    # 对象映射配置
│   └── MappingProfile.cs       # AutoMapper配置
├── Coordinators/               # 协调器(如果存在)
└── Api/                        # API接口定义(如果存在)
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入

### 项目引用
- **LYBT.Desktop.Core**: 核心框架和基础设施
- **LYBT.Desktop.Services**: 业务服务层
- **LYBT.Desktop.Infrastructure**: 基础设施和HTTP通信
- **LYBT.Shared.Interfaces**: 共享服务接口

## 核心特性

### 🌿 中药材数据模型

#### 药材基础信息
```csharp
public class HerbDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }              // 药材名称
    public string? PinyinName { get; set; }       // 拼音名称
    public string? EnglishName { get; set; }      // 英文名称
    public string? Aliases { get; set; }          // 别名
    public string? Source { get; set; }           // 来源(植物/动物/矿物)
    public string? Category { get; set; }         // 功效分类
    public string? Nature { get; set; }           // 性味
    public string? Meridian { get; set; }         // 归经
    public string? Functions { get; set; }        // 功效
    public string? Indications { get; set; }      // 主治
    public decimal UnitPrice { get; set; }        // 单价
    public string Unit { get; set; }              // 单位(克/两/钱)
    public decimal MinDosage { get; set; }        // 最小用量
    public decimal MaxDosage { get; set; }        // 最大用量
    public decimal RecommendedDosage { get; set; } // 推荐用量
    public string? ProcessingMethod { get; set; } // 炮制方法
    public string? Contraindications { get; set; } // 禁忌
    public string? Cautions { get; set; }         // 注意事项
    public bool IsActive { get; set; }            // 是否启用
}
```

#### 药材配伍信息
```csharp
public class HerbCompatibilityDto
{
    public Guid HerbId { get; set; }
    public Guid RelatedHerbId { get; set; }
    public string RelatedHerbName { get; set; }
    public CompatibilityType Type { get; set; }    // 配伍类型(相须/相使/相恶/相反等)
    public string? Description { get; set; }       // 配伍说明
    public string? Effect { get; set; }            // 配伍效果
}

public enum CompatibilityType
{
    相须,    // 相互协同增强
    相使,    // 一药为主，他药为辅
    相畏,    // 一药毒性被另一药减轻
    相杀,    // 一药能减轻另一药毒性
    相恶,    // 两药合用效力降低
    相反     // 两药合用产生毒性
}
```

### 📱 MVVM实现

#### HerbManagementViewModel核心功能
```csharp
public class HerbManagementViewModel : CoreViewModel
{
    // 药材列表
    public ObservableCollection<HerbDto> Herbs { get; set; }
    public HerbDto? SelectedHerb { get; set; }
    
    // 搜索和筛选
    public string SearchKeyword { get; set; }
    public string? SelectedCategory { get; set; }
    public string? SelectedNature { get; set; }
    public bool ShowActiveOnly { get; set; } = true;
    
    // 分页
    public int CurrentPage { get; set; }
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    
    // 命令
    public ICommand LoadHerbsCommand { get; }
    public ICommand SearchHerbsCommand { get; }
    public ICommand AddHerbCommand { get; }
    public ICommand EditHerbCommand { get; }
    public ICommand ViewHerbCommand { get; }
    public ICommand DeleteHerbCommand { get; }
    public ICommand UpdatePriceCommand { get; }
    public ICommand ExportHerbsCommand { get; }
    public ICommand ImportHerbsCommand { get; }
    
    // 搜索药材
    private async Task SearchHerbsAsync()
    {
        try
        {
            var query = new HerbQueryDto
            {
                PageIndex = CurrentPage,
                PageSize = PageSize,
                Keyword = SearchKeyword,
                Category = SelectedCategory,
                Nature = SelectedNature,
                IsActiveOnly = ShowActiveOnly
            };
            
            var result = await _herbService.GetPagedAsync(query);
            if (result.IsSuccess)
            {
                Herbs.Clear();
                foreach (var herb in result.Data.Items)
                {
                    Herbs.Add(herb);
                }
                TotalCount = result.Data.TotalCount;
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "搜索药材");
        }
    }
    
    // 批量更新价格
    private async Task UpdatePricesAsync()
    {
        try
        {
            var priceUpdateDialog = _container.Resolve<PriceUpdateDialog>();
            var result = priceUpdateDialog.ShowDialog();
            
            if (result == true)
            {
                await LoadHerbsAsync();
                ShowSuccessMessage("价格更新成功");
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "更新价格");
        }
    }
}
```

#### HerbAddEditDialogViewModel药材编辑
```csharp
public class HerbAddEditDialogViewModel : DialogViewModelBase
{
    // 药材基本信息
    public string Name { get; set; }
    public string? PinyinName { get; set; }
    public string? Aliases { get; set; }
    public string? Source { get; set; }
    public string? Category { get; set; }
    public string? Nature { get; set; }
    public string? Meridian { get; set; }
    public string? Functions { get; set; }
    public string? Indications { get; set; }
    
    // 价格和用量
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = "克";
    public decimal MinDosage { get; set; }
    public decimal MaxDosage { get; set; }
    public decimal RecommendedDosage { get; set; }
    
    // 炮制和注意事项
    public string? ProcessingMethod { get; set; }
    public string? Contraindications { get; set; }
    public string? Cautions { get; set; }
    
    // 配伍信息
    public ObservableCollection<HerbCompatibilityDto> Compatibilities { get; set; }
    
    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddCompatibilityCommand { get; }
    public ICommand RemoveCompatibilityCommand { get; }
    
    // 保存药材
    private async Task SaveHerbAsync()
    {
        try
        {
            var dto = IsEditMode ? new HerbUpdateDto() : new HerbCreateDto();
            
            // 设置基本信息
            dto.Name = Name;
            dto.PinyinName = PinyinName;
            dto.Aliases = Aliases;
            dto.Source = Source;
            dto.Category = Category;
            dto.Nature = Nature;
            dto.Meridian = Meridian;
            dto.Functions = Functions;
            dto.Indications = Indications;
            dto.UnitPrice = UnitPrice;
            dto.Unit = Unit;
            dto.MinDosage = MinDosage;
            dto.MaxDosage = MaxDosage;
            dto.RecommendedDosage = RecommendedDosage;
            dto.ProcessingMethod = ProcessingMethod;
            dto.Contraindications = Contraindications;
            dto.Cautions = Cautions;
            
            ServiceResult<HerbDto> result;
            if (IsEditMode)
            {
                result = await _herbService.UpdateAsync(CurrentHerbId, dto as HerbUpdateDto);
            }
            else
            {
                result = await _herbService.CreateAsync(dto as HerbCreateDto);
            }
            
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
            HandleException(ex, "保存药材信息");
        }
    }
}
```

### 🎨 用户界面设计

#### 药材管理主界面
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/> <!-- 搜索栏 -->
        <RowDefinition Height="*"/>    <!-- 药材列表 -->
        <RowDefinition Height="Auto"/> <!-- 操作按钮栏 -->
    </Grid.RowDefinitions>
    
    <!-- 搜索和筛选 -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
        <TextBox PlaceholderText="搜索药材..." Text="{Binding SearchKeyword}" Width="200"/>
        <ComboBox ItemsSource="{Binding Categories}" SelectedItem="{Binding SelectedCategory}" 
                  PlaceholderText="功效分类" Width="120"/>
        <ComboBox ItemsSource="{Binding Natures}" SelectedItem="{Binding SelectedNature}" 
                  PlaceholderText="性味" Width="100"/>
        <CheckBox IsChecked="{Binding ShowActiveOnly}" Content="仅显示启用"/>
        <Button Command="{Binding SearchHerbsCommand}" Content="搜索"/>
    </StackPanel>
    
    <!-- 药材列表 -->
    <DataGrid Grid.Row="1" ItemsSource="{Binding Herbs}" 
              SelectedItem="{Binding SelectedHerb}" AutoGenerateColumns="False">
        <DataGrid.Columns>
            <DataGridTextColumn Header="药材名称" Binding="{Binding Name}" Width="150"/>
            <DataGridTextColumn Header="别名" Binding="{Binding Aliases}" Width="120"/>
            <DataGridTextColumn Header="功效分类" Binding="{Binding Category}" Width="100"/>
            <DataGridTextColumn Header="性味" Binding="{Binding Nature}" Width="80"/>
            <DataGridTextColumn Header="归经" Binding="{Binding Meridian}" Width="100"/>
            <DataGridTextColumn Header="单价" Binding="{Binding UnitPrice, StringFormat=¥{0:F2}}" Width="80"/>
            <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60"/>
            <DataGridTextColumn Header="推荐用量" Binding="{Binding RecommendedDosage}" Width="80"/>
            <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsActive}" Width="60"/>
        </DataGrid.Columns>
    </DataGrid>
    
    <!-- 操作按钮 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="10">
        <Button Command="{Binding AddHerbCommand}" Content="新增药材"/>
        <Button Command="{Binding EditHerbCommand}" Content="编辑药材"/>
        <Button Command="{Binding ViewHerbCommand}" Content="查看详情"/>
        <Button Command="{Binding DeleteHerbCommand}" Content="删除药材"/>
        <Button Command="{Binding UpdatePriceCommand}" Content="批量更新价格"/>
        <Button Command="{Binding ExportHerbsCommand}" Content="导出药材"/>
        <Button Command="{Binding ImportHerbsCommand}" Content="导入药材"/>
    </StackPanel>
</Grid>
```

#### 药材编辑对话框
```xml
<TabControl>
    <!-- 基本信息标签页 -->
    <TabItem Header="基本信息">
        <Grid Margin="10">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <!-- 更多行定义 -->
            </Grid.RowDefinitions>
            
            <Label Content="药材名称*:"/>
            <TextBox Grid.Column="1" Text="{Binding Name}"/>
            
            <Label Grid.Row="1" Content="拼音名称:"/>
            <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding PinyinName}"/>
            
            <Label Grid.Row="2" Content="别名:"/>
            <TextBox Grid.Row="2" Grid.Column="1" Text="{Binding Aliases}"/>
            
            <!-- 更多字段... -->
        </Grid>
    </TabItem>
    
    <!-- 功效信息标签页 -->
    <TabItem Header="功效信息">
        <Grid Margin="10">
            <Label Content="性味:"/>
            <ComboBox ItemsSource="{Binding AvailableNatures}" SelectedItem="{Binding Nature}"/>
            
            <Label Content="归经:"/>
            <TextBox Text="{Binding Meridian}"/>
            
            <Label Content="功效:"/>
            <TextBox Text="{Binding Functions}" AcceptsReturn="True" Height="100"/>
            
            <Label Content="主治:"/>
            <TextBox Text="{Binding Indications}" AcceptsReturn="True" Height="100"/>
        </Grid>
    </TabItem>
    
    <!-- 价格用量标签页 -->
    <TabItem Header="价格用量">
        <Grid Margin="10">
            <Label Content="单价*:"/>
            <TextBox Text="{Binding UnitPrice}"/>
            
            <Label Content="单位:"/>
            <ComboBox ItemsSource="{Binding AvailableUnits}" SelectedItem="{Binding Unit}"/>
            
            <Label Content="最小用量:"/>
            <TextBox Text="{Binding MinDosage}"/>
            
            <Label Content="最大用量:"/>
            <TextBox Text="{Binding MaxDosage}"/>
            
            <Label Content="推荐用量:"/>
            <TextBox Text="{Binding RecommendedDosage}"/>
        </Grid>
    </TabItem>
    
    <!-- 配伍禁忌标签页 -->
    <TabItem Header="配伍禁忌">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            
            <Button Command="{Binding AddCompatibilityCommand}" Content="添加配伍关系"/>
            
            <DataGrid Grid.Row="1" ItemsSource="{Binding Compatibilities}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="相关药材" Binding="{Binding RelatedHerbName}"/>
                    <DataGridTextColumn Header="配伍类型" Binding="{Binding Type}"/>
                    <DataGridTextColumn Header="说明" Binding="{Binding Description}"/>
                </DataGrid.Columns>
            </DataGrid>
        </Grid>
    </TabItem>
</TabControl>
```

### 🔧 与处方模块的集成

#### 处方选药支持
```csharp
public class HerbSelectionService
{
    // 为处方提供药材选择
    public async Task<List<HerbDto>> GetHerbsForPrescriptionAsync(string symptoms, string syndrome)
    {
        try
        {
            var result = await _herbService.GetRecommendedHerbsAsync(symptoms, syndrome);
            return result.IsSuccess ? result.Data : new List<HerbDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取处方推荐药材失败");
            return new List<HerbDto>();
        }
    }
    
    // 检查药材配伍安全性
    public async Task<CompatibilityCheckResult> CheckCompatibilityAsync(List<Guid> herbIds)
    {
        try
        {
            var result = await _herbService.CheckCompatibilityAsync(herbIds);
            return result.IsSuccess ? result.Data : new CompatibilityCheckResult { IsSafe = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查药材配伍失败");
            return new CompatibilityCheckResult { IsSafe = false, ErrorMessage = "配伍检查失败" };
        }
    }
}
```

#### 价格计算支持
```csharp
public class PrescriptionCostCalculator
{
    public async Task<decimal> CalculateTotalCostAsync(List<PrescriptionItemDto> items)
    {
        decimal totalCost = 0;
        
        foreach (var item in items)
        {
            var herb = await _herbService.GetByIdAsync(item.HerbId);
            if (herb.IsSuccess && herb.Data != null)
            {
                totalCost += herb.Data.UnitPrice * item.Quantity;
            }
        }
        
        return totalCost;
    }
}
```

## 使用指南

### 模块注册和启动

```csharp
// 在App.xaml.cs中注册Herbs模块
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<HerbsModule>();
}

// 导航到药材管理界面
_regionManager.RequestNavigate("ContentRegion", "HerbManagementView");
```

### 药材操作流程

```csharp
// 1. 加载药材列表
await LoadHerbsAsync();

// 2. 搜索特定药材
await SearchHerbsAsync("当归");

// 3. 新增药材
await ShowAddHerbDialogAsync();

// 4. 编辑药材信息
await ShowEditHerbDialogAsync(selectedHerbId);

// 5. 批量更新价格
await UpdateHerbPricesAsync();

// 6. 导入导出药材数据
await ImportHerbsFromFileAsync(filePath);
await ExportHerbsToFileAsync(filePath);
```

### 中药材分类管理

```csharp
// 常用药材功效分类
public static class HerbCategories
{
    public const string JieBiaoYao = "解表药";       // 解表药
    public const string QingReYao = "清热药";       // 清热药
    public const string XieXiaYao = "泻下药";       // 泻下药
    public const string QuFengShiYao = "祛风湿药";   // 祛风湿药
    public const string HuaShiYao = "化湿药";       // 化湿药
    public const string LiShuiShenShiYao = "利水渗湿药"; // 利水渗湿药
    public const string WenLiYao = "温里药";        // 温里药
    public const string LiQiYao = "理气药";        // 理气药
    public const string XiaoShiYao = "消食药";      // 消食药
    public const string ZhiXueYao = "止血药";       // 止血药
    public const string HuoXueHuaYuYao = "活血化瘀药"; // 活血化瘀药
    public const string HuaTanZhiKeYao = "化痰止咳药"; // 化痰止咳药
    public const string AnShenYao = "安神药";       // 安神药
    public const string PingGanXiFengYao = "平肝息风药"; // 平肝息风药
    public const string BuXuYao = "补虚药";        // 补虚药
    public const string ShouSeYao = "收涩药";       // 收涩药
    public const string YongTuYao = "涌吐药";       // 涌吐药
    public const string ShaChongYao = "杀虫药";      // 杀虫药
    public const string WaiYongYao = "外用药";       // 外用药
}
```

## 开发规范

### 数据验证
- 药材名称不能为空且不能重复
- 单价必须大于0
- 用量范围必须合理(最小用量 ≤ 推荐用量 ≤ 最大用量)
- 性味、归经使用标准中医术语

### MVVM实现
- 所有ViewModel继承CoreViewModel或DialogViewModelBase
- 使用ObservableCollection管理药材集合
- 异步操作使用AsyncRelayCommand
- 通过EventAggregator与其他模块通信

### 用户体验
- 提供智能搜索和多维度筛选
- 支持药材信息的批量导入导出
- 提供丰富的药材详情展示
- 实现价格的历史跟踪和分析

### 数据安全
- 价格信息需要权限验证才能修改
- 重要操作记录审计日志
- 支持数据备份和恢复
- 导入数据需要格式验证

## 中医药材知识库

### 🌿 药材信息标准化
- **标准名称**: 使用《中华人民共和国药典》标准名称
- **性味归经**: 严格按照中医理论分类
- **功效主治**: 使用规范的中医术语描述
- **用法用量**: 遵循临床用药规范

### 🔍 智能功能
- **症状匹配**: 根据症状推荐相关药材
- **配伍检查**: 自动检测药材配伍禁忌
- **剂量建议**: 基于患者情况推荐合理剂量
- **价格分析**: 药材价格趋势分析和预测

### 📊 数据维护
- **定期更新**: 药材价格的定期更新机制
- **质量控制**: 药材信息的准确性验证
- **版本管理**: 药材数据的版本控制
- **标准化**: 确保数据符合行业标准

## 维护说明

### 价格管理
- **市场价格**: 关注中药材市场价格变化
- **供应商管理**: 维护可靠的药材供应商信息
- **价格预警**: 异常价格变动的预警机制
- **成本控制**: 帮助医院控制药材成本

### 数据质量
- **信息准确性**: 确保药材信息的准确性和时效性
- **标准化**: 持续改进数据标准化程度
- **完整性**: 补充缺失的药材信息
- **一致性**: 保持数据格式和内容的一致性

### 功能扩展
- **图像识别**: 药材图片识别功能
- **供应链**: 药材供应链管理功能
- **质量追溯**: 药材来源和质量追溯
- **移动应用**: 移动端药材查询应用

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*