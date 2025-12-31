# Implementation Tasks: 药材编辑控件重构

## 设计原则

- **两层控件架构**: HerbItemControl(单药材) + HerbListControl(药材列表)
- **控件集中定义**: 所有控件代码在Infrastructure/Controls
- **事件通知模式**: 控件通过事件通知外部变更（项目统一模式）
- **高内聚输出**: 控件输出完整药材列表，调用方直接对象赋值

---

## Phase 1: 完善HerbItemControl (1天)

### 1.1 重命名与文件结构
- [ ] 1.1.1 创建 `Infrastructure/Controls/HerbItem/` 目录
- [ ] 1.1.2 将 `HerbCardControl.xaml` 重命名为 `HerbItemControl.xaml`
- [ ] 1.1.3 将 `HerbCardControl.xaml.cs` 重命名为 `HerbItemControl.xaml.cs`
- [ ] 1.1.4 更新类名和XAML引用

### 1.2 创建内部ViewModel
- [ ] 1.2.1 创建 `HerbItemControlViewModel.cs`
- [ ] 1.2.2 实现属性: HerbId, HerbName, Dosage, Unit, UnitPrice, DecocteMethod
- [ ] 1.2.3 实现 FilteredHerbs 过滤后的药材建议列表
- [ ] 1.2.4 实现 IsDosageValid 剂量校验属性
- [ ] 1.2.5 实现 IsEmpty 空行判断属性

### 1.3 完善拼音码快速检索
- [ ] 1.3.1 实现 FilterHerbs() 方法 - 拼音码匹配过滤
- [ ] 1.3.2 输入时实时过滤显示建议列表
- [ ] 1.3.3 支持拼音首字母和全拼匹配

### 1.4 完善自动匹配药材功能
- [ ] 1.4.1 实现 TryAutoMatchHerb() 方法
- [ ] 1.4.2 输入完全匹配药材名时自动选中
- [ ] 1.4.3 优化匹配逻辑（待完善）

### 1.5 自动复制药材属性
- [ ] 1.5.1 实现 OnHerbSelected() 方法
- [ ] 1.5.2 选择药材后自动复制单位(Unit)
- [ ] 1.5.3 选择药材后自动复制单价(UnitPrice) - 不显示

### 1.6 剂量输入与校验
- [ ] 1.6.1 完善剂量输入框
- [ ] 1.6.2 实现剂量范围校验(1-500g)
- [ ] 1.6.3 显示剂量校验消息

### 1.7 煎法输入
- [ ] 1.7.1 完善煎法选择下拉框
- [ ] 1.7.2 绑定 DecocteMethod 枚举

### 1.8 事件与键盘操作
- [ ] 1.8.1 创建 `HerbItemChangedEventArgs.cs` 事件参数
- [ ] 1.8.2 添加 ItemChanged 事件 - 药材选择、剂量、煎法变更时触发
- [ ] 1.8.3 添加 DeleteRequested 事件
- [ ] 1.8.4 添加 NextItemRequested 事件 - Enter键触发
- [ ] 1.8.5 实现 Enter 键行为: 选中建议项/跳转到剂量/生成新槽位
- [ ] 1.8.6 实现 Tab 键在字段间跳转

### 1.9 公共方法
- [ ] 1.9.1 实现 LoadFromDto(HerbItemDto dto)
- [ ] 1.9.2 实现 ToDto() -> HerbItemDto
- [ ] 1.9.3 实现 Clear() 清空数据
- [ ] 1.9.4 实现 FocusHerbName() 设置焦点

### 1.10 验证
- [ ] 1.10.1 单独测试HerbItemControl功能
- [ ] 1.10.2 验证拼音码检索正常
- [ ] 1.10.3 验证自动匹配功能
- [ ] 1.10.4 验证键盘操作

---

## Phase 2: 创建HerbListControl (1.5天)

### 2.1 文件结构
- [ ] 2.1.1 创建 `Infrastructure/Controls/HerbList/` 目录
- [ ] 2.1.2 创建 `HerbListControl.xaml` UserControl
- [ ] 2.1.3 创建 `HerbListControl.xaml.cs` Code-Behind

### 2.2 创建内部ViewModel
- [ ] 2.2.1 创建 `HerbListControlViewModel.cs`
- [ ] 2.2.2 实现 Items 集合 (ObservableCollection<HerbItemControlViewModel>)
- [ ] 2.2.3 实现 ValidItemCount 计算属性
- [ ] 2.2.4 实现 HasDuplicates 计算属性

### 2.3 布局实现
- [ ] 2.3.1 使用 ItemsControl 渲染 HerbItemControl 列表
- [ ] 2.3.2 实现 Columns 属性 - 配置每行药材个数
- [ ] 2.3.3 使用 UniformGrid 或 WrapPanel 实现多列布局

### 2.4 空槽位管理
- [ ] 2.4.1 实现 EnsureSingleEmptySlot() - 始终只保留1个空槽
- [ ] 2.4.2 剂量输入后按回车 → 生成新空槽
- [ ] 2.4.3 光标自动跳转到新空槽的药材输入框

### 2.5 紧凑列表
- [ ] 2.5.1 实现 Compact() 方法
- [ ] 2.5.2 删除药材后，后面的自动往前靠

### 2.6 重复药材检测
- [ ] 2.6.1 实现 CheckDuplicate(Guid herbId) 方法
- [ ] 2.6.2 创建 `Models/DuplicateDosageStrategy.cs` 枚举
- [ ] 2.6.3 添加 DuplicateStrategy 输入属性

### 2.7 单个添加重复处理
- [ ] 2.7.1 实现 HandleSingleAddDuplicate() 方法
- [ ] 2.7.2 检测到重复时内嵌提示"当前药材已经存在无法添加"
- [ ] 2.7.3 禁止重复添加

### 2.8 批量导入重复处理
- [ ] 2.8.1 实现 HandleBatchImportDuplicates() 方法
- [ ] 2.8.2 逐个弹窗提示"xx药材已经存在"
- [ ] 2.8.3 医生确认一个再弹窗下一个
- [ ] 2.8.4 实现 CalculateMergedDosage() - 根据策略计算剂量
  - Max: 取较大值(默认)
  - Min: 取较小值
  - Sum: 相加
  - Average: 平均值
  - First: 保留第一个

### 2.9 拖拽排序
- [ ] 2.9.1 实现 MoveItem(int oldIndex, int newIndex) 方法
- [ ] 2.9.2 添加拖拽排序UI交互
- [ ] 2.9.3 可考虑使用GongSolutions.Wpf.DragDrop库

### 2.10 删除功能
- [ ] 2.10.1 实现 DeleteItemCommand - 每个药材项右侧删除按钮
- [ ] 2.10.2 实现 ClearAllCommand - 清空全部操作
- [ ] 2.10.3 删除后自动调用Compact()

### 2.11 批量导入方法
- [x] 2.11.1 实现 AddHerbs(IEnumerable<HerbItemDto> items) 公共方法
- [x] 2.11.2 调用重复检测处理
- [x] 2.11.3 添加成功后触发事件
- [x] 2.11.4 实现导入时药材信息同步 (Decision 8)
  - 从AllHerbs获取最新的HerbName/Unit/UnitPrice
  - 保留原始Dosage和DecocteMethod
  - 解决经验方无价格、历史处方价格过时问题

### 2.12 事件
- [ ] 2.12.1 创建 `HerbListChangedEventArgs.cs` 事件参数
- [ ] 2.12.2 定义 HerbListChangeType 枚举 (ItemAdded/Removed/Modified/Cleared/Loaded/Moved)
- [ ] 2.12.3 添加 HerbListChanged 事件
- [ ] 2.12.4 在列表发生任何变更时触发事件

### 2.13 输出属性
- [ ] 2.13.1 实现 HerbList 只读属性 - IReadOnlyList<HerbItemDto>
- [ ] 2.13.2 实现 CollectHerbList() 方法
- [ ] 2.13.3 实现 ItemCount 属性
- [ ] 2.13.4 实现 IsValid 属性

### 2.14 公共方法
- [ ] 2.14.1 实现 LoadFromDto(IEnumerable<HerbItemDto> items)
- [ ] 2.14.2 实现 Clear() 清空所有药材
- [ ] 2.14.3 实现 Validate() 执行校验

### 2.15 验证
- [ ] 2.15.1 单独测试HerbListControl功能
- [ ] 2.15.2 验证空槽位管理
- [ ] 2.15.3 验证紧凑列表
- [ ] 2.15.4 验证重复检测(单个+批量)
- [ ] 2.15.5 验证拖拽排序
- [ ] 2.15.6 验证事件触发

---

## Phase 3: 集成到处方模块 (1天)

### 3.1 创建数据模型
- [ ] 3.1.1 创建 `Infrastructure/Models/HerbItemDto.cs`
- [ ] 3.1.2 定义属性: HerbId, HerbName, Dosage, Unit, UnitPrice, DecocteMethod
- [ ] 3.1.3 添加 IsValid 计算属性

### 3.2 替换控件引用
- [ ] 3.2.1 修改 `PrescriptionEditorPanel.xaml`
- [ ] 3.2.2 将 HerbListEditor 替换为 HerbListControl
- [ ] 3.2.3 绑定 AllHerbs, Columns, DuplicateStrategy 属性
- [ ] 3.2.4 绑定 HerbListChanged 事件

### 3.3 简化PrescriptionPanelViewModel
- [ ] 3.3.1 移除 PrescriptionItemHandler 依赖
- [ ] 3.3.2 移除药材CRUD相关代码
- [ ] 3.3.3 保留处方级别逻辑(剂数、用法等)
- [ ] 3.3.4 处理 HerbListChanged 事件更新脏状态

### 3.4 更新保存逻辑
- [ ] 3.4.1 保存前调用 _herbListControl.Validate()
- [ ] 3.4.2 直接使用 _herbListControl.HerbList 获取药材列表
- [ ] 3.4.3 构造 PrescriptionInputDto { Items = HerbList }

### 3.5 导入功能
- [ ] 3.5.1 在页面提供导入按钮(方剂/历史处方)
- [ ] 3.5.2 按钮点击显示选择对话框
- [ ] 3.5.3 选择完成后调用 _herbListControl.AddHerbs(herbs)

### 3.6 验证
- [ ] 3.6.1 端到端测试处方开具流程
- [ ] 3.6.2 测试导入功能
- [ ] 3.6.3 测试保存功能
- [ ] 3.6.4 测试脏状态追踪

---

## Phase 4: 扩展复用与清理 (1天)

### 4.1 方剂模块复用
- [ ] 4.1.1 在方剂编辑页面使用 HerbListControl
- [ ] 4.1.2 配置适合方剂的属性(Columns等)
- [ ] 4.1.3 测试方剂编辑流程

### 4.2 删除冗余Handler文件
> **BLOCKED**: PrescriptionPanelViewModel仍使用旧的HerbItems集合和Handler功能，
> 需要先完成ViewModel到HerbListControl的完全迁移才能删除Handler。
> 建议在单独的提案中处理此迁移。

- [ ] 4.2.1 删除 `PrescriptionItemHandler.cs` (BLOCKED)
- [ ] 4.2.2 删除 `PrescriptionImportHandler.cs` (BLOCKED)
- [ ] 4.2.3 更新模块注册(如有) (BLOCKED)

### 4.3 删除旧控件
> **BLOCKED**: HerbListEditor仍被Formula模块和MedicalCaseEditControl使用。
> 已添加[Obsolete]标记，待相关模块迁移后删除。

- [ ] 4.3.1 删除 `HerbListEditor.xaml` 和 `.cs` (BLOCKED)
- [ ] 4.3.2 全局搜索确认无引用 (BLOCKED)
- [ ] 4.3.3 更新资源字典(如有) (BLOCKED)

### 4.4 单元测试
- [ ] 4.4.1 编写 HerbItemControlViewModel 单元测试
- [ ] 4.4.2 编写 HerbListControlViewModel 单元测试
- [ ] 4.4.3 测试重复检测逻辑
- [ ] 4.4.4 测试剂量取值策略

### 4.5 文档与归档
- [ ] 4.5.1 更新控件使用说明
- [ ] 4.5.2 记录事件模式规范
- [ ] 4.5.3 运行 `openspec validate` 确认通过
- [ ] 4.5.4 执行 `openspec archive` 归档变更

---

## Completion Criteria

### 已完成
- [x] HerbCardControl已重命名为HerbItemControl
- [x] HerbListControl已创建并投入使用
- [x] PrescriptionEditorPanel已迁移到HerbListControl
- [x] HerbListEditor添加[Obsolete]标记和迁移指引
- [x] FormulaHerbItem补充DecocteMethod字段
- [x] 编译0错误0警告

### 待后续提案完成
- [ ] HerbListEditor已删除 (BLOCKED - Formula/MedicalCaseEditControl仍在使用)
- [ ] PrescriptionItemHandler已删除 (BLOCKED - ViewModel迁移未完成)
- [ ] PrescriptionImportHandler已删除 (BLOCKED - ViewModel迁移未完成)
- [ ] 方剂模块复用成功 (延期到单独提案)
- [ ] 单元测试覆盖核心逻辑 (低优先级)
- [ ] 功能行为与重构前一致 (需集成测试验证)

---

## 变更文件清单

| 文件 | 操作 | 说明 |
|-----|------|------|
| `Infrastructure/Controls/HerbItem/HerbItemControl.xaml` | RENAME | 从HerbCardControl重命名 |
| `Infrastructure/Controls/HerbItem/HerbItemControl.xaml.cs` | MODIFY | 增强功能 |
| `Infrastructure/Controls/HerbItem/HerbItemControlViewModel.cs` | CREATE | 内部ViewModel |
| `Infrastructure/Controls/HerbItem/HerbItemChangedEventArgs.cs` | CREATE | 事件参数 |
| `Infrastructure/Controls/HerbList/HerbListControl.xaml` | CREATE | 药材列表控件 |
| `Infrastructure/Controls/HerbList/HerbListControl.xaml.cs` | CREATE | 控件代码后台 |
| `Infrastructure/Controls/HerbList/HerbListControlViewModel.cs` | CREATE | 内部ViewModel |
| `Infrastructure/Controls/HerbList/HerbListChangedEventArgs.cs` | CREATE | 事件参数 |
| `Infrastructure/Models/HerbItemDto.cs` | CREATE | 药材项输出DTO |
| `Infrastructure/Models/DuplicateDosageStrategy.cs` | CREATE | 剂量取值策略枚举 |
| `MedicalCase/ViewModels/PrescriptionPanelViewModel.cs` | MODIFY | 大幅简化 |
| `MedicalCase/Controls/PrescriptionEditorPanel.xaml` | MODIFY | 替换为HerbListControl |
| `MedicalCase/ViewModels/Components/PrescriptionItemHandler.cs` | DELETE | 迁移到控件 |
| `MedicalCase/ViewModels/Components/PrescriptionImportHandler.cs` | DELETE | 外部处理 |
| `Infrastructure/Controls/HerbListEditor.xaml` | DELETE | 被HerbListControl替代 |

---

## 总工时估算

| Phase | 工时 |
|-------|------|
| Phase 1: 完善HerbItemControl | 1天 |
| Phase 2: 创建HerbListControl | 1.5天 |
| Phase 3: 集成到处方模块 | 1天 |
| Phase 4: 扩展复用与清理 | 1天 |
| **总计** | **4.5天** |
