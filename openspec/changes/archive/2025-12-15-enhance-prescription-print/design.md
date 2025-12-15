# 处方打印系统重构设计方案

## 问题分析

### 当前实现的问题

1. **FlowDocument的局限性**：
   - FlowDocument设计用于"回流"(reflow)布局，内容会根据容器大小自动调整
   - 无法实现真正的固定位置布局（页眉固定、页脚固定）
   - 难以精确控制元素在页面上的绝对位置

2. **代码生成布局的缺点**：
   - 所有布局逻辑都在C#代码中，难以维护和调整
   - 无法直接预览设计效果
   - 修改布局需要重新编译

3. **打印预览体验不佳**：
   - 缩放控制问题
   - 布局与实际打印不完全一致

## 最佳实践方案

基于网络调研，推荐采用 **XAML模板 + FixedDocument** 方案：

### 核心原则

1. **FixedDocument替代FlowDocument**：
   - FixedDocument是WYSIWYG（所见即所得）
   - 元素位置精确固定，不会回流
   - 打印输出与预览完全一致

2. **XAML模板定义布局**：
   - 使用UserControl定义处方模板
   - 通过XAML声明式定义布局，可视化编辑
   - 使用数据绑定填充动态内容

3. **数据驱动**：
   - PrescriptionPrintDto作为ViewModel
   - 模板通过绑定自动填充数据

## 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                    PrescriptionPrintService                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ MapToPrintDto   │→ │ CreateTemplate  │→ │ ToFixed      │ │
│  │ (数据准备)      │  │ (XAML模板)      │  │ Document     │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
│                              ↓                    ↓         │
│                       ┌─────────────────┐  ┌──────────────┐ │
│                       │ UserControl     │  │ FixedPage    │ │
│                       │ DataBinding     │  │ FixedDoc     │ │
│                       └─────────────────┘  └──────────────┘ │
│                                                   ↓         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │               PreviewWindow                              ││
│  │  ┌─────────────┐     ┌───────────────────────────────┐  ││
│  │  │ 设置面板    │     │ DocumentViewer                │  ││
│  │  │ - 打印机    │     │ (FixedDocument预览)           │  ││
│  │  │ - 份数      │     │                               │  ││
│  │  │ - 纸张A5    │     │  ┌─────────────────────────┐  │  ││
│  │  │             │     │  │ 处方模板内容            │  │  ││
│  │  │ [打印]      │     │  │                         │  │  ││
│  │  │ [取消]      │     │  └─────────────────────────┘  │  ││
│  │  └─────────────┘     └───────────────────────────────┘  ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

## 详细设计

### 1. XAML处方模板 (PrescriptionPrintTemplate.xaml)

```xml
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionPrintTemplate"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Width="559" Height="794">  <!-- A5尺寸 148mm x 210mm @ 96DPI -->

    <Grid Margin="57,38,57,38">  <!-- 页边距：左右15mm, 上下10mm -->
        <Grid.RowDefinitions>
            <!-- 固定头部区域 -->
            <RowDefinition Height="Auto"/>  <!-- 标题 -->
            <RowDefinition Height="Auto"/>  <!-- 患者信息行1 -->
            <RowDefinition Height="Auto"/>  <!-- 患者信息行2 -->
            <RowDefinition Height="Auto"/>  <!-- 住址 -->
            <RowDefinition Height="Auto"/>  <!-- 诊断 -->
            <RowDefinition Height="Auto"/>  <!-- 诊见 -->

            <!-- 弹性内容区域 -->
            <RowDefinition Height="*"/>     <!-- Rp. + 药材列表 + 用法 -->

            <!-- 固定底部区域 -->
            <RowDefinition Height="Auto"/>  <!-- 分隔线 -->
            <RowDefinition Height="Auto"/>  <!-- 签名行 -->
            <RowDefinition Height="Auto"/>  <!-- 费用行 -->
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <TextBlock Grid.Row="0"
                   Text="{Binding HeaderText}"
                   FontSize="16" FontWeight="Bold"
                   HorizontalAlignment="Center"
                   Margin="0,0,0,12"/>

        <!-- ... 其他内容通过绑定 ... -->
    </Grid>
</UserControl>
```

### 2. UserControl转FixedPage的方法

```csharp
/// <summary>
/// 将UserControl转换为FixedPage
/// </summary>
private FixedPage CreateFixedPage(PrescriptionPrintDto dto)
{
    // 1. 创建模板实例并设置DataContext
    var template = new PrescriptionPrintTemplate();
    template.DataContext = dto;

    // 2. 强制测量和排列
    var pageSize = new Size(559, 794); // A5
    template.Measure(pageSize);
    template.Arrange(new Rect(pageSize));
    template.UpdateLayout();

    // 3. 创建FixedPage
    var fixedPage = new FixedPage
    {
        Width = pageSize.Width,
        Height = pageSize.Height
    };

    // 4. 添加模板到FixedPage
    fixedPage.Children.Add(template);
    FixedPage.SetLeft(template, 0);
    FixedPage.SetTop(template, 0);

    return fixedPage;
}
```

### 3. FixedDocument构建

```csharp
/// <summary>
/// 创建FixedDocument用于预览和打印
/// </summary>
public FixedDocument CreateFixedDocument(PrescriptionPrintDto dto)
{
    var document = new FixedDocument();

    // 设置页面尺寸
    document.DocumentPaginator.PageSize = new Size(559, 794);

    // 创建页面内容
    var pageContent = new PageContent();
    var fixedPage = CreateFixedPage(dto);

    // 使用IAddChild接口添加页面（关键技巧）
    ((IAddChild)pageContent).AddChild(fixedPage);
    document.Pages.Add(pageContent);

    return document;
}
```

### 4. 打印预览窗口

使用DocumentViewer显示FixedDocument，提供内置的：
- 缩放控制
- 页面导航
- 搜索功能

```csharp
private void ShowPreview(FixedDocument document)
{
    var previewWindow = new Window
    {
        Title = "处方预览",
        Width = 900,
        Height = 700
    };

    var mainGrid = new Grid();
    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

    // 左侧设置面板
    var settingsPanel = CreateSettingsPanel();
    Grid.SetColumn(settingsPanel, 0);

    // 右侧DocumentViewer（FixedDocument专用）
    var docViewer = new DocumentViewer
    {
        Document = document
    };
    Grid.SetColumn(docViewer, 1);

    mainGrid.Children.Add(settingsPanel);
    mainGrid.Children.Add(docViewer);

    previewWindow.Content = mainGrid;
    previewWindow.ShowDialog();
}
```

### 5. 打印执行

```csharp
private void ExecutePrint(FixedDocument document, string printerName, int copies)
{
    var printServer = new LocalPrintServer();
    var printQueue = string.IsNullOrEmpty(printerName)
        ? LocalPrintServer.GetDefaultPrintQueue()
        : printServer.GetPrintQueues().FirstOrDefault(q => q.Name == printerName);

    if (printQueue == null) return;

    for (int i = 0; i < copies; i++)
    {
        var writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
        writer.Write(document.DocumentPaginator);
    }
}
```

## 文件结构

```
src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/
├── Views/
│   ├── PrescriptionPrintTemplate.xaml      # NEW: 处方打印XAML模板
│   └── PrescriptionPrintTemplate.xaml.cs   # NEW: 模板代码后置
├── Services/
│   ├── PrescriptionPrintService.cs         # REFACTOR: 使用FixedDocument
│   └── PrescriptionFlowDocumentBuilder.cs  # DEPRECATED: 保留兼容性
├── Models/
│   └── PrescriptionPrintDto.cs             # 保持不变
```

## 实施步骤

### Phase 1: 创建XAML模板
1. 创建 `PrescriptionPrintTemplate.xaml`
2. 定义Grid布局（头部固定、中部弹性、底部固定）
3. 添加数据绑定到PrescriptionPrintDto
4. 使用设计器验证布局

### Phase 2: 实现转换逻辑
1. UserControl → FixedPage 转换方法
2. FixedPage → FixedDocument 组装
3. 处理A5纸张尺寸设置

### Phase 3: 重构预览窗口
1. 使用DocumentViewer替代FlowDocumentScrollViewer
2. 保持左侧设置面板
3. DocumentViewer自带缩放和导航控件

### Phase 4: 完善打印功能
1. 打印机选择保持不变
2. 使用XpsDocumentWriter打印FixedDocument
3. PrintTicket配置A5纸张

## 优势

1. **WYSIWYG**：预览与打印完全一致
2. **可维护性**：XAML模板可视化编辑
3. **精确控制**：固定布局，元素位置不变
4. **标准控件**：DocumentViewer提供完整功能
5. **数据驱动**：通过绑定自动更新内容

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| UserControl内容超出页面 | 中 | 设置MaxHeight，验证药材数量 |
| 字体渲染差异 | 低 | 统一使用华文楷体，设置FallbackFont |
| 旧代码兼容性 | 低 | 保留FlowDocumentBuilder，渐进迁移 |

## 参考资料

- [WPF Printing Part 2 – The Fixed Document](https://www.nbdtech.com/Blog/archive/2009/04/20/wpf-printing-part-2-the-fixed-document.aspx)
- [Custom Data Grid Document Paginator](https://www.codeproject.com/Articles/138233/Custom-Data-Grid-Document-Paginator)
- [WPF Print Engine](https://www.codeproject.com/Articles/238135/WPF-Print-Engine-Part-I)
- [Microsoft Docs: FixedDocument](https://learn.microsoft.com/en-us/dotnet/api/system.windows.documents.fixeddocument)
- [Microsoft Docs: Documents in WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/documents-in-wpf)
