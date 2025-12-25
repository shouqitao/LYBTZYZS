# Change: 重构MasterDetail编辑模式 - 消除冗余统一模式

## Why

当前MasterDetail ViewModel层经过多次迭代，产生了不同方案的遗留代码，存在以下问题：

1. **P0 Bug**: `OnExecuteAddAsync`中设置`SelectedItem = null`触发`RefreshCanExecuteChanged()`将`CurrentDetail`置空，导致新建保存失败
2. **代码冗余**: 每个模块维护独立的Edit属性(EditName, EditUserName等)，造成200-300行重复代码
3. **模式不一致**: 
   - Users/Patients/Herbs使用Edit属性模式
   - Formula使用CurrentDetail直接绑定模式
   - 混合模式增加维护成本
4. **遗留代码**: 存在废弃的辅助方法、注释掉的旧代码、未使用的字段

## What Changes

### Phase 1: 修复P0 Bug（立即执行）
- 修改`OnExecuteAddAsync`执行顺序，使用`_isCreatingNew`标志防止`CurrentDetail`被置空
- 影响: MasterDetailViewModelBase.cs

### Phase 2-4: 统一编辑模式（消除冗余）
- **BREAKING**: 移除所有模块的独立Edit属性
- 统一采用直接绑定`CurrentDetail`属性的模式（参考Formula模块）
- 移除`ClearEditProperties()`、`PopulateEditProperties()`等辅助方法
- 影响: Users, Patients, Herbs模块

### Phase 5: 清理迭代遗留代码
- 移除废弃的辅助方法
- 移除未使用的私有字段和属性
- 统一CloneDetail()实现模式
- 清理注释掉的旧代码块
- 统一命名规范

### Phase 6-7: MedicalCase与Formula模块检查
- MedicalCase: 新建入口在看诊工作台，确认CreateNewDetail()逻辑正确
- Formula: 确认已使用统一模式，清理可能的遗留代码

## Impact

- Affected specs: desktop-viewmodels
- Affected code:
  - `MasterDetailViewModelBase.cs` - 基类修复
  - `UserMasterDetailViewModel.cs` - 移除7个Edit属性及遗留代码
  - `PatientMasterDetailViewModel.cs` - 移除8个Edit属性及遗留代码
  - `HerbMasterDetailViewModel.cs` - 移除11个Edit属性及遗留代码
  - `FormulaMasterDetailViewModel.cs` - 验证并清理遗留代码
  - `MedicalCaseMasterDetailViewModel.cs` - 验证新建逻辑
  - 对应的XAML视图文件绑定更新

## 预期收益

| 指标 | 当前 | 优化后 |
|------|------|--------|
| Users ViewModel代码行 | ~450行 | ~280行 |
| Patients ViewModel代码行 | ~380行 | ~220行 |
| Herbs ViewModel代码行 | ~520行 | ~320行 |
| Edit属性总数 | 26个 | 0个 |
| 模式一致性 | 3种模式 | 1种统一模式 |
| 遗留代码 | 存在 | 清除 |
