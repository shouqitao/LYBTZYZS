# 医案流程数据验证技术债务跟踪

**创建日期**：2025-10-20
**关联Epic**：#1343 完成MVP功能（57个任务）
**优先级**：中（阶段2实施）
**状态**：待实施

---

## 📋 背景

**阶段1目标**：快速让4步医案流程走通，验证UI/UX交互框架
**当前实施**：已放宽处方验证逻辑，允许手工输入药材名称（无需HerbId）

**实施的修改**：
- 文件：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs`
- 修改点1：`GetAllItems()` 方法（line 327-345）- 检查HerbName而非HerbId
- 修改点2：`Validate()` 方法（line 217-232）- 跳过HerbId验证

```csharp
// 阶段1：检查药材名称而非HerbId，支持手工输入
if (!string.IsNullOrWhiteSpace(row.Item1.HerbName))
    result.Add(row.Item1);
```

---

## 🚨 技术债务清单

### 0. 所有步骤验证临时禁用（高优先级）⭐最新

**问题描述**：
- 阶段1为了让流程走通，临时禁用了所有验证和检查逻辑
- 包括：IValidatable接口验证、ISaveable保存失败检查、CurrentPatient空检查
- 代码位置：MedicalCaseFlowViewModel.cs ExecuteNextStepAsync方法

**需要恢复的验证**：
- [ ] IValidatable接口验证（line 229-243）
- [ ] ISaveable保存失败检查（line 246-262）
- [ ] CurrentPatient空检查（line 264-289）
- [ ] MedicalCase自动创建逻辑（line 273-284）

**恢复时机**：阶段2，在修复ViewModel数据丢失问题后

---

### 1. 处方数据完整性验证（高优先级）

**问题描述**：
- 当前仅检查药材名称非空，允许HerbId为空
- 无法关联到真实的药材主数据（Herbs表）
- 价格计算使用临时假设（每克1元）

**需要实施**：
- [ ] 集成Herbs模块，支持药材选择器
- [ ] 验证HerbId必须有效（关联到Herbs表）
- [ ] 从Herbs表获取真实单价
- [ ] 正确计算处方总价

**影响范围**：
- PrescriptionEditorViewModel.cs（GetAllItems、Validate、SingleDosagePrice）
- PrescriptionEditorView.xaml（添加药材选择器UI）

---

### 2. 处方数据持久化（高优先级）

**问题描述**：
- 当前SaveAsync()方法仅记录日志，未实际保存
- 缺少IPrescriptionRepository依赖
- MedicalCase.PrescriptionId未更新

**需要实施**：
- [ ] 注入IPrescriptionRepository依赖
- [ ] 实现创建Prescription API调用
- [ ] 实现更新MedicalCase.PrescriptionId
- [ ] 处理保存失败的回滚逻辑

**代码位置**：
- PrescriptionEditorViewModel.cs line 244-299（SaveAsync方法）

---

### 3. 处方数据在导航间的持久化（中优先级）

**问题描述**：
- 用户点击"上一步"/"下一步"时，PrescriptionEditorViewModel被重新创建
- ItemRows数据丢失，用户需要重新填写

**需要实施**：
- [ ] 在MedicalCaseFlowViewModel中缓存Step 3的ViewModel实例
- [ ] 或实现临时数据保存到MedicalCase草稿字段
- [ ] 或使用EventAggregator传递数据

**代码位置**：
- MedicalCaseFlowViewModel.cs line 463-469（FillPrescription case）

---

### 4. Step 2诊断数据验证（中优先级）

**问题描述**：
- ConsultationFormViewModel可能也存在类似的数据丢失问题
- 需要统一处理各步骤ViewModel的生命周期

**需要实施**：
- [ ] 检查ConsultationFormViewModel的验证逻辑
- [ ] 检查诊断数据在导航间是否丢失
- [ ] 统一Step 2-4的ViewModel缓存机制

---

## 🎯 实施优先级建议

**阶段1（当前）**：
- ✅ 放宽验证，让4步流程走通
- ✅ 验证前一步/后一步UI交互

**阶段2**：
1. 修复ViewModel重建导致数据丢失（优先级最高）
2. 实现处方数据持久化
3. 集成Herbs模块，支持药材选择器

**阶段3**：
- UI优化
- 完整的数据验证
- 错误处理和用户提示

---

## 📚 相关文件

- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PrescriptionEditorView.xaml`

---

## 📝 变更历史

| 日期 | 版本 | 变更描述 |
|------|------|---------|
| 2025-10-20 | v1.0 | 初始创建，记录阶段1放宽验证的技术债务 |
