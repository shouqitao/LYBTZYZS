# Proposal: unify-medicalcase-view-edit-pattern

## Summary

统一MedicalCaseWorkspaceView采用BaseDetailContainer的ViewContent/EditContent模式，并创建高完成度的药材编辑控件(HerbListEditor)供处方(Prescription)和验方(Formula)模块复用。

**核心目标**：
1. 医案界面重构 - 采用标准BaseDetailContainer模式
2. 药材编辑控件 - 创建可复用的HerbListEditor控件

## Problem Statement

### 当前现状分析

根据代码分析，当前项目中存在两种不同的查看/编辑模式切换架构：

**1. 标准模式（其他模块采用）**
- PatientDetailView、UserDetailView、HerbDetailView、FormulaDetailView
- 使用 `BaseDetailContainer` 容器控件
- 通过 `ViewContent` / `EditContent` 属性分离查看和编辑内容
- 统一的Header（返回+标题+编辑按钮）、Content、Footer（保存/取消）结构
- 代码简洁，模式统一

**2. 自定义模式（医案模块采用）**
- MedicalCaseWorkspaceView
- 使用 `MedicalCaseEditModeStateMachine` 自定义状态机
- 5:5分栏布局（诊断面板 + 处方面板）
- 复杂的按钮可见性规则（Clinical/Management模式 x 编辑/只读状态）
- 独立实现的Header（患者信息条）、Footer（操作栏）

### 问题

1. **架构不一致**：医案模块与其他模块的UI架构模式不统一，增加维护成本
2. **"分步骤"概念已模糊**：当前5:5布局表明诊断和处方已是并行编辑，而非顺序流程
3. **代码重复**：处方药材编辑器与验方药材编辑器结构高度相似，但各自独立实现
4. **查看模式缺失**：MedicalCaseWorkspaceView没有明确的只读查看模式内容定义

### 相似性分析：处方 vs 验方

| 特性 | PrescriptionPanelViewModel | FormulaDetailViewModel |
|------|---------------------------|------------------------|
| 基本信息 | TreatmentMethod, Usage, DosageCount | FormulaName, Category, Property, Effect, Usage |
| 药材列表 | HerbItems (PrescriptionHerbItemViewModel) | HerbItems (HerbItemViewModel) |
| 药材操作 | 添加/删除/修改剂量 | 添加/删除/修改剂量 |
| 价格计算 | SingleDosagePrice, TotalPrice | (无) |
| 导入功能 | FormulaImport, HistoryCopy | (无) |
| 显示控件 | HerbCardControl | HerbCardControl |

核心相同点：**药材列表的展示和编辑逻辑高度一致**

## Proposed Solution

### Phase 0: 扩展BaseDetailContainer支持分栏布局

**目标**：使BaseDetailContainer支持MedicalCase的5:5分栏特殊需求

**方案A - 扩展现有控件**（推荐）：
- 新增 `BaseSplitDetailContainer` 控件，继承BaseDetailContainer的设计理念
- 支持 `LeftViewContent` / `RightViewContent` / `LeftEditContent` / `RightEditContent`
- 保持Header和Footer结构与BaseDetailContainer一致

**方案B - 布局插槽**：
- 在BaseDetailContainer的ViewContent/EditContent中直接定义Grid分栏
- 优点：不需要新控件
- 缺点：失去统一抽象

### Phase 1: 重构MedicalCaseWorkspaceView

**目标**：采用BaseDetailContainer或BaseSplitDetailContainer模式

重构内容：
1. 使用容器控件统一Header结构
2. 定义ViewContent（只读查看模式）和EditContent（编辑模式）
3. 移除MedicalCaseEditModeStateMachine，使用BaseDetailContainer的IsEditMode
4. 简化按钮状态管理

**ViewContent示例布局**：
```
+--------------------------------------------------+
| [← 返回]        医案详情 - 张三      [编辑]      |
+--------------------------------------------------+
|  诊断信息 (只读)      |    处方信息 (只读)        |
|  - 主诉: TextBlock    |    - 药材列表 (HerbCard)  |
|  - 四诊: TextBlock    |    - 剂量/价格: TextBlock |
|  - 诊断: TextBlock    |    - 用法: TextBlock      |
+--------------------------------------------------+
```

**EditContent示例布局**：
```
+--------------------------------------------------+
| [← 返回]        编辑医案 - 张三                  |
+--------------------------------------------------+
|  诊断录入 (编辑)      |    处方编辑 (编辑)        |
|  - 主诉: TextBox      |    - 药材列表 (可编辑)    |
|  - 四诊: TextBox      |    - 添加/删除药材        |
|  - 诊断: TextBox      |    - 价格计算             |
+--------------------------------------------------+
| [取消]                          [保存] [完成看诊] |
+--------------------------------------------------+
```

### Phase 2: 创建HerbListEditor药材编辑控件 (必要)

**目标**：创建一个纯粹的药材列表容器控件，基于现有HerbCardControl组合封装，供处方和验方模块复用

**设计原则**：
- **单一职责** - HerbListEditor只负责药材列表的展示和编辑，不包含导入按钮、价格显示等
- **基于HerbCardControl** - 内部使用ItemsControl + HerbCardControl ItemTemplate
- **组合而非重写** - 不重复实现药材卡片逻辑，仅提供列表级别的管理功能

**HerbListEditor控件定位**：
- 是HerbCardControl的**纯粹列表容器**
- 内部使用 `ItemsControl` + `UniformGrid(Columns=4)` + `HerbCardControl`
- 导入按钮、价格汇总等放在外部由父级View控制

**HerbListEditor控件功能**：
1. **药材列表展示** - 一行4个药材，使用HerbCardControl展示
2. **药材添加** - 空白框输入药材名+剂量，回车确认
3. **药材删除** - 委托给HerbCardControl的删除命令
4. **剂量编辑** - 委托给HerbCardControl
5. **空白框管理** - 始终保持1个空白框在列表末尾
6. **模式切换** - IsEditMode传递给内部HerbCardControl

**UI展示规范**：
```
视觉展示：只显示药材名 + 剂量（简洁）
+------------------+------------------+------------------+------------------+
| 当归 10g         | 黄芪 15g         | 白术 12g         | 茯苓 10g         |
+------------------+------------------+------------------+------------------+
| 甘草 6g          | [空白输入框]     |                  |                  |
+------------------+------------------+------------------+------------------+

数据层面：HerbItemViewModel保留完整字段
- HerbName, Dosage（显示）
- HerbId, UnitPrice, SubTotal（计算用，不显示）
- HerbCategory, Property, Effect（后期预览功能预留）
```

**空白框管理策略**：
```
规则：
- 始终保持1个空白框在列表末尾（非4个）
- 空白框紧跟最后一个有效药材
- 删除药材后，后续药材自动向前补齐
- 回车确认后空白框自动后移并聚焦

示例（6种药材）：
HerbItems[0-5] = 药材1-6
HerbItems[6] = 空白（HerbName为空）

删除药材3后（5种药材）：
HerbItems[0-4] = 药材1,2,4,5,6（自动前移）
HerbItems[5] = 空白
```

**控件属性设计（简化版）**：
```csharp
public class HerbListEditor : UserControl
{
    // 数据绑定
    public IEnumerable HerbItems { get; set; }

    // 模式控制
    public bool IsEditMode { get; set; }

    // 命令（传递给内部HerbCardControl）
    public ICommand DeleteHerbCommand { get; set; }
    public ICommand DosageCompletedCommand { get; set; }
    public ICommand AddNewRowCommand { get; set; }
}
```

**使用示例**：
```xaml
<!-- 处方编辑 -->
<StackPanel>
    <!-- 导入按钮（外部） -->
    <StackPanel Orientation="Horizontal">
        <Button Content="导入验方" Command="{Binding ImportFormulaCommand}" />
        <Button Content="复制历史处方" Command="{Binding CopyHistoryCommand}" />
    </StackPanel>

    <!-- 药材列表（HerbListEditor） -->
    <components:HerbListEditor
        HerbItems="{Binding HerbItems}"
        IsEditMode="True"
        DeleteHerbCommand="{Binding DeleteHerbCommand}"
        DosageCompletedCommand="{Binding DosageCompletedCommand}"
        AddNewRowCommand="{Binding AddNewRowCommand}"/>

    <!-- 价格汇总（外部） -->
    <TextBlock Text="{Binding TotalPriceSummary}" />
</StackPanel>

<!-- 验方编辑 -->
<StackPanel>
    <!-- 导入按钮（外部） -->
    <StackPanel Orientation="Horizontal">
        <Button Content="导入验方" Command="{Binding ImportFormulaCommand}" />
        <Button Content="导入处方" Command="{Binding ImportPrescriptionCommand}" />
    </StackPanel>

    <!-- 药材列表（HerbListEditor） -->
    <components:HerbListEditor
        HerbItems="{Binding HerbItems}"
        IsEditMode="{Binding IsEditMode}"
        DeleteHerbCommand="{Binding DeleteHerbCommand}"
        DosageCompletedCommand="{Binding DosageCompletedCommand}"
        AddNewRowCommand="{Binding AddNewRowCommand}"/>
</StackPanel>
```

**导入功能设计**：

| 功能 | 处方编辑 | 验方编辑 | 说明 |
|------|----------|----------|------|
| 导入验方 | 需要 | 需要 | 从验方库选择，药材追加到列表 |
| 导入历史处方 | 需要 | 需要 | 从历史处方选择，药材追加到列表 |
| 清空药材 | 需要 | 需要 | 一键清空当前药材列表，方便重新组方 |

**导入验方对话框**（简单列表选择）：
- 验方不涉及患者，按名称/分类查询选择即可
- 复用现有验方选择对话框

**历史处方导入对话框**（两栏布局）：
```
┌─────────────────────────────────────────────────────────────────────┐
│  历史处方查询                                              [×]      │
├─────────────────────────────────────────────────────────────────────┤
│  查询: ○按患者 ○按诊断    [输入框]    [查询]                       │
├────────────────────────┬────────────────────────────────────────────┤
│                        │                                            │
│  【患者信息】          │  【处方详情】                              │
│  张三 · 男 · 45岁      │                                            │
│  138****1234           │  就诊日期: 2024-12-01                      │
│                        │  诊断: 风寒感冒                            │
│  ──────────────────    │                                            │
│                        │  药材清单:                                 │
│  【就诊记录】          │  麻黄 9g    桂枝 6g                        │
│  ☑ 2024-12-01         │  杏仁 10g   甘草 3g                        │
│  ☐ 2024-11-15         │  生姜 3片   大枣 4枚                       │
│  ☐ 2024-10-20         │                                            │
│  ☐ 2024-09-05         │  剂数: 7剂                                 │
│                        │                                            │
├────────────────────────┴────────────────────────────────────────────┤
│                                            [取消]  [导入选中处方]    │
└─────────────────────────────────────────────────────────────────────┘
```

**历史处方查询规则**：
| 设计项 | 确认结果 |
|-------|---------|
| 按患者查询 | 当前医生 + 指定患者的历史处方（默认当前患者） |
| 按诊断查询 | 当前医生 + TCMDiagnosis（辩证结果）匹配 |
| 结果排序 | 时间倒序（最新在前） |
| 导入行为 | 追加到现有药材列表 |
| 权限范围 | 仅当前医生的处方（可跨患者） |
| 多选支持 | 支持多选导入，合并药材 |

**处方编辑 vs 验方编辑的导入场景**：
| 场景 | 模块 | 操作流程 |
|------|------|---------|
| 复诊开方 | 处方编辑 | 复制历史处方 → 调整剂量 |
| 参考类似病例 | 处方编辑 | 按诊断查询 → 选择导入 |
| 组合多个单方 | 验方编辑 | 导入验方A → 导入验方B |
| 患者处方转验方 | 验方编辑 | 导入处方 → 调整 → 保存为验方 |

**价格显示策略**：

| 位置 | 是否显示价格 | 说明 |
|------|-------------|------|
| 药材卡片（HerbCardControl） | 不显示 | UI简洁 |
| 药材列表（HerbListEditor） | 不显示 | 只展示药材+剂量 |
| 处方面板底部 | 显示汇总 | 单剂价格 × 剂数 = 总价 |
| 打印单据 | 显示详细 | 单价、小计、总价 |
| 药材悬停预览 | 后期开发 | 显示单价、药性、功效等 |

**清理遗留代码**：
1. 删除 `LYBT.Desktop.MedicalCase/Controls/HerbCardControl.xaml`（已被共享版本替代）
2. 删除 `LYBT.Desktop.MedicalCase/Controls/HerbCardControl.xaml.cs`

## Impact

### 影响范围

**Phase 0** (准备工作):
- 修改: `LYBT.Desktop.Infrastructure/Views/BaseDetailContainer.xaml` - 新增FooterContent支持
- 修改: `LYBT.Desktop.Infrastructure/Views/BaseDetailContainer.xaml.cs` - 新增FooterContent依赖属性
- 删除: `LYBT.Desktop.MedicalCase/Controls/HerbCardControl.xaml` - 遗留控件清理
- 删除: `LYBT.Desktop.MedicalCase/Controls/HerbCardControl.xaml.cs` - 遗留控件清理

**Phase 1** (医案界面重构):
- 重构: `LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
- 重构: `LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- 可能删除: `MedicalCaseEditModeStateMachine.cs`（如果不再需要）

**Phase 2** (药材编辑控件):
- 新增: `LYBT.Desktop.Presentation/Components/HerbListEditor.xaml`
- 新增: `LYBT.Desktop.Presentation/Components/HerbListEditor.xaml.cs`
- 重构: `PrescriptionEditorPanel.xaml`（使用HerbListEditor）
- 重构: `FormulaDetailView.xaml`（使用HerbListEditor）

### 风险评估

- **中等风险**：MedicalCaseWorkspaceView是核心业务界面，重构需要充分测试
- **兼容性**：需要保持Clinical模式和Management模式的业务逻辑不变
- **测试**：现有ViewModel单元测试应保持通过

### 收益

1. **架构统一**：所有详情页面使用相同的容器模式
2. **维护性提升**：减少自定义状态机代码
3. **代码复用**：药材编辑器可在多处复用
4. **用户体验**：查看/编辑模式切换更加流畅和一致

## Alternatives Considered

### 替代方案1：保持现状

- 优点：无需修改，风险为零
- 缺点：架构不统一，维护成本高
- **决定：不采用**

### 替代方案2：仅重构View不提取共享控件

- 优点：工作量较小
- 缺点：错过复用机会
- **决定：可考虑作为分阶段实施**

### 替代方案3：使用BaseDetailContainer的Grid分栏（不新增控件）

- 优点：不引入新控件
- 缺点：分栏逻辑需要在每个View中重复
- **决定：视Phase 0评估结果决定**

## Dependencies

- 无外部依赖
- 依赖 `BaseDetailContainer` 已实现（已完成）
- 依赖 `HerbCardControl` 已存在（已完成）

## Success Criteria

1. MedicalCaseWorkspaceView使用BaseDetailContainer（ViewContent/EditContent模式）
2. 查看模式和编辑模式内容明确分离
3. 现有Clinical和Management模式功能完全保留
4. HerbListEditor控件被Prescription和Formula模块复用
5. 遗留HerbCardControl已清理（MedicalCase模块内）
6. 编译通过，无新增警告
7. 现有单元测试全部通过

## Implementation Notes

### Phase 0 决策记录

**评估日期**: 2025-12-09

**决策**: 采用方案B - 直接在ViewContent中使用Grid分栏

**理由**:
1. MedicalCase的5:5分栏是业务特定需求，不是其他模块会复用的通用模式
2. BaseDetailContainer的ViewContent/EditContent是ContentPresenter，可放置任意内容包括Grid分栏
3. 避免新增控件增加维护成本（仅MedicalCase使用）
4. PrescriptionEditorPanel已使用共享的`LYBT.Desktop.Presentation.Components.HerbCardControl`

**附加决策**: 扩展BaseDetailContainer支持自定义FooterContent
- 原因: MedicalCase的Footer比标准的"保存/取消"复杂，包含备注输入框、修改原因、多个按钮
- 实现: 新增`FooterContent`依赖属性，允许完全自定义Footer内容

**待清理代码**:
- `LYBT.Desktop.MedicalCase/Controls/HerbCardControl.xaml` - 遗留控件，已被共享版本替代
- `LYBT.Desktop.MedicalCase/Controls/HerbCardControl.xaml.cs` - 遗留控件代码

---

### 注意事项

1. **按钮状态复杂性**：MedicalCase有多种按钮状态组合（保存/暂存/完成看诊/打印），需要在Footer中妥善处理
2. **患者信息条**：考虑保留在Header区域或作为独立组件
3. **备注和修改原因**：当前在Footer中，需要考虑放置位置
4. **渐进式重构**：建议先实现View层重构，再评估是否提取共享控件

### 建议实施顺序

1. 先评估是否需要BaseSplitDetailContainer或直接在ViewContent中分栏
2. 创建MedicalCaseWorkspaceView的ViewContent只读版本
3. 调整EditContent为现有编辑内容
4. 测试Clinical和Management模式
5. 评估Phase 2的必要性
