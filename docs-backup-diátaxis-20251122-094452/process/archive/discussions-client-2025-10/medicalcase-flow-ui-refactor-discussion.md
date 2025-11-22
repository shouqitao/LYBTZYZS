# 医案流程UI重构需求讨论

## 文档状态
- **创建时间**：2025-10-21
- **讨论状态**：✅ 已确认
- **实施状态**：✅ 已完成
- **完成时间**：2025-10-21
- **相关Issue**：[#1565 优化医案流程UI布局](https://github.com/shouqitao/LYBTZYZS/issues/1565)
- **实施分支**：`feature/issue-1565-medicalcase-flow-ui-refactor`
- **相关文件**：
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

---

## 背景说明

### 当前UI结构

**MedicalCaseFlowView.xaml** 采用5行Grid布局：

| 行 | 高度 | 功能 | 分类 |
|----|------|------|------|
| Row 0 | 60px | 顶部导航栏（返回主页、标题） | 公共部分 |
| Row 1 | 80px | 4步骤流程进度条 | 公共部分 |
| Row 2 | 50px | 患者信息条（Step 2-4显示） | 公共部分 |
| Row 3 | * | 主内容区（ContentControl动态绑定） | **动态部分** |
| Row 4 | 80px | 底部操作栏（取消、保存草稿、上一步、下一步） | 公共部分 |

### 重构范围

**用户明确**：
- ✅ **动态部分**：中间的患者选择视图（Step 1）及其他步骤视图
- ✅ **公共部分**：顶部导航、步骤条、患者信息条、底部按钮
- 🎯 **本次重构**：先重构公共部分

---

## 需求确认

### ❓ Q1：截图中红色标注的具体含义

**问题**：请具体说明截图中红色标注（椭圆框、箭头）分别表示哪些UI调整需求？

**截图观察**：
- 🔴 红色椭圆框住了4个步骤条
- ➡️ 箭头指向主内容区域
- ➡️ 箭头指向底部按钮区域
- 🟢 右下角绿色"下一步"按钮被标注

**待确认**：
- [ ] 步骤条需要调整哪些方面（样式/布局/交互）？
- [ ] 主内容区需要调整什么（高度/边距/背景）？
- [ ] 底部按钮区需要调整什么（布局/按钮样式/位置）？
- [ ] "下一步"按钮需要特殊处理吗？

**用户回答**：
_（待填写）_

---

### Q2：公共部分重构的优先级

**问题**：_（待Q1确认后提问）_

---

### Q3：样式规范和设计参考

**问题**：_（待Q1确认后提问）_

---

## 当前代码分析

### 代码文件位置
- **View**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`
- **Code Behind**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml.cs`
- **ViewModel**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

### 关键特性
1. **步骤导航**：通过 `IsStep1/2/3/4` 绑定高亮显示当前步骤
2. **动态内容**：使用 Prism Region `WorkflowContentRegion` 加载不同步骤的View
3. **患者信息条**：通过 `PatientInfoBarVisible` 控制显示（Step 2-4可见）
4. **命令系统**：
   - `BackToHomeCommand` - 返回主页
   - `CancelCommand` - 取消流程
   - `SaveDraftCommand` - 保存草稿
   - `PreviousStepCommand` - 上一步
   - `NextStepCommand` - 下一步/完成

---

## 可行性评估

_（待需求确认后填写）_

---

## 实施方案

_（待需求确认后填写）_

---

## 变更历史

| 日期 | 问题 | 状态 | 决策 |
|------|------|------|------|
| 2025-10-21 | Q1：红色标注含义 | ❓待讨论 | - |
