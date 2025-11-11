# Formula（验方）模块CRUD功能完善需求文档

**文档版本**: v1.0  
**创建日期**: 2025-11-11  
**参考模块**: Users, Herbs, Patients  
**目标**: 对齐参考模块，实现完整且一致的验方管理CRUD功能

---

## 1. 概述

### 1.1 业务背景

验方管理是LYBTZYZS中医诊所系统的核心模块之一，用于管理中医经典方剂、自拟验方及相关药材组成信息。当前Formula模块已有基础CRUD功能，但存在UI/UX不一致、缺失功能、使用Dialog而非全页面等问题。

### 1.2 参考模块

本次需求对齐以下三个成熟模块：

- **Users模块**: 用户管理（完整的查看/编辑/删除功能）
- **Herbs模块**: 药材管理（使用IsReadOnly模式切换查看/编辑，已完成UI清理）
- **Patients模块**: 患者管理（独立的创建/查看/编辑页面，已完成UI清理）

### 1.3 目标与范围

**主要目标**:
1. 补充缺失的独立创建页面（FormulaCreateView）
2. 修复UI/UX不一致问题（删除重复内容、不必要按钮）
3. 废弃Dialog弹窗模式，统一使用全页面模式
4. 对齐参考模块的交互模式和视觉风格

**范围界定**:
- ✅ Client端UI/UX改进（主要工作）
- ✅ ViewModel逻辑完善
- ⚠️ Server端已完整，无需改动
- ❌ 不涉及业务规则变更

---

## 2. 功能性需求

### 2.1 创建验方（Create）

#### User Story
> 作为中医诊所管理员，我希望能够通过独立页面创建新验方，包括基本信息和药材组成，以便建立和维护验方数据库。

#### 功能描述

**当前状态**:
- 🔴 使用EditFormulaDialog弹窗创建
- 🔴 弹窗模式与参考模块不一致

**需求改进**:
1. **新建FormulaCreateView.xaml**（独立全页面）
   - 参考HerbCreateView.xaml设计
   - 标题栏：返回按钮 + "新增验方" 标题 + 保存/取消按钮
   - 内容区域：
     - 基本信息卡片（Expander可折叠）：验方名称、分类、来源、性味归经、功效、用法
     - 药材组成卡片（Expander默认展开）：药材列表（DataGrid）+ 添加/编辑/删除药材按钮
     - 备注卡片（Expander可折叠）：验方描述、备注

2. **新建FormulaCreateViewModel.cs**
   - 继承`BindableBase`，实现`INavigationAware`
   - 属性：`Formula`（FormulaInputDto）, `HerbItems`（ObservableCollection）
   - 命令：`SaveCommand`, `CancelCommand`, `AddHerbCommand`, `RemoveHerbCommand`
   - 验证：使用FormulaValidator验证必填字段和业务规则

3. **FormulaManagementViewModel改进**
   - `AddFormulaCommand`导航到FormulaCreateView，而非打开Dialog
   - 导航参数：无（新建模式）

#### 验收标准

- [ ] FormulaCreateView.xaml页面布局对齐HerbCreateView
- [ ] 点击"新增验方"按钮跳转到独立创建页面（非弹窗）
- [ ] 可添加/编辑/删除药材组成项
- [ ] 验方名称、功效、用法为必填字段，未填写时提示错误
- [ ] 至少添加1味药材才能保存
- [ ] 保存成功后返回列表页，并刷新数据
- [ ] 点击取消按钮返回列表页，不保存数据
- [ ] 编译无错误，运行时功能正常

---

### 2.2 查看验方（Read）

#### User Story
> 作为中医诊所管理员，我希望能够查看验方的完整详细信息，包括基本信息、药材组成、统计数据等，以便了解验方内容和使用情况。

#### 功能描述

**当前状态**:
- ✅ FormulaDetailView已实现查看功能
- 🔴 顶部有重复的基本信息大卡片（图标+名称+分类+总价）
- 🔴 标题栏有"打印"按钮（非必须功能）
- ⚠️ "使用记录"按钮位置不合理（应移到内容区）

**需求改进**:
1. **删除重复的顶部基本信息大卡片**
   - 删除FormulaDetailView.xaml中Grid.Row="0"的Border（包含80x80图标、验方名称、分类、药材数、总价、难度、状态徽章）
   - 保留Grid.Row="1"的"基本信息"Expander卡片（完整表单）

2. **删除"打印"按钮**
   - 从标题栏StackPanel中移除PrintCommand按钮
   - 参考HerbDetailView和PatientDetailView已移除打印功能

3. **优化"使用记录"按钮位置**
   - 考虑移到详情内容区域的独立卡片中
   - 或保留在标题栏但调整样式和位置

4. **保持IsReadOnly只读模式**
   - 查看时所有TextBox为IsReadOnly=true
   - 药材列表DataGrid为IsReadOnly=true

#### 验收标准

- [ ] 删除了顶部重复的基本信息大卡片
- [ ] 删除了"打印"按钮
- [ ] "使用记录"按钮位置优化（或移除如果非必须）
- [ ] 查看模式下所有字段为只读
- [ ] 药材组成列表正确显示序号、名称、用量、单位、单价等
- [ ] 显示创建时间和更新时间
- [ ] 编译无错误，运行时显示正常

---

### 2.3 编辑验方（Update）

#### User Story
> 作为中医诊所管理员，我希望能够编辑现有验方的信息，包括修改基本信息和调整药材组成，以便完善和更新验方数据。

#### 功能描述

**当前状态**:
- ✅ FormulaDetailView支持IsReadOnly切换（查看/编辑）
- 🔴 同时存在EditFormulaDialog弹窗编辑（冗余）
- ⚠️ IsReadOnly切换逻辑需要确认

**需求改进**:
1. **废弃EditFormulaDialog.xaml和EditFormulaDialogViewModel.cs**
   - 删除文件：`Views/EditFormulaDialog.xaml`, `ViewModels/EditFormulaDialogViewModel.cs`
   - 统一使用FormulaDetailView的IsReadOnly模式切换

2. **FormulaDetailView编辑模式**
   - 点击"编辑"按钮：IsReadOnly = false，显示"保存"/"取消"按钮
   - 点击"保存"按钮：验证 → 调用UpdateAsync → IsReadOnly = true
   - 点击"取消"按钮：恢复原始数据 → IsReadOnly = true

3. **FormulaManagementViewModel改进**
   - `EditCommand`导航到FormulaDetailView + 设置IsReadOnly=false参数
   - 而非打开EditFormulaDialog

4. **药材组成编辑**
   - 编辑模式下：DataGrid.IsReadOnly = false
   - 添加/编辑/删除药材操作按钮可用
   - 通过行内编辑或弹出小对话框编辑单个药材项

#### 验收标准

- [ ] 废弃并删除EditFormulaDialog相关文件
- [ ] 从列表页点击"编辑"按钮进入FormulaDetailView编辑模式
- [ ] 编辑模式下可修改所有字段
- [ ] 编辑模式下可添加/编辑/删除药材
- [ ] 点击"保存"验证通过后成功更新数据
- [ ] 点击"取消"放弃修改并返回只读模式
- [ ] 必填字段验证正确
- [ ] 编译无错误，运行时功能正常

---

### 2.4 删除验方（Delete）

#### User Story
> 作为中医诊所管理员，我希望能够删除不再使用的验方记录，以便保持验方数据库的整洁。

#### 功能描述

**当前状态**:
- ✅ FormulaManagementView列表页有"删除"按钮
- ✅ Server端已实现软删除（IsDeleted标记）

**需求改进**:
1. **确认对话框**
   - 删除前弹出确认对话框："确定要删除验方'{验方名称}'吗？此操作不可恢复。"
   - 确认后调用DeleteAsync

2. **业务规则检查**（如果需要）
   - 检查验方是否被处方引用
   - 如果被引用，提示无法删除："该验方正在被{N}个处方使用，无法删除"

3. **批量删除**（可选）
   - 支持选择多个验方批量删除
   - 调用BatchDeleteAsync

#### 验收标准

- [ ] 点击"删除"按钮弹出确认对话框
- [ ] 确认后成功删除验方（软删除）
- [ ] 删除后列表页自动刷新
- [ ] 如果验方被引用，提示无法删除
- [ ] 编译无错误，运行时功能正常

---

### 2.5 辅助功能

#### 2.5.1 搜索/筛选

**当前状态**: ✅ 已实现搜索功能（SearchCommand）

**需求改进**: 保持现有功能，无需改动。

#### 2.5.2 导入/导出

**当前状态**: ✅ 已实现导入模板、导出模板、导出验方功能

**需求改进**: 保持现有功能，确保按钮样式和位置符合统一规范。

#### 2.5.3 复制验方

**当前状态**: ✅ 已实现复制功能（CopyCommand）

**需求改进**: 保持现有功能，无需改动。

#### 2.5.4 药材验证

**当前状态**: ✅ 已实现药材验证功能（FormulaValidationView）

**需求改进**: 保持现有功能，无需改动。

---

## 3. 非功能性需求

### 3.1 性能要求

- 列表页加载时间：<2秒（100条记录）
- 详情页加载时间：<1秒
- 保存操作响应时间：<1秒
- 搜索响应时间：<500ms

### 3.2 安全性要求

- 所有输入字段进行XSS防护
- 验方名称、功效、用法等文本字段限制长度
- 药材用量验证为正数

### 3.3 可用性要求

- UI/UX对齐参考模块（Users/Herbs/Patients）
- 必填字段标红星提示
- 错误信息友好提示
- 操作响应即时反馈（加载遮罩、成功/失败提示）

### 3.4 兼容性要求

- 支持Windows 10/11
- .NET 8.0运行时
- 1920x1080及以上分辨率

---

## 4. 业务规则

### 4.1 验方数据规则

| 字段 | 类型 | 必填 | 长度限制 | 说明 |
|-----|------|------|---------|------|
| Name（验方名称） | string | ✅ | 最大100字符 | 唯一标识验方 |
| Effect（功效/功用） | string | ✅ | 最大200字符 | 验方的主要功效，如"清热生津" |
| Indications（主治） | string | ✅ | 最大500字符 | 主治症状，如"阳明气分热盛。症见壮热面赤，烦渴饮引，大汗恶热，脉洪大有力或滑数。" |
| Usage（用法） | string | ✅ | 最大200字符 | 服用方法和用量 |
| Category（分类） | string | ❌ | 最大100字符 | 验方分类（如解表剂、泻下剂） |
| Source（来源） | string | ❌ | 最大100字符 | 验方出处（如《伤寒论》） |
| Property（性味归经） | string | ❌ | 最大200字符 | 中医性味归经描述 |
| Description（描述） | string | ❌ | 最大1000字符 | 详细描述 |
| Remark（备注） | string | ❌ | 最大500字符 | 补充备注 |

### 4.2 药材组成规则

- 验方必须包含至少1味药材
- 每味药材必须指定：药材名称、用量、单位
- 药材用量必须为正数（>0）
- 药材单位：克(g)、两、钱、味等中医常用单位
- 炮制方法可选：生用、炒制、蜜炙等
- 支持药材排序（SortOrder字段）

### 4.3 验证规则

**客户端验证**（在FormulaValidator中实现）:
- 必填字段非空验证
- 字符串长度验证
- 数值范围验证（用量>0）
- 至少1味药材验证

**服务端验证**（已在FormulaService实现）:
- 使用FormulaInputDto的Data Annotations
- 验方名称唯一性检查（可选）
- 药材HerbId有效性检查

### 4.4 删除规则

- 软删除：设置IsDeleted=true，保留数据
- 删除前检查是否被处方引用（可选业务规则）
- 支持批量删除

---

## 5. 数据模型

### 5.1 FormulaDto（列表数据）

```csharp
public class FormulaDto : StatusDto, IRemarkable
{
    public Guid Id { get; set; }
    public string Name { get; set; }                  // 验方名称
    public string? Category { get; set; }             // 分类
    public string? Effect { get; set; }               // 功效
    public string? Source { get; set; }               // 来源
    public decimal TotalPrice { get; set; }           // 总价（计算字段）
    public int HerbCount { get; set; }                // 药材数量（计算字段）
    public CommonStatus Status { get; set; }          // 状态
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 5.2 FormulaDetailDto（详情数据）

```csharp
public class FormulaDetailDto : FormulaDto
{
    public string? Property { get; set; }                        // 性味归经
    public string? Usage { get; set; }                           // 用法
    public string? Indications { get; set; }                     // 适应症
    public string? Description { get; set; }                     // 描述
    public string? Difficulty { get; set; }                      // 配制难度
    public List<FormulaHerbItemDto> Herbs { get; set; } = new(); // 药材组成
}
```

### 5.3 FormulaHerbItemDto（药材组成项）

```csharp
public class FormulaHerbItemDto : BaseDto
{
    public Guid? HerbId { get; set; }              // 匹配的药材ID（验证后）
    public string? OriginalHerbName { get; set; }  // 原始药材名称（导入时）
    public bool IsValidated { get; set; }          // 是否已验证
    public string HerbName { get; set; }           // 药材名称
    public decimal Quantity { get; set; }          // 用量
    public string Unit { get; set; }               // 单位
    public decimal Price { get; set; }             // 单价
    public string? Preparation { get; set; }       // 炮制方法
    public string? Usage { get; set; }             // 用法
    public int SortOrder { get; set; }             // 排序
}
```

### 5.4 FormulaInputDto（创建/更新输入）

```csharp
public class FormulaInputDto : IRemarkable
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Effect { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Usage { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(500)]
    public string? Remark { get; set; }
    
    // ... 其他字段
    
    public List<FormulaHerbItemInputDto> Herbs { get; set; } = new();
}
```

---

## 6. UI/UX设计规范

### 6.1 页面结构（对齐参考模块）

#### FormulaManagementView（列表页）✅
```
┌─────────────────────────────────────────────┐
│ 工具栏（UnifiedManagementToolBar）           │
│ - 搜索框                                    │
│ - 操作按钮：导入/导出/新增/刷新/返回主页      │
├─────────────────────────────────────────────┤
│ 数据表格（UnifiedManagementTable）           │
│ - 列：名称/分类/功效/来源/总价/药材数/状态    │
│ - 操作：查看/编辑/复制/删除                  │
├─────────────────────────────────────────────┤
│ 分页栏（UnifiedPaginationBar）               │
└─────────────────────────────────────────────┘
```

#### FormulaCreateView（创建页）🔴 需新建
```
┌─────────────────────────────────────────────┐
│ 标题栏：← 返回 | 新增验方 | 💾保存 ✖取消   │
├─────────────────────────────────────────────┤
│ 滚动内容区域                                 │
│ ┌─────────────────────────────────────────┐ │
│ │ 📋 基本信息（Expander，默认展开）        │ │
│ │ - 验方名称* | 配制难度                   │ │
│ │ - 性味归经                               │ │
│ │ - 功效*                                  │ │
│ │ - 用法*                                  │ │
│ └─────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────┐ │
│ │ 💊 药材组成（Expander，默认展开）        │ │
│ │ - 工具栏：➕添加药材                     │ │
│ │ - DataGrid：序号/名称/用量/单位/炮制/... │ │
│ │ - 操作：编辑/删除                        │ │
│ └─────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────┐ │
│ │ 📝 备注（Expander，可折叠）              │ │
│ │ - 验方描述                               │ │
│ │ - 备注                                   │ │
│ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

#### FormulaDetailView（详情页）⚠️ 需改进
```
┌─────────────────────────────────────────────┐
│ 标题栏：← 返回 | 验方详情 | ✏编辑 📋复制   │
│        （查看模式）                          │
│ 或                                          │
│ 标题栏：← 返回 | 验方详情 | 💾保存 ✖取消   │
│        （编辑模式）                          │
├─────────────────────────────────────────────┤
│ 🔴 删除此重复卡片：                         │
│ ┌───────────────────────────────────────┐   │
│ │ 📜图标 | 验方名称 | 状态徽章            │   │
│ │        | 分类·药材数                   │   │
│ │        | 总价·难度                     │   │
│ └───────────────────────────────────────┘   │
├─────────────────────────────────────────────┤
│ 滚动内容区域                                 │
│ ┌─────────────────────────────────────────┐ │
│ │ 📋 基本信息（Expander）                  │ │
│ │ - 验方名称 | 配制难度                    │ │
│ │ - 性味归经                               │ │
│ │ - 功效                                   │ │
│ │ - 用法                                   │ │
│ │ - 创建时间 | 更新时间                    │ │
│ └─────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────┐ │
│ │ 💊 药材组成（Expander）                  │ │
│ │ - 统计：共X味药材，总价¥XX.XX           │ │
│ │ - DataGrid：序号/名称/用量/单位/单价/... │ │
│ └─────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────┐ │
│ │ 📝 详细描述（Expander）                  │ │
│ │ - 验方描述                               │ │
│ │ - 备注                                   │ │
│ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

### 6.2 布局规范

- **卡片样式**: 使用`{StaticResource CardStyle}`，自动阴影和圆角
- **Expander**: 用于组织复杂内容，默认展开重要信息
- **Grid布局**: 4列布局（Label 120px / Input * / Label 120px / Input *）
- **间距**: Margin使用`{StaticResource SpacingSmall}`统一间距
- **按钮颜色**: 
  - 主要操作：SuccessButton（绿色）
  - 次要操作：SecondaryButton（灰色）
  - 危险操作：DangerButton（红色）
  - 信息操作：InfoButton（蓝色）

### 6.3 交互规范

- **IsReadOnly切换**: 
  - 查看模式：所有TextBox.IsReadOnly=true，显示"编辑"按钮
  - 编辑模式：所有TextBox.IsReadOnly=false，显示"保存"/"取消"按钮
- **必填字段**: 标签后加红星（*）提示
- **验证提示**: 字段下方显示红色错误文本
- **确认对话框**: 删除等危险操作前弹出确认
- **加载状态**: 显示半透明遮罩 + 转圈进度条 + "正在加载..."文本
- **成功/失败提示**: 使用Toast通知或状态栏消息

### 6.4 样式规范

- **字体大小**: 
  - 标题：20pt（粗体）
  - 正文：14pt
  - 辅助文本：12pt（灰色）
- **颜色**: 
  - 主色：`{DynamicResource PrimaryHueMidBrush}`
  - 成功：`{DynamicResource SuccessBrush}`
  - 警告：`{DynamicResource WarningBrush}`
  - 危险：`{DynamicResource DangerBrush}`
- **图标**: 使用Unicode Emoji（📜💊📋✏💾✖等）

---

## 7. 架构设计

### 7.1 三层架构对齐

```
Client端（WPF）                    Server端（ASP.NET Core）
┌─────────────────────┐           ┌──────────────────────┐
│ Views               │           │ Controllers          │
│ - FormulaManagement │  ←HTTP→   │ - FormulaController  │
│ - FormulaCreate     │           └──────────────────────┘
│ - FormulaDetail     │                      ↓
└─────────────────────┘           ┌──────────────────────┐
         ↓                        │ Services             │
┌─────────────────────┐           │ - FormulaService     │
│ ViewModels          │           └──────────────────────┘
│ - FormulaManagement │                      ↓
│ - FormulaCreate     │           ┌──────────────────────┐
│ - FormulaDetail     │           │ Repositories         │
└─────────────────────┘           │ - FormulaRepository  │
         ↓                        └──────────────────────┘
┌─────────────────────┐                      ↓
│ Components          │           ┌──────────────────────┐
│ - FormulaCommand    │           │ DbContext            │
│   Handler           │           │ - AppDbContext       │
│ - FormulaData       │           └──────────────────────┘
│   Manager           │
│ - FormulaValidator  │
└─────────────────────┘
         ↓
┌─────────────────────┐
│ Shared Models       │
│ - FormulaDto        │
│ - FormulaDetailDto  │
│ - FormulaInputDto   │
└─────────────────────┘
```

### 7.2 组件设计

#### FormulaCommandHandler（命令处理器）
```csharp
public class FormulaCommandHandler
{
    private readonly IFormulaRepository _repository;
    private readonly ILogger<FormulaCommandHandler> _logger;
    
    // 创建验方
    public async Task<Result<Guid>> CreateFormulaAsync(FormulaInputDto input);
    
    // 更新验方
    public async Task<Result> UpdateFormulaAsync(Guid id, FormulaInputDto input);
    
    // 删除验方
    public async Task<Result> DeleteFormulaAsync(Guid id);
    
    // 批量删除
    public async Task<Result> BatchDeleteAsync(List<Guid> ids);
}
```

#### FormulaDataManager（数据管理器）
```csharp
public class FormulaDataManager
{
    private readonly IFormulaRepository _repository;
    
    // 获取分页列表
    public async Task<PagedResult<FormulaDto>> GetPagedAsync(int page, int pageSize, string? keyword);
    
    // 获取详情
    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id);
    
    // 搜索
    public async Task<List<FormulaDto>> SearchAsync(string keyword);
}
```

#### FormulaValidator（验证器）
```csharp
public class FormulaValidator : AbstractValidator<FormulaInputDto>
{
    public FormulaValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Effect).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Usage).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Herbs).NotEmpty().WithMessage("至少添加1味药材");
        RuleForEach(x => x.Herbs).SetValidator(new FormulaHerbItemValidator());
    }
}
```

### 7.3 导航流程

```
列表页（FormulaManagementView）
  ├─ [新增] → FormulaCreateView → [保存] → 返回列表页
  ├─ [查看] → FormulaDetailView（IsReadOnly=true）
  │           ├─ [编辑] → 切换IsReadOnly=false
  │           ├─ [保存] → 更新 → 切换IsReadOnly=true
  │           └─ [取消] → 放弃修改 → 切换IsReadOnly=true
  ├─ [编辑] → FormulaDetailView（IsReadOnly=false）
  ├─ [复制] → 复制数据 → FormulaCreateView（预填充数据）
  └─ [删除] → 确认对话框 → 删除 → 刷新列表
```

---

## 8. 缺失功能清单

| 序号 | 功能 | 当前状态 | 需实现内容 | 优先级 |
|-----|------|---------|-----------|--------|
| 1 | 独立创建页面 | ❌ 缺失 | 新建FormulaCreateView.xaml + FormulaCreateViewModel.cs | 🔴 P0 |
| 2 | 创建页面导航 | ❌ 错误 | FormulaManagementViewModel.AddFormulaCommand改为导航到FormulaCreateView | 🔴 P0 |
| 3 | ViewModel实现 | ❌ 缺失 | 实现FormulaCreateViewModel的保存/取消逻辑 | 🔴 P0 |
| 4 | 药材组成编辑 | ⚠️ 部分 | 创建页和详情页的药材添加/编辑/删除功能 | 🔴 P0 |

---

## 9. 错误修复清单

| 序号 | 问题 | 影响 | 修复方案 | 优先级 |
|-----|------|------|---------|--------|
| 1 | 使用Dialog创建 | UI/UX不一致 | 废弃EditFormulaDialog，改用FormulaCreateView | 🔴 P0 |
| 2 | 使用Dialog编辑 | UI/UX不一致 | 废弃EditFormulaDialog，使用FormulaDetailView的IsReadOnly切换 | 🔴 P0 |
| 3 | 顶部重复卡片 | 重复内容 | 删除FormulaDetailView.xaml中Grid.Row="0"的Border卡片 | 🟡 P1 |
| 4 | 打印按钮 | 不必要功能 | 删除FormulaDetailView标题栏的PrintCommand按钮 | 🟡 P1 |
| 5 | EditFormulaDialog.xaml | 冗余文件 | 删除EditFormulaDialog.xaml | 🟡 P1 |
| 6 | EditFormulaDialogViewModel.cs | 冗余文件 | 删除EditFormulaDialogViewModel.cs | 🟡 P1 |

---

## 10. 技术约束

### 10.1 技术栈

**允许使用**:
- ✅ .NET 8.0
- ✅ WPF
- ✅ Prism 8.x（ViewModelLocator, IRegionManager, IEventAggregator, IDialogService）
- ✅ EF Core 8.0（Server端Repository）
- ✅ LINQ（所有数据查询）
- ✅ xUnit + NSubstitute（单元测试）
- ✅ FluentValidation（数据验证）

**禁止使用**（MVP阶段）:
- ❌ Redis/RabbitMQ（无分布式需求）
- ❌ CQRS/MediatR（业务规则简单）
- ❌ 微服务（单体应用足够）
- ❌ GraphQL（REST API足够）

### 10.2 MVP原则

- **够用即好**: 实现基础CRUD，不超前设计
- **拒绝超前设计**: 不引入复杂架构模式
- **简单直接**: 优先简单实现，避免过度抽象
- **快速交付**: 功能完整可用即发布

### 10.3 架构约束

- **三层对齐**: Client端MVVM + Server端Repository-Service-Controller
- **依赖方向**: View → ViewModel → Components → Shared Models
- **接口统一**: IRepository<T>继承自Shared层
- **软删除**: 所有删除操作使用IsDeleted标记
- **异步优先**: 所有I/O操作必须使用async/await

---

## 11. 验收标准

### 11.1 功能完整性

- [ ] 创建验方：独立页面，支持添加药材，验证通过后保存成功
- [ ] 查看验方：显示完整信息，无重复内容，无不必要按钮
- [ ] 编辑验方：使用FormulaDetailView的IsReadOnly切换，保存成功
- [ ] 删除验方：确认对话框，软删除成功，列表刷新
- [ ] 搜索/筛选：快速搜索验方名称、功效等字段
- [ ] 导入/导出：正确导入导出Excel数据
- [ ] 复制验方：复制后生成新验方记录

### 11.2 UI/UX一致性

- [ ] 使用统一组件（UnifiedManagementToolBar/Table/PaginationBar）
- [ ] 删除了FormulaDetailView的顶部重复卡片
- [ ] 删除了"打印"按钮
- [ ] 新增了FormulaCreateView独立创建页面
- [ ] 废弃了EditFormulaDialog弹窗
- [ ] 布局和样式对齐Herbs/Patients模块

### 11.3 代码质量

- [ ] 编译无错误、无警告
- [ ] 代码符合项目编码规范（PascalCase, _camelCase）
- [ ] 中文注释完整
- [ ] 无硬编码字符串（使用资源文件）
- [ ] 异步方法正确使用async/await
- [ ] 无内存泄漏（ViewModel正确Dispose）

### 11.4 测试覆盖

- [ ] FormulaCreateViewModel单元测试（参考UserDetailViewModelTests）
- [ ] FormulaDetailViewModel单元测试
- [ ] FormulaCommandHandler单元测试
- [ ] FormulaValidator单元测试
- [ ] 手动测试所有CRUD操作
- [ ] 手动测试边界条件（必填字段、用量验证等）

---

## 12. 用户决策（已确认）

用户已确认所有开放问题，按照推荐选项执行：

| 问题 | 决策 | 说明 |
|-----|------|------|
| Q1: "使用记录"按钮 | ✅ 选项C | 暂时移除，非MVP核心功能 |
| Q2: 编辑模式 | ✅ 选项A | 使用FormulaDetailView的IsReadOnly切换，对齐Herbs模块 |
| Q3: 删除检查处方引用 | ✅ 选项B | 仅软删除，不检查引用，简化MVP实现 |
| Q4: 批量删除 | ✅ 选项B | 仅单个删除，简化MVP实现 |

### 关键字段确认

用户明确要求以下两个字段**必须为必填**：

| 字段 | 说明 | 示例 |
|-----|------|------|
| **Effect（功效/功用）** | 验方的主要功效 | "清热生津" |
| **Indications（主治）** | 主治症状的详细描述 | "阳明气分热盛。症见壮热面赤，烦渴饮引，大汗恶热，脉洪大有力或滑数。" |

**实施要求**:
1. FormulaInputDto中为Effect和Indications添加`[Required]`特性
2. UI中为这两个字段标红星（*）提示
3. 验证器中添加非空验证

---

## 13. 实施计划（参考）

### Phase 1: 清理和修复（1-2天）
1. 删除FormulaDetailView顶部重复卡片
2. 删除"打印"按钮
3. 优化"使用记录"按钮位置（或移除）
4. 测试FormulaDetailView的IsReadOnly切换

### Phase 2: 新增创建页面（2-3天）
1. 新建FormulaCreateView.xaml（参考HerbCreateView）
2. 新建FormulaCreateViewModel.cs
3. 实现保存/取消逻辑
4. 实现药材添加/编辑/删除
5. FormulaManagementViewModel.AddFormulaCommand改为导航

### Phase 3: 废弃Dialog模式（1天）
1. 删除EditFormulaDialog.xaml
2. 删除EditFormulaDialogViewModel.cs
3. FormulaManagementViewModel.EditCommand改为导航到FormulaDetailView

### Phase 4: 测试和完善（1-2天）
1. 编写单元测试
2. 手动测试所有CRUD操作
3. 修复发现的Bug
4. 代码审查和优化

**预计总工期**: 5-8天

---

## 14. 参考资料

- **Herbs模块实现**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/`
- **Patients模块实现**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/`
- **Users模块实现**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/`
- **统一组件库**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/`
- **Issue #1840**: 统一组件标准化
- **三层架构文档**: `docs/explanation/architecture/`

---

**文档状态**: ✅ 需求分析完成，待用户审核  
**下一步**: 用户审核需求文档 → 确认开放问题 → 开始实施

---
