# Tasks: enhance-prescription-print

## Phase 1: XAML模板创建 (已完成)

### Task 1.1: 创建处方打印XAML模板
- [x] 创建 `Views/PrescriptionPrintTemplate.xaml` - A5纸张模板
- [x] Grid布局：固定头部 + 弹性内容区 + 固定底部
- [x] 数据绑定到 `PrescriptionPrintDto`
- [x] 楷体字体样式定义
- [x] 代码后置 `PrescriptionPrintTemplate.xaml.cs`

### Task 1.2: 布局区域
- [x] 标题行：诊所名称 + 普通处方笺
- [x] 患者信息行1：姓名/性别/年龄/时间
- [x] 患者信息行2：门诊号/科别/电话
- [x] 住址行
- [x] 诊断行
- [x] 诊见行（四诊合并）
- [x] Rp. + 药材列表（WrapPanel）
- [x] 用法行
- [x] 分隔线
- [x] 签名行：医师/审核/调配
- [x] 费用行：诊疗费/药费/治疗费/合计

## Phase 2: FixedDocument转换 (已完成)

### Task 2.1: UserControl转FixedPage
- [x] 创建 `CreateFixedPage()` 方法
- [x] 设置DataContext绑定
- [x] Measure/Arrange/UpdateLayout

### Task 2.2: FixedDocument构建
- [x] 创建 `BuildFixedDocument()` 方法
- [x] 使用IAddChild接口添加页面
- [x] A5PageSize常量定义 (559x794px)

## Phase 3: 打印预览窗口重构 (已完成)

### Task 3.1: 预览区域重构
- [x] 使用DocumentViewer替代FlowDocumentScrollViewer
- [x] DocumentViewer内置缩放/导航/搜索功能
- [x] WYSIWYG预览效果

### Task 3.2: 设置面板
- [x] 左右分栏Grid布局
- [x] 打印机选择ComboBox
- [x] 份数调整 (+/- 按钮)
- [x] 纸张尺寸显示（A5只读）
- [x] 打印/取消按钮

## Phase 4: 打印方法更新 (已完成)

### Task 4.1: 打印执行
- [x] `ExecutePrint()` 使用FixedDocument
- [x] `ExecutePrintWithSettings()` 使用FixedDocument
- [x] `PreparePrintDocumentAsync()` 返回FixedDocument

### Task 4.2: 导出功能
- [x] `ExportToPdfAsync()` 使用FixedDocument

## Phase 5: 集成测试 (已完成)

### Task 5.1: 功能测试
- [x] 打印机选择功能
- [x] 份数设置功能
- [x] 纸张尺寸选择(A5/A4)
- [x] 缩放控制功能
- [x] 实际打印输出

### Task 5.2: UI/UX验证
- [x] 窗口布局美观
- [x] 操作流程顺畅
- [x] 打印输出与预览一致（WYSIWYG）
- [x] 所有字段下划线两端对齐
- [x] 签名行留空供手写

## 技术参考

- **设计文档**: `openspec/changes/active/enhance-prescription-print/design.md`
- **FixedDocument文档**: https://learn.microsoft.com/en-us/dotnet/api/system.windows.documents.fixeddocument
- **WPF Documents**: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/documents-in-wpf
- **NBD Tech Blog**: https://www.nbdtech.com/Blog/archive/2009/04/20/wpf-printing-part-2-the-fixed-document.aspx

## 文件结构

```
src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/
├── Views/
│   ├── PrescriptionPrintTemplate.xaml      # 处方打印XAML模板
│   └── PrescriptionPrintTemplate.xaml.cs   # 模板代码后置
├── Services/
│   ├── PrescriptionPrintService.cs         # 使用FixedDocument
│   └── PrescriptionFlowDocumentBuilder.cs  # 保留兼容性
├── Models/
│   └── PrescriptionPrintDto.cs             # 打印数据模型
```
