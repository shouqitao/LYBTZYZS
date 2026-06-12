# 医案工作台 UI 交互优化方案

> **文档版本**: v1.0  
> **创建日期**: 2026-04-10  
> **最后更新**: 2026-04-10  
> **状态**: 待评审  
> **目标**: 使 UI 交互更符合中医看诊逻辑，提高看诊效率

---

## 一、优化目标

### 1.1 核心原则

| 原则 | 说明 | 设计依据 |
|------|------|----------|
| **符合真实看诊流程** | UI 布局映射中医"望闻问切"诊疗顺序 | clinical-workflow.md Section 2.3 |
| **减少操作步骤** | 高频操作一键可达，避免多层弹窗 | 医生平均看诊时间 15-20 分钟/患者 |
| **信息分层展示** | 核心信息优先，次要信息可展开 | 屏幕空间有限 (1920x1080) |
| **实时反馈** | 输入即时验证，价格实时计算 | 避免提交后才发现错误 |
| **防错设计** | 关键操作二次确认，危险操作禁用而非隐藏 | BR-002/BR-003 校验前置 |

### 1.2 优化范围

| 优化项 | 当前状态 | 优化目标 | 优先级 |
|--------|----------|----------|--------|
| 诊断区布局 | 3 行平铺 | 按"望闻问切"分组 | P0 |
| 处方区交互 | 药材列表 + 底部信息栏 | 分步引导 + 实时计算 | P0 |
| 底部操作栏 | 缺失 | 场景化按钮 (Clinical/Management) | P0 |
| 验证反馈 | 仅中医诊断有验证 | 全字段验证 + 即时提示 | P1 |
| 处方决策引导 | 无 | 明确"是否需要处方"决策点 | P1 |
| 患者信息展示 | Full 模式有，Compact 模式无 | 统一患者信息条 | P1 |
| 快捷操作 | 工具条按钮 | 键盘快捷键 + 右键菜单 | P2 |
| 历史参考 | 弹窗选择 | 侧边栏快速参考 | P2 |

---

## 二、诊断区优化（望闻问切分组）

### 2.1 当前问题

**现状**：诊断区 3 行平铺布局
```
现病史 (整行)
舌诊 (左) | 脉诊 (右)
中医诊断* (整行)
```

**问题**：
1. ❌ 不符合中医"望闻问切"诊疗顺序
2. ❌ 字段间逻辑关系不清晰（舌诊/脉诊属于"望/切"，现病史属于"问"）
3. ❌ 中医辨证（核心必填）与辅助字段混排，视觉优先级不够

### 2.2 优化方案

#### 方案 A：分组折叠面板（推荐）

```
┌──────────────────────────────────────────────────────┐
│ 📋 诊断信息                                           │
├──────────────────────────────────────────────────────┤
│ ▼ 四诊信息 (可选)                      [展开/折叠]    │
│ ┌────────────────────────────────────────────────┐   │
│ │ 现病史 (问)                                     │   │
│ │ ┌────────────────────────────────────────────┐ │   │
│ │ │ 多行文本框，支持语音输入                      │ │   │
│ │ └────────────────────────────────────────────┘ │   │
│ │                                                 │   │
│ │ 舌诊 (望)              脉诊 (切)                 │   │
│ │ ┌──────────────────┐  ┌──────────────────┐     │   │
│ │ │ 文本框            │  │ 文本框            │     │   │
│ │ │ [常用舌象▼]      │  │ [常用脉象▼]      │     │   │
│ │ └──────────────────┘  └──────────────────┘     │   │
│ └────────────────────────────────────────────────┘   │
│                                                       │
│ ▼ 中医辨证 (必填) *                    [展开/折叠]    │
│ ┌────────────────────────────────────────────────┐   │
│ │ 中医诊断                                        │   │
│ │ ┌────────────────────────────────────────────┐ │   │
│ │ │ 文本框 (ValidatingTextBoxStyle)             │ │   │
│ │ │ [常用证型▼]                                 │ │   │
│ │ └────────────────────────────────────────────┘ │   │
│ └────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────┘
```

**关键改进**：
1. ✅ **分组映射诊疗逻辑**：
   - "四诊信息"组：现病史 (问) + 舌诊 (望) + 脉诊 (切)
   - "中医辨证"组：核心诊断结论 (必填)
2. ✅ **折叠面板**：默认展开"中医辨证"（必填），"四诊信息"可折叠（可选）
3. ✅ **常用词快捷选择**：
   - 舌诊：淡红舌/红舌/暗红舌/紫暗舌/胖大舌/瘦薄舌...
   - 脉象：浮脉/沉脉/迟脉/数脉/滑脉/涩脉/弦脉/紧脉...
   - 证型：风寒束表证/风热犯肺证/肝郁气滞证/脾胃虚弱证...
4. ✅ **视觉优先级**：必填区用红色 `*` 标记，边框高亮

#### 方案 B：步骤引导式（适合新手医生）

```
步骤 1/3: 问诊 → 步骤 2/3: 望切诊 → 步骤 3/3: 辨证
```

**适用场景**：实习医生/规培医生培训模式
**不推荐理由**：资深医生会觉得步骤繁琐，降低效率

### 2.3 技术实现

**文件修改**：
- `MedicalCaseEditControl.xaml`：
  - 将诊断区从 3 行 Grid 改为 Expander 分组
  - 添加常用词 ComboBox（数据源从配置/字典加载）
- `MedicalCaseEditControl.xaml.cs`：
  - 添加常用词列表 DependencyProperty
- `MedicalCaseDetailModel.cs`：
  - 添加常用词配置属性（或从全局配置加载）

**常用词数据源**：
```csharp
// 方案 1: 硬编码（快速实现）
public static class TcmCommonTerms
{
    public static string[] TongueDiagnoses = { "淡红舌", "红舌", "暗红舌", "紫暗舌", ... };
    public static string[] PulseDiagnoses = { "浮脉", "沉脉", "迟脉", "数脉", ... };
    public static string[] TcmDiagnoses = { "风寒束表证", "风热犯肺证", ... };
}

// 方案 2: 从数据库加载（长期方案）
// 通过 ISystemConfigApi.GetTcmTermsAsync() 获取
```

---

## 三、处方区优化（分步引导 + 实时计算）

### 3.1 当前问题

**现状**：药材列表 + 底部信息栏（剂数/用法/总价）

**问题**：
1. ❌ 无"是否需要处方"决策引导（PRD 要求 NeedsPrescription: true/false/null）
2. ❌ 价格计算不实时（药材变更后需手动触发）
3. ❌ 剂数/用法/折扣分散在底部，与处方核心操作距离远
4. ❌ 无处方完整性提示（完成看诊时 BR-003 校验可能失败）

### 3.2 优化方案

#### 3.2.1 处方决策引导

在处方区顶部添加决策栏：

```
┌──────────────────────────────────────────────────────┐
│ 💊 处方开具                                           │
├──────────────────────────────────────────────────────┤
│ 是否需要开具处方？                                     │
│ ○ 需要处方  ○ 不需要处方  ○ 稍后决定                   │
│                                                       │
│ [选择"需要处方"后展开下方药材编辑区]                     │
└──────────────────────────────────────────────────────┘
```

**交互逻辑**：
- 选择"需要处方" → 展开药材编辑区
- 选择"不需要处方" → 折叠药材编辑区，显示"本医案不开具处方"
- 选择"稍后决定" → 保持折叠，完成看诊时提示 BR-003 校验失败

**技术实现**：
- 添加 `NeedsPrescription` DependencyProperty (enum: True/False/Null)
- 绑定到 `MedicalCaseDetailModel.NeedsPrescription`
- 药材编辑区 `Visibility` 绑定到 `NeedsPrescription == True`

#### 3.2.2 药材编辑区重构

```
┌──────────────────────────────────────────────────────┐
│ 处方药材                              [套验方] [历史]  │
├──────────────────────────────────────────────────────┤
│ 药名      剂量    煎法      单价      小计      操作    │
│ ┌─────┐  ┌────┐  ┌──────┐  ┌──────┐  ┌──────┐  ┌───┐ │
│ │黄芪  │  │30g │  │常规  │  │0.12  │  │3.60  │  │×  │ │
│ └─────┘  └────┘  └──────┘  └──────┘  └──────┘  └───┘ │
│ ┌─────┐  ┌────┐  ┌──────┐  ┌──────┐  ┌──────┐  ┌───┐ │
│ │当归  │  │15g │  │后下  │  │0.25  │  │3.75  │  │×  │ │
│ └─────┘  └────┘  └──────┘  └──────┘  └──────┘  └───┘ │
│                                                       │
│ [+ 添加药材]                                          │
├──────────────────────────────────────────────────────┤
│ 处方配置                                              │
│ ┌────────────────────────────────────────────────┐   │
│ │ 剂数: [7] 剂    用法: [水煎服▼]  折扣: [1.0]    │   │
│ │                                                 │   │
│ │ 单剂价: ¥45.00    总价: ¥315.00                 │   │
│ └────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────┘
```

**关键改进**：
1. ✅ **处方配置集成**：剂数/用法/折扣与药材列表在同一区域，减少视线移动
2. ✅ **实时价格计算**：
   - 药材剂量变更 → 小计即时更新 → 单剂价/总价即时更新
   - 剂数/折扣变更 → 总价即时更新
3. ✅ **操作列**：每行药材右侧添加删除按钮（×），避免右键菜单
4. ✅ **快捷添加**：底部"+ 添加药材"按钮，支持键盘 Enter 快速添加

#### 3.2.3 价格计算逻辑

```csharp
// 在 PrescriptionEditorViewModel 中
public void RecalculatePrices()
{
    // 单剂价 = SUM(剂量 × 单价)
    SingleDosePrice = Items.Sum(x => x.Dosage * x.UnitPrice);
    
    // 总价 = 单剂价 × 剂数 × 折扣
    TotalPrice = SingleDosePrice * DosageCount * Discount;
    
    // 触发 PropertyChanged
    OnPropertyChanged(nameof(SingleDosePrice));
    OnPropertyChanged(nameof(TotalPrice));
}

// 监听药材集合变更
private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
{
    RecalculatePrices();
}

// 监听剂量/单价变更
private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName is nameof(PrescriptionItemDto.Dosage) or nameof(PrescriptionItemDto.UnitPrice))
    {
        RecalculatePrices();
    }
}
```

### 3.3 处方完整性提示

在处方区底部添加完整性检查：

```
┌──────────────────────────────────────────────────────┐
│ ✅ 处方完整性检查                                      │
│ ✓ 中医诊断已填写                                       │
│ ✓ 处方需求已标记 (需要)                                │
│ ✓ 处方药材 2 味                                        │
│ ✓ 帖数 7 剂                                            │
│                                                       │
│ 可以完成看诊                                           │
└──────────────────────────────────────────────────────┘
```

或校验失败时：

```
┌──────────────────────────────────────────────────────┐
│ ⚠️ 处方完整性检查                                      │
│ ✗ 中医诊断未填写 (必填)                                │
│ ✓ 处方需求已标记 (需要)                                │
│ ✓ 处方药材 2 味                                        │
│ ✗ 帖数未填写 (必填)                                    │
│                                                       │
│ 完成看诊前需解决以上问题                                │
└──────────────────────────────────────────────────────┘
```

**技术实现**：
- 添加 `PrescriptionCompletenessChecker` 类
- 监听相关属性变更，实时更新检查结果
- XAML 绑定到检查结果集合

---

## 四、底部操作栏优化（场景化按钮）

### 4.1 当前问题

**现状**：无统一底部操作栏，按钮分散在工具条

**问题**：
1. ❌ Clinical/Management 模式按钮未区分
2. ❌ 无 BR-002 离开决策入口
3. ❌ 打印按钮位置不固定

### 4.2 优化方案

#### 4.2.1 Clinical 模式底部按钮

```
┌──────────────────────────────────────────────────────┐
│ [挂起医案]          [打印处方笺]        [完成看诊]      │
│  (保存并退出)       (预览并打印)       (校验并完成)     │
└──────────────────────────────────────────────────────┘
```

**按钮行为**：
| 按钮 | 行为 | 校验 | 后续 |
|------|------|------|------|
| 挂起医案 | 保存当前数据，状态→Suspended | 无强制校验 | 返回患者选择 |
| 打印处方笺 | 打开打印预览，确认后打印 | 需有处方药材 | 继续编辑 |
| 完成看诊 | 执行 BR-003 校验，状态→Completed | 完整校验 | 返回患者选择 |

#### 4.2.2 Management 模式底部按钮

**ReadOnly 状态**：
```
┌──────────────────────────────────────────────────────┐
│ [编辑医案]          [打印处方笺]        [返回列表]      │
└──────────────────────────────────────────────────────┘
```

**Editing 状态**：
```
┌──────────────────────────────────────────────────────┐
│ [保存医案]          [取消编辑]          [打印处方笺]    │
│  (校验并保存)       (放弃变更)         (预览并打印)     │
└──────────────────────────────────────────────────────┘
```

### 4.3 技术实现

**文件修改**：
- `MedicalCaseMasterDetailControl.xaml`：
  - 添加底部按钮区域（DockPanel.Bottom 或 Grid 最后一行）
  - 按钮 Visibility 绑定到 `WorkspaceMode` 和 `EditMode`
- `MedicalCaseCommandsViewModel.cs`：
  - 已有命令：SaveCommand, SuspendCommand, CompleteCommand, PrintCommand
  - 需添加：EnterEditModeCommand, CancelEditCommand, ReturnToListCommand

---

## 五、患者信息条优化（统一展示）

### 5.1 当前问题

**现状**：
- Full 模式：患者信息在顶部 InfoCard（患者姓名/就诊日期/接诊医生/状态）
- Compact 模式：无患者信息展示

**问题**：
1. ❌ Compact 模式下医生不知道当前在看哪位患者
2. ❌ 患者信息与诊断/处方区距离远，需滚动查看

### 5.2 优化方案

在 EditControl 顶部添加固定患者信息条（Compact/Full 模式共用）：

```
┌──────────────────────────────────────────────────────┐
│ 👤 张三  |  男  |  45 岁  |  MC20260410001  |  🟢 进行中│
├──────────────────────────────────────────────────────┤
│ [诊断区]                                              │
│ [处方区]                                              │
│ [底部操作栏]                                          │
└──────────────────────────────────────────────────────┘
```

**信息项**：
- 患者姓名（加粗）
- 性别
- 年龄（实时计算）
- 医案编号
- 状态（彩色圆点 + 文字）

**技术实现**：
- 在 `MedicalCaseEditControl.xaml` 顶部添加患者信息条
- Full/Compact 模式共用此信息条
- 从 `MedicalCaseDetailModel` 绑定数据

---

## 六、验证框架补全

### 6.1 当前验证覆盖

| 字段 | 当前状态 | 优化后 |
|------|----------|--------|
| 中医诊断 | ✅ 必填验证 | ✅ 保持不变 |
| 现病史 | ❌ 无验证 | ⚠️ 可选，但超过 500 字提示 |
| 舌诊 | ❌ 无验证 | ⚠️ 可选，但超过 100 字提示 |
| 脉诊 | ❌ 无验证 | ⚠️ 可选，但超过 100 字提示 |
| 剂数 | ❌ 无验证 | ✅ 必填，1-99 整数 |
| 用法 | ❌ 无验证 | ✅ 必填（当 NeedsPrescription=true） |
| 备注 | ❌ 无验证 | ⚠️ 可选，超过 500 字提示 |

### 6.2 验证规则定义

```csharp
public static class MedicalCaseValidationRules
{
    // 中医诊断：必填
    public static ValidationRule TcmDiagnosisRequired = new()
    {
        Property = nameof(ConsultationItem.TcmDiagnosis),
        Rule = "Required",
        Message = "中医诊断为必填项",
        Severity = ValidationSeverity.Error
    };

    // 剂数：必填，1-99 整数
    public static ValidationRule DosageCountRequired = new()
    {
        Property = nameof(PrescriptionItem.DosageCount),
        Rule = "Range(1, 99)",
        Message = "剂数必须为 1-99 的整数",
        Severity = ValidationSeverity.Error
    };

    // 用法：当 NeedsPrescription=true 时必填
    public static ValidationRule UsageRequiredWhenPrescription = new()
    {
        Property = nameof(PrescriptionItem.Usage),
        Rule = "RequiredWhen(NeedsPrescription == true)",
        Message = "开具处方时用法为必填项",
        Severity = ValidationSeverity.Error
    };

    // 现病史：可选，长度限制
    public static ValidationRule PresentIllnessMaxLength = new()
    {
        Property = nameof(ConsultationItem.PresentIllness),
        Rule = "MaxLength(500)",
        Message = "现病史超过 500 字，建议精简",
        Severity = ValidationSeverity.Warning
    };

    // 舌诊/脉诊：可选，长度限制
    public static ValidationRule TongueDiagnosisMaxLength = new()
    {
        Property = nameof(ConsultationItem.TongueDiagnosis),
        Rule = "MaxLength(100)",
        Message = "舌诊超过 100 字，建议精简",
        Severity = ValidationSeverity.Warning
    };
}
```

### 6.3 XAML 验证样式应用

```xaml
<!-- 中医诊断（必填） -->
<TextBox Text="{Binding Consultation.TcmDiagnosis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}"
         Style="{DynamicResource ValidatingTextBoxStyle}"/>
<TextBlock Text="{Binding ErrorsSource[TcmDiagnosis]}"
           Style="{DynamicResource ValidationErrorMessageVisibleStyle}"/>

<!-- 剂数（必填） -->
<TextBox Text="{Binding Prescription.DosageCount, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}"
         Style="{DynamicResource ValidatingTextBoxStyle}"/>
<TextBlock Text="{Binding ErrorsSource[DosageCount]}"
           Style="{DynamicResource ValidationErrorMessageVisibleStyle}"/>

<!-- 用法（条件必填） -->
<ComboBox SelectedItem="{Binding Prescription.Usage, Mode=TwoWay, ValidatesOnNotifyDataErrors=True}"
          Style="{DynamicResource ValidatingComboBoxStyle}"/>
```

---

## 七、快捷操作优化

### 7.1 键盘快捷键

| 快捷键 | 功能 | 场景 |
|--------|------|------|
| `Ctrl+S` | 保存医案 | Clinical/Management |
| `Ctrl+P` | 打印处方笺 | Clinical/Management |
| `Ctrl+Enter` | 完成看诊 | Clinical |
| `Ctrl+H` | 挂起医案 | Clinical |
| `Ctrl+E` | 切换到编辑模式 | Management (ReadOnly) |
| `Esc` | 取消编辑/返回列表 | Management (Editing) |
| `F1` | 打开帮助 | 全局 |

### 7.2 右键菜单

在药材列表行添加右键菜单：

```
右键点击药材行 →
├─ 复制药材信息
├─ 删除药材
├─ 上移/下移
└─ 查看药材详情（弹窗显示性味归经、功效主治）
```

### 7.3 拖拽排序

支持药材行拖拽排序：
- 鼠标按住药材行左侧拖拽手柄
- 拖到目标位置释放
- 自动更新药材顺序

---

## 八、历史参考优化

### 8.1 当前问题

**现状**：历史处方复制需打开弹窗（HistoryCopyDialog），选择后关闭弹窗

**问题**：
1. ❌ 弹窗遮挡编辑区，无法对照参考
2. ❌ 选择后需关闭弹窗才能继续编辑
3. ❌ 无法同时参考多个历史医案

### 8.2 优化方案

添加侧边栏历史参考模式：

```
┌──────────────────────┬───────────────────────────────┐
│ 📚 历史医案参考       │  诊断区 + 处方区               │
│                      │                                │
│ [搜索] [清除]         │  [诊断表单]                     │
│                      │  [处方编辑]                     │
│ 2026-04-01  Completed│                                │
│ 中医诊断：风寒束表证   │                                │
│ 药材：黄芪 30g, ...   │                                │
│ [导入处方] [查看详情]  │                                │
│                      │                                │
│ 2026-03-15  Completed│                                │
│ 中医诊断：风热犯肺证   │                                │
│ 药材：金银花 15g, ... │                                │
│ [导入处方] [查看详情]  │                                │
└──────────────────────┴───────────────────────────────┘
```

**交互逻辑**：
- 点击"历史参考"按钮 → 右侧滑出侧边栏
- 侧边栏显示患者历史 Completed 医案列表
- 点击"导入处方" → 药材添加到当前处方
- 点击"查看详情" → 弹窗显示完整医案
- 点击外部区域 → 侧边栏收起

**技术实现**：
- 添加 `HistoryReferencePanel` UserControl
- 通过 `SlideInAnimation` 实现侧边栏滑入效果
- 数据源：`IMedicalCaseApi.GetPatientHistoryAsync(patientId)`

---

## 九、实施优先级与工作量

### 9.1 优先级矩阵

| 优化项 | 看诊效率提升 | 实现难度 | 优先级 | 预计工作量 |
|--------|--------------|----------|--------|------------|
| 诊断区分组折叠 | 🟡 中 | 🟢 低 | P0 | 2 小时 |
| 处方决策引导 | 🔴 高 | 🟢 低 | P0 | 1 小时 |
| 底部操作栏 | 🔴 高 | 🟡 中 | P0 | 3 小时 |
| 患者信息条 | 🔴 高 | 🟢 低 | P0 | 1 小时 |
| 实时价格计算 | 🔴 高 | 🟡 中 | P0 | 2 小时 |
| 验证框架补全 | 🟡 中 | 🟡 中 | P1 | 4 小时 |
| 处方完整性提示 | 🟡 中 | 🟡 中 | P1 | 2 小时 |
| 键盘快捷键 | 🟡 中 | 🟢 低 | P1 | 2 小时 |
| 常用词快捷选择 | 🟡 中 | 🟢 低 | P1 | 3 小时 |
| 侧边栏历史参考 | 🟡 中 | 🔴 高 | P2 | 1 天 |
| 拖拽排序 | 🟢 低 | 🔴 高 | P2 | 0.5 天 |

### 9.2 实施阶段

#### 阶段 1：核心体验优化（P0，预计 9 小时）

1. ✅ 添加患者信息条（Full/Compact 共用）
2. ✅ 诊断区分组折叠（Expander）
3. ✅ 处方决策引导（NeedsPrescription 选择）
4. ✅ 底部操作栏（Clinical/Management 按钮）
5. ✅ 实时价格计算（PrescriptionEditorViewModel）

#### 阶段 2：验证与提示（P1，预计 11 小时）

6. 验证框架补全（所有字段）
7. 处方完整性提示
8. 键盘快捷键
9. 常用词快捷选择

#### 阶段 3：高级功能（P2，预计 1.5 天）

10. 侧边栏历史参考
11. 拖拽排序
12. 右键菜单

---

## 十、风险评估

### 10.1 技术风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Expander 折叠导致布局错乱 | 🟡 中 | 使用 Grid 固定行高，避免 Auto |
| 实时价格计算性能问题 | 🟢 低 | 药材列表通常<30 味，计算量小 |
| 快捷键与系统冲突 | 🟢 低 | 避免使用系统保留快捷键 |
| 侧边栏动画卡顿 | 🟡 中 | 使用 RenderTransform 而非 LayoutTransform |

### 10.2 用户体验风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 折叠面板增加点击次数 | 🟡 中 | 默认展开必填区，可选区折叠 |
| 决策引导增加操作步骤 | 🟡 中 | 提供默认选项（需要处方） |
| 侧边栏遮挡编辑区 | 🟡 中 | 侧边栏宽度可调整，最大 400px |

---

## 十一、测试策略

### 11.1 单元测试

| 测试项 | 测试内容 |
|--------|----------|
| 价格计算 | 剂量/单价/剂数/折扣变更时价格正确 |
| 验证规则 | 必填/范围/条件验证正确触发 |
| 状态切换 | Clinical/Management 模式按钮正确显示 |
| 折叠面板 | 展开/折叠状态正确保存 |

### 11.2 UI 测试

| 测试项 | 测试方法 |
|--------|----------|
| 布局验证 | 1920x1080 / 1366x768 分辨率下无截断 |
| Tab 导航 | TabIndex 顺序符合诊疗流程 |
| 快捷键 | 所有快捷键功能正常 |
| 动画效果 | 侧边栏滑入/滑出流畅无卡顿 |

### 11.3 集成测试

| 测试项 | 测试方法 |
|--------|----------|
| 完整看诊流程 | 患者选择→诊断→处方→保存→打印→完成 |
| BR-003 校验 | 缺失必填项时完成看诊失败 |
| BR-002 离开决策 | 有未保存变更时弹窗正确 |

---

## 十二、附录

### 附录 A: 中医常用术语参考

#### 舌诊常用词

| 分类 | 术语 |
|------|------|
| 舌色 | 淡红舌、红舌、暗红舌、紫暗舌、青紫舌 |
| 舌形 | 胖大舌、瘦薄舌、老舌、嫩舌、裂纹舌、齿痕舌 |
| 舌苔 | 薄白苔、白厚苔、黄苔、黄腻苔、灰黑苔、无苔 |

#### 脉象常用词

| 分类 | 术语 |
|------|------|
| 浮沉 | 浮脉、沉脉、伏脉 |
| 迟数 | 迟脉、数脉、疾脉 |
| 虚实 | 虚脉、实脉、弱脉、细脉 |
| 滑涩 | 滑脉、涩脉 |
| 弦紧 | 弦脉、紧脉、革脉 |

#### 常见证型

| 系统 | 证型 |
|------|------|
| 外感 | 风寒束表证、风热犯肺证、暑湿感冒证 |
| 脾胃 | 脾胃虚弱证、脾胃湿热证、胃阴不足证 |
| 肝胆 | 肝郁气滞证、肝胆湿热证、肝阳上亢证 |
| 心肺 | 心脾两虚证、心肺气虚证、痰热壅肺证 |
| 肾系 | 肾阴亏虚证、肾阳不足证、肾精不足证 |

### 附录 B: 处方用法选项

| 用法 | 说明 |
|------|------|
| 水煎服 | 常规水煎，每日 1 剂，分 2-3 次服用 |
| 水煎频服 | 少量多次频服，适用于呕吐/咽喉疾病 |
| 开水泡服 | 沸水冲泡，代茶饮 |
| 研末冲服 | 研成细末，开水冲服 |
| 外用 | 煎汤外洗/熏蒸/湿敷 |
| 打粉装胶囊 | 打粉后装入胶囊服用 |

### 附录 C: 相关文件索引

| 文件 | 路径 | 说明 |
|------|------|------|
| MedicalCaseEditControl.xaml | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` | 编辑控件 XAML |
| MedicalCaseEditControl.xaml.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml.cs` | 编辑控件代码 |
| PrescriptionEditorViewModel.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/PrescriptionEditorViewModel.cs` | 处方编辑器 VM |
| MedicalCaseCommandsViewModel.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs` | 命令 VM |
| MedicalCaseDetailModel.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseDetailModel.cs` | 数据模型 |
| WorkspaceState.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/WorkspaceState.cs` | 工作区状态 |

---

*文档版本: v1.0 | 创建日期: 2026-04-10 | 状态: 待评审*
