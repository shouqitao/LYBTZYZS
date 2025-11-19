# 管理界面批量删除功能需求确认

**版本**: v1.0
**创建日期**: 2025-11-19
**状态**: 需求已确认
**相关Issue**: #2150
**确认日期**: 2025-11-19

---

## 需求概述

**业务目标**: 为所有管理界面的列表提供批量删除功能，提高操作效率。

**目标用户**: 管理员、医生

**核心场景**:
1. 用户需要删除多个药材/患者/验方/用户记录
2. 用户在列表中勾选需要删除的项目
3. 用户点击批量删除按钮
4. 系统确认后执行删除操作
5. 系统显示删除结果

---

## 功能性需求

### FR-001: 显示checkbox选择列
**描述**: 列表第一列显示checkbox，用于选择要删除的项目

**User Story**:
```
作为 用户
我想要 在列表第一列看到checkbox
以便 选择需要批量删除的项目
```

**验收标准**:
- [ ] checkbox列显示在DataGrid第一列
- [ ] 每行都有checkbox可以勾选
- [ ] checkbox状态与选中状态同步

### FR-002: 支持全选功能
**描述**: 表头checkbox支持全选/取消全选

**验收标准**:
- [ ] 表头有checkbox控制全选
- [ ] 点击表头checkbox可以全选所有行
- [ ] 再次点击可以取消全选
- [ ] 部分选中时表头checkbox显示中间状态

### FR-003: 支持单个选择
**描述**: 可以单独勾选或取消某些行

**验收标准**:
- [ ] 点击行checkbox可以选中/取消该行
- [ ] 支持Ctrl+点击多选
- [ ] 支持Shift+点击范围选择
- [ ] 选中状态有视觉反馈

### FR-004: 显示批量删除按钮
**描述**: 工具栏显示批量删除按钮

**验收标准**:
- [ ] 批量删除按钮位于工具栏左侧
- [ ] 按钮文字为"批量删除"
- [ ] 按钮使用DangerButton样式（红色）
- [ ] 未选择项目时按钮禁用

### FR-005: 执行批量删除
**描述**: 点击批量删除按钮删除选中的所有项目

**验收标准**:
- [ ] 点击按钮执行批量删除
- [ ] 逐个删除选中的项目
- [ ] 删除失败的项目不影响其他项目
- [ ] 删除过程中显示忙碌指示器

### FR-006: 删除前确认
**描述**: 批量删除前显示确认对话框

**验收标准**:
- [ ] 显示确认对话框
- [ ] 对话框显示将要删除的项目数量
- [ ] 对话框提示"此操作不可恢复"
- [ ] 用户可以取消操作

### FR-007: 显示操作结果
**描述**: 批量删除后显示操作结果

**验收标准**:
- [ ] 显示成功删除的数量
- [ ] 显示失败删除的数量
- [ ] 失败时显示失败项目列表（最多5个）
- [ ] 全部成功时显示成功消息
- [ ] 部分失败时显示警告消息

---

## 非功能性需求

### NFR-001: 性能
- checkbox选择操作响应时间 < 100ms
- 批量删除100项 < 10s
- 删除过程不阻塞UI线程

### NFR-002: 安全
- 只能删除当前用户有权限删除的数据
- 批量删除前必须确认
- 删除操作有完整的日志记录

### NFR-003: 可用性
- checkbox列在第一列，易于发现
- 批量删除按钮位置明显
- 操作提示清晰友好
- 支持键盘快捷键（Ctrl+A全选）

### NFR-004: 一致性
- 所有管理界面使用统一的批量删除交互
- 确认对话框格式统一
- 结果提示格式统一

---

## 业务规则

### BR-001: 批量删除权限控制
- **规则**: 只能删除当前用户有权限删除的数据
- **理由**: 保证数据安全，防止误删
- **实现**: ViewModel层调用已有的删除方法（包含权限检查）

### BR-002: 删除确认
- **规则**: 批量删除前必须显示确认对话框
- **理由**: 批量删除影响范围大，需要用户明确确认
- **实现**: OnExecuteBatchDeleteAsync方法中调用ShowConfirmationAsync

### BR-003: 结果反馈
- **规则**: 删除后必须显示操作结果（成功数/失败数）
- **理由**: 用户需要知道操作是否成功
- **实现**: 统计成功和失败数量，调用ShowSuccessMessageAsync或ShowWarningMessageAsync

### BR-004: 失败处理
- **规则**: 部分删除失败时，不影响其他项的删除
- **理由**: 提高容错性，避免一个失败导致全部失败
- **实现**: 使用foreach逐个删除，捕获单个异常

### BR-005: 空选择处理
- **规则**: 未选择任何项时，批量删除按钮禁用
- **理由**: 避免无效操作
- **实现**: BatchDeleteCommand的CanExecute检查SelectedItems.Count > 0

---

## UI设计草案

### 列表布局
```
┌─────────────────────────────────────────────────────┐
│ 工具栏                                               │
│ [批量删除] [导入] [导出] [+ 新增] [刷新] [返回主页]  │
├─────────────────────────────────────────────────────┤
│ DataGrid                                            │
│ ☑ 全选 │ 名称    │ 类型    │ 状态    │ 操作        │
│ ☐     │ 药材A   │ 中药材  │ 启用    │ [查看][编辑]│
│ ☑     │ 药材B   │ 中药材  │ 启用    │ [查看][编辑]│
│ ☑     │ 药材C   │ 中药材  │ 禁用    │ [查看][编辑]│
├─────────────────────────────────────────────────────┤
│ 分页栏                                               │
│ [首页] [上一页] 第1页/共10页 [下一页] [末页]        │
└─────────────────────────────────────────────────────┘
```

### 确认对话框
```
┌─────────────────────────────────┐
│ 批量删除确认                     │
├─────────────────────────────────┤
│ 确认删除选中的 3 个项目吗？      │
│ 此操作不可恢复。                 │
│                                 │
│         [取消]    [确认]         │
└─────────────────────────────────┘
```

### 结果提示
```
成功情况：
批量删除完成！
成功：3个
失败：0个

部分失败情况：
批量删除完成！
成功：2个
失败：1个

失败的项目：
药材A（权限不足）
```

---

## 技术实现约束

### 架构层分配
- **Client端**:
  - UnifiedManagementTable（添加checkbox列）
  - BaseMasterDataListView（传递SelectedItems）
  - 各模块ViewModel（实现OnExecuteBatchDeleteAsync）
  - 各模块View（启用ShowCheckBoxColumn）

### 技术栈
- WPF DataGrid原生控件
- Prism DelegateCommand
- ObservableCollection双向绑定
- 不引入第三方UI库（符合MVP约束）

### 涉及文件
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Views/HerbManagementView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbManagementViewModel.cs`
- 其他模块类似

---

## 开放问题

### Q1: checkbox列的实现方式
**问题**: 使用哪种方式实现checkbox列？

**选项**:
- A. DataGridCheckBoxColumn绑定IsSelected（简单但需要修改数据模型）
- B. DataGridCheckBoxColumn + SelectedItems集合同步（推荐，符合MVVM）

**用户选择**: ✓ 选项B - DataGridCheckBoxColumn + SelectedItems集合同步

### Q2: 全选功能的实现
**问题**: 如何实现全选功能？

**选项**:
- A. 使用DataGrid.SelectAll方法（简单）
- B. 自定义HeaderCheckBox + 手动全选逻辑（灵活）

**用户选择**: ✓ 选项A - 使用DataGrid.SelectAll方法

### Q3: 批量删除的事务处理
**问题**: 批量删除是否需要事务处理？

**选项**:
- A. 单个删除，部分失败不影响其他（友好）
- B. 事务处理，全部成功或全部失败（严格）

**用户选择**: ✓ 选项A - 单个删除，部分失败不影响其他

### Q4: 是否需要批量删除撤销功能
**问题**: 用户误删后是否需要撤销？

**选项**:
- A. 不支持撤销（简单，依赖确认对话框）
- B. 支持撤销（复杂，需要回收站机制）

**用户选择**: ✓ 选项A - 不支持撤销，依赖确认对话框

---

## 涉及模块

- [ ] 药材管理（HerbManagementView）
- [ ] 患者管理（PatientManagementView）
- [ ] 验方管理（FormulaManagementView）
- [ ] 用户管理（UserManagementView）
- [ ] 病案管理（MedicalCaseManagementView）- 可选

---

## 参考资料

- [UnifiedListViewModelBase源码](../../src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedListViewModelBase.cs)
- [BaseMasterDataListView源码](../../src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml)
- [WPF DataGrid官方文档](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid)

---

**下一步**:
1. ✓ 用户确认需求（已完成）
2. 调用 `lybtzyzs-design-generator` 生成设计文档
3. 实现功能
