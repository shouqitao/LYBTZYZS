# 患者选择器需求文档

## 📋 文档信息

| 项目 | 内容 |
|-----|------|
| **功能名称** | 患者选择器 (Patient Selector) |
| **Spec编号** | SPEC-2025-002 |
| **创建日期** | 2025-10-14 |
| **创建人** | Claude Code |
| **状态** | 待审批 |
| **版本** | v1.0 |
| **依赖关系** | 无（基础组件） |
| **被依赖** | clinical-workbench（临床工作台） |

---

## 1. 项目背景

### 1.1 问题描述

在门诊系统中，"选择患者"是所有业务流程的起点。当前系统存在以下问题：

1. **重复开发**：各模块独立实现患者选择，代码重复
2. **体验不一致**：不同模块的搜索逻辑和交互各异
3. **功能分散**：搜索、创建患者的代码分散在各处
4. **难以维护**：修改患者选择逻辑需要改多处代码

### 1.2 解决方案

开发一个**独立的、可复用的患者选择器组件**，提供统一的患者搜索、创建和选择功能，供所有业务模块使用。

### 1.3 组件定位

- **类型**：可复用的基础UI组件（UserControl）
- **位置**：`LYBT.Desktop.Common.Components.PatientSelector`
- **职责**：搜索、创建、选择患者，发布选择事件
- **解耦方式**：通过 EventAggregator 发布事件，不依赖任何业务模块

---

## 2. 目标用户

### 2.1 直接用户（开发者）

- **前端开发者**：在业务模块中集成患者选择器
- **使用场景**：临床工作台、病案管理、处方管理、统计报表等

### 2.2 最终用户（医护人员）

- **医生**：门诊看诊时选择患者
- **护士**：接诊登记时选择或创建患者
- **药师**：配药时查询患者信息

---

## 3. 功能需求

### FR1: 患者搜索

**优先级**：P0（必须）

**描述**：
- 提供搜索框，支持实时搜索
- 支持按姓名模糊搜索
- 支持按手机号精确搜索
- 显示搜索结果列表

**验收标准**：
- [x] 输入≥2个字符开始搜索
- [x] 防抖延迟300ms
- [x] 支持姓名拼音/汉字搜索
- [x] 支持手机号搜索（11位数字）
- [x] 搜索结果按相关性排序：
  1. 精确匹配优先
  2. 最近就诊优先
  3. 就诊次数多优先
- [x] 最多显示10条结果
- [x] 显示Loading状态
- [x] 无结果时提示"未找到患者，是否创建新患者？"

**搜索结果显示格式**：
```
张三              男/45岁
138****5678       上次就诊：2025-01-10
──────────────────────────────────
李四              女/32岁  
159****1234       上次就诊：2024-12-20
```

**技术实现**：
- 搜索请求：`IPatientRepository.SearchAsync(keyword, pageSize: 10)`
- 防抖：使用ReactiveUI或手动实现
- 排序：服务端返回已排序结果，客户端不再排序

---

### FR2: 患者创建（快速）

**优先级**：P0（必须）

**描述**：
- 点击"新患者"按钮或"未找到患者"提示，打开创建对话框
- 快速创建模式：只录入核心必填信息
- 创建成功后自动选中该患者

**创建对话框布局**：
```
┌───────────────────────────────────────┐
│  新建患者档案                          │
├───────────────────────────────────────┤
│  * 姓名：    [___________________]    │
│                                       │
│  * 性别：    ○ 男  ○ 女  ○ 未知      │
│                                       │
│  * 手机号：  [___________________]    │
│             （用于登记和查询）         │
│                                       │
│    年龄：    [_____] 岁（可选）       │
│                                       │
│    身份证：  [___________________]    │
│             （可选，18位）             │
│                                       │
│  [取消]                  [创建并选择]  │
└───────────────────────────────────────┘
```

**验收标准**：
- [x] 必填项：姓名、性别、手机号
- [x] 可选项：年龄、身份证
- [x] 姓名：1-50字符
- [x] 手机号：11位数字，1开头，格式验证
- [x] 身份证：15或18位，支持X结尾，格式验证
- [x] 年龄与身份证联动：填写身份证自动计算年龄
- [x] 重复检查：创建前检查手机号是否已存在
- [x] 如手机号重复，提示"该手机号已被 XXX 使用，是否直接选择？"
- [x] 创建成功后：
  - 关闭对话框
  - 自动选中新患者
  - 发布 `PatientSelectedEvent`
  - Toast提示"患者档案创建成功"

**数据模型**：
```csharp
CreatePatientRequest {
    Name: string (必填, 1-50)
    Gender: Gender enum (必填)
    PhoneNumber: string (必填, 11位)
    Age: int? (可选, 1-150)
    IdCard: string? (可选, 15或18位)
}
```

---

### FR3: 患者选择确认

**优先级**：P0（必须）

**描述**：
- 从搜索结果中选择患者
- 显示选中状态
- 发布患者选择事件

**验收标准**：
- [x] 点击搜索结果选中患者
- [x] 支持键盘上下箭头选择
- [x] 回车键确认选择
- [x] 选中后发布 `PatientSelectedEvent`
- [x] 事件Payload包含完整患者信息
- [x] 搜索框显示选中患者姓名
- [x] 结果列表收起

---

### FR4: 事件发布（核心接口）

**优先级**：P0（必须）

**描述**：
- 定义 `PatientSelectedEvent` 及其 Payload
- 患者选择后发布事件
- 供业务模块订阅

**事件定义**：
```csharp
namespace LYBT.Desktop.Common.Events;

/// <summary>
/// 患者选择事件
/// </summary>
public class PatientSelectedEvent : PubSubEvent<PatientSelectedPayload> { }

/// <summary>
/// 患者选择事件负载
/// </summary>
public class PatientSelectedPayload
{
    /// <summary>
    /// 患者ID（核心标识）
    /// </summary>
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; } = string.Empty;
    
    /// <summary>
    /// 性别（男/女/未知）
    /// </summary>
    public string Gender { get; set; } = string.Empty;
    
    /// <summary>
    /// 年龄
    /// </summary>
    public int? Age { get; set; }
    
    /// <summary>
    /// 手机号
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 上次就诊日期
    /// </summary>
    public DateTime? LastVisitDate { get; set; }
    
    /// <summary>
    /// 就诊次数
    /// </summary>
    public int VisitCount { get; set; }
    
    /// <summary>
    /// 过敏史（重要提醒）
    /// </summary>
    public string? AllergyHistory { get; set; }
    
    /// <summary>
    /// 选择时间戳
    /// </summary>
    public DateTime SelectedAt { get; set; } = DateTime.Now;
}
```

**验收标准**：
- [x] 事件类定义在 `LYBT.Desktop.Common.Events`
- [x] Payload 包含上述所有字段
- [x] 患者选择后正确发布事件
- [x] 订阅者能接收到完整数据
- [x] 不依赖任何业务模块

**设计说明**：
- 传递完整信息而非只传PatientId：避免订阅者重复查询
- 包含AllergyHistory：安全提醒，订阅者可直接显示
- SelectedAt：便于追踪和日志记录

---

## 4. 非功能需求

### NFR1: 性能要求

**优先级**：P0

- 组件加载时间 ≤ 500ms
- 搜索响应时间 ≤ 300ms（不含网络延迟）
- 创建患者响应时间 ≤ 1秒
- 防抖延迟：300ms
- 搜索结果缓存：5分钟（减少重复请求）

### NFR2: 可用性要求

**优先级**：P0

- 支持键盘导航（Tab、上下箭头、Enter）
- 错误提示友好（标注错误字段，说明原因）
- 无结果提示清晰（引导用户创建新患者）
- Loading状态明确（搜索时显示进度图标）
- 自动聚焦：组件挂载后自动聚焦搜索框

### NFR3: 可复用性要求

**优先级**：P0

- 作为UserControl可在任何View中嵌入
- 不依赖特定布局或父容器
- 通过事件解耦，不依赖业务模块
- 可配置样式（继承MaterialDesign主题）

### NFR4: 可维护性要求

**优先级**：P0

- 遵循 MVVM 三层架构
- ViewModel 单元测试覆盖率 ≥ 80%
- 代码符合 `unified-design-standard.md` 规范
- 清晰的注释和文档

### NFR5: 可扩展性要求

**优先级**：P1

- 预留高级搜索接口（按身份证、地址等）
- 预留智能推荐接口（常用患者）
- 预留历史记录接口（最近选择的患者）

---

## 5. 约束条件

### 5.1 技术约束

1. **架构约束**：
   - MVVM 三层架构
   - Prism 框架（EventAggregator、Command）
   - MaterialDesignInXaml 界面库

2. **位置约束**：
   - 组件位置：`src/Client/Desktop/Core/LYBT.Desktop.Common/Components/PatientSelector/`
   - 事件位置：`src/Client/Desktop/Core/LYBT.Desktop.Common/Events/`

3. **依赖约束**：
   - 可依赖：`LYBT.Desktop.Models.PatientItem`、`IPatientRepository`
   - 禁止依赖：任何业务模块（如 MedicalCase、Consultation）

### 5.2 业务约束

1. **数据完整性**：
   - 手机号唯一性（不允许重复）
   - 创建患者前必须检查重复

2. **权限约束**：
   - MVP 阶段不考虑权限控制
   - 所有用户均可搜索和创建患者

3. **并发约束**：
   - MVP 阶段不考虑并发冲突
   - 同一时间只有一个创建请求

### 5.3 时间约束

- 开发周期：1-2周
- 测试周期：3天
- 上线时间：作为 clinical-workbench 的前置依赖

---

## 6. MVP 范围

### 6.1 MVP 包含功能

✅ **必须实现**：
- 患者搜索（姓名、手机号）
- 搜索结果排序和显示
- 快速创建患者（必填项）
- 重复检查
- 患者选择确认
- 发布 PatientSelectedEvent
- 基础验证（手机号、身份证格式）
- 键盘导航支持

### 6.2 MVP 不包含功能

❌ **后续迭代**：
- 高级搜索（按身份证、地址、病历号）
- 智能推荐（常用患者）
- 历史记录（最近选择）
- 患者档案详情查看
- 患者档案编辑
- 批量导入患者
- 患者头像上传
- 搜索历史记录

---

## 7. 界面设计（概要）

### 7.1 组件布局

**嵌入模式**（在工作台中使用）：
```
┌─────────────────────────────────────┐
│  [🔍 搜索患者（姓名/手机号）____] [新患者] │
├─────────────────────────────────────┤
│  搜索结果（下拉列表）                │
│  ┌───────────────────────────────┐ │
│  │ 张三        男/45岁            │ │
│  │ 138****5678  上次：2025-01-10  │ │
│  ├───────────────────────────────┤ │
│  │ 李四        女/32岁            │ │
│  │ 159****1234  上次：2024-12-20  │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
```

**独立模式**（在对话框中使用）：
```
┌─────────────────────────────────────┐
│  选择患者                            │
├─────────────────────────────────────┤
│  [🔍 搜索患者______________] [新患者]  │
│                                     │
│  搜索结果：                          │
│  ┌───────────────────────────────┐ │
│  │ [患者列表...]                  │ │
│  └───────────────────────────────┘ │
│                                     │
│  [取消]                      [确定]  │
└─────────────────────────────────────┘
```

### 7.2 交互流程

```
搜索框输入 → 防抖300ms → 发起搜索 → 显示结果 → 点击选择 → 发布事件

        ↓（无结果）
        
提示"未找到患者" → 点击"创建新患者" → 打开对话框 → 填写信息 → 创建 → 发布事件
```

---

## 8. 技术设计（概要）

### 8.1 组件结构

```
PatientSelector/
  ├── Views/
  │   ├── PatientSelectorControl.xaml          # 搜索框组件
  │   ├── PatientSelectorControl.xaml.cs
  │   ├── CreatePatientDialog.xaml             # 创建对话框
  │   └── CreatePatientDialog.xaml.cs
  │
  ├── ViewModels/
  │   ├── PatientSelectorViewModel.cs          # 搜索逻辑
  │   └── CreatePatientViewModel.cs            # 创建逻辑
  │
  ├── Models/
  │   └── (复用 PatientItem)
  │
  └── Converters/
      └── PhoneNumberMaskConverter.cs          # 手机号脱敏
```

### 8.2 依赖注入

```csharp
// PatientSelectorViewModel
public PatientSelectorViewModel(
    IPatientRepository patientRepository,
    IEventAggregator eventAggregator,
    IDialogService dialogService)
{
    _patientRepository = patientRepository;
    _eventAggregator = eventAggregator;
    _dialogService = dialogService;
}
```

### 8.3 事件发布

```csharp
// 选择患者后
var payload = new PatientSelectedPayload
{
    PatientId = selectedPatient.Id,
    PatientName = selectedPatient.Name,
    Gender = selectedPatient.Gender,
    Age = selectedPatient.Age,
    PhoneNumber = selectedPatient.PhoneNumber,
    LastVisitDate = selectedPatient.LastVisitDate,
    VisitCount = selectedPatient.VisitCount,
    AllergyHistory = selectedPatient.AllergyHistory,
    SelectedAt = DateTime.Now
};

_eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);
```

### 8.4 业务模块订阅示例

```csharp
// ClinicalWorkbenchViewModel
public ClinicalWorkbenchViewModel(IEventAggregator eventAggregator)
{
    eventAggregator
        .GetEvent<PatientSelectedEvent>()
        .Subscribe(OnPatientSelected);
}

private void OnPatientSelected(PatientSelectedPayload payload)
{
    // 1. 更新选中患者
    SelectedPatient = payload;
    
    // 2. 加载历史病案
    await LoadPatientHistoryAsync(payload.PatientId);
    
    // 3. 启用"新建病案"按钮
    CanCreateMedicalCase = true;
    
    // 4. 显示过敏史提醒
    if (!string.IsNullOrEmpty(payload.AllergyHistory))
    {
        ShowAllergyWarning(payload.AllergyHistory);
    }
}
```

---

## 9. 验收标准

### 9.1 功能验收

✅ **搜索功能**：
- [ ] 输入2个字符开始搜索
- [ ] 姓名模糊匹配正确
- [ ] 手机号精确匹配正确
- [ ] 结果排序符合规则
- [ ] 防抖延迟300ms生效
- [ ] 显示Loading状态
- [ ] 无结果提示显示

✅ **创建功能**：
- [ ] 必填项验证生效
- [ ] 手机号格式验证正确
- [ ] 身份证格式验证正确
- [ ] 年龄与身份证联动正确
- [ ] 重复检查生效
- [ ] 创建成功自动选中
- [ ] 发布PatientSelectedEvent

✅ **选择功能**：
- [ ] 点击结果正确选中
- [ ] 键盘导航生效
- [ ] 事件Payload完整
- [ ] 订阅者能接收事件

### 9.2 性能验收

- [ ] 搜索响应 ≤ 300ms
- [ ] 创建响应 ≤ 1秒
- [ ] 组件加载 ≤ 500ms

### 9.3 可用性验收

- [ ] 支持键盘导航
- [ ] 错误提示友好
- [ ] 无结果提示清晰
- [ ] Loading状态明确

### 9.4 可复用性验收

- [ ] 可在多个模块中嵌入使用
- [ ] 不依赖特定业务逻辑
- [ ] 事件订阅者可独立工作

---

## 10. 风险与缓解

### 风险1：搜索性能问题

**风险等级**：中

**影响**：患者数据量大时搜索慢

**缓解措施**：
- 服务端分页和索引优化
- 客户端防抖和结果限制
- 添加搜索缓存

### 风险2：手机号重复冲突

**风险等级**：低

**影响**：不同患者使用同一手机号

**缓解措施**：
- 创建前强制检查重复
- 提示用户选择已有患者
- 记录重复尝试日志

### 风险3：事件订阅丢失

**风险等级**：低

**影响**：业务模块未正确订阅事件

**缓解措施**：
- 在文档中明确订阅方式
- 提供订阅示例代码
- 单元测试验证事件发布

---

## 11. 后续迭代计划

### Phase 2: 体验优化（2025年3月）

- 智能推荐（常用患者快速选择）
- 搜索历史记录
- 高级搜索（身份证、地址）

### Phase 3: 功能增强（2025年4月）

- 患者档案详情查看
- 患者档案编辑
- 患者头像上传

---

## 12. 相关文档

- **架构标准**：`docs/architecture/client/unified-design-standard.md`
- **编码规范**：`docs/development/standards.md`
- **依赖需求**：`clinical-workbench/requirements.md`（被本组件依赖）

---

## 13. 附录

### 附录A：使用示例

#### 示例1：在XAML中嵌入

```xaml
<UserControl xmlns:common="clr-namespace:LYBT.Desktop.Common.Components.PatientSelector">
    <Grid>
        <common:PatientSelectorControl />
    </Grid>
</UserControl>
```

#### 示例2：订阅事件

```csharp
public class MyViewModel : BindableBase
{
    public MyViewModel(IEventAggregator eventAggregator)
    {
        eventAggregator
            .GetEvent<PatientSelectedEvent>()
            .Subscribe(OnPatientSelected);
    }
    
    private void OnPatientSelected(PatientSelectedPayload payload)
    {
        Debug.WriteLine($"患者选择：{payload.PatientName}");
    }
}
```

### 附录B：验证规则

| 字段 | 验证规则 | 错误提示 |
|-----|---------|---------|
| 姓名 | 1-50字符 | "姓名长度为1-50个字符" |
| 手机号 | 11位数字，1开头 | "请输入正确的手机号" |
| 身份证 | 15或18位，支持X | "请输入正确的身份证号" |
| 年龄 | 1-150 | "年龄范围为1-150岁" |

---

**文档结束**

_此文档将与 `clinical-workbench/requirements.md` 联合审批。_
