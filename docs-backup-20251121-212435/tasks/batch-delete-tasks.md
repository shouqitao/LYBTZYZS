# 管理界面批量删除功能 任务分解文档

## 📋 元数据

- **Epic**: #2150
- **设计文档**: [batch-delete-design.md](../explanation/architecture/client/batch-delete-design.md)
- **需求文档**: [batch-delete-discussion.md](../explanation/architecture/client/batch-delete-discussion.md)
- **总工作量**: 30-41小时
- **实施阶段**: Phase 1-4
- **任务数量**: 11个
- **涉及模块**: Herbs、Patients、Formula、Users

---

## 🎯 任务清单（Task Checklist）

### Phase 1: 基础架构和控件层（8-11小时）

#### Task 1.1: UnifiedManagementTable添加checkbox列功能

- **工作量**: 4-5小时
- **依赖**: 无
- **类型**: Control Layer
- **优先级**: 🔴 高（关键路径）

**文件范围**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml.cs`

**实现内容**:
1. 添加ShowCheckBoxColumn依赖属性（DependencyProperty）
2. 添加SelectedItems依赖属性（支持双向绑定）
3. 实现AddCheckBoxColumn方法（动态添加DataGridCheckBoxColumn）
4. 实现RemoveCheckBoxColumn方法
5. 实现OnShowCheckBoxColumnChanged回调（属性变更时添加/移除列）
6. 实现DataGrid_SelectionChanged事件（同步SelectedItems）
7. 在DataGrid_Loaded中订阅SelectionChanged事件

**验收标准**:
- [ ] 编译通过：`dotnet build LYBT.Desktop.sln -c Release --no-restore` 0 errors, 0 warnings
- [ ] ShowCheckBoxColumn="True"时DataGrid第一列显示checkbox
- [ ] ShowCheckBoxColumn="False"时checkbox列被移除
- [ ] checkbox勾选状态同步到SelectedItems集合
- [ ] DataGrid选中状态与SelectedItems双向同步
- [ ] 代码包含完整的XML注释

**技术要点**:
- 使用DataGridCheckBoxColumn绑定到DataGridRow.IsSelected（WPF内置属性）
- DisplayIndex=0确保checkbox列在第一列
- 使用RelativeSource查找DataGridRow
- SelectedItems类型为IList（兼容ObservableCollection<T>）
- 事件订阅在Loaded事件中完成，避免null引用

**业务规则**:
- 无直接业务规则，属于UI基础设施

---

#### Task 1.2: BaseMasterDataListView配置和绑定

- **工作量**: 2-3小时
- **依赖**: Task 1.1
- **类型**: View Layer
- **优先级**: 🔴 高（关键路径）

**文件范围**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml`

**实现内容**:
1. 在BaseMasterDataListView.xaml.cs中添加SelectedItems依赖属性
2. 在BaseMasterDataListView.xaml.cs中添加ShowCheckBoxColumn依赖属性
3. 在BaseMasterDataListView.xaml中绑定SelectedItems到UnifiedManagementTable
4. 在BaseMasterDataListView.xaml中绑定ShowCheckBoxColumn到UnifiedManagementTable
5. 使用RelativeSource绑定到UserControl的属性

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] BaseMasterDataListView的SelectedItems属性变化时，UnifiedManagementTable的SelectedItems同步变化
- [ ] BaseMasterDataListView的ShowCheckBoxColumn属性控制UnifiedManagementTable的checkbox列显示
- [ ] 属性支持双向绑定（FrameworkPropertyMetadataOptions.BindsTwoWayByDefault）
- [ ] 代码包含完整的XML注释

**技术要点**:
- 依赖属性使用FrameworkPropertyMetadata配置双向绑定
- XAML绑定使用RelativeSource={RelativeSource AncestorType=UserControl}
- 属性透传模式（BaseMasterDataListView → UnifiedManagementTable）

**业务规则**:
- 无直接业务规则，属于UI基础设施

---

#### Task 1.3: 控件层单元测试

- **工作量**: 2-3小时
- **依赖**: Task 1.1, Task 1.2
- **类型**: Test
- **优先级**: 🟡 中

**文件范围**:
- `tests/UnitTests/Client/Infrastructure/Controls/UnifiedManagementTableTests.cs`（新建）
- `tests/UnitTests/Client/Infrastructure/Views/BaseMasterDataListViewTests.cs`（新建）

**实现内容**:
1. UnifiedManagementTable测试用例：
   - ShowCheckBoxColumn=True时添加checkbox列
   - ShowCheckBoxColumn=False时移除checkbox列
   - checkbox勾选触发SelectedItems同步
   - DataGrid.SelectedItems变化触发SelectedItems同步
2. BaseMasterDataListView测试用例：
   - SelectedItems属性绑定正确
   - ShowCheckBoxColumn属性绑定正确

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 所有测试用例通过：`dotnet test`
- [ ] 代码覆盖率 ≥80%（使用coverlet或VS Code Coverage）
- [ ] 测试用例命名清晰（AAA模式：Arrange-Act-Assert）

**技术要点**:
- 使用xUnit测试框架
- 使用NSubstitute模拟依赖（如果需要）
- WPF控件测试需要在STA线程中运行（[STAFact]特性）

**业务规则**:
- 无直接业务规则

---

### Phase 2: ViewModel基类实现（5-7小时）

#### Task 2.1: UnifiedListViewModelBase实现批量删除

- **工作量**: 3-4小时
- **依赖**: Task 1.2
- **类型**: ViewModel Base Layer
- **优先级**: 🔴 高（关键路径）

**文件范围**:
- `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedListViewModelBase.cs`

**实现内容**:
1. 添加SelectedItems属性（ObservableCollection<T>）
2. 添加BatchDeleteCommand属性（DelegateCommand）
3. 在构造函数中初始化BatchDeleteCommand
4. 实现CanExecuteBatchDelete方法（检查SelectedItems.Count > 0）
5. 实现ExecuteBatchDeleteAsync方法（模板方法）：
   - 显示确认对话框（ShowConfirmationAsync）
   - 复制选中项列表（避免迭代修改集合）
   - 调用OnExecuteBatchDeleteAsync抽象方法
   - 清空SelectedItems
   - 刷新列表（LoadDataAsync）
6. 定义OnExecuteBatchDeleteAsync抽象方法（子类实现）
7. 定义ShowConfirmationAsync抽象方法
8. 定义ShowSuccessMessageAsync抽象方法
9. 定义ShowWarningMessageAsync抽象方法
10. 使用ObservesProperty自动刷新CanExecute

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] BatchDeleteCommand正确初始化
- [ ] CanExecute检查SelectedItems.Count > 0
- [ ] ExecuteBatchDeleteAsync流程正确（确认→执行→清空→刷新）
- [ ] OnExecuteBatchDeleteAsync为抽象方法，由子类实现
- [ ] 代码包含完整的XML注释
- [ ] 符合模板方法模式

**技术要点**:
- 模板方法模式：基类定义流程，子类实现细节
- ObservesProperty(() => SelectedItems.Count)自动刷新CanExecute
- 使用ToList()复制集合，避免foreach中修改集合
- async/await异步模式

**业务规则**:
- **BR-002**: 批量删除前必须显示确认对话框
- **BR-005**: 未选择任何项时，批量删除按钮禁用

---

#### Task 2.2: ViewModel基类单元测试

- **工作量**: 2-3小时
- **依赖**: Task 2.1
- **类型**: Test
- **优先级**: 🟡 中

**文件范围**:
- `tests/UnitTests/Client/Models/ViewModels/Base/UnifiedListViewModelBaseTests.cs`（新建）

**实现内容**:
1. 创建测试用的具体ViewModel类（继承UnifiedListViewModelBase）
2. 测试用例：
   - SelectedItems为空时，BatchDeleteCommand.CanExecute返回false
   - SelectedItems有数据时，BatchDeleteCommand.CanExecute返回true
   - ExecuteBatchDeleteAsync调用OnExecuteBatchDeleteAsync
   - ExecuteBatchDeleteAsync完成后清空SelectedItems
   - ExecuteBatchDeleteAsync完成后调用LoadDataAsync
   - 用户取消确认时不执行删除

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 所有测试用例通过：`dotnet test`
- [ ] 代码覆盖率 ≥80%
- [ ] 测试用例使用AAA模式（Arrange-Act-Assert）

**技术要点**:
- 使用xUnit + NSubstitute
- Mock ShowConfirmationAsync返回true/false
- Mock OnExecuteBatchDeleteAsync
- 验证方法调用次数（Received()）

**业务规则**:
- **BR-005**: 空选择处理

---

### Phase 3: 各模块实现和UI集成（12-16小时）

#### Task 3.1: Herbs模块批量删除实现

- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Module Implementation
- **优先级**: 🔴 高

**文件范围**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbManagementViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Views/HerbManagementView.xaml`

**实现内容**:
1. HerbManagementViewModel实现OnExecuteBatchDeleteAsync方法：
   - 逐个调用_herbRepository.DeleteAsync(item.Id)
   - 统计成功数和失败数
   - 收集失败项目（最多5个）
   - 生成结果消息
   - 显示成功/警告消息
   - 记录日志
2. HerbManagementView.xaml配置：
   - ShowCheckBoxColumn="True"
   - SelectedItems双向绑定
   - 批量删除按钮已存在，确认绑定到BatchDeleteCommand
3. 集成测试（手动验证或自动化）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 药材管理界面显示checkbox列
- [ ] 勾选多个药材后批量删除按钮可用
- [ ] 点击批量删除显示确认对话框（提示数量）
- [ ] 确认后执行删除，显示成功/失败结果
- [ ] 部分删除失败时不影响其他药材删除
- [ ] 失败时显示失败药材名称和原因
- [ ] 代码包含完整的XML注释和业务规则引用

**技术要点**:
- foreach逐个删除，try-catch捕获单个异常
- 调用已有的_herbRepository.DeleteAsync（包含权限检查）
- 使用ILogger记录删除操作和异常

**业务规则**:
- **BR-001**: 权限控制（通过Repository.DeleteAsync）
- **BR-003**: 结果反馈（成功数/失败数/失败项目）
- **BR-004**: 失败处理（部分失败不影响其他）

---

#### Task 3.2: Patients模块批量删除实现

- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Module Implementation
- **优先级**: 🔴 高

**文件范围**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientManagementViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientManagementView.xaml`

**实现内容**:
（与Task 3.1类似，针对患者模块）
1. PatientManagementViewModel实现OnExecuteBatchDeleteAsync
2. PatientManagementView.xaml配置checkbox和绑定
3. 集成测试

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 患者管理界面批量删除功能正常
- [ ] 确认对话框显示正确
- [ ] 结果反馈显示正确（成功数/失败数）
- [ ] 符合业务规则BR-001/003/004

**技术要点**:
- 调用_patientRepository.DeleteAsync（软删除）
- 患者删除涉及关联数据，需注意外键约束
- 记录删除日志（患者姓名、身份证号等敏感信息脱敏）

**业务规则**:
- **BR-001**: 权限控制
- **BR-003**: 结果反馈
- **BR-004**: 失败处理

---

#### Task 3.3: Formula模块批量删除实现

- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Module Implementation
- **优先级**: 🔴 高

**文件范围**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaManagementView.xaml`

**实现内容**:
（与Task 3.1类似，针对验方模块）
1. FormulaManagementViewModel实现OnExecuteBatchDeleteAsync
2. FormulaManagementView.xaml配置checkbox和绑定
3. 集成测试

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 验方管理界面批量删除功能正常
- [ ] 确认对话框和结果反馈正确
- [ ] 符合业务规则BR-001/003/004

**技术要点**:
- 调用_formulaRepository.DeleteAsync
- 验方可能被处方引用，删除前检查引用关系
- 记录删除日志（验方名称、分类等）

**业务规则**:
- **BR-001**: 权限控制
- **BR-003**: 结果反馈
- **BR-004**: 失败处理

---

#### Task 3.4: Users模块批量删除实现

- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Module Implementation
- **优先级**: 🔴 高

**文件范围**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/UserManagementView.xaml`

**实现内容**:
（与Task 3.1类似，针对用户模块）
1. UserManagementViewModel实现OnExecuteBatchDeleteAsync
2. UserManagementView.xaml配置checkbox和绑定
3. 集成测试

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 用户管理界面批量删除功能正常
- [ ] 确认对话框和结果反馈正确
- [ ] 符合业务规则BR-001/003/004
- [ ] 不能删除当前登录用户

**技术要点**:
- 调用_userRepository.DeleteAsync
- 检查不能删除当前登录用户
- 用户删除可能影响权限系统，需谨慎处理
- 记录删除日志（用户名、角色等）

**业务规则**:
- **BR-001**: 权限控制（不能删除当前用户）
- **BR-003**: 结果反馈
- **BR-004**: 失败处理

---

### Phase 4: 用户体验优化和测试（5-7小时）

#### Task 4.1: UX优化（全选、快捷键、样式）

- **工作量**: 3-4小时
- **依赖**: Task 3.1, Task 3.2, Task 3.3, Task 3.4
- **类型**: UX Enhancement
- **优先级**: 🟡 中

**文件范围**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml.cs`

**实现内容**:
1. 添加全选功能：
   - 在checkbox列表头添加CheckBox控件
   - 绑定IsChecked到全选状态（三态：全选/部分选中/未选中）
   - 点击表头checkbox触发DataGrid.SelectAll() / UnselectAll()
2. 添加键盘快捷键支持：
   - Ctrl+A全选
   - 处理InputBindings或KeyDown事件
3. 优化确认对话框样式（如果需要）
4. 优化结果提示样式（如果需要）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 点击表头checkbox可以全选/取消全选所有行
- [ ] 部分选中时表头checkbox显示中间状态（Indeterminate）
- [ ] Ctrl+A快捷键可以全选
- [ ] 全选/取消全选操作响应时间 < 100ms
- [ ] UI样式符合设计规范

**技术要点**:
- CheckBox三态绑定（IsThreeState=True）
- DataGrid.SelectAll() / UnselectAll()方法
- KeyBinding或KeyDown事件处理
- 性能优化：避免全选大量数据时卡顿

**业务规则**:
- 无直接业务规则，属于UX增强

---

#### Task 4.2: 端到端测试和文档

- **工作量**: 2-3小时
- **依赖**: Task 4.1
- **类型**: Testing & Documentation
- **优先级**: 🟢 低

**文件范围**:
- `tests/E2ETests/Client/BatchDelete/BatchDeleteE2ETests.cs`（新建）
- `docs/explanation/architecture/client/README.md`（更新）
- `docs/how-to/manage-herbs.md`等操作指南（更新）

**实现内容**:
1. E2E测试用例：
   - 完整批量删除流程（勾选→点击按钮→确认→验证结果）
   - 全选功能测试
   - Ctrl+A快捷键测试
   - 部分删除失败场景测试
   - 性能测试（批量删除100项 < 10s）
2. 文档更新：
   - 更新Client端架构文档（批量操作模式）
   - 更新各模块操作指南（批量删除步骤）
   - 更新代码规范文档（如果有新模式）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 所有E2E测试用例通过
- [ ] 批量删除100项性能 < 10s
- [ ] UI不阻塞（删除过程中仍可响应）
- [ ] 架构文档已更新
- [ ] 操作指南已更新（4个模块）

**技术要点**:
- 使用Selenium或WinAppDriver进行WPF UI自动化测试
- 性能测试使用Stopwatch计时
- 文档使用Markdown格式，符合Diátaxis框架

**业务规则**:
- 无直接业务规则

---

## 📊 任务统计

- **总任务数**: 11个
- **总工作量**: 30-41小时
- **Phase数量**: 4个
- **关键路径长度**: 7个任务（Task 1.1 → 1.2 → 2.1 → 3.x → 4.1 → 4.2）
- **并行任务**: Phase 3的4个模块任务可以并行

---

## 🔗 依赖关系图

### Phase 1内部依赖
```
Task 1.1 (无依赖)
  └─> Task 1.2
       └─> Task 1.3
```

### Phase 2内部依赖
```
Task 2.1 (依赖Task 1.2)
  └─> Task 2.2
```

### Phase 3内部依赖
```
Task 3.1 (依赖Task 2.1) ┐
Task 3.2 (依赖Task 2.1) ├─ 可并行
Task 3.3 (依赖Task 2.1) │
Task 3.4 (依赖Task 2.1) ┘
```

### Phase 4内部依赖
```
Task 4.1 (依赖Task 3.1, 3.2, 3.3, 3.4)
  └─> Task 4.2
```

### 跨Phase依赖
```
Phase 1 → Phase 2 → Phase 3 → Phase 4

关键路径：
Task 1.1 → Task 1.2 → Task 2.1 → Task 3.x（任一模块）→ Task 4.1 → Task 4.2
```

---

## ⚠️ 关键路径

### 主线任务（必须按顺序完成）

1. **Task 1.1**: UnifiedManagementTable添加checkbox列功能
2. **Task 1.2**: BaseMasterDataListView配置和绑定
3. **Task 2.1**: UnifiedListViewModelBase实现批量删除
4. **Task 3.x**: 任一模块批量删除实现（4个模块任选其一）
5. **Task 4.1**: UX优化
6. **Task 4.2**: 端到端测试和文档

### 并行任务（可同时进行）

- **Phase 3并行**：Task 3.1、3.2、3.3、3.4可以由不同开发者同时开发
- **测试并行**：Task 1.3可以在Task 1.2完成后开始，与Task 2.1部分并行
- **测试并行**：Task 2.2可以在Task 2.1完成后开始，与Phase 3部分并行

---

## 📝 实施建议

### 优先级排序

1. **🔴 高优先级**（关键路径任务）：
   - Task 1.1, 1.2（基础架构，阻塞后续所有任务）
   - Task 2.1（ViewModel基类，阻塞所有模块）
   - Task 3.1, 3.2, 3.3, 3.4（核心功能）

2. **🟡 中优先级**（增强任务）：
   - Task 1.3, 2.2（单元测试）
   - Task 4.1（UX优化）

3. **🟢 低优先级**（优化和文档）：
   - Task 4.2（E2E测试和文档）

### 并行策略

**推荐并行组合**：
- **阶段1**：Task 1.1（1人，4-5h）
- **阶段2**：Task 1.2 + Task 1.3（1人 + 1人，2-3h + 2-3h）
- **阶段3**：Task 2.1 + Task 2.2（1人 + 1人，3-4h + 2-3h）
- **阶段4**：Task 3.1/3.2/3.3/3.4（4人并行，每人3-4h）
- **阶段5**：Task 4.1 + Task 4.2（1人 + 1人，3-4h + 2-3h）

**最快完成时间**：
- 单人开发：30-41小时（约4-5天）
- 2人并行：18-25小时（约2-3天）
- 4人并行（Phase 3）：12-18小时（约1.5-2天）

### 风险提示

1. **Task 1.1复杂度风险**：
   - 涉及WPF DataGrid动态列添加，可能遇到绑定问题
   - 建议预留缓冲时间，实际可能需要5-6小时

2. **Task 3.x模块差异风险**：
   - 各模块的Repository删除逻辑可能不一致
   - 需要统一错误处理和日志记录模式

3. **Task 4.1性能风险**：
   - 全选大量数据（>1000行）可能导致UI卡顿
   - 需要性能测试和优化

4. **跨Phase依赖风险**：
   - Phase 3必须等Phase 2完成，不能提前开始
   - 建议Phase 2完成后立即Code Review，避免返工

### 质量保证策略

1. **Code Review时机**：
   - Task 1.2完成后（基础架构关键点）
   - Task 2.1完成后（ViewModel基类关键点）
   - Task 3.1完成后（首个模块，作为其他模块参考）

2. **测试策略**：
   - 单元测试跟随开发（Task 1.3, 2.2）
   - 集成测试在Phase 3每个模块完成后执行
   - E2E测试在Phase 4统一执行

3. **文档更新策略**：
   - 代码注释随开发更新
   - 架构文档在Phase 2完成后更新
   - 操作指南在Phase 4统一更新

---

## 🧪 测试策略

### 单元测试（Task 1.3, 2.2）

**测试范围**：
- UnifiedManagementTable checkbox列添加/移除
- SelectedItems同步机制
- BatchDeleteCommand CanExecute逻辑
- ExecuteBatchDeleteAsync流程

**测试框架**：
- xUnit
- NSubstitute（Mock）
- Coverlet（代码覆盖率）

**覆盖率要求**：
- UnifiedManagementTable: ≥80%
- UnifiedListViewModelBase: ≥80%

### 集成测试（Task 3.x）

**测试范围**：
- 各模块批量删除完整流程
- Repository调用正确性
- 异常处理和日志记录

**测试方法**：
- 手动测试 + 自动化测试
- 使用真实Repository（连接测试数据库）

### E2E测试（Task 4.2）

**测试范围**：
- 完整用户交互流程（勾选→删除→确认→验证）
- 全选功能
- 键盘快捷键
- 性能测试

**测试工具**：
- Selenium / WinAppDriver（WPF UI自动化）
- Stopwatch（性能计时）

**性能指标**：
- checkbox选择响应 < 100ms
- 批量删除100项 < 10s
- UI不阻塞

---

## 📚 参考资料

- **需求文档**: [batch-delete-discussion.md](../explanation/architecture/client/batch-delete-discussion.md)
- **设计文档**: [batch-delete-design.md](../explanation/architecture/client/batch-delete-design.md)
- **架构指南**: [Client端架构指南](../explanation/architecture/client/README.md)
- **业务规则**: [核心业务规则](../explanation/business-rules.md)
- **WPF DataGrid文档**: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid
- **Prism框架指南**: [Prism Framework Guide](../reference/prism-framework-guide.md)

---

## 💡 下一步操作

1. **审查task文档**：确认任务拆分合理性、工作量估算准确性
2. **调整任务粒度**：如果某个任务过大（>5小时），考虑进一步拆分
3. **批量生成Issues**：使用lybtzyzs-issue-template读取本task文档，批量创建GitHub Issues
4. **分配开发者**：根据并行策略分配任务给团队成员
5. **启动开发**：按照Phase顺序开始实施

---

**维护者**：Claude Code  
**创建时间**：2025-11-19  
**文档版本**：v1.0
