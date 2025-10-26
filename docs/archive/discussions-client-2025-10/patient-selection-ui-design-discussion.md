# PatientSelectionView UI 设计讨论

> **文档状态**: 🔄 讨论中
> **创建时间**: 2025-01-21
> **相关Issue**: TBD
> **讨论参与**: 用户 + Claude Code

---

## 📋 讨论背景

PatientSelectionView 是医案流程（MedicalCaseFlow）的 Step 1，负责为后续诊断确认患者身份。当前需要优化布局，支持两个患者来源：
1. **待看诊列表**（未来挂号模块实现后启用）
2. **全库搜索**（现有功能）

---

## 🎯 设计目标

### 核心需求
- 左右分栏布局：左侧患者信息 + 待看诊列表，右侧搜索 + 结果列表
- 高度对齐：待看诊列表高度 = 搜索区高度
- 统一选择：两个患者来源共享同一个 `SelectedPatient` 绑定
- 简化信息：患者信息显示 5 个核心字段

---

## 📐 布局结构（已确认）

### 整体：两列布局

```
┌─────────────────────────────────────────────────────────────┐
│                   PatientSelectionView                      │
├──────────────────────┬──────────────────────────────────────┤
│ 左列（固定宽度 320px）│ 右列（自适应宽度 *）                 │
├──────────────────────┼──────────────────────────────────────┤
│ 📋 患者信息          │ 🔍 搜索患者（支持姓名/拼音码/手机号）│
│ ─────────────────    │ ─────────────────────────────────    │
│ 姓名：李四           │ [搜索框] [搜索] [新建患者]          │
│ 年龄：45岁           │                                      │
│ 手机：138****5678    │ (高度 120px，与左侧待看诊列表对齐)   │
│ 地址：XX省XX市...    ├──────────────────────────────────────┤
│ 身份证：3401****1234 │ 姓名  性别  年龄  手机号  最近就诊   │
│                      │ ────────────────────────────────────│
│ (Height="*" 占剩余)  │ [搜索结果 DataGrid]                 │
│                      │                                      │
│ [下一步（进入诊断）] │ (Height="*" 占据剩余空间)           │
├──────────────────────┼──────────────────────────────────────┤
│ 👥 待看诊患者 (0人)  │ 共 50 条  第 1/10 页 [上一页][下一页]│
│ ─────────────────    │                                      │
│ [待看诊列表 DataGrid]│ (Height="Auto" 自适应)              │
│                      │                                      │
│ (Height=120px 固定)  │                                      │
└──────────────────────┴──────────────────────────────────────┘
```

---

## ✅ 已确认的设计决策

### 1. 布局尺寸
- **左列宽度**: `Width="320"` (固定)
- **右列宽度**: `Width="*"` (自适应)
- **高度对齐**:
  - 左列待看诊列表: `Height="120"`
  - 右列搜索区: `Height="120"`

### 2. 患者信息显示内容（简化版）
| 字段 | 显示格式 | 说明 |
|------|---------|------|
| 姓名 | `李四` | 大字体、加粗 |
| 年龄 | `45岁` | 标准字体 |
| 手机号 | `138****5678` | 中间4位脱敏 |
| 地址 | `XX省XX市XX区XX街道...` | 支持换行 |
| 身份证号 | `3401**********1234` | 前6位+星号+后4位 |

**移除字段**（与原讨论相比）:
- ❌ 性别
- ❌ 过敏史
- ❌ 拼音码
- ❌ 最近就诊

### 3. 选择行为（统一性）
- 两个患者来源（待看诊列表 + 搜索结果）共享 `SelectedPatient` 绑定
- 从任一列表选中患者 → 左上角患者信息区自动更新
- 双击患者行 → 等同于点击"下一步"按钮，直接进入 Step 2

### 4. 数据模型策略（倾向方案A - 最小侵入）
**决策**: 待看诊列表暂时复用 `PatientDto`
- 挂号模块未实现前，待看诊列表为空
- XAML 中的 `RegistrationTime` 和 `Status` 列绑定为空或默认值
- 未来挂号模块实现时再调整数据契约

**理由**:
- 遵循 YAGNI 原则（You Aren't Gonna Need It）
- 不改动现有 `PatientDto` 契约，零风险
- MVP 阶段避免提前设计

---

## 🔄 待讨论的设计点

### Q1: 身份证脱敏实现方式

**方案A: ViewModel 提供脱敏属性**
- 在 `PatientDto` 中添加 `MaskedIdNumber` 属性
- 优点: XAML 绑定简单
- 缺点: 修改 DTO 契约，增加冗余字段

**方案B: XAML 使用 ValueConverter**
- 创建 `IdNumberMaskConverter`
- 绑定: `{Binding SelectedPatient.IdNumber, Converter={StaticResource IdNumberMaskConverter}}`
- 优点: 不修改 DTO，脱敏逻辑封装在 View 层，可复用
- 缺点: 需要额外创建 Converter 类

**倾向**: 方案B（Converter）

---

### Q2: 待看诊列表列设计

由于高度固定（120px），列表可能只显示 2-3 行数据，列需要精简：

**候选列**:
- ✅ 姓名（80px）
- ✅ 性别（50px）
- ✅ 年龄（50px）
- ✅ 挂号时间（80px，格式 `HH:mm`）
- ❓ 状态（60px，如"待诊"/"就诊中"）

**问题**: 是否需要"状态"列？如果需要，可以用颜色区分：
- 待诊: 橙色背景
- 就诊中: 绿色背景

---

### Q3: "下一步"按钮位置

**方案A: 患者信息区底部（左列内）**
```
┌─────────────────┐
│ 📋 患者信息      │
│ 姓名：李四       │
│ ...             │
│ [下一步按钮]    │ ← 位置A
└─────────────────┘
```
- 优点: 患者信息和操作按钮逻辑关联，左列形成"选择-确认"闭环
- 缺点: 按钮可能不够醒目

**方案B: 整个界面底部（独立行）**
```
┌─────────────────┬─────────────┐
│ 患者信息         │ 搜索结果     │
├─────────────────┴─────────────┤
│     [下一步（进入诊断）]       │ ← 位置B
└───────────────────────────────┘
```
- 优点: 按钮醒目，符合传统表单提交习惯
- 缺点: 占据额外空间，破坏左右列的独立性

**倾向**: 方案A

---

### Q4: 手机号脱敏逻辑

当前设计中患者信息显示"手机号"，是否需要脱敏？
- **完整显示**: `13812345678`（医生需要完整号码联系患者）
- **脱敏显示**: `138****5678`（保护隐私）

**问题**: 医生在诊断过程中是否需要查看完整手机号？

---

## 📝 实施注意事项

### ViewModel 需要添加的属性
```csharp
// 待看诊患者列表（挂号模块未实现前为空）
public ObservableCollection<PatientDto> WaitingPatients { get; } = new();

// 是否已选中患者（用于UI绑定）
public bool HasSelectedPatient => SelectedPatient != null;
```

### ViewModel 需要修改的逻辑
```csharp
// SelectedPatient setter 需要触发 HasSelectedPatient 变化通知
public PatientDto? SelectedPatient
{
    set
    {
        if (SetProperty(ref _selectedPatient, value))
        {
            RaisePropertyChanged(nameof(HasSelectedPatient)); // ⭐ 新增
            SelectPatientCommand.RaiseCanExecuteChanged();
        }
    }
}
```

### 需要创建的资源
- `IdNumberMaskConverter` (如果采用方案B)
- `PhoneNumberMaskConverter` (如果手机号需要脱敏)

---

## 🚧 后续计划

1. **Phase 1: 完成设计讨论**（当前阶段）
   - 确认待讨论的 4 个设计点（Q1-Q4）
   - 形成最终设计方案

2. **Phase 2: 创建 Issue**
   - 基于确认的设计方案创建 GitHub Issue
   - 明确验收标准和实施范围

3. **Phase 3: XAML 实现**
   - 修改 `PatientSelectionView.xaml`
   - 创建必要的 Converter 资源

4. **Phase 4: ViewModel 实现**
   - 修改 `PatientSelectionViewModel.cs`
   - 添加 `WaitingPatients` 属性和 `HasSelectedPatient` 属性

5. **Phase 5: 测试验证**
   - 编译通过（0 errors, 0 warnings）
   - UI 布局正确显示
   - 选择行为符合预期

---

## 📚 相关文档

- `docs/architecture/client/README.md` - Client端架构总览
- `docs/architecture/client/ui-standards.md` - UI设计规范
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml` - 父视图
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs` - ViewModel

---

## 📅 讨论历史

| 日期 | 讨论内容 | 决策 |
|------|---------|------|
| 2025-01-21 | 初始布局方向讨论 | 确认两列布局、高度对齐要求 |
| 2025-01-21 | 患者信息显示内容 | 简化为5个核心字段 |
| 2025-01-21 | 数据模型策略 | 倾向最小侵入方案（复用PatientDto） |
| 2025-01-21 | 讨论暂停，转入文档 | 待完成角色重构讨论后继续 |
