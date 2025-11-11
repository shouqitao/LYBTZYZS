# 验方编辑区增强功能 - 任务分解文档

## 📋 元数据

- **Epic**: 待创建
- **设计文档**: `docs/explanation/formula-editing-area-design.md` v1.0
- **需求文档**: `docs/requirements/formula-editing-area-requirements.md` v2.0
- **总工作量**: 22小时
- **实施阶段**: Phase 1-3
- **预计完成时间**: 3.5个工作日（按每日7小时计算）
- **创建日期**: 2025-11-11
- **最后修订**: 2025-11-11（删除Phase 3导入验方功能，调整为复制验方）

---

## 🎯 任务清单（Task Checklist）

### Phase 1: 基础编辑功能（8小时）

**Phase目标**: 实现8列DataGrid基础布局和3个核心命令（添加行、删除行、清空）

#### Task 1.1: 创建FormulaItemRow数据模型
- **工作量**: 1小时
- **依赖**: 无
- **类型**: Model
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Models/FormulaItemRow.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 包含8个属性：Herb1-4, Quantity1-4
  - [ ] 实现BindableBase继承
  - [ ] 实现ToHerbItems()转换方法
  - [ ] 每个属性都有PropertyChanged通知
- **技术要点**:
  - 继承Prism的BindableBase
  - 使用SetProperty()方法触发PropertyChanged
  - ToHerbItems()方法过滤null的Herb项

#### Task 1.2: 创建FormulaHerbFilterManager组件
- **工作量**: 2小时
- **依赖**: 无
- **类型**: Component
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaHerbFilterManager.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 实现InitializeAsync()加载所有药材
  - [ ] 实现FilterHerbs()支持名称+拼音码双重匹配
  - [ ] maxResults参数默认值为5
  - [ ] 实现GetNextFocusColumn()焦点跳转逻辑
  - [ ] 依赖注入：IHerbRepository, ILogger
- **技术要点**:
  - 名称匹配：Name.Contains(searchText)
  - 拼音码匹配：PinYinCode.StartsWith(searchText)
  - 使用ObservableCollection<HerbDto>作为FilteredHerbs
  - 参考实现：LYBT.Desktop.Prescriptions/ViewModels/Components/HerbFilterManager.cs

#### Task 1.3: 增强FormulaDataManager组件
- **工作量**: 2小时
- **依赖**: Task 1.1, Task 1.2
- **类型**: Component
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaDataManager.cs`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 新增ConvertRowsToHerbItems()方法
  - [ ] 新增ConvertHerbItemsToRowsAsync()方法
  - [ ] 转换逻辑正确：4个药材一组转换为FormulaItemRow
  - [ ] 自动设置SortOrder
  - [ ] 异步方法使用async/await
- **技术要点**:
  - ConvertRowsToHerbItems: 遍历rows，调用row.ToHerbItems()，重新设置SortOrder
  - ConvertHerbItemsToRowsAsync: 每4个herbItems打包为一个row，调用IHerbRepository.GetByIdAsync获取HerbDto
  - 处理不足4个药材的最后一行

#### Task 1.4: 增强FormulaCommandHandler组件
- **工作量**: 1.5小时
- **依赖**: Task 1.3
- **类型**: Component
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaCommandHandler.cs`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 新增AddHerbCommand命令
  - [ ] 新增RemoveHerbCommand命令
  - [ ] 新增ClearAllCommand命令
  - [ ] 新增事件：OnHerbAdded, OnHerbRemoved, OnHerbsCleared
  - [ ] CanExecute逻辑：检查IsReadOnly和IsLoading状态
- **技术要点**:
  - 使用Prism的DelegateCommand
  - 事件通知模式：通过事件通知ViewModel执行操作
  - CanExecute条件：!IsReadOnly && !IsLoading

#### Task 1.5: 修改FormulaDetailView XAML布局
- **工作量**: 1.5小时
- **依赖**: Task 1.1, Task 1.4
- **类型**: View (XAML)
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 8列DataGrid布局正确（药材1、用量1、药材2、用量2...药材4、用量4）
  - [ ] ComboBox绑定FilteredHerbs
  - [ ] 按钮区包含：添加行、删除行、清空按钮
  - [ ] 按钮绑定对应Command
  - [ ] IsReadOnly状态控制DataGrid和按钮禁用
- **技术要点**:
  - 参考实现：LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml (lines 199-319)
  - ComboBox设置：IsEditable="True", DisplayMemberPath="Name"
  - DataGrid设置：AutoGenerateColumns="False", CanUserAddRows="True"
  - RelativeSource绑定：{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}

---

### Phase 2: 8列快速录入（8小时）

**Phase目标**: 实现智能匹配（名称+拼音码）和键盘导航，提升录入效率

#### Task 2.1: 实现ComboBox智能匹配过滤
- **工作量**: 3.5小时
- **依赖**: Task 1.2, Task 1.5
- **类型**: ViewModel + View
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs`（修改）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] **名称匹配测试**: 输入"黄芪"显示"黄芪"、"生黄芪"等（前5个）
  - [ ] **拼音码匹配测试**: 输入"HQ"显示所有拼音码为"HQ"的药材（黄芪、黄岐等，前5个）
  - [ ] **实时过滤**: TextChanged事件触发FilterHerbsCommand
  - [ ] **下拉自动展开**: IsDropDownOpen绑定，输入时自动展开
  - [ ] FilteredHerbs集合实时更新
- **技术要点**:
  - XAML: ComboBox添加TextChanged事件触发器
  - ViewModel: 新增FilterHerbsCommand，调用_herbFilterManager.FilterHerbs(searchText, maxResults: 5)
  - 双重匹配逻辑在FormulaHerbFilterManager中实现
  - IsDropDownOpen绑定控制下拉列表自动展开

#### Task 2.2: 实现ComboBox键盘导航
- **工作量**: 2.5小时
- **依赖**: Task 2.1
- **类型**: ViewModel + View
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs`（修改）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`（修改）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml.cs`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] **上下键滚动**: 下箭头显示第6、7、8...个结果（如超过5个）
  - [ ] **回车确认**: 回车后药材选中，光标自动跳转至用量列
  - [ ] **Tab切换**: Tab键顺序跳转8列（药材1→用量1→药材2→用量2→...→用量4→下一行药材1）
  - [ ] **鼠标点击**: 鼠标点击下拉列表直接选中
  - [ ] PreviewKeyDown事件处理正确
- **技术要点**:
  - XAML: ComboBox添加PreviewKeyDown事件触发器
  - ViewModel: 新增HandleKeyNavigationCommand
  - CodeBehind: 处理焦点跳转逻辑（FocusManager.SetFocusedElement）
  - 键盘事件判断：e.Key == Key.Enter, e.Key == Key.Down, e.Key == Key.Up, e.Key == Key.Tab

#### Task 2.3: 优化ComboBox自动完成体验
- **工作量**: 1小时
- **依赖**: Task 2.1, Task 2.2
- **类型**: View (XAML)
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] MaxDropDownHeight="200"（显示约5个结果）
  - [ ] IsEditable="True"允许输入
  - [ ] 选中后自动跳转至用量列
  - [ ] UpdateSourceTrigger="PropertyChanged"实时更新
- **技术要点**:
  - ComboBox属性配置：IsEditable, MaxDropDownHeight, IsDropDownOpen
  - 绑定模式：SelectedItem, Text, DisplayMemberPath
  - SelectionChanged事件：选中后调用焦点跳转逻辑

#### Task 2.4: 实现药材和用量数据验证
- **工作量**: 1小时
- **依赖**: Task 2.3
- **类型**: Component
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaValidator.cs`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 验证药材不能为空（如果用量>0）
  - [ ] 验证用量必须>0（如果药材不为空）
  - [ ] 验证同一行不能有重复药材
  - [ ] 保存时触发验证，显示错误消息
- **技术要点**:
  - ValidateHerbRows()方法：遍历HerbRows，检查每个非空Herb对应的Quantity
  - 重复药材检查：同一行的Herb1-4不能有相同HerbId
  - 错误消息：返回具体的验证错误信息

---

### Phase 3: 复制验方（6小时）

**Phase目标**: 在验方列表页增加"复制"按钮，点击后进入编辑界面，保存按钮变为"另存为我的验方"

**核心逻辑**:
- 列表页点击"复制" → 导航至详情页（预填充数据，编辑模式）
- 详情页保存按钮文案变为"另存为我的验方"
- 保存时创建新验方记录（新Id，CreatedBy为当前用户）

#### Task 3.1: 增强FormulaManagementViewModel
- **工作量**: 1.5小时
- **依赖**: 无
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 新增CopyFormulaCommand命令
  - [ ] 调用_dataManager.CreateFormulaCopy()
  - [ ] 导航至FormulaDetailView，传递IsCopy=true参数
  - [ ] CanExecute逻辑：检查SelectedFormula不为null
- **技术要点**:
  - 使用Prism的DelegateCommand
  - 获取当前用户名：_sessionManager.CurrentUser?.UserName ?? "Unknown"
  - 导航参数：new NavigationParameters { { "Formula", copiedFormula }, { "IsCopy", true } }

#### Task 3.2: 修改FormulaManagementView添加复制按钮
- **工作量**: 0.5小时
- **依赖**: Task 3.1
- **类型**: View (XAML)
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaManagementView.xaml`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 添加"复制"按钮至按钮区
  - [ ] 绑定CopyFormulaCommand
  - [ ] 按钮位置：在"编辑"和"删除"按钮之间
- **技术要点**:
  - Button定义：Content="复制", Command="{Binding CopyFormulaCommand}"
  - 按钮样式：与其他按钮保持一致

#### Task 3.3: 增强FormulaDataManager.CreateFormulaCopy()
- **工作量**: 2小时
- **依赖**: 无
- **类型**: Component
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaDataManager.cs`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 复制验方基础信息：Name保持相同（需求要求）
  - [ ] 复制药材列表：新Guid，保持HerbId、HerbName、Quantity、Preparation、Usage、SortOrder
  - [ ] 更换CreatedBy为currentUserName参数
  - [ ] 设置CreatedAt为DateTime.Now
  - [ ] 不复制Price字段（验方不涉及价格）
- **技术要点**:
  - 深拷贝：遍历sourceFormula.Herbs，创建新FormulaHerbItemDto对象
  - 验证sourceFormula不为null
  - 返回新的FormulaDto对象

#### Task 3.4: 修改FormulaDetailViewModel支持复制模式
- **工作量**: 1小时
- **依赖**: Task 3.3
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs`（修改）
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`（修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] InitializeAsync()检测IsCopy参数
  - [ ] 如果IsCopy=true，预填充Formula数据
  - [ ] 调用ConvertHerbItemsToRowsAsync()转换HerbRows
  - [ ] 设置IsReadOnly=false（编辑模式）
  - [ ] **保存按钮文案变更**: "保存" → "另存为我的验方"（IsCopy=true时）
  - [ ] 保存时验证Name不为空
- **技术要点**:
  - NavigationParameters获取：var isCopy = parameters.GetValue<bool>("IsCopy");
  - NavigationParameters获取：var copiedFormula = parameters.GetValue<FormulaDto>("Formula");
  - 条件判断：if (isCopy && copiedFormula != null)
  - XAML动态文案：Content="{Binding SaveButtonText}"，ViewModel中根据IsCopy设置不同文案

#### Task 3.5: 集成测试复制功能
- **工作量**: 1小时
- **依赖**: Task 3.1, Task 3.2, Task 3.3, Task 3.4
- **类型**: Test
- **文件范围**:
  - 手动测试（无代码文件）
- **验收标准**:
  - [ ] 列表页点击"复制"按钮 → 导航至详情页
  - [ ] 详情页自动填充源验方数据（Name、药材列表）
  - [ ] 保存按钮文案显示"另存为我的验方"
  - [ ] 修改药材后点击保存
  - [ ] 数据库验证：Formulas表有2条记录（Name相同，Id不同）
  - [ ] 验证CreatedBy为当前用户
  - [ ] 验证CreatedAt为当前时间
  - [ ] 验证药材列表独立（FormulaHerbs表有2组记录）
- **技术要点**:
  - 完整流程测试：列表页 → 点击复制 → 详情页 → 验证按钮文案 → 修改 → 保存 → 验证数据库
  - SQL查询验证：SELECT * FROM Formulas WHERE Name = '复制的验方名'
  - SQL查询验证：SELECT * FROM FormulaHerbs WHERE FormulaId = '新FormulaId'

---

## 📊 任务统计

- **总任务数**: 12个
- **总工作量**: 22小时
- **Phase数量**: 3个
- **关键路径长度**: 7个任务（主线任务）

**工作量分布**:
- Phase 1: 8小时（5个任务）
- Phase 2: 8小时（4个任务）
- Phase 3: 6小时（5个任务，复制验方功能）

---

## 🔗 依赖关系图

### Phase 1依赖
```
Task 1.1 (FormulaItemRow模型) → Task 1.3 (FormulaDataManager增强)
Task 1.2 (FormulaHerbFilterManager) → Task 1.3 (FormulaDataManager增强)
Task 1.3 → Task 1.4 (FormulaCommandHandler增强)
Task 1.1, Task 1.4 → Task 1.5 (FormulaDetailView XAML)
```

### Phase 2依赖
```
Task 1.2, Task 1.5 → Task 2.1 (ComboBox智能匹配)
Task 2.1 → Task 2.2 (ComboBox键盘导航)
Task 2.1, Task 2.2 → Task 2.3 (ComboBox优化)
Task 2.3 → Task 2.4 (数据验证)
```

### Phase 3依赖（复制验方）
```
Task 3.1 (FormulaManagementViewModel) → Task 3.2 (FormulaManagementView)
Task 3.3 (FormulaDataManager.CreateFormulaCopy) → Task 3.4 (FormulaDetailViewModel支持复制)
Task 3.1, Task 3.2, Task 3.3, Task 3.4 → Task 3.5 (集成测试)
```

### 关键路径（主线任务）
```
Phase 1:
Task 1.1 (模型) → Task 1.3 (DataManager) → Task 1.4 (CommandHandler) → Task 1.5 (XAML)

Phase 2:
Task 1.5 → Task 2.1 (智能匹配) → Task 2.2 (键盘导航) → Task 2.3 (优化) → Task 2.4 (验证)

Phase 3:
Task 3.3 (CreateFormulaCopy) → Task 3.4 (FormulaDetailViewModel) → Task 3.5 (测试)
```

---

## ⚠️ 关键路径

**主线任务**（必须按顺序完成）：
1. Phase 1关键路径（4个任务）:
   - Task 1.1: 创建FormulaItemRow模型
   - Task 1.3: 增强FormulaDataManager
   - Task 1.4: 增强FormulaCommandHandler
   - Task 1.5: 修改FormulaDetailView XAML
2. Phase 2关键路径（4个任务）:
   - Task 2.1: ComboBox智能匹配
   - Task 2.2: ComboBox键盘导航
   - Task 2.3: ComboBox优化
   - Task 2.4: 数据验证
3. Phase 3关键路径（3个任务）:
   - Task 3.3: CreateFormulaCopy实现
   - Task 3.4: FormulaDetailViewModel支持复制
   - Task 3.5: 集成测试

**并行任务**（可同时进行）：
- **Phase 1**: Task 1.1 和 Task 1.2 可并行（互不依赖）
- **Phase 3**: Task 3.1+3.2 和 Task 3.3 可并行（互不依赖，但都需要在3.4之前完成）

---

## 📝 实施建议

### 优先级排序

1. **🔴 高优先级**：关键路径任务
   - Phase 1全部任务（基础功能，其他Phase依赖）
   - Phase 2: Task 2.1-2.3（核心交互体验）
   - Phase 3: Task 3.3-3.4（复制核心逻辑）

2. **🟡 中优先级**：增强体验任务
   - Phase 2: Task 2.4（数据验证）
   - Phase 3: Task 3.1-3.2（列表页按钮）

3. **🟢 低优先级**：测试和优化任务
   - Phase 3: Task 3.5（集成测试）

### 并行策略

- **Phase 1**:
  - Task 1.1 (FormulaItemRow) 和 Task 1.2 (FormulaHerbFilterManager) 可由不同开发者并行完成
  - Task 1.3必须等待Task 1.1和Task 1.2完成

- **Phase 2**:
  - Task 2.1完成后，Task 2.2和Task 2.3可部分并行（但Task 2.3依赖Task 2.2的键盘事件逻辑）

- **Phase 3**:
  - Task 3.1+3.2 (列表页) 和 Task 3.3 (CreateFormulaCopy) 可并行
  - Task 3.4必须等待Task 3.3完成

### 风险提示

1. **Phase 2键盘导航复杂度**:
   - Task 2.2涉及WPF焦点管理，可能需要多次调试
   - 建议预留缓冲时间（+0.5小时）

2. **Phase 3数据库验证**:
   - Task 3.5需要真实数据库环境
   - 建议在本地SQL Server测试，不要在开发环境直接测试

---

## 🧪 测试策略

### 单元测试（可选）

- **Phase 1**:
  - FormulaItemRow.ToHerbItems()转换逻辑测试
  - FormulaDataManager.ConvertRowsToHerbItems()测试
  - FormulaDataManager.ConvertHerbItemsToRowsAsync()测试

- **Phase 2**:
  - FormulaHerbFilterManager.FilterHerbs()双重匹配测试

- **Phase 3**:
  - FormulaDataManager.CreateFormulaCopy()深拷贝测试

### 集成测试

- **Phase 1**:
  - 启动应用 → 导航至FormulaDetailView → 点击"添加行"/"删除行"/"清空"按钮 → 验证UI响应

- **Phase 2**:
  - 启动应用 → 导航至FormulaDetailView → 编辑模式 → 药材ComboBox输入 → 验证下拉列表、键盘导航、选择逻辑

- **Phase 3**:
  - 启动应用 → 导航至FormulaManagementView → 点击"复制" → 详情页修改 → 保存 → 数据库查询验证（2条记录）

### E2E测试（必需）

**Phase 1验收测试**:
1. 启动应用，导航至验方详情页
2. 点击"添加行"按钮，DataGrid新增空行
3. 点击"删除行"按钮，最后一行被删除
4. 点击"清空"按钮，弹出确认对话框，确认后所有行被清空
5. 切换至只读模式（IsReadOnly=true），所有按钮禁用

**Phase 2验收测试**:
1. 在药材1列输入"黄芪"，下拉列表显示"黄芪"、"生黄芪"等（前5个）
2. 在药材1列输入"HQ"，下拉列表显示所有拼音码为"HQ"的药材（黄芪、黄岐等，前5个）
3. 按下箭头，如果超过5个结果，显示第6、7、8...个结果
4. 高亮药材后按回车，药材选中，光标跳转至用量列
5. 按Tab键，光标依次跳转8列
6. 鼠标点击下拉列表中的药材，直接选中
7. 保存时验证：药材不能为空、用量必须>0

**Phase 3验收测试**（复制验方）:
1. 在验方列表页选中验方，点击"复制"按钮
2. 导航至详情页，验证按钮文案变为"另存为我的验方"
3. 验证验方名称、功效、药材列表自动填充
4. 修改药材数量，点击保存
5. 数据库验证：有2条验方，Name相同但Id不同，CreatedBy为当前用户
6. 验证FormulaHerbs表有2组独立的药材记录

---

## 📚 参考文档

### 设计文档
- `docs/explanation/formula-editing-area-design.md` v1.0

### 需求文档
- `docs/requirements/formula-editing-area-requirements.md` v2.0

### 架构文档
- `docs/explanation/architecture/client/README.md` - Client端MVVM架构
- `docs/explanation/architecture/shared/README.md` - Shared层架构
- `docs/explanation/business-rules.md` - 业务规则

### 参考实现
- **8列布局**: `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml` (lines 199-319)
- **拼音码过滤**: `LYBT.Desktop.Prescriptions/ViewModels/Components/HerbFilterManager.cs`
- **对话框模式**: `LYBT.Desktop.Prescriptions/Views/HerbSelectionDialog.xaml`
- **组件化模式**: `LYBT.Desktop.Patients/ViewModels/PatientDetailViewModel.cs`

### 代码模式
- `docs/reference/quick-reference/code-patterns.md` - 组件化ViewModel模式

---

## 📅 变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|-----|------|---------|------|
| 2025-11-11 | v1.0 | 初始版本，基于formula-editing-area-design.md v1.0 | Claude Code |

---

**下一步**: 使用 **lybtzyzs-issue-template** skill 批量生成GitHub Issues

**实施顺序**: Phase 1 → Phase 2 → Phase 3

**预计完成时间**: 22工时 ≈ 3.5个工作日（按每日7小时计算）
