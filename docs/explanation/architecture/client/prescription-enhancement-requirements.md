# 处方功能增强需求讨论

**版本**: v1.0
**创建日期**: 2025-11-20
**状态**: 📝 需求讨论
**Epic**: 医案模块完善
**相关文档**:
- [医案模块设计](medical-case-design.md)
- [ADR-006: 医案聚合根重构](../../decisions/ADR-006-medicalcase-consultation-prescription-refactoring.md)
- [Formula模块设计](../../server/formula-design.md)

---

## 📋 需求概述

### 业务目标
完善医案模块的处方功能,实现Formula级别的药材编辑体验,支持拼音码智能搜索和实时价格计算,为医生提供"简单明了"的看诊工具。

### 核心设计原则
> **🎯 方便医生看诊,UI交互简单明了**

### 目标用户
- **主要用户**: 医生(诊疗过程中开具处方)
- **次要用户**: 管理员(查看和编辑已有处方)

### 核心场景
1. **场景1**: 医生在诊疗过程中快速开具处方
   - 通过拼音码搜索药材(如输入"dg"匹配"当归")
   - 自动跳转焦点,减少鼠标点击
   - 实时显示价格,辅助决策

2. **场景2**: 医生导入常用经验方到处方
   - 从Formula模块导入药材组合
   - 自动计算价格(基于当前药材单价)

3. **场景3**: 医生修改处方药材和剂量
   - 支持重复检测,避免添加相同药材
   - 实时验证剂量合理性(0.1g ~ 500g)

### 当前问题
基于代码分析(PrescriptionEditorDialogViewModel.cs),当前Prescription模块存在以下问题:

| 问题 | 当前状态 | 期望状态 |
|------|---------|---------|
| 药材列表管理 | ❌ 缺失 | ✅ ObservableCollection<PrescriptionItemViewModel> HerbItems |
| 拼音码过滤 | ❌ 缺失 | ✅ AllHerbs, FilteredHerbs, SelectedHerb属性 |
| 药材卡片控件 | ❌ 缺失 | ✅ PrescriptionHerbCardControl.xaml |
| 价格计算 | ⚠️ 仅有字段 | ✅ 实时计算逻辑 |
| 焦点管理 | ❌ 缺失 | ✅ 自动跳转(TextBox→Dosage→下一个) |
| 界面模式 | ⚠️ Dialog(空间受限) | ✅ 全屏页面(充足交互空间) |

---

## ✨ 功能性需求

### FR-001: 处方药材组合编辑

**优先级**: 🔴 高(核心功能)

**User Story**:
```
作为 医生
我想要 在处方中添加、编辑、删除药材
以便 快速组合处方并调整剂量
```

**功能描述**:
- 支持动态添加药材(最多30味,参考临床实践)
- 支持修改单味药材的剂量(0.1g ~ 500g)
- 支持删除不需要的药材(Shift+Delete快捷键)
- 自动添加空行(到达末尾时)

**验收标准**:
- [x] 药材列表使用ObservableCollection<PrescriptionItemViewModel>管理
- [x] 支持Delete命令删除药材
- [x] 支持AddNewRowCommand自动添加空行
- [x] 药材数量显示在标题栏(如"药材组成 (共12味)")

**技术实现要点**:
```csharp
// PrescriptionDetailViewModel.cs
public class PrescriptionDetailViewModel : UnifiedViewModelBase
{
    // 药材列表
    public ObservableCollection<PrescriptionItemViewModel> HerbItems { get; } = new();

    // 药材数量(计算属性)
    public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);

    // 命令
    public DelegateCommand<PrescriptionItemViewModel> DeleteHerbCommand { get; }
    public DelegateCommand AddNewRowCommand { get; }
}
```

**参考实现**:
- FormulaDetailViewModel.cs (Lines 25-40: HerbItems管理)
- FormulaDetailView.xaml (Lines 222-238: UniformGrid 4列布局)

---

### FR-002: 拼音码智能搜索

**优先级**: 🔴 高(核心交互)

**User Story**:
```
作为 医生
我想要 通过拼音码快速搜索药材(如输入"dg"找到"当归")
以便 减少输入时间,提高开方效率
```

**功能描述**:
- 支持中文名称匹配(如"当" → "当归")
- 支持拼音码匹配(如"dg" → "danggui")
- 支持首字母跳跃式匹配(如"dg" → "d_g_")
- 最多显示5个建议,按匹配分数倒序排列

**7级匹配评分算法**:
```
1. 名称完全匹配：100分 (herbName == searchText)
2. 拼音码完全匹配：90分 (pinyinCode == searchText)
3. 名称前缀匹配：80分 (herbName.StartsWith(searchText))
4. 拼音码前缀匹配：70分 (pinyinCode.StartsWith(searchText))
5. 名称包含匹配：50分 (herbName.Contains(searchText))
6. 拼音码包含匹配：40分 (pinyinCode.Contains(searchText))
7. 拼音码模糊匹配：30分 (IsPinyinFuzzyMatch)
```

**验收标准**:
- [x] HerbName输入时实时触发FilterHerbs方法
- [x] FilteredHerbs集合实时更新(最多5个结果)
- [x] SelectedHerb属性自动填充HerbId, HerbName, Unit
- [x] 精确匹配时不显示建议列表(避免Popup一直显示)

**技术实现要点**:
```csharp
// PrescriptionItemViewModel.cs
public class PrescriptionItemViewModel : UnifiedViewModelBase, IHerbItem
{
    // 所有药材列表引用(由父ViewModel注入)
    public ObservableCollection<HerbDto>? AllHerbs { get; set; }

    // 过滤后的药材列表
    public ObservableCollection<HerbDto> FilteredHerbs { get; private set; } = new();

    // 选中的药材(自动填充)
    public HerbDto? SelectedHerb { get; set; }

    // 拼音码过滤逻辑
    private void FilterHerbs() { /* 7级评分算法 */ }
    private int GetMatchScore(HerbDto herb, string searchText) { /* ... */ }
}
```

**参考实现**:
- FormulaHerbItemViewModel.cs (Lines 174-319: 拼音码过滤完整实现)

---

### FR-003: 实时价格计算

**优先级**: 🔴 高(业务核心)

**User Story**:
```
作为 医生
我想要 实时看到处方的价格(单味小计+总价)
以便 根据患者经济情况调整处方
```

**功能描述**:
- 单味药材小计: HerbUnitPrice × Dosage
- 处方总价: Σ(单味小计) × DosageCount × Discount
- 客户端实时计算显示
- Server端验证计算准确性(防止篡改)

**验收标准**:
- [x] PrescriptionItemViewModel包含UnitPrice属性
- [x] 剂量或剂数变化时实时更新TotalAmount
- [x] 折扣范围验证(0.1 ~ 1.0,即1折~10折)
- [x] Server端Service层二次验证价格计算

**价格计算公式**:
```csharp
// Client端实时计算
public decimal TotalAmount
{
    get
    {
        var subtotal = HerbItems
            .Where(h => h.HerbId != Guid.Empty)
            .Sum(h => h.UnitPrice * h.Dosage);

        return subtotal * DosageCount * Discount;
    }
}

// Server端验证
public class PrescriptionService
{
    public async Task<decimal> CalculateTotalAmountAsync(
        List<PrescriptionItemDto> items,
        int dosageCount,
        decimal discount)
    {
        // 重新查询药材单价(不信任客户端)
        // 重新计算总价
        // 对比客户端传递的TotalAmount
    }
}
```

**参考实现**:
- PrescriptionItemViewModel.cs (Lines 88-94: UnitPrice字段)
- FormulaDetailViewModel.cs (Lines 233: HerbCount计算属性模式)

---

### FR-004: 自动焦点管理

**优先级**: 🟡 中(提升体验)

**User Story**:
```
作为 医生
我想要 通过键盘快速录入药材(无需频繁使用鼠标)
以便 提高开方速度,减少操作疲劳
```

**功能描述**:
- TextBox Enter键 → 选择药材 → 跳转DosageTextBox
- DosageTextBox Enter键 → 重复检测 → 跳转下一个TextBox
- 剂量框自动全选(GotFocus时)
- 水平优先遍历(4列UniformGrid,从左到右)
- 末尾自动添加新行

**验收标准**:
- [x] Down/Up键在建议列表中导航(焦点保持TextBox)
- [x] Enter键确认选择并跳转
- [x] Shift+Delete删除当前药材
- [x] 到达末尾时自动添加4个空槽位

**交互流程**:
```
用户操作               → 系统响应
═══════════════════════════════════════════════════════
输入"dg"              → 显示5个匹配结果(当归、大黄...)
Down键                → 高亮"当归"(焦点保持TextBox)
Enter键               → 填充"当归",跳转剂量框
输入"10"              → 自动全选"10"
Enter键               → 检测重复,跳转下一个药材TextBox
到达第12个(末尾)      → 自动添加4个空槽位(第13-16个)
```

**技术实现要点**:
```csharp
// PrescriptionHerbCardControl.xaml.cs
private void OnDosageKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        // 1. 触发重复检测命令
        if (DosageCompletedCommand?.CanExecute(DataContext) == true)
        {
            DosageCompletedCommand.Execute(DataContext);
        }

        // 2. 跳转到下一个药材卡片
        MoveFocusToNextHerbName();
    }
}

private void MoveFocusToNextHerbName()
{
    // 查找父级ItemsControl
    // 计算下一个索引(currentIndex + 1)
    // 到达末尾时触发AddNewRowCommand
    // 焦点移动到下一个HerbCardControl的TextBox
}
```

**参考实现**:
- HerbCardControl.xaml.cs (Lines 281-350: MoveFocusToNextHerbName完整实现)

---

### FR-005: 处方界面优化

**优先级**: 🔴 高(体验核心)

**User Story**:
```
作为 医生
我想要 在全屏页面中编辑处方(而非对话框)
以便 有充足的空间进行复杂的药材编辑和价格计算
```

**功能描述**:
- 从Dialog模式改为全屏页面模式
- 参考FormulaDetailView的布局设计
- 分为两块区域:
  - 区块1: 处方基本字段(编号、剂数、用法、折扣等)
  - 区块2: 药材组成(UniformGrid 4列布局)

**界面对比**:
```
现有(Dialog模式)              →  期望(全屏页面模式)
═══════════════════════════════════════════════════════
空间: 600x400 (受限)          →  1200x800 (充足)
滚动: 整体滚动                →  区块独立滚动
药材编辑: 拥挤                →  4列网格,清晰
建议列表: 可能超出边界        →  充足空间显示
导航: 保存/取消按钮           →  顶部返回+底部保存/取消
```

**验收标准**:
- [x] 创建PrescriptionDetailView.xaml(全屏页面)
- [x] 创建PrescriptionDetailViewModel.cs(主ViewModel)
- [x] MedicalCaseFlowViewModel调整导航逻辑(DialogService→RegionManager)
- [x] 保留PrescriptionEditorDialogView(向后兼容,标记Obsolete)

**导航变更**:
```csharp
// 原导航方式(Dialog)
await _dialogService.ShowDialogAsync(
    "PrescriptionEditorDialogView",
    parameters);

// 新导航方式(Region)
_regionManager.RequestNavigate(
    RegionNames.MainContentRegion,
    "PrescriptionDetailView",
    parameters);
```

**参考实现**:
- FormulaDetailView.xaml (Lines 1-345: 全屏页面布局)
- FormulaDetailViewModel.cs (Lines 1-350: ViewModel架构)

---

### FR-006: 经验方导入处方

**优先级**: 🟢 低(便捷功能)

**User Story**:
```
作为 医生
我想要 将常用经验方导入到处方
以便 快速开具标准化处方,减少重复劳动
```

**功能描述**:
- 从Formula模块选择经验方
- 自动复制药材信息(HerbId, HerbName, Dosage, Unit)
- 自动查询药材单价(从Herb表)
- 自动计算总价

**验收标准**:
- [x] LoadFormulaTemplateCommand打开经验方选择对话框
- [x] 选择后调用ImportFormulaIntoPrescriptionAsync API
- [x] 自动填充HerbItems集合
- [x] 自动计算TotalAmount

**API接口**:
```csharp
// Server端(已有)
PUT /api/v1/medicalcases/{caseId}/prescription/import-formula/{formulaId}

// Client端调用
public async Task ImportFormulaAsync(Guid formulaId)
{
    var prescription = await _prescriptionRepository
        .ImportFormulaIntoPrescriptionAsync(MedicalCaseId, formulaId);

    // 刷新HerbItems
    LoadPrescriptionItems(prescription);

    // 重新计算价格
    RaisePropertyChanged(nameof(TotalAmount));
}
```

**数据转换规则**:
| Formula字段 | Prescription字段 | 转换规则 |
|------------|-----------------|---------|
| HerbId | HerbId | 直接复制 |
| HerbName | HerbName | 直接复制 |
| Quantity | Dosage | 直接复制(字段名不同) |
| Unit | Unit | 直接复制 |
| - | UnitPrice | 从Herb表查询 |

**参考实现**:
- IMedicalCaseRepository.cs (Lines: ImportFormulaIntoPrescriptionAsync方法定义)
- Server端MedicalCaseService.cs (导入逻辑实现)

---

## 🔒 非功能性需求

### NFR-001: 性能需求

**指标**:
- 药材列表加载: < 500ms (AllHerbs缓存,通常<100个药材)
- 拼音码过滤响应: < 100ms (客户端内存过滤,无网络延迟)
- 价格实时计算: < 50ms (纯客户端计算)
- 处方保存: < 1s (包括网络延迟+Server端验证)

**约束**:
- 单个处方药材数量 ≤ 30味(参考临床实践)
- AllHerbs缓存大小 ≤ 500个药材记录
- FilteredHerbs最多显示5个建议

**优化策略**:
- AllHerbs在ViewModel初始化时一次性加载并缓存
- 拼音码过滤使用LINQ内存查询(避免数据库查询)
- 价格计算使用计算属性(PropertyChanged触发)

---

### NFR-002: 可用性需求

**键盘导航**:
- 全程可键盘操作(Down/Up/Enter/Shift+Delete)
- Tab键顺序: TextBox → DosageTextBox → 下一个TextBox
- 焦点管理自动化,减少鼠标点击

**错误提示**:
- 药材重复: 弹窗提示"该药材已存在,请直接修改剂量"
- 剂量超限: TextBox显示红色边框+提示文字
- 处方为空: 保存时提示"处方至少需要1味药材"

**数据恢复**:
- 意外关闭时保留草稿(基于MedicalCase暂存功能)
- 自动保存间隔: 30秒(可选)

**视觉反馈**:
- 建议列表高亮当前选中项(蓝色背景)
- 选中药材后TextBox显示药材名称(黑色加粗)
- 价格实时更新时显示动画(可选)

---

### NFR-003: 安全需求

**权限控制**:
- 仅医生可创建/编辑处方(基于角色验证)
- 管理员可查看处方(只读模式)
- 非授权用户无法访问处方功能

**数据验证**:
- Server端二次验证价格计算(防止客户端篡改)
- Server端验证药材ID合法性(防止伪造)
- Server端验证剂量范围(0.1g ~ 500g)

**审计日志**:
- 记录处方创建操作(CreatedBy, CreatedAt)
- 记录处方修改操作(UpdatedBy, UpdatedAt)
- 记录经验方导入操作(ImportedFrom)

---

### NFR-004: 可维护性需求

**代码复用**:
- PrescriptionHerbCardControl参考HerbCardControl实现
- 拼音码过滤逻辑参考FormulaHerbItemViewModel
- 避免直接复制粘贴(保持职责清晰)

**接口一致性**:
- PrescriptionItemViewModel实现IHerbItem接口
- 与Formula共享组件(如未来的HerbSelectorDialog)

**组件化架构**:
- 使用PrescriptionDataManager负责数据访问
- 使用PrescriptionCommandHandler负责命令逻辑
- ViewModel不直接调用Repository(Epic #1773规范)

**测试友好**:
- DataManager和CommandHandler可独立单元测试
- ViewModel依赖注入(便于Mock)

---

### NFR-005: 兼容性需求

**现有数据兼容**:
- 不破坏现有Prescription数据结构
- 新增字段使用默认值(如UnitPrice默认为0)
- 支持从旧版本数据平滑升级

**聚合根模式兼容**:
- 保持通过MedicalCase聚合根访问
- API路径不变: PUT /api/v1/medicalcases/{caseId}/prescription

**向后兼容**:
- 保留PrescriptionEditorDialogView(标记Obsolete)
- 支持两种导航方式共存(过渡期)
- 数据库Schema向后兼容

---

## 📐 业务规则

### BR-001: 药材唯一性

**规则描述**:
同一处方中不允许重复添加相同药材。

**业务理由**:
- 避免剂量混淆(如"当归 10g"和"当归 15g"同时存在)
- 符合中医处方规范(一味药材一个剂量)
- 防止用户误操作

**实现方式**:
```csharp
// PrescriptionDetailViewModel.cs
public DelegateCommand<PrescriptionItemViewModel> DosageCompletedCommand { get; }

private void OnDosageCompleted(PrescriptionItemViewModel item)
{
    // 检测重复
    var duplicate = HerbItems
        .Where(h => h != item)
        .FirstOrDefault(h => h.HerbId == item.HerbId);

    if (duplicate != null)
    {
        // 提示用户
        _dialogService.ShowMessageBox(
            "该药材已存在,请直接修改剂量",
            "重复药材");

        // 清空当前行
        item.HerbId = Guid.Empty;
        item.HerbName = string.Empty;
        item.Dosage = 0;

        return;
    }

    // 继续跳转逻辑...
}
```

**触发时机**: 剂量输入完成后(Enter键)

**用户反馈**: 弹窗提示"该药材已存在,请直接修改剂量"

**参考实现**: FormulaDetailViewModel.cs (重复检测逻辑)

---

### BR-002: 剂量合理性

**规则描述**:
单味药材剂量范围0.1g ~ 500g。

**业务理由**:
- 0.1g以下: 可能是输入错误(如0.01g)
- 500g以上: 超出临床常规剂量,可能是误输入
- 提前拦截异常数据,避免开具不合理处方

**实现方式**:
```csharp
// PrescriptionItemViewModel.cs
[Required(ErrorMessage = "用量不能为空")]
[Range(0.1, 500, ErrorMessage = "用量必须在0.1到500之间")]
public decimal Dosage
{
    get => _dosage;
    set => SetProperty(ref _dosage, value);
}
```

**触发时机**:
- 剂量输入框失去焦点时
- Enter键提交时
- 保存处方时

**用户反馈**:
- TextBox显示红色边框
- 提示文字: "用量必须在0.1到500之间"

**特殊情况**: 贵细药材(如西洋参)可能剂量<0.1g,后续可支持例外配置

---

### BR-003: 处方完整性

**规则描述**:
处方至少包含1味药材。

**业务理由**:
- 空处方无临床意义
- 防止误操作(如点击保存时药材列表为空)
- 符合中医处方规范

**实现方式**:
```csharp
// Client端验证
public async Task<bool> SavePrescriptionAsync()
{
    var validHerbs = HerbItems.Where(h => h.HerbId != Guid.Empty).ToList();

    if (validHerbs.Count == 0)
    {
        await _dialogService.ShowMessageBoxAsync(
            "处方至少需要1味药材",
            "验证失败");
        return false;
    }

    // 继续保存...
}

// Server端验证
public class PrescriptionService
{
    public async Task<PrescriptionDto> UpdatePrescriptionAsync(
        Guid caseId,
        UpdatePrescriptionDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new BusinessException("处方至少需要1味药材");
        }

        // 继续处理...
    }
}
```

**触发时机**: 保存处方时(Client端+Server端双重验证)

---

### BR-004: 价格计算规则

**规则描述**:
```
TotalAmount = Σ(HerbUnitPrice × Dosage) × DosageCount × Discount
```

**业务理由**:
- 单味小计 = 药材单价 × 剂量
- 处方总价 = 所有单味小计之和 × 剂数 × 折扣
- 剂数: 开几副药(通常7副)
- 折扣: 优惠比例(1.0=原价, 0.9=9折)

**实现方式**:
```csharp
// Client端实时计算
public decimal TotalAmount
{
    get
    {
        // 计算所有药材的小计
        var subtotal = HerbItems
            .Where(h => h.HerbId != Guid.Empty)
            .Sum(h => h.UnitPrice * h.Dosage);

        // 乘以剂数和折扣
        return subtotal * DosageCount * Discount;
    }
}

// Server端验证
public class PrescriptionService
{
    public async Task<decimal> CalculateTotalAmountAsync(
        List<PrescriptionItemDto> items,
        int dosageCount,
        decimal discount)
    {
        decimal subtotal = 0;

        foreach (var item in items)
        {
            // 重新查询药材单价(不信任客户端)
            var herb = await _herbRepository.GetByIdAsync(item.HerbId);
            if (herb == null)
                throw new BusinessException($"药材{item.HerbName}不存在");

            subtotal += herb.UnitPrice * item.Dosage;
        }

        return subtotal * dosageCount * discount;
    }
}
```

**约束条件**:
- Discount范围: [0.1, 1.0] (1折 ~ 10折)
- DosageCount范围: [1, 30] (最多开30副)

**Server端验证**: 对比客户端传递的TotalAmount与重新计算的结果,差异>0.01元则拒绝

---

### BR-005: 聚合根访问模式

**规则描述**:
处方的创建/修改必须通过MedicalCase聚合根,禁止直接访问Prescription API。

**业务理由**:
- ADR-006强制聚合根模式
- 保证医案(诊断+处方)的数据一致性
- 统一事务边界

**实现方式**:
```csharp
// ✅ 正确: 通过MedicalCase聚合根
PUT /api/v1/medicalcases/{caseId}/prescription
POST /api/v1/medicalcases/{caseId}/prescription

// ❌ 错误: 直接访问Prescription (已废弃)
POST /api/v1/prescriptions
PUT /api/v1/prescriptions/{id}
```

**Client端调用**:
```csharp
// 通过IMedicalCaseRepository
await _medicalCaseRepository.UpdatePrescriptionAsync(
    medicalCaseId,
    updatePrescriptionDto);

// 禁止直接调用IPrescriptionRepository
// await _prescriptionRepository.UpdateAsync(prescriptionId, dto); // ❌
```

**Server端保护**:
- 直接访问Prescription API返回410 Gone
- 强制客户端使用聚合根路径

**参考文档**: ADR-006-medicalcase-consultation-prescription-refactoring.md

---

### BR-006: 经验方导入规则

**规则描述**:
从Formula导入时,保留药材信息但重置价格。

**业务理由**:
- Formula模块不涉及价格(UnitPrice固定为0)
- 药材价格可能随时间变动,需根据当前价格重新计算
- 防止使用过期价格开方

**实现方式**:
```csharp
// Server端ImportFormulaIntoPrescriptionAsync
public async Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(
    Guid medicalCaseId,
    Guid formulaId)
{
    // 1. 查询经验方
    var formula = await _formulaRepository.GetByIdAsync(formulaId);

    // 2. 转换为处方项目
    var prescriptionItems = new List<PrescriptionItemDto>();

    foreach (var formulaItem in formula.HerbItems)
    {
        // 重新查询药材单价(不使用Formula的价格)
        var herb = await _herbRepository.GetByIdAsync(formulaItem.HerbId);

        prescriptionItems.Add(new PrescriptionItemDto
        {
            HerbId = formulaItem.HerbId,
            HerbName = formulaItem.HerbName,
            Dosage = formulaItem.Quantity,  // 字段名不同
            Unit = formulaItem.Unit,
            UnitPrice = herb.UnitPrice  // 从Herb表查询最新价格
        });
    }

    // 3. 创建处方
    return await CreatePrescriptionAsync(medicalCaseId, new CreatePrescriptionDto
    {
        Items = prescriptionItems,
        DosageCount = 7,  // 默认7副
        Discount = 1.0m   // 默认原价
    });
}
```

**数据转换映射**:
| Formula字段 | Prescription字段 | 数据来源 |
|------------|-----------------|---------|
| HerbId | HerbId | 直接复制 |
| HerbName | HerbName | 直接复制 |
| Quantity | Dosage | 直接复制 |
| Unit | Unit | 直接复制 |
| - | UnitPrice | 从Herb表查询 |

**触发时机**: 用户点击"导入经验方"按钮

---

## 🗃️ 数据模型草案

### Client端增强

#### PrescriptionDetailViewModel.cs (新建)
```csharp
public class PrescriptionDetailViewModel : UnifiedViewModelBase
{
    // 组件化依赖
    private readonly PrescriptionDataManager _dataManager;
    private readonly PrescriptionCommandHandler _commandHandler;
    private readonly IContainerProvider _containerProvider;

    // 处方基本信息
    public Guid MedicalCaseId { get; set; }
    public string PrescriptionNo { get; set; }
    public int DosageCount { get; set; } = 7;
    public decimal Discount { get; set; } = 1.0m;
    public string Usage { get; set; }
    public string MedicalAdvice { get; set; }
    public string Remark { get; set; }

    // 药材列表
    public ObservableCollection<PrescriptionItemViewModel> HerbItems { get; } = new();
    public ObservableCollection<HerbDto> AllHerbs { get; private set; } = new();

    // 计算属性
    public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);
    public decimal TotalAmount => HerbItems
        .Where(h => h.HerbId != Guid.Empty)
        .Sum(h => h.UnitPrice * h.Dosage) * DosageCount * Discount;

    // 命令
    public DelegateCommand<PrescriptionItemViewModel> DeleteHerbCommand { get; }
    public DelegateCommand<PrescriptionItemViewModel> DosageCompletedCommand { get; }
    public DelegateCommand AddNewRowCommand { get; }
    public DelegateCommand LoadFormulaTemplateCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelEditCommand { get; }
    public DelegateCommand BackCommand { get; }
}
```

#### PrescriptionItemViewModel.cs (增强)
```csharp
public class PrescriptionItemViewModel : UnifiedViewModelBase, IHerbItem
{
    // 基本属性
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = "g";
    public decimal UnitPrice { get; set; }  // ✅ 新增: 从AllHerbs获取
    public string? Remark { get; set; }

    // 拼音码过滤(参考Formula)
    public ObservableCollection<HerbDto>? AllHerbs { get; set; }  // ✅ 新增
    public ObservableCollection<HerbDto> FilteredHerbs { get; } = new();  // ✅ 新增
    public HerbDto? SelectedHerb { get; set; }  // ✅ 新增

    // 方法
    private void FilterHerbs() { /* 7级评分算法 */ }  // ✅ 新增
    private int GetMatchScore(HerbDto herb, string searchText) { /* ... */ }  // ✅ 新增
}
```

### UI组件

#### PrescriptionDetailView.xaml (新建)
```xml
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionDetailView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 顶部操作栏 -->
            <RowDefinition Height="*" />     <!-- 内容区域 -->
            <RowDefinition Height="Auto" />  <!-- 底部按钮栏 -->
        </Grid.RowDefinitions>

        <!-- 顶部操作栏 -->
        <Border Grid.Row="0">
            <Button Content="← 返回" Command="{Binding BackCommand}" />
            <TextBlock Text="处方编辑" FontSize="20" />
        </Border>

        <!-- 内容区域 -->
        <ScrollViewer Grid.Row="1">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />  <!-- 区块1: 基本字段 -->
                    <RowDefinition Height="Auto" />  <!-- 区块2: 药材列表 -->
                </Grid.RowDefinitions>

                <!-- 区块1: 基本字段 -->
                <Border Grid.Row="0">
                    <!-- 编号、剂数、用法、折扣等 -->
                </Border>

                <!-- 区块2: 药材列表 -->
                <Border Grid.Row="1">
                    <TextBlock Text="{Binding HerbCount, StringFormat='药材组成 (共{0}味)'}" />
                    <ItemsControl ItemsSource="{Binding HerbItems}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <UniformGrid Columns="4" />
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <controls:PrescriptionHerbCardControl />
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </Border>
            </Grid>
        </ScrollViewer>

        <!-- 底部按钮栏 -->
        <Border Grid.Row="2">
            <Button Content="取消" Command="{Binding CancelEditCommand}" />
            <Button Content="保存" Command="{Binding SaveCommand}" />
        </Border>
    </Grid>
</UserControl>
```

#### PrescriptionHerbCardControl.xaml (新建,参考HerbCardControl)
```xml
<UserControl x:Class="LYBT.Desktop.Prescriptions.Controls.PrescriptionHerbCardControl">
    <Border BorderBrush="#E0E0E0" BorderThickness="1" Padding="10">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />  <!-- 药材名称TextBox -->
                <RowDefinition Height="Auto" />  <!-- 剂量TextBox -->
                <RowDefinition Height="Auto" />  <!-- 单价显示 -->
                <RowDefinition Height="Auto" />  <!-- 删除按钮 -->
            </Grid.RowDefinitions>

            <!-- 药材名称TextBox + 建议Popup -->
            <TextBox Grid.Row="0"
                     x:Name="HerbNameTextBox"
                     Text="{Binding HerbName, UpdateSourceTrigger=PropertyChanged}" />
            <Popup x:Name="SuggestionsPopup"
                   PlacementTarget="{Binding ElementName=HerbNameTextBox}">
                <ListBox x:Name="SuggestionsListBox"
                         ItemsSource="{Binding FilteredHerbs}"
                         DisplayMemberPath="Name" />
            </Popup>

            <!-- 剂量TextBox -->
            <TextBox Grid.Row="1"
                     x:Name="DosageTextBox"
                     Text="{Binding Dosage}" />

            <!-- 单价显示 -->
            <TextBlock Grid.Row="2"
                       Text="{Binding UnitPrice, StringFormat='单价: ¥{0:F2}'}" />

            <!-- 删除按钮 -->
            <Button Grid.Row="3"
                    Content="删除"
                    Command="{Binding DeleteCommand}" />
        </Grid>
    </Border>
</UserControl>
```

### Server端(无新增Entity)

现有Prescription Entity结构保持不变,仅增强Service层验证逻辑:

```csharp
// PrescriptionService.cs (增强)
public class PrescriptionService
{
    // ✅ 新增: 价格计算验证
    public async Task<decimal> CalculateTotalAmountAsync(
        List<PrescriptionItemDto> items,
        int dosageCount,
        decimal discount)
    {
        // 重新查询药材单价
        // 重新计算总价
        // 验证客户端传递的TotalAmount
    }

    // ✅ 新增: 经验方导入增强
    public async Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(
        Guid medicalCaseId,
        Guid formulaId)
    {
        // 查询Formula
        // 转换为Prescription
        // 重新查询药材单价
    }
}
```

---

## 🏗️ 架构约束

### AC-001: 技术栈约束(基于MVP Constitution)

#### ✅ 允许技术
- **WPF (.NET 8)**: Client端UI框架
- **Prism 8.x**: MVVM框架、模块化、依赖注入
- **MaterialDesignThemes 5.1.x**: UI组件库
- **Entity Framework Core 8**: Server端ORM
- **ASP.NET Core Web API**: Server端框架
- **SQL Server**: 数据库
- **FluentValidation**: 验证框架
- **AutoMapper**: DTO映射

#### ❌ 禁止技术(MVP黑名单)
- **Redis/RabbitMQ**: 分布式组件(违反MVP简单原则)
- **MediatR/CQRS**: 复杂模式(过度设计)
- **Docker/Kubernetes**: 容器化(非MVP范围)
- **SignalR**: 实时通信(当前无需)

---

### AC-002: 架构层分配

#### Client端(LYBTZYZS.Desktop.Prescriptions)
```
LYBT.Desktop.Prescriptions/
├── Views/
│   ├── PrescriptionDetailView.xaml            (新建, 全屏处方编辑页面)
│   └── PrescriptionEditorDialogView.xaml      (保留, 标记Obsolete)
├── ViewModels/
│   ├── PrescriptionDetailViewModel.cs         (新建, 主ViewModel)
│   ├── PrescriptionItemViewModel.cs           (增强, 新增拼音码过滤)
│   ├── Components/
│   │   ├── PrescriptionDataManager.cs        (新建, 数据管理组件)
│   │   └── PrescriptionCommandHandler.cs     (新建, 命令处理组件)
└── Controls/
    └── PrescriptionHerbCardControl.xaml.cs    (新建, 药材卡片控件)
```

#### Server端(LYBTZYZS.Server.Modules.Prescriptions)
```
LYBT.Server.Modules.Prescriptions/
├── Services/
│   └── PrescriptionService.cs                (增强, 价格验证+经验方导入)
├── Repositories/
│   └── PrescriptionRepository.cs             (保持不变, 通过MedicalCase聚合根)
└── Controllers/
    └── MedicalCaseController.cs              (保持不变, 聚合根API端点)
```

#### Shared(LYBTZYZS.Shared.Models)
```
LYBT.Shared.Models/
└── Contracts/
    └── Prescriptions/
        ├── PrescriptionDto.cs                 (保持不变)
        ├── PrescriptionItemDto.cs             (保持不变)
        └── UpdatePrescriptionDto.cs           (保持不变)
```

---

### AC-003: 聚合根模式约束(ADR-006)

#### API路径规范
```
✅ 正确: 通过MedicalCase聚合根
PUT  /api/v1/medicalcases/{caseId}/prescription
POST /api/v1/medicalcases/{caseId}/prescription
PUT  /api/v1/medicalcases/{caseId}/prescription/import-formula/{formulaId}

❌ 错误: 直接访问Prescription (已废弃)
POST /api/v1/prescriptions
PUT  /api/v1/prescriptions/{id}
```

#### Client端访问模式
```csharp
// ✅ 正确: 通过IMedicalCaseRepository
await _medicalCaseRepository.UpdatePrescriptionAsync(
    medicalCaseId,
    updatePrescriptionDto);

// ❌ 错误: 直接调用IPrescriptionRepository
await _prescriptionRepository.UpdateAsync(prescriptionId, dto);
```

#### Server端保护
- 直接访问Prescription API返回 `410 Gone`
- 响应体: `{ "error": "Please use /api/v1/medicalcases/{caseId}/prescription" }`

---

### AC-004: 组件化架构约束(Epic #1773)

#### 必须使用DataManager + CommandHandler模式
```csharp
// PrescriptionDetailViewModel.cs
public class PrescriptionDetailViewModel : UnifiedViewModelBase
{
    // ✅ 正确: 使用组件化依赖
    private readonly PrescriptionDataManager _dataManager;
    private readonly PrescriptionCommandHandler _commandHandler;

    // ❌ 错误: 直接注入Repository
    // private readonly IPrescriptionRepository _repository;

    public PrescriptionDetailViewModel(
        PrescriptionDataManager dataManager,
        PrescriptionCommandHandler commandHandler,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _dataManager = dataManager;
        _commandHandler = commandHandler;
    }
}
```

#### 组件职责划分
- **DataManager**: 负责数据加载、缓存、Repository调用
- **CommandHandler**: 负责命令逻辑、业务规则验证
- **ViewModel**: 负责UI绑定、用户交互编排

---

### AC-005: 接口一致性约束

#### IHerbItem接口实现
```csharp
// PrescriptionItemViewModel.cs 必须实现 IHerbItem
public class PrescriptionItemViewModel : UnifiedViewModelBase, IHerbItem
{
    // IHerbItem接口属性
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public decimal Dosage { get; set; }
    public string Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Remark { get; set; }
}
```

#### 接口一致性的好处
- 与Formula共享组件(如未来的HerbSelectorDialog)
- 统一数据结构,便于序列化/反序列化
- 支持多态(如List<IHerbItem>可同时包含Formula和Prescription项)

---

## ❓ 开放问题

### Q1: 处方界面交互模式

**问题**: 处方编辑使用Dialog还是全屏页面?

**背景**: 当前使用PrescriptionEditorDialogView(Dialog模式),但复杂的药材编辑交互(拼音码过滤+价格计算)可能导致空间拥挤。

**选项对比**:
| 选项 | 优点 | 缺点 | 推荐度 |
|-----|------|------|--------|
| A. 保持Dialog模式 | 改动小,保持现有流程 | 空间受限(600x400),交互拥挤 | ⭐ |
| B. 改为全屏页面 | 交互空间充足,符合"UI交互简单明了" | 改动较大,需调整流程 | ⭐⭐⭐⭐⭐ |
| C. 混合模式 | 灵活性高 | 复杂度增加,可能混淆用户 | ⭐⭐ |

**建议**: 选B(全屏页面)

**理由**:
1. 用户明确强调"方便医生看诊,UI交互简单明了"
2. Formula模块使用全屏页面,药材编辑体验良好
3. 拼音码过滤的建议Popup需要充足空间
4. 价格计算区域需要清晰展示
5. 长期看,全屏页面更易扩展(如未来添加打印预览)

**风险**: MedicalCaseFlowViewModel需要调整导航逻辑(DialogService → RegionManager)

---

### Q2: 药材单价数据来源

**问题**: PrescriptionItemViewModel的UnitPrice从哪里获取?

**背景**: Formula模块不涉及价格,但Prescription需要实时计算总价。

**选项对比**:
| 选项 | 优点 | 缺点 | 推荐度 |
|-----|------|------|--------|
| A. 从Herb表实时查询 | 价格始终最新 | 可能有网络延迟,影响体验 | ⭐⭐ |
| B. AllHerbs缓存包含单价 | 响应速度快(<100ms) | 价格可能不是最新(需刷新机制) | ⭐⭐⭐⭐⭐ |
| C. 混合模式(缓存+异步更新) | 兼顾速度和准确性 | 实现复杂,MVP阶段过度设计 | ⭐⭐⭐ |

**建议**: 选B(AllHerbs缓存包含单价)

**理由**:
1. 药材价格通常不会在看诊过程中变动
2. AllHerbs初始化时一次性加载(<500个药材,<500ms)
3. 拼音码过滤需要AllHerbs缓存,复用同一数据源
4. 如需刷新,提供"刷新药材列表"按钮即可

**实现细节**:
```csharp
// PrescriptionDataManager.cs
public async Task<ObservableCollection<HerbDto>> LoadAllHerbsAsync()
{
    // 从Server端一次性加载所有药材(包含单价)
    var herbs = await _herbRepository.GetAllAsync();
    return new ObservableCollection<HerbDto>(herbs);
}

// PrescriptionItemViewModel.cs
public HerbDto? SelectedHerb
{
    set
    {
        if (SetProperty(ref _selectedHerb, value) && value != null)
        {
            HerbId = value.Id;
            HerbName = value.Name;
            Unit = value.Unit;
            UnitPrice = value.UnitPrice;  // 从缓存获取单价
        }
    }
}
```

---

### Q3: PrescriptionHerbCardControl实现方式

**问题**: 新建控件还是复用HerbCardControl?

**背景**: Formula和Prescription的药材卡片UI类似,但Prescription需要显示价格。

**选项对比**:
| 选项 | 优点 | 缺点 | 推荐度 |
|-----|------|------|--------|
| A. 完全复用HerbCardControl | 代码复用率高 | 需要条件判断(如ShowPrice属性) | ⭐⭐⭐ |
| B. 新建PrescriptionHerbCardControl | 职责清晰,易于维护 | 代码有重复(拼音码逻辑) | ⭐⭐⭐⭐⭐ |
| C. 提取共享基类HerbCardControlBase | 避免重复又保持清晰 | 过度设计,违反MVP原则 | ⭐ |

**建议**: 选B(新建PrescriptionHerbCardControl,参考HerbCardControl实现)

**理由**:
1. 保持简单,避免过度抽象(MVP原则)
2. Formula和Prescription的业务逻辑存在差异(价格计算)
3. 拼音码过滤逻辑在ViewModel层(FormulaHerbItemViewModel),可直接参考实现
4. 两个控件的差异主要在UI(价格显示),代码重复较少

**实现策略**:
- 参考HerbCardControl.xaml.cs的焦点管理逻辑
- 参考FormulaHerbItemViewModel.cs的拼音码过滤逻辑
- 新增价格显示区域(TextBlock显示UnitPrice)

---

### Q4: 处方草稿功能

**问题**: 是否需要处方草稿(类似医案草稿)?

**背景**: 医生可能希望预先准备常用处方草稿,但这与Formula模块的经验方功能重叠。

**选项对比**:
| 选项 | 优点 | 缺点 | 推荐度 |
|-----|------|------|--------|
| A. 不需要,处方随医案暂存 | 简单,依赖现有功能 | 无法单独保存处方草稿 | ⭐⭐⭐⭐⭐ |
| B. 需要独立的处方草稿功能 | 灵活性高 | 与Formula重复,增加复杂度 | ⭐⭐ |

**建议**: 选A(本期不做,依赖现有MedicalCase暂存功能)

**理由**:
1. MedicalCase已有暂存功能(SaveAsDraftAsync),处方随医案一起暂存
2. Formula模块已提供经验方功能,可导入到处方
3. MVP阶段避免功能重复,聚焦核心药材编辑+价格计算
4. 后续如有需求,可单独评估

**替代方案**: 引导医生使用Formula模块管理常用处方模板

---

### Q5: 处方打印格式

**问题**: 是否需要处方打印功能?

**背景**: 诊所通常需要打印处方给患者,但打印格式涉及复杂的排版和模板设计。

**选项对比**:
| 选项 | 优点 | 缺点 | 推荐度 |
|-----|------|------|--------|
| A. 本期不做,后续单独实现 | 聚焦核心编辑功能 | 无法完整闭环 | ⭐⭐⭐⭐⭐ |
| B. 本期实现基础打印 | 完整闭环 | 增加工作量,可能影响交付 | ⭐⭐ |

**建议**: 选A(本期不做,后续单独实现)

**理由**:
1. 本期聚焦药材编辑+价格计算核心功能
2. 打印功能涉及模板设计、纸张尺寸、打印机适配等复杂问题
3. 可先使用简单的导出Word/PDF功能作为临时方案
4. 后续单独Epic实现标准化打印模板

**临时方案**: 提供"导出为PDF"功能(使用简单的HTML模板)

---

## 📎 参考资料

### 架构文档
- [医案模块Client端设计](medical-case-design.md) - MedicalCaseFlowViewModel流程编排
- [医案模块Server端设计](../../server/medical-case-design.md) - 聚合根模式和业务规则
- [ADR-006: 医案聚合根重构](../../decisions/ADR-006-medicalcase-consultation-prescription-refactoring.md) - 强制聚合根模式
- [Formula模块设计](../../server/formula-design.md) - 药材编辑参考实现

### 代码参考
- `FormulaDetailViewModel.cs` (Lines 25-350) - 药材列表管理、命令处理
- `FormulaHerbItemViewModel.cs` (Lines 174-319) - 拼音码过滤7级评分算法
- `HerbCardControl.xaml.cs` (Lines 1-450) - 焦点管理、键盘导航、建议Popup
- `PrescriptionItemViewModel.cs` (Lines 1-150) - 当前实现(需增强)
- `PrescriptionEditorDialogViewModel.cs` (Lines 1-300) - 当前Dialog实现(保留但标记Obsolete)

### 相关Issue/ADR
- Epic #1773: Desktop模块组件化重构 - DataManager + CommandHandler模式
- ADR-008: Repository模式设计 - 聚合根访问规范
- Issue #2149: Formula模块拼音码过滤实现 - 7级评分算法来源

---

## 📝 下一步行动

### 立即行动(需用户确认)
1. **确认开放问题Q1-Q5的选择**
   - Q1: 处方界面交互模式 → 建议全屏页面
   - Q2: 药材单价数据来源 → 建议AllHerbs缓存
   - Q3: PrescriptionHerbCardControl → 建议新建控件
   - Q4: 处方草稿功能 → 建议本期不做
   - Q5: 处方打印格式 → 建议后续实现

2. **生成设计文档**
   - 调用 `lybtzyzs-design-generator` 生成详细技术设计
   - 包含: 类图、时序图、数据库Schema、API端点定义

3. **生成任务清单**
   - 调用 `lybtzyzs-task-breakdown` 拆分可执行子任务
   - Phase 1: 基础架构(PrescriptionDetailView, PrescriptionDetailViewModel)
   - Phase 2: 拼音码过滤(PrescriptionItemViewModel增强)
   - Phase 3: 药材卡片控件(PrescriptionHerbCardControl)
   - Phase 4: 价格计算(Client端+Server端验证)
   - Phase 5: 经验方导入(LoadFormulaTemplateCommand)
   - Phase 6: 集成测试与优化

### 后续跟进(设计阶段后)
4. **创建GitHub Epic**
   - Epic标题: "医案模块完善 - 处方功能增强"
   - 关联本需求文档和设计文档

5. **拆分GitHub Issues**
   - 基于任务清单创建可执行Issues
   - 每个Issue包含验收标准和技术要点

6. **执行开发**
   - 按Phase顺序渐进执行
   - 每完成一个Phase保存Graphiti记忆

---

**文档维护**:
- 创建人: Claude Code (lybtzyzs-requirements-generator)
- 最后更新: 2025-11-20
- 变更历史:
  - v1.0 (2025-11-20): 初始版本,基于THINK阶段深度调研生成
