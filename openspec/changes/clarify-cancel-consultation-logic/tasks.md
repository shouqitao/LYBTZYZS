# Tasks: clarify-cancel-consultation-logic

## Overview

| Phase | Description | Est. Time |
|-------|-------------|-----------|
| 1 | UI文案优化 | 30min |
| 2 | 取消前自动保存 | 45min |
| 3 | 测试验证 | 30min |

**Total Estimated Time**: ~2h

---

## Phase 1: UI文案优化

### Task 1.1: 更新取消确认对话框文案
**Priority**: P1
**Effort**: 15min
**Status**: Done

- [x] 修改 `ExecuteCancelConsultation` 中的确认提示
- [x] 明确说明取消后数据将被软删除，无法直接继续
- [x] 建议用户如需临时离开应使用"暂停看诊"

**当前文案**:
```
"确定要取消本次看诊吗？未保存的数据将丢失！"
```

**建议文案**:
```
"确定要取消本次看诊吗？

取消后，本次就诊记录将被标记为已取消，无法继续编辑。
如果只是临时离开，请使用「暂停看诊」保存进度。"
```

---

### Task 1.2: 更新暂停按钮Tooltip
**Priority**: P2
**Effort**: 10min
**Status**: Done

- [x] 更新 MedicalCaseWorkspaceView.xaml 中暂停按钮的 ToolTip
- [x] 明确说明暂停=保存当前进度，可随时继续

**建议Tooltip**:
```
"保存当前进度并暂时离开。下次选择该患者时可继续看诊。"
```

---

### Task 1.3: 更新取消按钮Tooltip
**Priority**: P2
**Effort**: 5min
**Status**: Done

- [x] 更新取消按钮的 ToolTip
- [x] 明确取消=作废本次就诊

**建议Tooltip**:
```
"作废本次就诊。数据将保留供审计查看，但无法继续编辑。"
```

---

## Phase 2: 取消前自动保存

### Task 2.1: 修改 ExecuteCancelConsultation 逻辑
**Priority**: P1
**Effort**: 30min
**Status**: Done

- [x] 在调用 `_lifecycleHandler.CancelAsync()` 前保存当前数据
- [x] 保存诊断数据（如果有修改）
- [x] 保存处方数据（如果有修改）
- [x] 添加错误处理（保存失败不阻止取消）

**伪代码**:
```csharp
private async void ExecuteCancelConsultation()
{
    // 1. 确认对话框
    if (!await ShowConfirmationAsync(...)) return;

    // 2. 尝试保存当前数据（审计用途，失败不阻止取消）
    try
    {
        if (ConsultationPanelViewModel is ISaveable cs) await cs.SaveAsync();
        if (PrescriptionPanelViewModel is ISaveable ps) await ps.SaveAsync();
    }
    catch (Exception ex)
    {
        Logger.LogWarning(ex, "取消前保存失败，继续执行取消操作");
    }

    // 3. 执行软删除
    var result = await _lifecycleHandler.CancelAsync(MedicalCaseId);
    // ...
}
```

---

### Task 2.2: 添加取消原因记录（可选）
**Priority**: P3
**Effort**: 15min
**Status**: Pending
**Dependencies**: Task 2.1

- [ ] 在取消确认对话框中添加可选的取消原因输入
- [ ] 将原因保存到 Remark 字段

**注意**: 此任务为可选增强，可在后续迭代实现

---

## Phase 3: 测试验证

### Task 3.1: 手动测试场景
**Priority**: P1
**Effort**: 20min
**Status**: Pending
**Dependencies**: Task 1.1, Task 2.1

- [ ] 测试取消看诊流程
  - [ ] 验证确认对话框文案
  - [ ] 验证数据在取消前已保存
  - [ ] 验证软删除后医案不再显示在列表中
- [ ] 测试暂停看诊流程
  - [ ] 验证数据保存到Draft状态
  - [ ] 验证重新选择患者时可继续看诊

---

### Task 3.2: 验证现有单元测试
**Priority**: P2
**Effort**: 10min
**Status**: Done

- [x] 运行 MedicalCaseWorkspaceViewModel 相关测试
- [x] 确保无回归问题 (2/2 测试通过)

---

## Implementation Notes

### 修改的文件
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseLifecycleHandler.cs` - 改用标准DELETE端点
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` - 添加DELETE端点
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseService.cs` - 添加DeleteAsync接口
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs` - 实现DeleteAsync方法

### API端点修复（运行时发现的问题）
- **问题**: 客户端调用 `DELETE /api/v1/medicalcases/{id}/soft` 返回404
- **原因**: 服务端未实现该端点
- **修复**: 添加 `DELETE /api/v1/medicalcases/{id}` 端点（BaseRepository默认软删除）
- **客户端**: 改用 `DeleteMedicalCaseAsync` 替代 `SoftDeleteMedicalCaseAsync`

### 不修改的文件
- `MedicalCaseEnums.cs` - 状态枚举保持不变
- `MedicalCaseModel.cs` - 实体模型保持不变

### 验收标准
1. 取消确认对话框清晰说明操作后果
2. 取消前自动保存已填写数据
3. 暂停/取消的语义区分明确
4. 无回归问题
