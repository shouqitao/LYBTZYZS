# Tasks: 医案界面UI优化

**Change ID**: `optimize-medicalcase-ui`
**Total Tasks**: 7
**Completed**: 0

---

## Phase 1: 移除Header患者信息

### Task 1.1: 删除ActionButtons患者信息
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
- **Action**: 删除`BaseDetailContainer.ActionButtons`中的患者信息StackPanel
- **Status**: [ ] Pending
- **Validation**: 编译通过，Header区域不再显示患者姓名和信息

---

## Phase 2: 诊断区2x2无滚动条布局

### Task 2.1: 重构ConsultationPanel为2x2网格
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml`
- **Action**:
  - 改为2x2网格：现病史|中医诊断（上）、舌诊|脉诊（下）
  - 上行`Height="*"`自适应填充
  - 下行`Height="Auto"`根据内容调整
  - 移除所有`VerticalScrollBarVisibility="Auto"`
  - 移除固定`MinHeight`/`Height`值
  - 保留`TextWrapping="Wrap"`和`AcceptsReturn="True"`
- **Status**: [ ] Pending
- **Validation**: 无滚动条，4个字段自适应填充空间

---

## Phase 3: 待诊队列三种状态

### Task 3.1: 新建PendingCaseType枚举
- **File**: `src/Shared/LYBT.Shared.Models/Enums/PendingCaseType.cs` (新建)
- **Action**: 创建枚举定义Waiting/InProgress/Suspended三种状态
- **Status**: [ ] Pending
- **Validation**: 编译通过

### Task 3.2: 修改PendingMedicalCaseDto
- **File**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/PendingMedicalCaseDto.cs`
- **Action**:
  - 将`Type`从`string`改为`PendingCaseType`
  - 添加`TypeDisplay`计算属性
- **Status**: [ ] Pending
- **Validation**: 编译通过

### Task 3.3: 修改待诊队列查询逻辑
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **Action**:
  - 修改`GetPendingCasesAsync`方法
  - 根据MedicalCaseStatus和Consultation记录判定PendingCaseType
- **Status**: [ ] Pending
- **Validation**: 待诊队列返回正确的Type值

### Task 3.4: 更新PendingQueueControl UI
- **File**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml`
- **Action**:
  - 绑定TypeDisplay属性
  - 添加状态颜色转换器（Waiting灰/InProgress绿/Suspended橙）
- **Status**: [ ] Pending
- **Validation**: 三种状态有颜色区分

---

## Validation

### Task V.1: 编译验证
- **Action**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **Status**: [ ] Pending
- **Validation**: 编译成功，无错误

---

## Task Summary

| Phase | Task | 描述 | 状态 |
|-------|------|------|------|
| 1 | 1.1 | 删除ActionButtons患者信息 | [ ] |
| 2 | 2.1 | 重构ConsultationPanel为2x2网格 | [ ] |
| 3 | 3.1 | 新建PendingCaseType枚举 | [ ] |
| 3 | 3.2 | 修改PendingMedicalCaseDto | [ ] |
| 3 | 3.3 | 修改待诊队列查询逻辑 | [ ] |
| 3 | 3.4 | 更新PendingQueueControl UI | [ ] |
| V | V.1 | 编译验证 | [ ] |
