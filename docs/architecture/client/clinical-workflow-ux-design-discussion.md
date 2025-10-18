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

### ❓ [待讨论-Q2] 现有患者选择界面处理方式？

**背景**：Issue #1457已完成患者选择功能，但UI设计质量未知。

**选项A**：审查现有代码，能用则优化，不能用则重构
**选项B**：完全推倒重来，按新设计开发
**选项C**：保持现有实现，暂不优化

**当前状态**：❓ 待讨论（需先审查代码）

---

### ❓ [待讨论-Q3] DataGrid 8列布局需要立即明确吗？

**背景**：prescription-entry-requirements-2025-10-16.md提到"8列DataGrid"，但具体列定义需要确认。

**问题**：
- 是每行4个药材，每药材2列（名称+剂量）？
- 还是每行2个药材，每药材4列（名称+剂量+单位+备注）？
- 或其他布局？

**需要行动**：
- 立即查阅prescription-entry-requirements文档
- 或用户直接说明列定义

**当前状态**：❓ 待明确

---

### ❓ [待讨论-Q4] 是否需要创建详细的UI原型图？

**选项A**：创建详细文档（Markdown + ASCII原型图）
- 优点：设计与开发对齐，减少返工
- 缺点：增加1天设计时间

**选项B**：直接开始编码，边做边调整
- 优点：快速启动
- 缺点：可能需要返工

**当前状态**：❓ 待用户确认

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

### 📋 待执行（按一问一答原则）

**立即行动**：
1. ❓ 向用户提出Q1（首页Dashboard是否需要）
2. ❓ 等待用户决策，更新文档
3. ❓ 继续提出Q2、Q3、Q4
4. ❓ 所有问题明确后，创建GitHub Issues

**后续行动**（Q1-Q4确认后）：
5. 审查现有代码（PatientSelectionView、Styles目录）
6. 明确DataGrid列定义
7. 创建Phase 0 Epic Issue
8. 开始Task 1（全局样式系统）

---

## 11. 文档变更记录

| 日期 | 版本 | 变更描述 | 修改人 |
|------|------|---------|-------|
| 2025-10-18 | v1.0 | 初始版本，完成UI/UX深度分析 | Claude |

---

**📌 重要提醒**：
- 本文档是讨论基础，不是最终决策
- 所有 ❓ [待讨论] 标记的问题需要逐一确认
- 达成共识后，更新状态为 ✅ [已确认]
- 文档作为唯一事实来源（Single Source of Truth）
