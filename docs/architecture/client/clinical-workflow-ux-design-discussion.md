# 就诊流程UI/UX设计讨论文档

> **文档类型**: 架构设计讨论 - Client端UI/UX
> **创建日期**: 2025-10-18
> **最后更新**: 2025-10-18
> **状态**: 讨论中
> **关联Epic**: #1343 MVP就诊功能

---

## 📋 文档目的

基于用户反馈，医生登录后看到空白主界面，缺少工作入口感。本文档旨在系统设计一套完整的就诊流程UI/UX，包括：
1. 是否需要首页/Dashboard
2. 核心就诊界面布局设计
3. 三种处方录入方式的UI实现
4. 导航流程与用户体验优化
5. WPF技术实现方案

---

## 1. 当前状态分析

### ✅ 已完成功能
- **Issue #1457**: 患者选择功能（PatientSelectionView）
- **Server端API**: CRUD、验方导入、历史处方查询已完成
- **ViewModel骨架**: PrescriptionViewModel基本结构存在

### ❌ 当前问题
1. **空白主界面**: 医生登录后主区域空白，不知道下一步做什么
2. **导航不清晰**: 左侧菜单是"功能导向"而非"流程导向"
3. **核心就诊界面缺失**: ConsultationView不存在
4. **处方录入UI未实现**: Entry Method #1/#2/#3全部未开发
5. **用户体验问题**: 缺少工作节奏的"锚点"

### 🎯 设计目标
- ✅ 流程清晰：医生知道每一步该做什么
- ✅ 操作高效：支持键盘快捷操作，减少鼠标点击
- ✅ 视觉简洁：WPF原生控件，简单大方（Q2-3决策）
- ✅ 响应迅速：大数据量场景下性能优化
- ✅ 容错性强：异常处理、自动保存、操作可撤销

---

## 2. 核心UI/UX设计方案

### 2.1 整体导航结构

```
登录成功
    ↓
主界面（MainWindow）
├─ 顶部栏：标题 + 医生信息 + [退出登录]
├─ 左侧菜单：导航（ListBox/TreeView）
└─ 右侧主区域：ContentControl（动态加载View）
    ↓
首页/Dashboard（可选，待讨论Q1）
    ↓
患者选择界面（PatientSelectionView）
    ↓
就诊主界面（ConsultationView）⭐核心
├─ 患者信息区（顶部固定）
├─ 诊断区（可折叠）
└─ 处方区（Tab切换三种录入方式）
    ↓
完成就诊 → 返回患者选择
```

---

### 2.2 核心就诊界面布局设计（ConsultationView）

#### 方案A：三段式布局（✅ 推荐）

```
┌─────────────────────────────────────────────────────────────┐
│ 患者信息条（顶部固定，浅蓝色背景）                              │
│ 姓名：张三 | 性别：男 | 年龄：45岁 | 电话：138xxxx | [查看历史] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─ 诊断区（Expander可折叠）────────────────────────────────┐ │
│ │ ▼ 诊断信息                                               │ │
│ │                                                          │ │
│ │ 主诉（必填）：     [TextBox 多行]                         │ │
│ │ 现病史（必填）：   [TextBox 多行]                         │ │
│ │ 中医诊断（必填）： [TextBox 多行]                         │ │
│ │                                                          │ │
│ │ ▶ 其他四诊（选填，默认折叠）                              │ │
│ │   望诊：[TextBox]  闻诊：[TextBox]                        │ │
│ │   问诊：[TextBox]  切诊：[TextBox]                        │ │
│ │   治疗原则：[TextBox]                                     │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─ 处方区（主要工作区）─────────────────────────────────────┐ │
│ │ [📝 手工录入] [📋 验方导入] [🕐 历史复制]                  │ │
│ │ ─────────────────────────────────────────────────────── │ │
│ │                                                          │ │
│ │ [DataGrid - 8列表格，4药材/行]                           │ │
│ │ 药材1  剂量  单位  药材2  剂量  单位  药材3  剂量  单位... │ │
│ │                                                          │ │
│ │ [+添加行] [清空] 药材总数：X味                            │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
│ 底部操作栏（固定）                                           │
│ [保存草稿] [完成就诊] [打印处方] [取消]      最后保存：XX:XX  │
└─────────────────────────────────────────────────────────────┘
```

**优点**：
- ✅ 信息层级清晰（上→下阅读顺序）
- ✅ 诊断区可折叠，处方区获得更多空间
- ✅ 处方录入占据主要视觉区域（核心工作区）
- ✅ 底部操作栏始终可见

**缺点**：
- ⚠️ 低分辨率显示器需要滚动（通过折叠诊断区缓解）

---

#### 方案B：左右分栏式（备选）

```
┌──────────────────────┬────────────────────────────────┐
│ 患者信息（顶部横跨）  │                                │
├──────────────────────┤                                │
│                      │                                │
│ 诊断区（左侧1/3）     │   处方区（右侧2/3）             │
│ - 主诉               │   [Tab切换]                    │
│ - 现病史             │   [DataGrid]                   │
│ - 中医诊断           │                                │
│ - 四诊（折叠）        │                                │
│                      │                                │
└──────────────────────┴────────────────────────────────┘
```

**优点**：
- ✅ 诊断和处方同时可见，无需滚动

**缺点**：
- ❌ 处方DataGrid横向空间受限（8列可能拥挤）
- ❌ 诊断内容较多时，左侧过长不协调
- ❌ 1366x768分辨率显示拥挤

**对比结论**：推荐方案A（三段式）

---

### 2.3 处方录入三种方式的Tab设计

#### Tab1 - 手工录入（Entry Method #1）

**功能**：
- DataGrid表格编辑（8列布局，待明确具体列定义）
- 药材ComboBox支持拼音码过滤（输入"dg"过滤"当归"）
- 焦点自动跳转：名称 → 剂量 → 单位 → 下一个药材
- 实时验证：剂量必须数字且>0

**UI元素**：
```
[DataGrid - 8列]
药材1名称  剂量  单位  药材2名称  剂量  单位  ...

底部工具栏：
[+添加行] [删除选中行] [清空全部] [导出]
药材总数：12味  总剂量：XXX克
```

**交互细节**：
- Tab键：列间切换
- Enter键：ComboBox选择后自动跳转
- Ctrl+↓：添加新行
- Delete：删除当前行

---

#### Tab2 - 验方导入（Entry Method #2）

**功能**：
- 搜索框：搜索验方名称
- 验方列表：显示常用验方（名称、组成、主治）
- 点击验方：自动填充到DataGrid
- 可在DataGrid中继续编辑（加减药材）

**UI元素**：
```
搜索：[TextBox 搜索验方名称]

验方列表：
┌─────────────────────────────────────┐
│ ▶ 四君子汤                          │
│   组成：人参、白术、茯苓、甘草       │
│   主治：脾胃气虚                     │
├─────────────────────────────────────┤
│ ▶ 六味地黄丸                        │
│   组成：熟地黄、山萸肉...            │
└─────────────────────────────────────┘

[导入选中验方] [取消]
```

**交互流程**：
1. 搜索/浏览验方列表
2. 点击验方查看详情
3. 点击【导入】按钮 → 自动填充到DataGrid
4. 可在手工录入Tab中继续调整

---

#### Tab3 - 历史复制（Entry Method #3）

**功能**：
- 双模式切换：
  - 模式A：当前患者历史下拉框（快速选择最近5次）
  - 模式B：全局搜索对话框（PrescriptionSearchDialog）
- 选择历史处方：复制到DataGrid
- 可继续编辑

**UI元素**：
```
当前患者历史处方：
[ComboBox - 显示最近5次处方]
2025-10-15 - 四君子汤加减（12味）
2025-10-01 - 六味地黄丸加减（8味）
...

[复制选中处方]

────────────────────────────
或
[🔍 全局搜索其他患者处方]
```

**PrescriptionSearchDialog设计**：
```
全局处方搜索
─────────────────────────────
搜索条件：
患者姓名：[TextBox]
处方日期：[DatePicker] 至 [DatePicker]
包含药材：[TextBox]

[搜索]

搜索结果：
┌────────────────────────────────────────┐
│ 患者   日期        处方组成             │
├────────────────────────────────────────┤
│ 李四  2025-10-10  当归15g 黄芪30g...   │
│ 王五  2025-09-20  党参20g 白术15g...   │
└────────────────────────────────────────┘

[复制选中] [关闭]
```

---

### 2.4 键盘导航与快捷键

**全局快捷键**：
- `Ctrl+N`：新增患者
- `Ctrl+F`：搜索患者
- `Ctrl+S`：保存草稿
- `Ctrl+Enter`：完成就诊
- `Esc`：取消/返回

**处方录入快捷键**：
- `Tab`：列间切换
- `Enter`：确认选择并跳转
- `Ctrl+↓`：添加新行
- `Delete`：删除当前行
- `Ctrl+D`：复制当前行

---

### 2.5 视觉设计风格

#### 色彩方案（基于Q2-3决策：简单大方）

| 用途 | 颜色 | 用途说明 |
|-----|------|---------|
| **主色调** | #2196F3（蓝色） | 医疗系统标准色，按钮、标题 |
| **辅助色** | #757575（灰色） | 次要信息、禁用状态 |
| **成功色** | #4CAF50（绿色） | 成功提示、完成状态 |
| **警告色** | #F44336（红色） | 错误提示、必填项 |
| **背景色** | #F5F5F5（浅灰） | 主区域背景 |
| **内容背景** | #FFFFFF（白色） | 卡片、输入框背景 |

#### 排版规范

| 元素 | 字体 | 大小 | 颜色 |
|-----|------|------|------|
| **一级标题** | 微软雅黑 Bold | 16px | #212121 |
| **正文** | 微软雅黑 Regular | 14px | #424242 |
| **说明文字** | 微软雅黑 Regular | 12px | #757575 |
| **行高** | - | 1.5倍 | - |

#### 控件样式统一化

**Button样式**：
- 圆角：4px
- 内边距：10px 20px
- 主按钮：蓝色背景+白色文字
- 次按钮：白色背景+蓝色边框

**TextBox样式**：
- 边框：1px solid #BDBDBD
- 圆角：2px
- 获得焦点：边框变为2px solid #2196F3

**DataGrid样式**：
- 斑马纹：偶数行 #F9F9F9
- 鼠标悬停：#E3F2FD
- 选中行：#BBDEFB

**实现方式**：
- 创建ResourceDictionary：`Styles/Controls.xaml`
- App.xaml中引用全局样式
- 所有控件自动应用统一样式

---

## 3. WPF技术实现方案

### 3.1 MVVM架构与View组织

**View层结构**：

```
Views/
├─ MainWindow.xaml（主窗口）
│  ├─ 顶部：TitleBar + UserInfo
│  ├─ 左侧：NavigationMenu（ListBox）
│  └─ 右侧：ContentControl（动态加载）
│
├─ Home/
│  └─ HomeView.xaml（首页/Dashboard，可选）
│
├─ Patients/
│  ├─ PatientSelectionView.xaml（患者选择）
│  └─ PatientDetailControl.xaml（患者详情UserControl）
│
├─ Consultation/
│  ├─ ConsultationView.xaml（就诊主界面）⭐核心
│  ├─ PatientInfoBar.xaml（患者信息条UserControl）
│  ├─ ConsultationFormControl.xaml（诊断表单UserControl）
│  └─ PrescriptionEditorControl.xaml（处方编辑器UserControl）
│      ├─ ManualEntryControl.xaml（手工录入）
│      ├─ FormulaImportControl.xaml（验方导入）
│      └─ HistoryImportControl.xaml（历史复制）
│
└─ Dialogs/
   ├─ FormulaTemplateDialog.xaml（验方选择对话框）
   └─ PrescriptionSearchDialog.xaml（处方全局搜索对话框）
```

**ViewModel对应**：
- `HomeViewModel` → `HomeView`
- `PatientSelectionViewModel` → `PatientSelectionView`
- `ConsultationViewModel` → `ConsultationView`⭐核心
- `PrescriptionEditorViewModel` → `PrescriptionEditorControl`

---

### 3.2 导航框架设计

#### 方案A：ContentControl + NavigationService（✅ 推荐）

**NavigationService接口**：
```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>(object parameter = null);
    void GoBack();
    bool CanGoBack();
}
```

**MainViewModel实现**：
```csharp
public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public void NavigateTo<TViewModel>(object parameter = null)
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        if (viewModel is INavigationAware aware)
            aware.OnNavigatedTo(parameter);

        CurrentViewModel = viewModel as ViewModelBase;
    }
}
```

**MainWindow.xaml绑定**：
```xaml
<ContentControl Content="{Binding CurrentViewModel}">
    <ContentControl.Resources>
        <DataTemplate DataType="{x:Type vm:HomeViewModel}">
            <v:HomeView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:ConsultationViewModel}">
            <v:ConsultationView />
        </DataTemplate>
    </ContentControl.Resources>
</ContentControl>
```

**优点**：
- ✅ 流畅切换，无页面刷新感
- ✅ ViewModel状态自动保持
- ✅ 支持参数传递

---

### 3.3 性能优化策略

#### 1. 虚拟化（Virtualization）

**患者列表虚拟化**：
```xaml
<ListView VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          VirtualizingPanel.ScrollUnit="Pixel">
    <!-- 只渲染可见项，数千患者也流畅 -->
</ListView>
```

#### 2. 药材ComboBox优化

**方案**：使用AutoCompleteBox（自实现或第三方）
```csharp
// 输入2个字符后才开始过滤
private void OnTextChanged(string text)
{
    if (text.Length < 2) return;

    var filtered = _allHerbs
        .Where(h => h.PinyinCode.Contains(text) || h.Name.Contains(text))
        .Take(50); // 限制显示前50个

    ItemsSource = filtered;
}
```

#### 3. 异步加载

**所有Repository调用使用async/await**：
```csharp
public async Task LoadPatientsAsync()
{
    IsLoading = true;
    try
    {
        Patients = await _patientRepository.GetRecentPatientsAsync(50);
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### 4. 缓存策略

**药材列表启动时加载**：
```csharp
public class HerbCacheService
{
    private List<HerbDto> _cachedHerbs;

    public async Task InitializeAsync()
    {
        _cachedHerbs = await _herbRepository.GetAllAsync();
    }

    public List<HerbDto> GetAll() => _cachedHerbs;
}
```

---

### 3.4 数据验证与用户反馈

#### 实时验证（IDataErrorInfo）

```csharp
public class ConsultationViewModel : ViewModelBase, IDataErrorInfo
{
    private string _chiefComplaint;
    public string ChiefComplaint
    {
        get => _chiefComplaint;
        set => SetProperty(ref _chiefComplaint, value);
    }

    public string this[string propertyName]
    {
        get
        {
            if (propertyName == nameof(ChiefComplaint))
            {
                if (string.IsNullOrWhiteSpace(ChiefComplaint))
                    return "主诉为必填项";
            }
            return null;
        }
    }
}
```

#### 提交验证

```csharp
private bool ValidateBeforeComplete()
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(ChiefComplaint))
        errors.Add("主诉为必填项");

    if (PrescriptionItems.Count == 0)
        errors.Add("处方至少需要1味药材");

    if (errors.Any())
    {
        MessageBox.Show(string.Join("\n", errors), "验证失败");
        return false;
    }

    return true;
}
```

#### Toast通知（可选）

```xaml
<!-- 右下角Toast通知 -->
<Border x:Name="ToastNotification"
        Background="#4CAF50"
        CornerRadius="4"
        Padding="15"
        Visibility="Collapsed">
    <TextBlock Text="保存成功" Foreground="White"/>
</Border>
```

---

## 4. 响应式布局适配

### 4.1 目标分辨率

| 分辨率 | 支持级别 | 布局策略 |
|--------|---------|---------|
| **1366x768** | ✅ 最低支持 | 诊断区默认折叠 |
| **1920x1080** | ✅ 推荐 | 诊断区展开 |
| **2560x1440** | ✅ 高分辨率 | 所有区域舒展显示 |

### 4.2 Grid布局策略

**使用*而非固定宽度**：
```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200" MinWidth="150"/><!-- 左侧菜单 -->
        <ColumnDefinition Width="*"/><!-- 主内容区自适应 -->
    </Grid.ColumnDefinitions>
</Grid>
```

### 4.3 ScrollViewer包裹内容

**低分辨率可滚动**：
```xaml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel>
        <!-- 诊断区 -->
        <!-- 处方区 -->
    </StackPanel>
</ScrollViewer>
```

---

## 5. 开发任务分解

### Phase 0 - UI/UX基础架构（P0优先级）

#### Task 1: 全局样式系统（1-2天）
- [ ] 创建`Styles/Colors.xaml`（色彩定义）
- [ ] 创建`Styles/Controls.xaml`（Button、TextBox、DataGrid样式）
- [ ] 创建`Styles/Typography.xaml`（字体排版）
- [ ] `App.xaml`引用全局样式
- [ ] 测试样式在不同分辨率下的效果

#### Task 2: 导航框架（1天）
- [ ] 实现`INavigationService`接口
- [ ] `MainViewModel`导航逻辑
- [ ] `MainWindow`布局调整（左侧菜单+右侧ContentControl）
- [ ] DataTemplate注册（ViewModel → View映射）
- [ ] 导航历史管理（GoBack功能）

#### Task 3: 首页Dashboard（0.5天，可选，待讨论Q1）
- [ ] `HomeView` + `HomeViewModel`
- [ ] 显示医生信息、当前日期
- [ ] 【开始接诊】按钮 → 导航到`PatientSelectionView`
- [ ] 简单统计（今日接诊数，可选）

#### Task 4: 优化患者选择界面（1-2天）
- [ ] 审查现有`PatientSelectionView`（Issue #1457）
- [ ] 优化UI布局（搜索框+患者列表+详情面板）
- [ ] 实现拼音码搜索功能
- [ ] 虚拟化ListView（数千患者性能优化）
- [ ] 【选择患者】→ 导航到`ConsultationView`，传递PatientDto

---

### Phase 0 - 核心就诊界面（P0优先级）

#### Task 5: ConsultationView主框架（1天）
- [ ] 创建`ConsultationView` + `ConsultationViewModel`
- [ ] 三段式布局（患者信息+诊断+处方）
- [ ] `PatientInfoBar` UserControl（显示患者基本信息）
- [ ] 诊断区Expander可折叠设计
- [ ] 底部操作栏（保存/完成/打印/取消）

#### Task 6: 诊断表单（ConsultationFormControl）（2-3天）
- [ ] 创建`ConsultationFormControl` UserControl
- [ ] 8个字段布局（Grid 2列布局）
  - 主诉（必填）、现病史（必填）、中医诊断（必填）
  - 望诊、闻诊、问诊、切诊、治疗原则（选填，Expander折叠）
- [ ] TextBox样式统一（多行、高度自适应）
- [ ] 实时验证（IDataErrorInfo）
- [ ] 数据绑定到`ConsultationDto`

#### Task 7: 处方录入 - Entry Method #1（3-4天）⭐最复杂
- [ ] 创建`PrescriptionEditorControl` + TabControl
- [ ] 创建`ManualEntryControl` + DataGrid
- [ ] 明确DataGrid 8列具体定义（待确认Q3）
- [ ] 药材ComboBox + 拼音码过滤（实时搜索）
- [ ] 焦点自动跳转逻辑（Tab/Enter键处理）
- [ ] 剂量/单位输入验证
- [ ] 添加行、删除行、清空功能
- [ ] 底部统计：药材总数、总剂量

#### Task 8: 处方录入 - Entry Method #2（2天）
- [ ] 创建`FormulaImportControl`
- [ ] 创建`FormulaTemplateDialog`（验方选择对话框）
- [ ] 验方列表展示（搜索框+ListView）
- [ ] 验方详情显示（组成、主治）
- [ ] 选择验方 → 自动填充到DataGrid
- [ ] 支持继续在DataGrid中编辑

#### Task 9: 处方录入 - Entry Method #3（2-3天）
- [ ] 创建`HistoryImportControl`
- [ ] 当前患者历史下拉框（最近5次）
- [ ] 创建`PrescriptionSearchDialog`（全局搜索对话框）
  - 搜索条件：患者姓名、日期范围、包含药材
  - 搜索结果列表（DataGrid）
- [ ] 选择历史处方 → 复制到DataGrid
- [ ] 支持继续编辑

#### Task 10: 就诊流程控制（1-2天）
- [ ] 底部操作栏实现
  - [保存草稿]：调用API保存，Toast提示
  - [完成就诊]：验证必填项 → 调用API → 导航返回
  - [打印处方]：调用打印服务（可选，Phase 1）
  - [取消]：确认提示 → 返回
- [ ] 状态管理（诊断中/处方中/已完成）
- [ ] 自动保存草稿（每5分钟，本地存储）
- [ ] 异常处理（网络失败、数据冲突）
- [ ] 成功/失败反馈（MessageBox/Toast）

---

### Phase 1 - 增强功能（P1优先级，Phase 0完成后）

#### Task 11: 打印功能（1天）
- [ ] FlowDocument模板设计
- [ ] 数据绑定生成打印内容
- [ ] PrintDialog集成
- [ ] 打印预览功能

#### Task 12: 键盘快捷键增强（0.5天）
- [ ] 全局快捷键注册（Ctrl+N/F/S/Enter）
- [ ] 快捷键提示界面（可选）

#### Task 13: 错误恢复与草稿管理（1天）
- [ ] 程序启动时检测草稿
- [ ] 提示恢复草稿对话框
- [ ] 本地SQLite存储草稿数据

---

## 6. 工作量估算

### 6.1 总工作量

| 分类 | 任务数 | 预估工作量 |
|-----|-------|-----------|
| **基础架构（Task 1-4）** | 4 | 3.5-5.5天 |
| **核心就诊界面（Task 5-10）** | 6 | 10.5-13.5天 |
| **增强功能（Task 11-13）** | 3 | 2.5天 |
| **总计** | 13 | **16.5-21.5天** |

### 6.2 Phase 0关键路径（P0必须）

```
Task 2（导航框架） → Task 4（患者选择） → Task 5（主框架） →
Task 6（诊断表单） → Task 7（Entry Method #1） → Task 10（流程控制）
```

**关键路径工作量**：10-12天

**与mvp-development-strategy-discussion.md对齐**：
- 原估算：2周（10个工作日）
- 现估算：10-12天（考虑Task 1/3/8/9为增强功能）
- ✅ 基本吻合

---

## 7. 风险识别与缓解措施

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| **DataGrid 8列定义不明确** | 🔴 高 | 🔴 高 | 立即查阅prescription-entry-requirements.md确认，或向用户明确（Q3） |
| **现有PatientSelectionView质量未知** | 🟡 中 | 🟡 中 | 先审查代码，评估是否可复用，不行则重构 |
| **药材拼音码过滤性能** | 🟡 中 | 🟡 中 | 使用虚拟化、限制过滤触发频率、缓存药材列表 |
| **设计-开发理解偏差** | 🟡 中 | 🟡 中 | 创建详细设计文档（本文档）+原型图，先评审再开发（Q4） |
| **WPF技能缺口** | 🟢 低 | 🟡 中 | 提前学习、Context7查询文档、参考示例代码 |

---

## 8. 待讨论问题（按一问一答原则）

### ✅ [已确认-Q1] 首页Dashboard是否需要？

**✅ 用户决策**：选项A - 简单Dashboard

**决策时间**：2025-10-18

**决策说明**：
- 采用简单Dashboard设计
- 显示医生信息、当前日期、【开始接诊】按钮
- 简单统计（今日接诊数，可选）
- 快速搜索功能（可选）

**实施影响**：
- Task 3（首页Dashboard）加入P0关键路径
- 工作量：0.5天
- 登录流程：登录成功 → HomeView(Dashboard) → 点击【开始接诊】→ PatientSelectionView → ConsultationView

**原选项分析**：

**选项A**：✅ **简单Dashboard**（已选择）
- **内容**：
  - 显示当前日期、医生姓名
  - 核心行动按钮：【开始接诊】（导航到患者选择）
  - 简单统计：今日已接诊X人（可选）
  - 快速搜索：患者姓名/电话快速查找（可选）
- **优点**：
  - 提供工作节奏的"锚点"
  - 医生有明确的起始页
  - 可扩展（后续增加统计图表）
- **工作量**：0.5天

**选项B**：❌ 直接进入患者选择
- **操作**：登录成功 → 直接显示PatientSelectionView
- **优点**：
  - 开发成本最低
  - 减少一次点击
- **缺点**：
  - 缺少"主页"的概念
  - 医生可能需要频繁切换患者，没有"返回首页"的锚点
  - 左侧菜单没有"首页"选项，导航不完整

**选项C**：⚠️ 完整Dashboard（高级）
- **内容**：
  - 多维度统计图表（今日/本周/本月接诊量）
  - 待办事项提醒
  - 患者预约日历
- **缺点**：
  - 开发成本高（2-3天）
  - MVP阶段不推荐
  - 小型诊所可能用不上

---

### ✅ [已确认-Q2] 现有患者选择界面处理方式

**✅ 用户决策**：选项A - 审查优化（增量改进）

**决策时间**：2025-10-19

**代码审查结果**：
- ✅ 代码质量优秀：符合MVVM架构，完善的异步处理和错误处理
- ✅ UI设计简洁：5列DataGrid（姓名、性别、年龄、手机、最近就诊）
- ✅ 功能完整：支持搜索（姓名/拼音码/手机号）、双击选择、刷新
- ⚠️ 需要优化：虚拟化性能、拼音码提示、新建患者功能

**实施要求**（用户明确）：
1. ✅ **拼音码功能**：MVP必需，需优化UI提示（搜索框Placeholder）
2. ✅ **新建患者功能**：必须开发（当前仅"开发中"提示，不可接受）
3. ✅ **虚拟化优化**：启用`VirtualizingPanel.IsVirtualizing="True"`
4. ✅ **布局对齐检查**：与UI讨论文档方案对齐（如需调整）

**工作量估算**：1-1.5天
- 虚拟化优化：0.5小时
- 拼音码提示增强：0.5小时
- 新建患者功能开发：1天（快速创建对话框）
- 布局对齐调整：0.5天（如需要）

**原选项分析**：

**选项A**：✅ **审查优化**（已选择）
- 保留现有高质量代码
- 增量优化4处改进点
- 符合MVP"增量优化"原则
- 快速交付（1-1.5天）

**选项B**：❌ 完全推倒重来
- 浪费已完成代码
- 增加2-3天开发时间
- 违反MVP原则

**选项C**：❌ 保持现状
- 新建患者功能缺失（用户不接受）
- 拼音码提示缺失（MVP必需）

---

### ✅ [已明确-Q3] DataGrid 8列布局定义

**✅ 文档确认**：基于 `docs/reports/prescription-entry-requirements-2025-10-16.md` 第1.1节

**确认时间**：2025-10-19

**明确定义**：
- ✅ **一行4个药材**（每个药材占2列）
- ✅ **每个药材2列**：药材名称 + 用量
- ✅ **总共8列**：药材1、用量1、药材2、用量2、药材3、用量3、药材4、用量4

**表格布局示例**：
```
┌────────┬──────┬────────┬──────┬────────┬──────┬────────┬──────┐
│ 药材1  │ 用量1 │ 药材2  │ 用量2 │ 药材3  │ 用量3 │ 药材4  │ 用量4 │
├────────┼──────┼────────┼──────┼────────┼──────┼────────┼──────┤
│ 黄芪   │ 15g  │ 红枣   │ 3个  │ 五味子 │ 6g   │ 细辛   │ 6g   │
│ 当归   │ 10g  │ 白芍   │ 15g  │ 川芎   │ 6g   │ 熟地   │ 20g  │
│ 党参   │ 12g  │ 茯苓   │ 10g  │ 甘草   │ 6g   │        │      │
└────────┴──────┴────────┴──────┴────────┴──────┴────────┴──────┘
```

**技术实现要点**（来自需求文档）：
1. ✅ **数据模型**：`PrescriptionItemRow`包含4个`PrescriptionItemViewModel`
2. ✅ **药材列**：可编辑ComboBox，支持拼音码过滤
3. ✅ **用量列**：TextBox，输入数字+单位（如"15g"、"3个"）
4. ✅ **焦点跳转**：药材 → 用量 → 下一个药材（Tab/Enter键）
5. ✅ **单位处理**：用量中包含单位（不单独一列）

**与UI讨论文档第1.1节对齐**：✅ 一致

**参考文档**：
- `docs/reports/prescription-entry-requirements-2025-10-16.md` 第1.1-1.2节
- 数据模型映射：`PrescriptionItemRow`示例代码（第64-92行）

---

### ✅ [已确认-Q4] 是否需要创建详细的UI原型图

**✅ 用户决策**：选项A - 创建详细文档（Markdown + ASCII原型图）

**决策时间**：2025-10-19

**实施计划**：
1. ✅ **创建UI原型图文档**：`clinical-workflow-ui-prototypes.md`
   - ConsultationView完整布局图（三段式）
   - PrescriptionEditor三种录入方式布局图
   - 焦点跳转流程图
   - 样式规范文档（色彩、字体、控件样式）

2. ✅ **原型图内容**：
   - ASCII格式布局图（精确尺寸和比例）
   - 所有控件位置和属性（宽度、高度、边距）
   - 交互流程图（焦点跳转、快捷键）
   - 样式代码清单（ResourceDictionary定义）

3. ✅ **投入产出分析**：
   - 设计时间：1天（6-8小时）
   - 节省返工时间：3天（基于历史经验）
   - 返工率：从30%降低到<5%
   - 净收益：2天工作量节省

**工作量估算**：1天（6-8小时）

**原选项分析**：

**选项A**：✅ **创建详细文档**（已选择）
- 设计与开发100%对齐
- 所有UI细节提前明确
- 可以在开发前确认用户体验
- 后续开发人员可直接参考

**选项B**：❌ 直接开始编码
- 快速启动但高返工风险
- 沟通成本高
- 样式不统一风险

**选项C**：❌ 高保真设计稿
- 工作量巨大（2-3天）
- 违反MVP原则

---

## 9. 参考文档

- **开发策略讨论**：`docs/architecture/shared/mvp-development-strategy-discussion.md`
- **处方录入需求**：`docs/reports/prescription-entry-requirements-2025-10-16.md`
- **Phase 2调查报告**：`docs/reports/phase2-code-investigation-2025-10-18.md`
- **Client端架构**：`docs/architecture/client/README.md`
- **CLAUDE.md**：Section 1.6 需求讨论与文档化规范
- **Epic #1343**：GitHub Issue（57个任务清单）

---

## 10. 下一步行动

### ✅ 已完成
1. ✅ 深度UI/UX分析（sequential-thinking 25步）
2. ✅ 创建本讨论文档
3. ✅ Q1确认：简单Dashboard（0.5天）
4. ✅ Q2确认：审查优化PatientSelectionView（1-1.5天）
5. ✅ Q3明确：DataGrid 8列布局定义（一行4药材，每药材2列）
6. ✅ Q4确认：创建详细UI原型图文档（1天）

### 📋 待执行（按优先级）

**立即行动**（P0优先级）：
1. 🔄 **创建UI原型图文档**：`clinical-workflow-ui-prototypes.md`
   - ConsultationView完整布局图（1920x1080基准）
   - PrescriptionEditor三种录入方式详细布局
   - 焦点跳转流程图
   - 样式规范文档（ResourceDictionary代码）
   - **预计工作量**：6-8小时

**后续行动**（Q4完成后）：
2. 创建Phase 0 GitHub Epic Issue（就诊流程UI/UX实现）
3. 创建子Issues（Task 1-13，基于第5节任务分解）
4. 开始Task 1：全局样式系统（1-2天）
5. 开始Task 2：导航框架（1天）
6. 开始Task 4：优化患者选择界面（1-1.5天）
7. 开始Task 5-10：核心就诊界面（10.5-13.5天）

---

## 11. 文档变更记录

| 日期 | 版本 | 变更描述 | 修改人 |
|------|------|---------|-------|
| 2025-10-18 | v1.0 | 初始版本，完成UI/UX深度分析 | Claude |
| 2025-10-19 | v2.0 | 完成Q1-Q4讨论，所有问题已确认 | Claude + 用户 |
| 2025-10-19 | v2.0 | Q1: 简单Dashboard（选项A） | 用户决策 |
| 2025-10-19 | v2.0 | Q2: 审查优化PatientSelectionView（选项A） | 用户决策 |
| 2025-10-19 | v2.0 | Q3: 明确DataGrid 8列布局（一行4药材） | 文档确认 |
| 2025-10-19 | v2.0 | Q4: 创建详细UI原型图文档（选项A） | 用户决策 |
| 2025-10-19 | v3.0 | 开启Phase 2系统性重新设计（Section 12） | Claude + 用户 |
| 2025-10-19 | v3.0 | RQ1确认：小型诊所 + 主要使用诊断功能 | 用户决策 |
| 2025-10-19 | v3.1 | 基于现有分析提出三种View架构方案 | Claude |
| 2025-10-19 | v4.0 | RQ2确认方案A + 完成详细View设计 | Claude + 用户 |
| 2025-10-19 | v5.0 | 修正为4步流程（删除医案基本信息步骤），MedicalCase是核心 | Claude |
| 2025-10-19 | v6.0 | 添加全页显示设计原则 + 小屏幕兼容性优化（1366x768/1280x720） | Claude + 用户 |

---

**📌 重要提醒**：
- 本文档是讨论基础，不是最终决策
- 所有 ❓ [待讨论] 标记的问题需要逐一确认
- 达成共识后，更新状态为 ✅ [已确认]
- 文档作为唯一事实来源（Single Source of Truth）

---

## 12. 系统性重新设计（Phase 2）⭐新设计方向

### 12.1 重新设计背景

**时间**：2025-10-19

**触发原因**：用户反馈"view可以完全抛开当前设计进行系统性的完整设计"

**重新设计范围**：
- ✅ **不受Q1-Q4约束**：完全重新思考UI/UX架构
- ✅ **系统性完整设计**：从用户流程、信息架构、交互模式全面重新设计
- ✅ **保留技术约束**：仍使用WPF、MVVM、Prism框架，符合三层对齐架构

**与Phase 1的关系**：
- **Phase 1设计**（Section 1-11）：作为参考基线，记录了第一轮思考和决策
- **Phase 2设计**（本章节）：全新设计方向，可能推翻Phase 1的所有决策
- **已创建的Epic #1483和Task Issues**：待Phase 2设计确认后，决定是否保留/修改/关闭

---

### 12.2 重新设计核心问题（按一问一答原则）

> **重要提醒**：按照CLAUDE.md Section 1.6要求，每次只讨论一个问题，等待用户确认后再进入下一个问题。

---

### ✅ [已确认-RQ1] 就诊流程的核心用户旅程是什么？

**问题说明**：

在重新设计UI/UX之前，我们需要从医生的实际工作场景出发，重新思考整个就诊流程。不受之前Dashboard、三段式布局等具体设计的约束，而是从"医生每天如何工作"这个根本问题出发。

**典型场景分析**：

医生的一天工作可能有以下几种模式：

**场景A：流水线式接诊**（社区诊所、中医门诊）
```
上午8:00-12:00持续接诊：
1. 患者A进入 → 询问病情 → 录入诊断 → 开处方 → 患者离开
2. 患者B进入 → 询问病情 → 录入诊断 → 开处方 → 患者离开
3. 患者C进入 → ...
（中间没有明显的"切换患者"步骤，是连续的流）
```

**场景B：预约式接诊**（专家门诊、名医工作室）
```
9:00 患者A（预约）
9:30 患者B（预约）
10:00 患者C（预约）
（有明确的时间间隔，每个患者之间可能有准备时间）
```

**场景C：混合式工作**（小型诊所）
```
- 接诊中间可能需要查看库存
- 接诊中间可能需要查询患者历史
- 接诊中间可能需要处理其他事务
（需要在不同功能模块间频繁切换）
```

**核心问题**：

**RQ1：您的诊所是哪种工作模式？或者说，医生最常见的工作节奏是什么样的？**

**选项A：流水线式**
- UI设计重点：快速录入、最少点击、连续流程
- 可能的设计方向：
  - 不需要Dashboard
  - 患者选择 → 就诊界面 → 【完成】→ 立即回到患者选择（无中断）
  - 就诊界面保持在屏幕上，只切换患者信息
  - 快捷键优先（F1新患者、F2搜索、Enter完成）

**选项B：预约式**
- UI设计重点：患者信息展示、充裕的录入时间、详细的历史查看
- 可能的设计方向：
  - 需要Dashboard显示今日预约列表
  - 更宽松的界面布局（不需要极致压缩）
  - 可能需要患者详情侧边栏（查看历史就诊记录）

**选项C：混合式**
- UI设计重点：灵活的多任务切换、状态保持、草稿自动保存
- 可能的设计方向：
  - 需要主界面/Dashboard作为"锚点"
  - 左侧导航需要支持快速切换功能模块
  - 就诊中的数据自动保存（切换后可恢复）

**选项D：其他**
- 您可以描述实际的工作场景，我将据此设计

---

**✅ 用户决策**：小型诊所 + 主要使用诊断相关功能

**决策时间**：2025-10-19

**关键信息**：
- ✅ **诊所类型**：小型诊所
- ✅ **核心场景**：医生主要使用诊断相关功能
- ✅ **用户角色**：医生使用医生相关功能（非管理员、非前台）

**设计影响分析**：
- ✅ **UI焦点**：诊断流程应占据主要界面空间和设计资源
- ✅ **其他功能**：可能需要，但优先级较低（如库存管理、系统设置等）
- ✅ **多任务需求**：待明确（RQ2将讨论）

---

~~**请告诉我您的决策**：选择A/B/C/D，或描述实际场景。~~

---

### 12.3 基于现有分析的View重新设计方案

**基础依据**（来自已有文档）：

根据以下文档的分析结果：
- `docs/architecture/shared/clinical-workflow-current-process.md` - 就诊流程逻辑
- `docs/reports/clinical-workflow-analysis-2025-10-18.md` - 架构分析报告
- `docs/reports/prescription-entry-requirements-2025-10-16.md` - 处方录入需求

**已明确的核心信息**：
1. ✅ **小型诊所场景**（RQ1已确认）
2. ✅ **医生主要使用诊断功能**（RQ1已确认）
3. ✅ **完整流程**：患者选择 → 病案录入 → 诊断录入 → 处方开具 → 完成
4. ✅ **核心问题**：
   - HomeView功能过载（10+导航命令）
   - 流程不连贯（患者选择后无自动创建医案）
   - 缺少流程进度提示
   - ClinicalWorkstation左侧菜单与流程脱节

---

### 12.4 View重新设计核心方案（三选一）

基于已有分析，我提出三种View架构设计方案，请您选择一种：

---

#### 方案A：流程导向单页面设计 ⭐推荐（基于小型诊所+诊断为主）

**核心理念**：医生看诊是连续流程，不应被打断或分散

**架构设计**：
```
登录成功
  ↓
HomeView（极简设计）
├─ 核心区域（80%空间）：
│  └─ 🩺 开始看诊（大按钮）
├─ 次要区域（20%空间）：
│  ├─ 今日接诊：X人
│  ├─ 🔍 快速查找患者
│  └─ ⚙️ 设置（折叠菜单：患者管理/处方查询/系统设置）
└─ 点击【开始看诊】→ MedicalCaseFlowView

  ↓

MedicalCaseFlowView（全屏流程视图）⭐核心改动
├─ 顶部：流程进度条（固定）
│  └─ [选患者✓] → [填病案●] → [录诊断] → [开处方] → [完成]
├─ 患者信息条（固定，浅蓝背景）
│  └─ 姓名：张三 | 性别：男 | 年龄：45 | [更换患者]
├─ 主内容区（动态切换，无需左侧菜单）：
│  ├─ Step 1: PatientSelectionView（内嵌）
│  ├─ Step 2: ConsultationForm（诊断表单）
│  ├─ Step 3: PrescriptionEditor（处方编辑器）
│  └─ Step 4: CompletionView（完成提示）
└─ 底部操作栏（固定）
   ├─ [上一步] [下一步] [保存草稿] [取消]
   └─ 最后保存：2025-10-19 14:30
```

**交互流程**：
```
1. 点击【开始看诊】→ 显示 PatientSelectionView（内嵌在主内容区）
2. 选择患者 → 自动创建医案 → 自动跳转到 Step 2（诊断表单）
3. 填写诊断 → 点击【下一步】→ 自动保存 → 跳转到 Step 3（处方编辑器）
4. 填写处方 → 点击【完成看诊】→ 保存 → 显示 Step 4（完成提示）
5. 提示：是否继续看诊？
   - 是 → 返回 Step 1（患者选择）
   - 否 → 返回 HomeView
```

**关键特性**：
- ✅ **无左侧导航菜单**：流程步骤自动推进
- ✅ **流程进度条可见**：医生随时知道当前位置
- ✅ **支持【上一步】**：医生可以返回修改
- ✅ **自动保存**：每步完成自动保存草稿
- ✅ **连贯性强**：患者选择 → 完成看诊一气呵成

**优点**：
- ✅ 符合小型诊所"流水线式接诊"场景
- ✅ 操作简单，学习成本低
- ✅ 流程清晰，不会迷失在菜单中

**缺点**：
- ⚠️ 灵活性较低（如果医生需要频繁跳转到其他模块）
- ⚠️ 不适合"需要查看多个患者历史对比"的场景

**技术实现**：
```csharp
// MedicalCaseFlowViewModel.cs
private int _currentStep = 1;
private ViewModelBase _currentStepViewModel;

private void NavigateToStep(int step)
{
    _currentStep = step;

    CurrentStepViewModel = step switch
    {
        1 => new PatientSelectionViewModel(),
        2 => new ConsultationFormViewModel(MedicalCaseId),
        3 => new PrescriptionEditorViewModel(MedicalCaseId),
        4 => new CompletionViewModel(),
        _ => CurrentStepViewModel
    };

    UpdateProgressBar();
}

private async void NextStep()
{
    // 保存当前步骤
    await SaveCurrentStepAsync();

    // 跳转到下一步
    NavigateToStep(_currentStep + 1);
}
```

---

#### 方案B：传统左侧菜单 + 流程优化设计

**核心理念**：保留现有架构，优化流程连贯性

**架构设计**：
```
登录成功
  ↓
HomeView（优化设计）
├─ 主动作区：🩺 开始看诊（大按钮）
├─ 快速访问区：今日患者列表（点击直接进入看诊）
└─ 设置区：⚙️ 系统设置（折叠）

  ↓

ClinicalWorkstationView（保留左侧菜单结构）
├─ 顶部：流程进度条 + 患者信息条
├─ 左侧菜单（简化）：
│  ├─ 📋 病案录入
│  ├─ 🔬 诊断录入
│  ├─ 💊 处方开具
│  └─ ✓ 完成看诊
├─ 主内容区：ContentControl（动态加载View）
└─ 底部操作栏：[保存] [取消] [返回主页]
```

**改进点**：
- ✅ 患者选择后自动创建医案
- ✅ 病案保存后自动跳转到诊断录入
- ✅ 诊断保存后自动跳转到处方开具
- ✅ 添加流程进度条
- ✅ 左侧菜单仍可用（支持手动跳转）

**优点**：
- ✅ 改动最小（基于现有代码优化）
- ✅ 灵活性高（医生可以手动跳转）

**缺点**：
- ⚠️ 左侧菜单可能分散注意力
- ⚠️ 流程引导不如方案A明确

---

#### 方案C：混合设计（流程模式 + 自由模式切换）

**核心理念**：默认流程模式，高级用户可切换自由模式

**架构设计**：
```
ClinicalWorkstationView
├─ 模式切换按钮：[流程模式●] [自由模式○]
├─ 流程模式：同方案A（无左侧菜单，自动推进）
└─ 自由模式：同方案B（左侧菜单，手动导航）
```

**优点**：
- ✅ 新手友好（流程模式）
- ✅ 高级用户灵活（自由模式）

**缺点**：
- ❌ 复杂度最高
- ❌ 两套交互逻辑，维护成本高
- ❌ 违反MVP"简单优先"原则

---

### 12.5 方案对比总结

| 维度 | 方案A：流程导向单页面 | 方案B：左侧菜单优化 | 方案C：混合模式 |
|-----|---------------------|-------------------|----------------|
| **学习成本** | ⭐⭐⭐⭐⭐ 最低 | ⭐⭐⭐ 中等 | ⭐⭐ 较高 |
| **操作效率** | ⭐⭐⭐⭐⭐ 最高（流水线） | ⭐⭐⭐⭐ 较高 | ⭐⭐⭐ 中等 |
| **灵活性** | ⭐⭐ 较低 | ⭐⭐⭐⭐ 较高 | ⭐⭐⭐⭐⭐ 最高 |
| **开发成本** | ⭐⭐⭐⭐ 中等 | ⭐⭐⭐⭐⭐ 最低（优化现有） | ⭐⭐ 较高 |
| **符合小型诊所** | ⭐⭐⭐⭐⭐ 最符合 | ⭐⭐⭐⭐ 符合 | ⭐⭐⭐ 中等 |
| **MVP适用性** | ⭐⭐⭐⭐⭐ 最适合 | ⭐⭐⭐⭐ 适合 | ⭐⭐ 不适合 |

---

### ✅ [已确认-RQ2] View架构设计方案

**✅ 用户决策**：选择方案A - 流程导向单页面设计

**决策时间**：2025-10-19

**核心确认**：
- ✅ MedicalCaseFlowView全屏流程视图
- ✅ 无左侧导航菜单
- ✅ 顶部流程进度条
- ✅ 自动推进流程（患者选择 → 诊断 → 处方 → 完成）
- ✅ 支持【上一步】【下一步】按钮
- ✅ 最符合小型诊所连续接诊场景

**设计影响**：
- ✅ HomeView需要极简化设计（突出【开始看诊】）
- ✅ ClinicalWorkstation完全重构为MedicalCaseFlowView
- ✅ 左侧菜单移除，改为流程步骤自动切换
- ✅ 需要实现流程状态机（4步流程控制）
- ✅ 需要实现自动保存和草稿恢复

---

### 12.6 详细View设计修正（基于MedicalCase为核心）⭐重要修正

**核心概念纠正**：
- ✅ **MedicalCase（医案）是DDD聚合根**，是核心容器
- ✅ **Consultation（诊断）是医案的内容**，不是独立流程
- ✅ **Prescription（处方）是医案的内容**，不是独立流程
- ✅ **1:1:1严格关系**：1个医案 = 1个诊断 + 1个处方
- ✅ **容器先于内容创建**：先创建MedicalCase，再创建Consultation和Prescription并关联

**错误命名修正**：
- ❌ ConsultationFlowView（错误，把诊断当核心）
- ✅ MedicalCaseFlowView（正确，医案是核心）

**正确流程**：
```
选择患者 → 创建医案（MedicalCase） → 填写医案基本信息 →
填写诊断（Consultation，关联到MedicalCase） →
填写处方（Prescription，关联到MedicalCase） →
完成医案（更新Status=Completed）
```

---

#### 12.6.1 HomeView - 极简主页设计

**设计目标**：突出【开始看诊】主动作，隐藏次要功能

**布局设计（1920x1080基准）**：

```
┌────────────────────────────────────────────────────────────────┐
│ LYBT 中医诊疗系统                    当前医生：张医生  [退出] │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│                        【今日统计】                             │
│                    ┌──────────────────┐                        │
│                    │ 今日接诊：12 人   │                        │
│                    │ 待完成：2 个       │                        │
│                    └──────────────────┘                        │
│                                                                │
│                    ┌────────────────────┐                      │
│                    │                    │                      │
│                    │   🩺 开始看诊       │                      │
│                    │  (大按钮 200x80)   │                      │
│                    │                    │                      │
│                    └────────────────────┘                      │
│                                                                │
│                    ┌────────────────────┐                      │
│                    │ 🔍 快速查找患者     │                      │
│                    │ [搜索框]           │                      │
│                    └────────────────────┘                      │
│                                                                │
│              ┌─ 今日患者列表（可选）────────────┐              │
│              │ ▼ 点击展开查看今日待诊患者        │              │
│              │                                  │              │
│              │ □ 李四 - 男 - 45岁 - 09:30      │              │
│              │ □ 王五 - 女 - 38岁 - 10:00      │              │
│              │ ...                              │              │
│              └──────────────────────────────────┘              │
│                                                                │
│                                                                │
│              ⚙️ 其他功能（折叠，默认隐藏）                      │
│              ├─ 患者管理                                       │
│              ├─ 处方查询                                       │
│              ├─ 药材管理                                       │
│              └─ 系统设置                                       │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**关键尺寸**：
- 【开始看诊】按钮：200x80px，绿色背景（#4CAF50），白色文字，24px字体
- 【快速查找】输入框：300x40px，灰色边框（#BDBDBD）
- 今日统计卡片：200x60px，浅蓝背景（#E3F2FD）
- 其他功能菜单：默认折叠，点击展开

**交互逻辑**：
```csharp
// HomeViewModel.cs
public DelegateCommand StartConsultationCommand { get; }  // 开始看诊
public DelegateCommand QuickSearchCommand { get; }        // 快速查找
public string SearchKeyword { get; set; }                 // 搜索关键字

private void StartConsultation()
{
    // 导航到 MedicalCaseFlowView，从 Step 1（患者选择）开始
    _regionManager.RequestNavigate("MainRegion", "MedicalCaseFlowView", 
        new NavigationParameters { { "StartStep", 1 } });
}

private void QuickSearch()
{
    if (string.IsNullOrWhiteSpace(SearchKeyword)) return;
    
    // 打开患者搜索对话框，直接填充搜索关键字
    var parameters = new DialogParameters
    {
        { "SearchKeyword", SearchKeyword }
    };
    
    _dialogService.ShowDialog("PatientSelectionDialog", parameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            var patient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
            // 直接进入医案流程（跳过 Step 1）⭐修正
            NavigateToMedicalCaseFlow(patient);
        }
    });
}
```

---

#### 12.6.2 MedicalCaseFlowView - 医案流程视图（核心）⭐修正

**设计目标**：医案录入完整流程，从创建医案到完成医案

**核心理解**：
- ✅ MedicalCase是容器，患者选择后立即创建
- ✅ Consultation是医案内容，填写后关联到MedicalCase.ConsultationId
- ✅ Prescription是医案内容，填写后关联到MedicalCase.PrescriptionId
- ✅ 最终MedicalCase.Status = Completed

**全页显示设计原则**（⭐小屏幕优化）：
- ✅ **每个Step占据整个主内容区**：切换Step时整页内容完全替换（类似Page Navigation）
- ✅ **固定区域最小化**：只有顶部导航栏（60px）+ 进度条（80px）+ 患者信息条（50px）+ 底部操作栏（80px）固定
- ✅ **主内容区最大化**：每个Step可使用完整的主内容区域（1920x1080下约810px高度）
- ✅ **小屏幕兼容性**：
  - 1366x768：主内容区约558px（足够显示诊断表单或处方表格）
  - 1280x720：主内容区约510px（紧凑但仍可用）
  - 响应式字体：根据屏幕高度调整表单控件间距
- ✅ **【上一步】【下一步】按钮始终可见**：底部操作栏固定，无需滚动即可操作

**整体布局（1920x1080基准）**：

```
┌────────────────────────────────────────────────────────────────┐
│ 顶部导航栏（固定，高度60px）                                     │
│ [← 返回主页]  LYBT 诊疗流程              张医生  [退出登录]     │
├────────────────────────────────────────────────────────────────┤
│ 流程进度条（固定，高度80px）⭐修正为4步流程                          │
│ ┌──────┐         ┌──────┐         ┌──────┐         ┌──────┐   │
│ │选患者│   →     │填诊断│   →     │填处方│   →     │完成案│   │
│ │  ✓  │         │  ●  │         │      │         │      │   │
│ └──────┘         └──────┘         └──────┘         └──────┘   │
│ Step 1           Step 2           Step 3           Step 4       │
│ 选择患者         填写诊断         填写处方         完成医案     │
│ (创建MedicalCase) (Consultation：  (Prescription)  (Status=     │
│                   主诉+四诊+诊断)                   Completed)   │
├────────────────────────────────────────────────────────────────┤
│ 患者信息条（固定，高度50px，浅蓝背景）- Step 2-5 显示           │
│ 👤 姓名：张三 | 性别：男 | 年龄：45岁 | 电话：138xxxx           │
│                                              [更换患者]        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│                    主内容区（动态切换）                         │
│                    （高度：1080-60-80-50-80 = 810px）          │
│                                                                │
│  【根据当前Step显示不同内容】⭐修正为4步流程                     │
│                                                                │
│  Step 1: PatientSelectionView（患者选择 + 自动创建MedicalCase）│
│  Step 2: ConsultationForm（诊断：主诉+现病史+四诊+诊断+治疗原则）│
│  Step 3: PrescriptionEditor（处方：药材+剂量+用法）            │
│  Step 4: CompletionView（完成医案，Status=Completed）          │
│                                                                │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│ 底部操作栏（固定，高度80px）                                    │
│ [← 上一步] [下一步 →] [保存草稿]  最后保存：14:30  [取消]     │
└────────────────────────────────────────────────────────────────┘
```

**流程状态机**：

```csharp
// MedicalCaseFlowViewModel.cs ⭐修正为4步流程
public enum FlowStep
{
    SelectPatient = 1,         // 患者选择 → 自动创建MedicalCase
    FillConsultation = 2,      // 填写诊断（Consultation：主诉+现病史+四诊+诊断+治疗原则）
    FillPrescription = 3,      // 填写处方（Prescription：药材+剂量+用法）
    CompleteMedicalCase = 4    // 完成医案（更新MedicalCase.Status=Completed）
}

private FlowStep _currentStep = FlowStep.SelectPatient;
private ViewModelBase _currentStepViewModel;

// 流程数据（在各步骤间传递）⭐核心是MedicalCase
private PatientDto _selectedPatient;
private Guid _medicalCaseId;           // ✅ 核心：医案ID（容器）
private Guid _consultationId;          // ✅ 诊断ID（内容，关联到医案）
private Guid _prescriptionId;          // ✅ 处方ID（内容，关联到医案）

// 导航到指定步骤
private async void NavigateToStep(FlowStep step)
{
    // 保存当前步骤数据（如果有）
    if (_currentStepViewModel is ISaveable saveable)
    {
        await saveable.SaveAsync();
    }
    
    _currentStep = step;
    
    // 创建对应ViewModel ⭐修正命名和参数
    CurrentStepViewModel = step switch
    {
        FlowStep.SelectPatient => new PatientSelectionViewModel(),
        FlowStep.FillConsultation => new ConsultationFormViewModel(_medicalCaseId),       // ✅ 填写诊断，关联到医案
        FlowStep.FillPrescription => new PrescriptionEditorViewModel(_medicalCaseId),     // ✅ 填写处方，关联到医案
        FlowStep.CompleteMedicalCase => new CompletionViewModel(_medicalCaseId),          // ✅ 完成医案
        _ => _currentStepViewModel
    };

    // 更新进度条
    UpdateProgressBar();

    // 更新患者信息条可见性（Step 2开始显示，因为医案已创建）
    PatientInfoBarVisible = step >= FlowStep.FillConsultation;
    
    // 更新按钮可用性
    CanGoBack = step > FlowStep.SelectPatient;
    CanGoNext = step < FlowStep.CompleteMedicalCase;
}

// 下一步
private async void NextStep()
{
    // 验证当前步骤
    if (_currentStepViewModel is IValidatable validatable)
    {
        if (!validatable.Validate())
        {
            MessageBox.Show("请完善必填项", "验证失败");
            return;
        }
    }
    
    // 保存当前步骤
    await SaveCurrentStepAsync();
    
    // ⭐核心逻辑：Step 1 选择患者后，立即创建医案（容器先于内容）
    if (_currentStep == FlowStep.SelectPatient)
    {
        var patientVM = _currentStepViewModel as PatientSelectionViewModel;
        _selectedPatient = patientVM.SelectedPatient;
        
        // ✅ 创建医案（MedicalCase）- DDD聚合根，作为容器先创建
        // Status = Active, ConsultationId = null, PrescriptionId = null
        _medicalCaseId = await CreateMedicalCaseAsync(_selectedPatient.Id);
    }
    
    // 跳转到下一步
    NavigateToStep(_currentStep + 1);
}

// 上一步
private void PreviousStep()
{
    if (_currentStep > FlowStep.SelectPatient)
    {
        NavigateToStep(_currentStep - 1);
    }
}

// 保存草稿
private async void SaveDraft()
{
    await SaveCurrentStepAsync();
    
    // Toast提示
    ShowToast("草稿已保存");
}

// 取消
private void Cancel()
{
    var result = MessageBox.Show("是否放弃当前流程？", "确认", MessageBoxButton.YesNo);
    if (result == MessageBoxResult.Yes)
    {
        // 返回主页
        _regionManager.RequestNavigate("MainRegion", "HomeView");
    }
}
```

---

#### 12.6.3 Step 1: PatientSelectionView（患者选择）

**设计目标**：快速搜索和选择患者

**布局设计**：

```
┌────────────────────────────────────────────────────────────────┐
│                     患者选择                                    │
│                                                                │
│  搜索：[________________________________]  [🔍 搜索] [新建患者] │
│        支持姓名/拼音码/手机号                                   │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │ 患者列表（DataGrid，高度600px）                           │ │
│  ├─────┬──────┬──────┬────────────┬──────────────────┬────┤ │
│  │ 姓名 │ 性别 │ 年龄 │ 手机号     │ 最近就诊          │ 操作│ │
│  ├─────┼──────┼──────┼────────────┼──────────────────┼────┤ │
│  │ 李四│ 男   │ 45  │ 138xxxx1234│ 2025-10-15        │[选择│ │
│  │ 王五│ 女   │ 38  │ 139xxxx5678│ 2025-10-10        │[选择│ │
│  │ 赵六│ 男   │ 52  │ 137xxxx9012│ 2025-10-08        │[选择│ │
│  │ ... │      │     │            │                   │    │ │
│  └─────┴──────┴──────┴────────────┴──────────────────┴────┘ │
│                                                                │
│  提示：双击患者行或点击【选择】按钮进入看诊流程                  │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**交互逻辑**：
```csharp
// PatientSelectionViewModel.cs（内嵌在MedicalCaseFlow中）
public ObservableCollection<PatientDto> Patients { get; }
public PatientDto SelectedPatient { get; set; }
public string SearchKeyword { get; set; }

public DelegateCommand SearchCommand { get; }
public DelegateCommand NewPatientCommand { get; }
public DelegateCommand<PatientDto> SelectPatientCommand { get; }

private async void Search()
{
    var pagedData = await _patientRepository.GetPagedAsync(1, 50, SearchKeyword);
    Patients.Clear();
    foreach (var patient in pagedData.Items)
    {
        Patients.Add(patient);
    }
}

private void SelectPatient(PatientDto patient)
{
    SelectedPatient = patient;

    // 通知父ViewModel（MedicalCaseFlowViewModel）患者已选择
    // 父ViewModel会自动调用 NextStep()，创建医案并跳转到 Step 2
    RaisePatientSelected(patient);
}

private void NewPatient()
{
    // 打开快速新建患者对话框
    _dialogService.ShowDialog("QuickCreatePatientDialog", result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            var newPatient = result.Parameters.GetValue<PatientDto>("NewPatient");
            SelectPatient(newPatient);
        }
    });
}
```

---

#### 12.6.4 Step 2: ConsultationForm（诊断表单 - 基于现有实现）⭐修正

**设计目标**：填写诊断信息（包含主诉+四诊+诊断+治疗原则）

**核心理解**：
- ✅ **基于现有实现**：`MedicalCaseEntryViewModel` (Issue #1463)
- ✅ 包含所有诊断相关字段（主诉、现病史、四诊、中医诊断、治疗原则、备注）
- ✅ 保存时创建Consultation实体并关联到MedicalCase
- ✅ 无需单独的"医案基本信息"步骤

**布局设计**（基于`MedicalCaseEntryViewModel`）：

```
┌────────────────────────────────────────────────────────────────┐
│                     诊断录入（Consultation）                    │
│                                                                │
│  ┌─ 基本诊断信息（2列布局）───────────────────────────────────┐│
│  │                                                             ││
│  │  主诉（必填）：          现病史：                           ││
│  │  [_______________]      [_______________]                  ││
│  │  [_______________]      [_______________]                  ││
│  │  [_______________]      [_______________]                  ││
│  │                                                             ││
│  │  中医诊断（必填）：      治疗原则：                         ││
│  │  [_______________]      [_______________]                  ││
│  │  [_______________]      [_______________]                  ││
│  │  [_______________]      [_______________]                  ││
│  │                                                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                │
│  ┌─ 四诊合参（2列布局）───────────────────────────────────────┐│
│  │                                                             ││
│  │  望诊：                  闻诊：                             ││
│  │  [_______________]      [_______________]                  ││
│  │  [_______________]      [_______________]                  ││
│  │                                                             ││
│  │  问诊：                  切诊：                             ││
│  │  [_______________]      [_______________]                  ││
│  │  [_______________]      [_______________]                  ││
│  │                                                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                │
│  备注：[_______________________________________________________]││
│                                                                │
│  辅助操作：[📋 从历史导入] [🗑️ 清空表单]                       │
│                                                                │
│  提示：填写完成后点击【下一步】进入处方录入                      │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**字段验证**：
```csharp
// MedicalCaseEntryViewModel.cs
public string ChiefComplaint { get; set; }      // 主诉（必填）
public string PresentIllness { get; set; }      // 现病史（必填）
public string PastHistory { get; set; }         // 既往史（选填）
public string AllergyHistory { get; set; }      // 过敏史（选填）

public bool Validate()
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(ChiefComplaint))
        errors.Add("主诉为必填项");
    
    if (string.IsNullOrWhiteSpace(PresentIllness))
        errors.Add("现病史为必填项");
    
    if (errors.Any())
    {
        MessageBox.Show(string.Join("
", errors), "验证失败");
        return false;
    }
    
    return true;
}

public async Task SaveAsync()
{
    // 如果医案ID为空，先创建医案
    if (_medicalCaseId == Guid.Empty)
    {
        var createDto = new MedicalCaseCreateDto
        {
            PatientId = _patientId,
            DoctorId = _currentDoctorId,
            VisitDate = DateTime.Now,
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            PastHistory = PastHistory,
            AllergyHistory = AllergyHistory
        };
        
        var created = await _medicalCaseRepository.CreateAsync(createDto);
        _medicalCaseId = created.Id;
    }
    else
    {
        // 更新医案
        var updateDto = new MedicalCaseUpdateDto
        {
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            PastHistory = PastHistory,
            AllergyHistory = AllergyHistory
        };
        
        await _medicalCaseRepository.UpdateAsync(_medicalCaseId, updateDto);
    }
}
```

---

#### 12.6.5 Step 3: PrescriptionEditor（处方编辑器 - 基于现有实现）⭐修正序号

**设计目标**：三种录入方式，快速开方

**核心理解**：
- ✅ Prescription是医案的内容，不是独立实体
- ✅ 创建Prescription时必须关联MedicalCaseId
- ✅ 创建成功后，更新MedicalCase.PrescriptionId = newPrescriptionId
- ✅ 1:1关系：一个医案只有一个处方

**布局设计**：

```
┌────────────────────────────────────────────────────────────────┐
│                     处方录入                                    │
│                                                                │
│  [📝 手工录入] [📋 验方导入] [🕐 历史复制]  ← Tab切换           │
│  ─────────────────────────────────────────────────────────────│
│                                                                │
│  【当前Tab：手工录入】                                          │
│                                                                │
│  ┌─ 处方表格（8列布局）───────────────────────────────────────┐│
│  │ 药材1    用量1  药材2    用量2  药材3    用量3  药材4  用量4│││
│  ├─────────┼──────┼─────────┼──────┼─────────┼──────┼────────┤││
│  │ 黄芪▼   │ 15g  │ 红枣▼   │ 3个  │ 五味子▼ │ 6g   │ 细辛▼ │6g││
│  │ 当归▼   │ 10g  │ 白芍▼   │ 15g  │ 川芎▼   │ 6g   │ 熟地▼ │20g│
│  │ 党参▼   │ 12g  │ 茯苓▼   │ 10g  │ 甘草▼   │ 6g   │       │  ││
│  │ [添加行]│      │         │      │         │      │       │  ││
│  └─────────┴──────┴─────────┴──────┴─────────┴──────┴────────┘│
│                                                                │
│  药材总数：11味   总剂量：119g                                 │
│                                                                │
│  ┌─ 处方信息───────────────────────────────────────────────┐  │
│  │                                                          │  │
│  │  剂数：[7▼] 帖          用法：[水煎服，一日一剂_________]│  │
│  │                                                          │  │
│  │  单剂价格：¥ 35.50      总价格：¥ 248.50                │  │
│  │                                                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                │
│  提示：填写完成后点击【下一步】完成看诊                         │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**关键技术实现**：
```csharp
// PrescriptionEditorViewModel.cs
public ObservableCollection<PrescriptionItemRowViewModel> ItemRows { get; }  // 表格数据
public int Dosages { get; set; } = 7;                    // 剂数
public string Usage { get; set; } = "水煎服，一日一剂";  // 用法
public decimal SingleDosagePrice { get; private set; }   // 单剂价格
public decimal TotalPrice { get; private set; }          // 总价格

// 添加行
private void AddRow()
{
    ItemRows.Add(new PrescriptionItemRowViewModel(AllHerbs));
}

// Tab切换：验方导入
private void ShowFormulaImport()
{
    CurrentTab = PrescriptionEditorTab.FormulaImport;
    
    // 加载验方列表
    LoadFormulasAsync();
}

// 导入验方
private void ImportFormula(FormulaDto formula)
{
    ItemRows.Clear();
    
    foreach (var herbGroup in formula.Items.GroupBy(4))  // 每4个药材一行
    {
        var row = new PrescriptionItemRowViewModel(AllHerbs);
        row.Herb1 = herbGroup.ElementAtOrDefault(0);
        row.Herb2 = herbGroup.ElementAtOrDefault(1);
        row.Herb3 = herbGroup.ElementAtOrDefault(2);
        row.Herb4 = herbGroup.ElementAtOrDefault(3);
        ItemRows.Add(row);
    }
    
    // 切换回手工录入Tab（可继续编辑）
    CurrentTab = PrescriptionEditorTab.ManualEntry;
}

// 保存处方
public async Task SaveAsync()
{
    var items = new List<PrescriptionItemDto>();
    
    foreach (var row in ItemRows)
    {
        if (row.Herb1 != null) items.Add(row.Herb1.ToDto());
        if (row.Herb2 != null) items.Add(row.Herb2.ToDto());
        if (row.Herb3 != null) items.Add(row.Herb3.ToDto());
        if (row.Herb4 != null) items.Add(row.Herb4.ToDto());
    }
    
    var createDto = new PrescriptionCreateDto
    {
        MedicalCaseId = _medicalCaseId,
        Dosages = Dosages,
        Usage = Usage,
        Items = items
    };
    
    var created = await _prescriptionRepository.CreateAsync(createDto);
    _prescriptionId = created.Id;
    
    // ✅ 核心步骤：更新医案关联（Prescription是医案的内容）
    // MedicalCase.PrescriptionId = newPrescriptionId
    // 至此1:1:1关系完整建立
    await _medicalCaseRepository.UpdatePrescriptionIdAsync(_medicalCaseId, _prescriptionId);
}
```

---

#### 12.6.6 Step 4: CompletionView（完成医案）⭐修正序号

**设计目标**：完成医案，更新状态为Completed

**核心理解**：
- ✅ 此步骤的核心操作：更新MedicalCase.Status = Completed
- ✅ 此时医案已包含完整内容：ConsultationId和PrescriptionId都已关联
- ✅ 1:1:1关系完整建立：MedicalCase → Consultation → Prescription
- ✅ 引导医生选择：继续看诊（下一个患者） or 返回主页

**布局设计**：

```
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│                                                                │
│                         ✅ 看诊完成                             │
│                                                                │
│                  处方已保存，病案号：MC20251019001              │
│                                                                │
│                                                                │
│              ┌─────────────────────────────────┐               │
│              │                                 │               │
│              │      🩺 继续看诊                │               │
│              │   (返回患者选择，开始下一位)     │               │
│              │                                 │               │
│              └─────────────────────────────────┘               │
│                                                                │
│              ┌─────────────────────────────────┐               │
│              │                                 │               │
│              │      🏠 返回主页                │               │
│              │   (结束看诊，返回HomeView)       │               │
│              │                                 │               │
│              └─────────────────────────────────┘               │
│                                                                │
│                                                                │
│              其他操作：                                         │
│              [🖨️ 打印处方] [📄 查看病案详情]                   │
│                                                                │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**交互逻辑**：
```csharp
// CompletionViewModel.cs
public DelegateCommand ContinueConsultationCommand { get; }  // 继续看诊
public DelegateCommand ReturnHomeCommand { get; }            // 返回主页
public DelegateCommand PrintPrescriptionCommand { get; }     // 打印处方
public DelegateCommand ViewDetailCommand { get; }            // 查看详情

private void ContinueConsultation()
{
    // ✅ 重置医案流程，返回 Step 1（患者选择）
    // 下一次选择患者后将创建新的MedicalCase
    _flowViewModel.ResetToStep(FlowStep.SelectPatient);
}

private void ReturnHome()
{
    // 返回 HomeView
    _regionManager.RequestNavigate("MainRegion", "HomeView");
}
```

---

### 12.7 技术实现关键点

#### 12.7.1 流程状态持久化

**自动保存草稿**：
```csharp
// MedicalCaseFlowViewModel.cs
private DispatcherTimer _autoSaveTimer;

public MedicalCaseFlowViewModel()
{
    // 每5分钟自动保存草稿
    _autoSaveTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMinutes(5)
    };
    _autoSaveTimer.Tick += async (s, e) => await AutoSaveDraftAsync();
    _autoSaveTimer.Start();
}

private async Task AutoSaveDraftAsync()
{
    if (_medicalCaseId != Guid.Empty)
    {
        await SaveCurrentStepAsync();
        
        // 保存流程状态到本地
        var draftState = new FlowDraftState
        {
            MedicalCaseId = _medicalCaseId,
            CurrentStep = _currentStep,
            PatientId = _selectedPatient?.Id,
            LastSaved = DateTime.Now
        };
        
        await _localStorageService.SaveDraftAsync(draftState);
    }
}

// 恢复草稿
public async Task RestoreDraftAsync()
{
    var draft = await _localStorageService.GetLatestDraftAsync();
    if (draft != null)
    {
        var result = MessageBox.Show(
            $"发现未完成的草稿（{draft.LastSaved:yyyy-MM-dd HH:mm}），是否恢复？",
            "恢复草稿",
            MessageBoxButton.YesNo
        );
        
        if (result == MessageBoxResult.Yes)
        {
            _medicalCaseId = draft.MedicalCaseId;
            NavigateToStep(draft.CurrentStep);
        }
    }
}
```

#### 12.7.2 数据验证接口

```csharp
// ISaveable.cs
public interface ISaveable
{
    Task<bool> SaveAsync();
}

// IValidatable.cs
public interface IValidatable
{
    bool Validate();
}

// 所有StepViewModel实现这两个接口
public class ConsultationFormViewModel : ViewModelBase, ISaveable, IValidatable
{
    public bool Validate()
    {
        // 验证必填项
    }
    
    public async Task<bool> SaveAsync()
    {
        // 保存数据到Server
    }
}
```

---

### 12.8 与Phase 1设计的对比

| 维度 | Phase 1设计 | Phase 2设计（方案A） |
|-----|------------|---------------------|
| **主界面** | ClinicalWorkstation + 左侧菜单 | MedicalCaseFlowView + 流程步骤 ⭐修正 |
| **导航方式** | 手动点击左侧菜单 | 自动推进 + 【下一步】按钮 |
| **流程可见性** | 无进度提示 | 顶部流程进度条 |
| **患者选择** | 对话框（单独窗口） | 内嵌在流程中（Step 1） |
| **流程连贯性** | 每步手动跳转 | 自动创建医案 + 自动跳转 |
| **草稿管理** | 未实现 | 自动保存 + 恢复草稿 |
| **完成引导** | 无明确提示 | Step 4 完成提示 + 继续看诊 |
| **学习成本** | 较高（需要理解菜单结构） | 低（流程自动推进） |
| **适用场景** | 多功能模块切换 | 连续流水线接诊 |

---

**文档状态**：
- Phase 1设计（Section 1-11）：已完成Q1-Q4确认，作为参考基线
- Phase 2设计（Section 12）：
  - ✅ RQ1已确认（小型诊所 + 主要诊断功能）
  - ✅ RQ2已确认（方案A - 流程导向单页面设计）
  - ✅ 已完成详细View设计（12.6节）
  - ✅ 已完成技术实现关键点（12.7节）
  - ❓ 下一步：创建GitHub Epic和Task Issues

---
