# 医案模块-处方功能增强需求文档

> **文档类型**: Requirements Specification (需求规格说明书)
> **创建日期**: 2025-11-20
> **Epic**: 医案模块完善
> **关联模块**: MedicalCase, Consultation, Prescription
> **参考实现**: Formula模块药材编辑逻辑

---

## 1. 需求概述

### 1.1 Epic背景

医案模块是中医诊所管理系统的核心业务模块,完整的医案包含两个关键部分:
- **诊断(Consultation)**: 记录望闻问切、中医诊断、治疗原则
- **处方(Prescription)**: 记录药材组成、用量、价格

当前Prescription模块基础框架已搭建,但缺少核心的药材编辑功能。用户明确要求:
> "处方界面有组合药方的功能,**逻辑参考经验方中的药材编辑**。但是处方需要计算价格。"

### 1.2 核心目标

**目标1: 移植Formula模块的药材编辑体验**
- 7级智能拼音过滤算法
- HerbCardControl卡片式UI组件
- 键盘自动焦点管理(TextBox → Dosage → Next Card)

**目标2: 集成价格计算功能**
- 实时计算每个药材项的小计: ItemAmount = UnitPrice × Dosage
- 实时计算处方总价: TotalAmount = Σ(ItemAmount) × DosageCount × (1 - Discount)

**目标3: 支持经验方模板导入**
- 医生可以选择Formula作为处方模板
- 自动填充药材名称、用量、单位
- 自动查询并填充单价

### 1.3 设计原则

**核心设计原则** (用户强调):
> "医案模块的设计有一个重点是: **方便医生看诊**。(新建阶段的UI交互要简单明了)"

具体体现:
- **键盘操作优先**: 减少鼠标使用,提高录入速度
- **拼音码输入**: 支持拼音首字母快速匹配(例如"dg"匹配"当归")
- **自动焦点管理**: 输入完成后自动跳转,无需手动切换
- **智能容错**: 模糊匹配,降低输入错误率
- **实时反馈**: 价格实时计算,所见即所得

---

## 2. 功能性需求

### FR-001: 7级智能拼音药材搜索

**需求描述**:
用户在药材名称输入框输入文本时,系统实时过滤药材列表并按智能评分排序。

**详细规格**:

**7级评分规则**:
1. **100分**: 精确名称匹配 (例如: 输入"当归" = "当归")
2. **90分**: 精确拼音匹配 (例如: 输入"danggui" = "danggui")
3. **80分**: 名称前缀匹配 (例如: 输入"当" → "当归")
4. **70分**: 拼音前缀匹配 (例如: 输入"dg" → "danggui")
5. **50分**: 名称包含匹配 (例如: 输入"归" → "当归")
6. **40分**: 拼音包含匹配 (例如: 输入"gg" → "danggui")
7. **30分**: 拼音模糊匹配 (例如: 输入"dg" → "d_a_n_g_g_u_i"首字母跳跃)

**过滤逻辑**:
- 计算所有药材的匹配分数
- 按分数从高到低排序
- 最多显示5个匹配结果
- 如果输入文本与某个药材精确匹配(忽略大小写),自动隐藏下拉列表

**参考实现**: `FormulaHerbItemViewModel.GetMatchScore()` + `IsPinyinFuzzyMatch()`

**验收标准**:
- [ ] 输入"dg"能匹配到"当归"并排在前3位
- [ ] 输入"黄"能匹配到"黄芪"、"黄连"、"黄柏"等
- [ ] 输入完整名称后下拉列表自动消失
- [ ] 过滤响应时间 < 100ms

---

### FR-002: 药材卡片UI组件(HerbCardControl)

**需求描述**:
提供卡片式药材录入界面,支持药材名称选择、用量输入、单价显示、小计显示。

**UI组件结构**:

```
┌─────────────────────────────────────────────────┐
│ HerbCardControl                                 │
├─────────────────────────────────────────────────┤
│ 药材名称: [当归          ▼]  用量: [10  ] g     │
│ 单价: ¥15.00         小计: ¥150.00        [X]  │
└─────────────────────────────────────────────────┘
```

**组件属性**:
- `HerbName` (string): 药材名称,支持TextBox输入和ComboBox选择
- `Dosage` (decimal): 单剂用量
- `Unit` (string): 单位,只读显示
- `UnitPrice` (decimal): 单价,只读显示
- `ItemAmount` (decimal): 小计,计算属性 = UnitPrice × Dosage
- `IsEditMode` (bool): 是否编辑模式,控制删除按钮显示

**交互行为**:
- 编辑模式: 显示删除按钮,药材名称可编辑
- 只读模式: 隐藏删除按钮,所有字段只读
- 单价和小计自动计算,用户不可直接编辑

**参考实现**: `FormulaDetailView.xaml` 中的 HerbCardControl (Lines 231-235)

**验收标准**:
- [ ] 卡片在编辑模式下显示删除按钮
- [ ] 用量变化时小计实时更新
- [ ] 药材选择后自动填充单位和单价
- [ ] 支持键盘导航(Tab键切换)

---

### FR-003: 键盘自动焦点管理

**需求描述**:
实现智能焦点自动跳转,提高键盘录入效率。

**焦点流转路径**:

```
药材名称TextBox → (Enter) → 用量TextBox → (Enter) → 下一个卡片的药材名称TextBox
                                              ↓
                                    (最后一个卡片则创建新卡片)
```

**键盘快捷键**:
- **Enter键**: 确认当前输入,跳转到下一个输入框
- **Down/Up键**: 在下拉列表中上下选择药材
- **Escape键**: 关闭下拉列表

**特殊处理**:
- 药材名称输入完成(从下拉列表选择或按Enter)后,自动跳转到用量输入框
- 用量输入完成(按Enter)后,自动跳转到下一个药材卡片
- 如果是最后一个卡片,自动创建新的空白卡片并跳转

**参考实现**: Formula模块的焦点管理逻辑

**验收标准**:
- [ ] 全程使用键盘可以完成10个药材的录入
- [ ] 按Enter键无需等待即可跳转
- [ ] 自动创建的新卡片焦点在药材名称输入框

---

### FR-004: 处方价格计算集成

**需求描述**:
实时计算处方总价,包含药材小计、剂数、折扣等因素。

**价格计算公式**:

```
ItemAmount(药材小计) = UnitPrice × Dosage
SubTotal(药材总价) = Σ(ItemAmount)
TotalAmount(最终总价) = SubTotal × DosageCount × (1 - Discount)
```

**计算触发时机**:
- 药材选择变化 → 更新UnitPrice → 重新计算ItemAmount和TotalAmount
- 用量(Dosage)变化 → 重新计算ItemAmount和TotalAmount
- 剂数(DosageCount)变化 → 重新计算TotalAmount
- 折扣(Discount)变化 → 重新计算TotalAmount

**价格来源**:
- `UnitPrice`从Herbs表的Price字段查询
- 价格快照保存到PrescriptionItem表,历史处方不受Herbs表价格变动影响

**显示格式**:
- 所有价格保留2位小数
- 使用人民币符号"¥"
- 大额数字使用千分位分隔符(例如: ¥1,234.56)

**参考实现**: `PriceCalculator.cs` (已存在)

**验收标准**:
- [ ] 修改用量后小计立即更新
- [ ] 修改剂数后总价立即更新
- [ ] 价格显示保留2位小数
- [ ] 保存后再次打开,价格与保存时一致(快照)

---

### FR-005: 经验方模板导入

**需求描述**:
医生可以从Formula列表选择经验方作为处方模板,自动填充药材信息。

**导入流程**:

```
1. 用户点击"导入经验方"按钮
2. 弹出Formula列表选择对话框
3. 用户选择一个Formula
4. 系统读取Formula的HerbItems
5. 为每个HerbItem创建PrescriptionItem:
   - HerbId = Formula.HerbItem.HerbId
   - HerbName = Formula.HerbItem.HerbName
   - Dosage = Formula.HerbItem.Dosage (或Quantity)
   - Unit = Formula.HerbItem.Unit
6. 查询Herbs表,填充UnitPrice
7. 显示在处方编辑界面,允许医生调整
```

**导入后可编辑操作**:
- 调整用量(Dosage)
- 删除不需要的药材
- 添加新的药材
- 修改剂数和折扣

**边界条件**:
- 如果Formula中的某个药材在Herbs表中找不到,提示用户并跳过该药材
- 如果处方已有药材,导入前询问用户是"替换"还是"追加"

**验收标准**:
- [ ] 导入后药材名称、用量、单位正确
- [ ] 单价自动从Herbs表查询并填充
- [ ] 导入后可以编辑和删除药材
- [ ] 导入不会覆盖用户已输入的药材(除非用户选择替换)

---

### FR-006: 处方药材项CRUD操作

**需求描述**:
支持处方药材项的增删改查操作。

**Create(创建)**:
- 点击"添加药材"按钮 → 创建新的空白HerbCardControl
- 在最后一个HerbCardControl输入完成按Enter → 自动创建新卡片

**Read(查看)**:
- 处方编辑界面显示所有已添加的药材卡片
- 以卡片列表形式展示,每行显示4个卡片(响应式布局)

**Update(更新)**:
- 直接在HerbCardControl中修改药材名称 → 触发重新过滤和选择
- 直接修改用量 → 触发价格重新计算

**Delete(删除)**:
- 点击HerbCardControl右上角的删除按钮(X)
- 确认后移除该药材项
- 删除后重新计算总价

**验收标准**:
- [ ] 新增药材卡片焦点在药材名称输入框
- [ ] 删除药材后总价立即更新
- [ ] 编辑药材名称后单价自动更新
- [ ] 最少保留1个药材卡片(不能全部删除)

---

### FR-007: 剂数(DosageCount)输入

**需求描述**:
处方编辑界面顶部提供剂数输入框,影响总价计算。

**UI位置**:
- 处方编辑Dialog顶部
- 药材列表上方
- 与"导入经验方"按钮在同一行

**输入约束**:
- 数据类型: 整数(int)
- 取值范围: 1-30
- 默认值: 7 (一周用量)

**交互行为**:
- 剂数变化 → 触发TotalAmount重新计算
- 超出范围时显示验证错误提示
- 支持键盘输入和上下箭头调整

**验收标准**:
- [ ] 默认显示7
- [ ] 修改剂数后总价立即更新
- [ ] 输入0或31时显示错误提示
- [ ] 错误状态下无法保存处方

---

### FR-008: 折扣(Discount)应用

**需求描述**:
支持处方折扣设置,可选功能。

**UI位置**:
- 总价显示区域
- 剂数输入框下方

**输入约束**:
- 数据类型: decimal
- 取值范围: 0-1 (0表示无折扣,0.1表示9折,1表示免费)
- 默认值: 0 (无折扣)

**显示格式**:
- 内部存储: 0.1
- 用户界面显示: "10%折扣" 或 "9折"

**计算影响**:
```
原价 = Σ(ItemAmount) × DosageCount
折扣金额 = 原价 × Discount
最终总价 = 原价 - 折扣金额 = 原价 × (1 - Discount)
```

**验收标准**:
- [ ] 默认折扣为0(无折扣)
- [ ] 设置10%折扣后总价减少10%
- [ ] 设置100%折扣(Discount=1)后总价为0
- [ ] 折扣变化时总价立即更新

---

### FR-009: 处方总价显示

**需求描述**:
处方编辑界面底部显示总价明细区域。

**显示内容**:

```
┌─────────────────────────────────────────────────┐
│ 价格明细                                        │
├─────────────────────────────────────────────────┤
│ 药材总价(单剂):                    ¥  450.00   │
│ 剂数:                                    ×  7   │
│ 折扣:                                      10%   │
├─────────────────────────────────────────────────┤
│ 最终总价:                          ¥2,835.00   │
└─────────────────────────────────────────────────┘
```

**计算明细**:
- **药材总价(单剂)**: Σ(UnitPrice × Dosage)
- **剂数**: DosageCount
- **折扣**: Discount (百分比显示)
- **最终总价**: 药材总价 × 剂数 × (1 - 折扣)

**实时更新触发**:
- 药材增删改 → 更新药材总价和最终总价
- 用量变化 → 更新药材总价和最终总价
- 剂数变化 → 更新最终总价
- 折扣变化 → 更新最终总价

**验收标准**:
- [ ] 所有价格保留2位小数
- [ ] 任何变动后价格立即更新
- [ ] 大额数字使用千分位分隔符
- [ ] 计算结果与后端验证一致

---

### FR-010: 处方保存/更新

**需求描述**:
支持新建和更新处方,通过MedicalCase聚合根API。

**API端点**:
- **新建**: `POST /api/v1/medicalcases/{caseId}/prescription`
- **更新**: `PUT /api/v1/medicalcases/{caseId}/prescription`

**请求体(PrescriptionInputDto)**:
```json
{
  "id": null,  // 新建时为null,更新时必填
  "dosageCount": 7,
  "discount": 0.1,
  "items": [
    {
      "herbId": "guid",
      "herbName": "当归",
      "dosage": 10,
      "unit": "g",
      "notes": "酒炒"
    }
  ]
}
```

**后端计算**:
- 后端根据HerbId查询Herbs表获取UnitPrice
- 后端计算ItemAmount和TotalAmount
- 保存时存储价格快照到PrescriptionItem表

**保存前验证**:
- [ ] 至少有1个药材项
- [ ] 所有药材项的HerbId不能为Guid.Empty
- [ ] 所有用量必须>0
- [ ] 剂数必须在1-30范围内
- [ ] 折扣必须在0-1范围内

**保存成功后**:
- 关闭处方编辑Dialog
- 返回病案详情页
- 显示保存成功提示

**验收标准**:
- [ ] 新建处方调用POST端点
- [ ] 更新处方调用PUT端点
- [ ] 保存失败时显示详细错误信息
- [ ] 保存成功后可以再次打开编辑

---

### FR-011: 经验方导入对话框

**需求描述**:
提供经验方导入弹窗,支持按名称/主治搜索,左右分栏显示列表和详情。

**UI布局**:
- **左侧 (40%)**: 经验方列表 + 搜索框
- **右侧 (60%)**: 选中经验方的详情 + 导入按钮

**搜索功能**:
- 搜索字段: `Formula.Name` (经验方名称), `Formula.Effect` (主治)
- 搜索方式: 模糊匹配,支持拼音
- 搜索范围: 所有经验方 (不限当前患者)

**列表显示**:
- 显示字段: 经验方名称
- 排序规则: 按创建时间倒序,最新的在上方
- 选择方式: 单选RadioButton

**详情显示**:
- 经验方名称、分类、主治、功效、用法
- 药材组成列表 (药材名称、用量、单位)

**导入操作**:
- 点击"导入"按钮 → 将选中经验方的药材导入到处方编辑区
- 自动查询Herbs表,填充每个药材的当前单价
- 检测重复药材,触发聚合提醒 (参见FR-013)

**验收标准**:
- [ ] 搜索"补中益气"能匹配到"补中益气汤"
- [ ] 选择经验方后右侧显示完整详情
- [ ] 导入后单价自动从Herbs表查询
- [ ] 弹窗关闭不影响处方编辑区已有药材

---

### FR-012: 历史处方导入对话框

**需求描述**:
提供历史处方导入弹窗,默认显示当前患者的历史处方,支持按诊断筛选。

**UI布局**:
- **左侧 (40%)**: 历史处方列表 + 筛选框
- **右侧 (60%)**: 选中处方的详情 + 导入按钮

**筛选功能**:
- 筛选字段: `MedicalCase.Consultation.TCMDiagnosis` (中医诊断)
- 默认范围: 当前患者的所有历史医案
- 筛选方式: 诊断关键词模糊匹配

**列表显示**:
- 显示字段: 看诊时间、患者姓名
- 排序规则: 按看诊时间倒序,最近的在上方
- 选择方式: 单选RadioButton

**详情显示**:
- 患者信息: 姓名、看诊时间
- 诊断信息: 主诉、中医诊断、治疗原则
- 处方信息: 药材组成 (药材名称、用量、单位、原始单价)

**导入操作**:
- 点击"导入"按钮 → 将选中处方的药材导入到当前处方编辑区
- **重新查询Herbs表获取当前单价** (不使用历史快照)
- 检测重复药材,触发聚合提醒 (参见FR-013)

**验收标准**:
- [ ] 默认只显示当前患者的历史处方
- [ ] 筛选"脾胃虚弱"能找到相关处方
- [ ] 导入后使用当前Herbs表价格,不是历史快照
- [ ] 可以连续导入多个历史处方 (累积)

---

### FR-013: 重复药材检测与聚合提醒

**需求描述**:
当导入经验方或历史处方时,检测重复药材并触发一次性聚合提醒。

**检测时机**:
- 导入经验方时
- 导入历史处方时
- 多次导入累积时

**检测逻辑**:
```csharp
var duplicates = new List<DuplicateHerbInfo>();

foreach (var importedHerb in importedHerbs)
{
    var existing = currentHerbs.FirstOrDefault(h => h.HerbId == importedHerb.HerbId);

    if (existing != null)
    {
        duplicates.Add(new DuplicateHerbInfo
        {
            HerbName = existing.HerbName,
            CurrentDosage = existing.Dosage,
            ImportedDosage = importedHerb.Dosage,
            FinalDosage = Math.Max(existing.Dosage, importedHerb.Dosage)
        });

        existing.Dosage = Math.Max(existing.Dosage, importedHerb.Dosage);
    }
}
```

**提醒方式 - 一次性聚合提醒** (用户补充 2025-11-20):
- 检测到重复药材后,**弹出一个对话框**显示所有重复项
- 用户**只需确认一次**,不是每个药材一个弹窗
- 对话框内容:
  ```
  检测到重复药材

  以下药材已存在于处方中,已自动取最大剂量:

  • 当归: 10g → 15g (取较大值)
  • 黄芪: 30g → 30g (保持不变)
  • 甘草: 5g → 8g (取较大值)

  请检查合并后的药材列表,必要时可手动调整。

                             [确定]
  ```

**合并规则**:
- **剂量合并**: 取最大值 `Math.Max(currentDosage, importedDosage)`
- **保留单位**: 使用现有药材的单位
- **保留备注**: 现有药材的备注不变
- **价格更新**: 如果导入的药材单价更高,更新为导入的单价

**无重复情况**:
- 如果导入的药材都是新药材,直接追加,不弹窗

**验收标准**:
- [ ] 导入含3个重复药材的经验方,弹窗显示3个重复项
- [ ] 用户点击"确定"后,重复药材剂量已合并
- [ ] 用户只需确认一次,不是每个药材一个弹窗
- [ ] 无重复药材时不弹窗,直接追加

---

## 3. 非功能性需求

### NFR-001: 性能要求

**拼音过滤性能**:
- 单次过滤响应时间 < 100ms
- 药材列表规模: 500种常用药材
- 过滤算法时间复杂度: O(n) (n=药材数量)

**UI渲染性能**:
- 药材卡片渲染帧率 ≥ 60fps
- 支持同时显示20个药材卡片无卡顿
- 价格实时计算延迟 < 50ms

**数据加载性能**:
- 药材列表加载时间 < 500ms
- 处方数据加载时间 < 300ms
- 经验方列表加载时间 < 500ms

---

### NFR-002: 可用性要求

**键盘操作优先**:
- 全程使用键盘可以完成处方录入(无需鼠标)
- Tab键切换输入框
- Enter键确认并跳转
- Escape键取消操作

**拼音输入容错**:
- 支持拼音首字母输入(例如"dg"匹配"当归")
- 支持部分汉字输入(例如"当"匹配"当归")
- 支持模糊匹配,降低输入错误率

**错误提示清晰**:
- 验证错误提示具体字段和原因
- 例如: "用量必须在0.1-500之间,当前值: 0"
- 例如: "请至少添加1个药材"

**草稿自动保存**:
- 每30秒自动保存草稿(如果有变更)
- 关闭Dialog前提示保存草稿
- 下次打开自动恢复草稿

---

### NFR-003: 安全性要求

**角色权限**:
- 处方创建: 仅Doctor角色
- 处方查看: Doctor和Admin角色
- 处方编辑: Doctor和Admin角色
- 处方删除: 仅Admin角色

**价格数据保护**:
- UnitPrice不在前端硬编码,通过API查询
- 价格计算在后端验证,防止前端篡改
- 保存时后端重新计算TotalAmount,忽略前端传递的值

**数据完整性**:
- 所有处方操作通过事务保证原子性
- 处方与MedicalCase的关联不可破坏
- 删除MedicalCase时级联删除Prescription

---

### NFR-004: 可维护性要求

**组件化设计**:
- HerbCardControl可复用(Prescription和Formula共享)
- 拼音过滤算法封装为独立类,可测试
- 价格计算逻辑封装为PriceCalculator,可测试

**架构约束遵循**:
- MVVM模式: ViewModel不直接访问Repository
- DataManager模式: 数据加载封装到PrescriptionDataManager
- CommandHandler模式: 命令逻辑封装到SavePrescriptionCommandHandler

**代码质量**:
- 单元测试覆盖率 ≥ 80%
- 所有公共方法添加XML注释
- 遵循C#编码规范和命名约定

---

## 4. 业务规则

### BR-001: 医案聚合根规则

**规则描述**:
MedicalCase是聚合根,所有Prescription操作必须通过MedicalCase API。

**强制约束**:
- Prescription不能独立创建,必须关联到MedicalCase
- Prescription的CRUD必须通过MedicalCase聚合根API
- 直接访问独立Prescription端点返回410 Gone

**API路径规范**:
```
✅ Correct:
  POST /api/v1/medicalcases/{caseId}/prescription
  PUT  /api/v1/medicalcases/{caseId}/prescription
  GET  /api/v1/medicalcases/{caseId}/prescription

❌ Wrong (返回410 Gone):
  POST /api/v1/prescriptions
  PUT  /api/v1/prescriptions/{id}
```

**参考文档**: ADR-006 聚合根模式约束

---

### BR-002: 诊断与处方的关系

**规则描述**:
Consultation完成后才能创建Prescription。

**前置条件**:
- ConsultationFormViewModel.Step1CompletedAt 不为null
- ConsultationFormViewModel.PrescriptionEnabled = true

**流程控制**:
```
诊断完成(CompleteStep1Command) → 判断PrescriptionEnabled
  ├─ true  → 导航到处方录入页
  └─ false → 导航到病案汇总页
```

**验证规则**:
- 如果MedicalCase没有Consultation,无法创建Prescription
- 如果PrescriptionDisabled=true,跳过处方录入

---

### BR-003: 药材价格来源与快照

**规则描述**:
UnitPrice来自Herbs表,保存时创建价格快照。

**价格查询**:
- 药材选择后,查询 `Herbs.FirstOrDefault(h => h.Id == HerbId).Price`
- 如果Herb不存在或Price为null,提示用户并阻止选择

**价格快照**:
- 保存处方时,将当前UnitPrice存储到PrescriptionItem表
- 历史处方的价格不受Herbs表价格变动影响
- 即使Herbs表的Price更新,已保存的Prescription显示快照价格

**示例**:
```
当归在Herbs表的Price: ¥15.00
2025-01-01 创建处方A,当归UnitPrice快照: ¥15.00

Herbs表更新当归Price为 ¥18.00
2025-01-10 创建处方B,当归UnitPrice快照: ¥18.00

查看处方A → 当归仍显示 ¥15.00 (快照)
查看处方B → 当归显示 ¥18.00 (快照)
```

---

### BR-004: 处方草稿与正式保存

**规则描述**:
区分草稿保存和正式保存。

**草稿保存**:
- 目的: 避免数据丢失,支持多次编辑
- 存储位置: 数据库临时表或Prescription表的IsConfirmed=false记录
- 触发时机: 每30秒自动保存,或用户点击"保存草稿"按钮

**正式保存**:
- 目的: 生成正式Prescription记录,关联到MedicalCase
- 存储位置: Prescription表,IsConfirmed=true
- 触发时机: 用户点击"保存处方"按钮

**草稿恢复**:
- 打开处方编辑Dialog时,检查是否有草稿
- 如果有草稿,提示用户"发现未保存的草稿,是否恢复?"
- 用户确认后恢复草稿内容

---

### BR-005: 处方数据完整性

**规则描述**:
处方必须满足最低数据要求才能保存。

**必填验证**:
- [ ] 至少包含1个药材项
- [ ] 每个药材项的HerbId不能为Guid.Empty
- [ ] 每个药材项的HerbName不能为空
- [ ] 每个药材项的Dosage > 0
- [ ] 每个药材项的Unit不能为空
- [ ] 每个药材项的UnitPrice ≥ 0

**范围验证**:
- [ ] Dosage在0.1-500范围内
- [ ] DosageCount在1-30范围内
- [ ] Discount在0-1范围内

**计算验证**:
- [ ] 后端重新计算TotalAmount
- [ ] 如果前端传递的TotalAmount与后端计算不一致,使用后端计算结果

---

### BR-006: 导入时价格来源规则

**规则描述**:
导入经验方或历史处方时,必须使用Herbs表的当前价格,不使用历史快照。

**经验方导入**:
- Formula表不存储价格信息
- 导入时查询: `Herbs.FirstOrDefault(h => h.Id == HerbId).Price`
- 如果Herb不存在或Price为null,提示用户并跳过该药材

**历史处方导入**:
- PrescriptionItem表存储的UnitPrice是历史快照
- 导入时**忽略历史快照**,重新查询当前价格
- 查询逻辑: `Herbs.FirstOrDefault(h => h.Id == HerbId).Price`

**价格更新时机**:
- 导入时: 立即查询并填充UnitPrice
- 保存时: 后端重新计算并存储价格快照到PrescriptionItem表
- 编辑时: 使用保存时的快照价格,不重新查询

**示例场景**:
```
当归在Herbs表的当前价格: ¥18.00

患者A历史处方 (2025-01-01创建):
  当归 10g, UnitPrice快照: ¥15.00

医生导入患者A历史处方到新处方:
  当归 10g, UnitPrice: ¥18.00 (重新查询,不使用¥15.00快照)

保存新处方后:
  当归 10g, UnitPrice快照: ¥18.00 (保存时的价格)
```

**业务原因**:
- 用户明确要求: "要求价格肯定是要获取当前价格的"
- 保证新处方使用最新价格,避免价格过期
- 历史处方的快照仅用于查看历史记录,不用于导入

**验收标准**:
- [ ] 导入2年前的历史处方,单价使用当前Herbs表价格
- [ ] 导入经验方,单价从Herbs表查询
- [ ] 保存处方后再次编辑,单价显示保存时的快照价格

---

## 5. 数据模型草案

### 5.1 Prescription实体

```csharp
/// <summary>
/// 处方实体 - 聚合根子实体
/// </summary>
public class Prescription
{
    /// <summary>处方ID</summary>
    public Guid Id { get; set; }

    /// <summary>关联的医案ID</summary>
    public Guid MedicalCaseId { get; set; }

    /// <summary>剂数(1-30)</summary>
    [Range(1, 30)]
    public int DosageCount { get; set; } = 7;

    /// <summary>折扣(0-1, 0表示无折扣)</summary>
    [Range(0, 1)]
    public decimal Discount { get; set; } = 0;

    /// <summary>总价(快照)</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>是否正式保存(false表示草稿)</summary>
    public bool IsConfirmed { get; set; } = false;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    public MedicalCase MedicalCase { get; set; }
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; }
}
```

### 5.2 PrescriptionItem实体

```csharp
/// <summary>
/// 处方药材项实体
/// </summary>
public class PrescriptionItem
{
    /// <summary>药材项ID</summary>
    public Guid Id { get; set; }

    /// <summary>关联的处方ID</summary>
    public Guid PrescriptionId { get; set; }

    /// <summary>药材ID</summary>
    public Guid HerbId { get; set; }

    /// <summary>药材名称(快照,防止Herb删除后丢失)</summary>
    [Required]
    [StringLength(100)]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>单剂用量</summary>
    [Range(0.1, 500)]
    public decimal Dosage { get; set; }

    /// <summary>单位</summary>
    [Required]
    [StringLength(10)]
    public string Unit { get; set; } = "g";

    /// <summary>单价(快照,记录保存时的价格)</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>小计(单剂) = UnitPrice × Dosage</summary>
    public decimal ItemAmount { get; set; }

    /// <summary>备注(例如: 酒炒、后下)</summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    // 导航属性
    public Prescription Prescription { get; set; }
    public Herb Herb { get; set; }
}
```

### 5.3 PrescriptionInputDto

```csharp
/// <summary>
/// 处方输入DTO - 统一创建和更新
/// Epic #1736: InputDto Pattern
/// </summary>
public class PrescriptionInputDto
{
    /// <summary>处方ID(更新时必填,创建时为null)</summary>
    public Guid? Id { get; set; }

    /// <summary>剂数</summary>
    [Required]
    [Range(1, 30, ErrorMessage = "剂数必须在1-30之间")]
    public int DosageCount { get; set; } = 7;

    /// <summary>折扣(0-1)</summary>
    [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
    public decimal Discount { get; set; } = 0;

    /// <summary>药材项列表</summary>
    [Required]
    [MinLength(1, ErrorMessage = "至少需要1个药材项")]
    public List<PrescriptionItemInputDto> Items { get; set; } = new();
}
```

### 5.4 PrescriptionItemInputDto

```csharp
/// <summary>
/// 处方药材项输入DTO
/// </summary>
public class PrescriptionItemInputDto
{
    /// <summary>药材ID</summary>
    [Required(ErrorMessage = "药材不能为空")]
    public Guid HerbId { get; set; }

    /// <summary>药材名称(冗余,方便前端显示)</summary>
    [Required]
    [StringLength(100)]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>单剂用量</summary>
    [Required]
    [Range(0.1, 500, ErrorMessage = "用量必须在0.1-500之间")]
    public decimal Dosage { get; set; }

    /// <summary>单位</summary>
    [Required]
    [StringLength(10)]
    public string Unit { get; set; } = "g";

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
```

**注意**: UnitPrice和ItemAmount不在InputDto中,由后端查询Herbs表并计算。

---

## 6. 架构约束

### AC-001: 技术栈约束

**✅ Allowed (允许使用)**:
- .NET 8
- WPF, Prism 8.x
- Entity Framework Core 8
- MaterialDesignThemes 5.1.x
- FluentValidation
- AutoMapper
- NSubstitute (单元测试Mock)

**❌ Forbidden (MVP黑名单)**:
- Redis / RabbitMQ (分布式组件,过度设计)
- MediatR / CQRS (非MVP范围)
- Docker / Kubernetes (部署复杂度)
- SignalR (当前不需要实时通信)

**参考文档**: `docs/reference/mvp-constraints.md`

---

### AC-002: 架构层分配

**前端(Client层)**:
- `LYBT.Desktop.Prescriptions/Controls/HerbCardControl.xaml` (新建)
- `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionItemViewModel.cs` (增强)
- `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionEditorDialogViewModel.cs` (增强)
- `LYBT.Desktop.Prescriptions/Services/PrescriptionHerbFilterManager.cs` (升级)
- `LYBT.Desktop.Prescriptions/DataManagers/PrescriptionDataManager.cs` (可能需要新建)

**后端(Server层)**:
- `LYBT.Server.Application/Services/PrescriptionService.cs`
- `LYBT.Server.Presentation/Controllers/MedicalCaseController.cs`
- `LYBT.Server.Infrastructure/Repositories/PrescriptionRepository.cs` (internal)

**共享层(Shared层)**:
- `LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionInputDto.cs`
- `LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionItemInputDto.cs`
- `LYBT.Shared.Models/Validators/PrescriptionValidator.cs`
- `LYBT.Shared.Models/Interfaces/Repositories/IPrescriptionRepository.cs`

---

### AC-003: 聚合根约束(ADR-006)

**约束规则**:
- MedicalCase是聚合根
- Prescription和Consultation是聚合根的子实体
- 所有子实体的CRUD必须通过聚合根API

**API设计约束**:

**✅ Correct (正确)**:
```
POST   /api/v1/medicalcases/{caseId}/prescription
PUT    /api/v1/medicalcases/{caseId}/prescription
GET    /api/v1/medicalcases/{caseId}/prescription
DELETE /api/v1/medicalcases/{caseId}/prescription
```

**❌ Wrong (错误,返回410 Gone)**:
```
POST   /api/v1/prescriptions
PUT    /api/v1/prescriptions/{id}
GET    /api/v1/prescriptions/{id}
DELETE /api/v1/prescriptions/{id}
```

**Controller实现示例**:
```csharp
[ApiController]
[Route("api/v1/medicalcases/{caseId}")]
public class MedicalCaseController : ControllerBase
{
    [HttpPost("prescription")]
    public async Task<IActionResult> CreatePrescription(
        Guid caseId,
        [FromBody] PrescriptionInputDto dto)
    {
        // Implementation
    }
}
```

---

### AC-004: 组件架构约束(Epic #1773)

**约束规则**:
- ViewModel不直接访问Repository
- 使用DataManager封装数据加载逻辑
- 使用CommandHandler封装命令执行逻辑

**DataManager模式**:
```csharp
public class PrescriptionDataManager
{
    private readonly IPrescriptionService _service;

    public async Task<List<HerbDto>> LoadHerbsAsync()
    {
        // 封装Herbs数据加载逻辑
    }

    public async Task<PrescriptionDto?> LoadPrescriptionAsync(Guid caseId)
    {
        // 封装Prescription数据加载逻辑
    }
}
```

**CommandHandler模式**:
```csharp
public class SavePrescriptionCommandHandler
{
    private readonly IPrescriptionService _service;

    public async Task<Result> ExecuteAsync(PrescriptionInputDto dto)
    {
        // 封装保存处方逻辑
        // 包括验证、调用Service、错误处理
    }
}
```

**ViewModel使用示例**:
```csharp
public class PrescriptionEditorDialogViewModel : UnifiedViewModelBase
{
    private readonly PrescriptionDataManager _dataManager;
    private readonly SavePrescriptionCommandHandler _saveHandler;

    public PrescriptionEditorDialogViewModel(
        PrescriptionDataManager dataManager,
        SavePrescriptionCommandHandler saveHandler,
        ...)
    {
        _dataManager = dataManager;
        _saveHandler = saveHandler;
    }

    private async Task LoadDataAsync()
    {
        AllHerbs = await _dataManager.LoadHerbsAsync();
    }

    private async Task SaveAsync()
    {
        var result = await _saveHandler.ExecuteAsync(ToDto());
    }
}
```

---

## 7. 开放问题与决策记录

### ✅ OQ-001: HerbCardControl组件复用策略 (已解决)

**问题描述**:
Formula和Prescription都需要HerbCardControl,是创建2个独立组件还是共享1个?

**最终决策**: 共享组件,Formula返回固定0价格

**决策理由**:
- 用户确认: "方案1" (2025-11-20)
- Formula不查询Herbs.Price,直接返回0
- Prescription查询Herbs.Price获取实时价格
- HerbCardControl通过`IsPriceVisible`控制显示

**实现方案**:
```csharp
// FormulaHerbItemViewModel
public decimal UnitPrice => 0m;  // 固定返回0,不查询数据库

// PrescriptionItemViewModel
private decimal _unitPrice;
public decimal UnitPrice
{
    get => _unitPrice;
    set
    {
        if (SetProperty(ref _unitPrice, value))
        {
            RaisePropertyChanged(nameof(ItemAmount));
        }
    }
}
public decimal ItemAmount => UnitPrice * Dosage;  // O(1)计算
```

**组件位置**: `LYBT.Shared.Components/Controls/HerbCardControl.xaml`

---

### ✅ OQ-002: 处方编辑界面形式 (已解决)

**问题描述**:
使用Dialog(弹窗)还是Full-screen Page(全屏页面)?

**最终决策**: 诊断+处方一体化全屏页面,1920×1080优化布局

**决策理由**:
- 用户原话: "我现在甚至在考虑因为显示器是横评。主流显示器1920*1080,考虑诊断+处方 一个界面的设计"
- 用户选择: Q1=A (允许只诊断不开处方), Q2=A (废弃ConsultationFormView), Q3=A (编辑也用一体化界面)

**UI布局设计**:
```
┌─────────────────────────────────────────────────────────────┐
│ 医案录入页面 (患者: 张三, 病案ID: xxx)                      │
├──────────────────────┬──────────────────────────────────────┤
│ 诊断区 (左40%)       │ 处方区 (右60%)                       │
├──────────────────────┼──────────────────────────────────────┤
│ [主诉]               │ [导入经验方] [导入历史处方] [添加药材]│
│ [现病史]             │ ┌─────────┬─────────┬─────────┬──────┐│
│ [中医诊断] *必填     │ │药材卡片1│药材卡片2│药材卡片3│...  ││
│ [治疗原则]           │ └─────────┴─────────┴─────────┴──────┘│
│ [望闻问切]           │ 剂数: [7] 折扣: [0%]                 │
│ [备注]               │ 药材总价: ¥450.00                    │
│                      │ 最终总价: ¥3,150.00                  │
│ ☑ 开处方            │                                      │
│ ☐ 不开处方          │                                      │
└──────────────────────┴──────────────────────────────────────┘
│ [保存草稿] [保存并完成]                                      │
└─────────────────────────────────────────────────────────────┘
```

**工作流程**:
```
患者选择 (PatientSelectionView)
   ↓ (携带PatientId)
医案录入页面 (MedicalCaseEditorView)
   ├─ 诊断区: ConsultationFormViewModel
   └─ 处方区: PrescriptionEditorViewModel
   ↓
保存 → 医案管理列表
```

**关键约束**:
- 隐藏自动生成字段(CreatedAt, UpdatedAt)
- 导航模式,符合系统统一模式
- 如果界面拥挤,后期改为Tab切换

---

### ✅ OQ-003: 草稿保存位置 (已解决)

**问题描述**:
草稿保存到数据库还是本地存储?

**最终决策**: 数据库存储,使用IsConfirmed字段区分草稿和正式记录

**决策理由**:
- 支持工作流中断场景: "医生可能会有急诊或者临时走开。需要退出当前患者的当前状态。但是事情处理完成后还要继续看诊"
- 跨设备同步,数据不丢失
- 患者选择界面显示"待诊列表",可查看有草稿的患者

**业务流程**:
```
医生看诊患者A → 急诊患者B → 保存患者A草稿 (IsConfirmed=false)
   ↓
处理患者B → 返回待诊列表 → 选择患者A → 恢复草稿 → 继续录入
```

**技术实现**:
- `IsConfirmed=false`: 草稿
- `IsConfirmed=true`: 正式医案
- 自动保存: 每30秒 (如果有变更)
- 手动保存: 用户点击"保存草稿"按钮

**UI需求**:
- 患者选择界面: "待诊列表"显示有草稿的患者
- 打开编辑页面: 检测草稿,提示"发现未保存的草稿,是否恢复?"

---

### ✅ OQ-004: 经验方导入方式 (已解决)

**问题描述**:
如何实现经验方和历史处方的导入功能?

**最终决策**: 手动组方为主,两种导入辅助(经验方+历史处方),一次性聚合提醒重复药材

**完整业务逻辑** (用户原话):
> "其实默认是手动组成方子。类似经验方。医生一味中药一味中药的添加。然后可以通过导入'经验方'和导入'历史处方'导入。"

**导入方式1: 经验方导入**
- 触发: 用户点击"导入经验方"按钮
- 弹窗布局: 左右分栏
  ```
  ┌──────────────────────────────────────────────────┐
  │ 导入经验方                                   [X]  │
  ├─────────────────────┬────────────────────────────┤
  │ 经验方列表 (左40%)  │ 经验方详情 (右60%)         │
  ├─────────────────────┼────────────────────────────┤
  │ [搜索框: 名称/主治]│ 名称: [补中益气汤]         │
  │ ☑ 补中益气汤        │ 主治: [脾胃虚弱...]        │
  │ ☐ 四君子汤          │ 药材组成:                  │
  │ ☐ 六味地黄丸        │ - 黄芪 30g                 │
  │ ...                 │ - 党参 15g                 │
  │                     │ - 白术 10g                 │
  │                     │ - 当归 10g                 │
  │                     │ ...                        │
  │                     │ [导入]                     │
  └─────────────────────┴────────────────────────────┘
  ```
- 查询字段: 经验方名称、主治 (模糊查找)
- 导入操作: 将经验方的药材组合导入到处方编辑区,医生可微调/删除

**导入方式2: 历史处方导入**
- 触发: 用户点击"导入历史处方"按钮
- 弹窗布局: 左右分栏
  ```
  ┌──────────────────────────────────────────────────┐
  │ 导入历史处方                                 [X]  │
  ├─────────────────────┬────────────────────────────┤
  │ 历史处方列表 (左40%)│ 处方详情 (右60%)           │
  ├─────────────────────┼────────────────────────────┤
  │ [筛选: 诊断关键词]  │ 患者: [李四]               │
  │ ☑ 2025-01-15 李四   │ 看诊时间: [2025-01-15]     │
  │ ☐ 2025-01-10 李四   │ 诊断: [脾胃虚弱,气血不足]  │
  │ ☐ 2025-01-05 李四   │ 药材组成:                  │
  │ ...                 │ - 黄芪 30g ¥15.00          │
  │                     │ - 党参 15g ¥8.00           │
  │                     │ - 白术 10g ¥5.00           │
  │                     │ ...                        │
  │                     │ [导入]                     │
  └─────────────────────┴────────────────────────────┘
  ```
- 默认范围: 当前患者的历史处方
- 筛选字段: 诊断 (TCMDiagnosis字段)
- 显示内容: 患者名称、看诊时间、诊断结果、药方组成
- 导入操作: 将历史处方的药材导入到当前处方编辑区

**重复药材合并规则**:
- **累积导入**: 可以多次导入 (经验方A 5味 + 经验方B 8味 = 13味药材,减去重复)
- **重复检测**: 检测到重复药材时,触发聚合提醒
- **剂量合并**: 取最大剂量 `Math.Max(currentDosage, importedDosage)`

**重复药材提醒方式** (用户补充 2025-11-20):
- **一次性聚合提醒** (推荐):
  ```
  ┌──────────────────────────────────────────────────┐
  │ 检测到重复药材                               [!]  │
  ├──────────────────────────────────────────────────┤
  │ 以下药材已存在于处方中,已自动取最大剂量:        │
  │                                                    │
  │ • 当归: 10g → 15g (取较大值)                      │
  │ • 黄芪: 30g → 30g (保持不变)                      │
  │ • 甘草: 5g → 8g (取较大值)                        │
  │                                                    │
  │ 请检查合并后的药材列表,必要时可手动调整。        │
  │                                    [确定]          │
  └──────────────────────────────────────────────────┘
  ```
  - 优点: 用户只需确认一次
  - 显示内容: 所有重复药材的原剂量 → 新剂量
- **多次单独提醒** (不推荐):
  - 缺点: 多个重复药材需要确认多次,繁琐

**价格来源规则**:
- 历史处方导入: **使用当前Herbs表的最新价格** (不使用历史快照)
- 经验方导入: 查询Herbs表获取实时价格
- 用户原话: "要求价格肯定是要获取当前价格的"

**技术细节Q&A**:
- Q1: 多次导入是否累积? A: "是的如果没有重复就是 5+8,有重复的情况。重复的药材剂量去最大的一个"
- Q2: 历史处方价格? A: "这个是好问题。要求价格肯定是要获取当前价格的"

---

## 8. 参考实现

### 8.1 Formula模块的7级拼音匹配算法

**源文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaHerbItemViewModel.cs`

**核心方法**: `GetMatchScore()` + `IsPinyinFuzzyMatch()`

**完整代码参考** (Lines 233-318):

```csharp
private int GetMatchScore(HerbDto herb, string searchText)
{
    if (string.IsNullOrWhiteSpace(searchText))
        return 0;

    var herbName = herb.Name?.ToLower() ?? string.Empty;
    var pinyinCode = herb.PinYinCode?.ToLower() ?? string.Empty;

    // 1. Exact name match: 100 points
    if (herbName == searchText)
        return 100;

    // 2. Exact pinyin match: 90 points
    if (!string.IsNullOrEmpty(pinyinCode) && pinyinCode == searchText)
        return 90;

    // 3. Name prefix match: 80 points
    if (herbName.StartsWith(searchText))
        return 80;

    // 4. Pinyin prefix match: 70 points
    if (!string.IsNullOrEmpty(pinyinCode) && pinyinCode.StartsWith(searchText))
        return 70;

    // 5. Name contains match: 50 points
    if (herbName.Contains(searchText))
        return 50;

    // 6. Pinyin contains match: 40 points
    if (!string.IsNullOrEmpty(pinyinCode) && pinyinCode.Contains(searchText))
        return 40;

    // 7. Pinyin fuzzy match: 30 points
    if (!string.IsNullOrEmpty(pinyinCode) && IsPinyinFuzzyMatch(pinyinCode, searchText))
        return 30;

    return 0;
}

private bool IsPinyinFuzzyMatch(string pinyinCode, string searchText)
{
    if (string.IsNullOrEmpty(pinyinCode) || string.IsNullOrEmpty(searchText))
        return false;

    int searchIndex = 0;
    foreach (char c in pinyinCode)
    {
        if (searchIndex < searchText.Length && c == searchText[searchIndex])
        {
            searchIndex++;
        }

        if (searchIndex == searchText.Length)
        {
            return true;
        }
    }

    return searchIndex == searchText.Length;
}
```

### 8.2 Formula模块的FilterHerbs逻辑

**源文件**: `FormulaHerbItemViewModel.cs`

**核心方法**: `FilterHerbs()` (Lines 179-225)

**移植要点**:
1. 复制整个`FilterHerbs()`方法到`PrescriptionHerbFilterManager.cs`
2. 复制`GetMatchScore()`和`IsPinyinFuzzyMatch()`方法
3. 替换`Logger`为`ILogger<PrescriptionHerbFilterManager>`
4. 保持算法逻辑完全一致

### 8.3 Formula模块的HerbCardControl

**源文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/HerbCardControl.xaml`

**移植要点**:
1. 复制XAML和CodeBehind到`LYBT.Shared.Components/Controls/`
2. 添加`IsPriceVisible`依赖属性
3. 绑定UnitPrice和ItemAmount (Prescription需要,Formula不需要)
4. 保持卡片布局和交互逻辑一致

---

## 9. 验收标准

### 9.1 功能验收

**拼音过滤验收**:
- [ ] 输入"dg"能匹配到"当归",且排在前3位
- [ ] 输入"黄"能匹配到"黄芪"、"黄连"、"黄柏"
- [ ] 输入完整名称后下拉列表自动消失
- [ ] 过滤响应时间 < 100ms

**HerbCardControl验收**:
- [ ] 编辑模式下显示删除按钮
- [ ] 用量变化时小计实时更新
- [ ] 药材选择后自动填充单位和单价
- [ ] 支持Tab键切换输入框

**键盘导航验收**:
- [ ] 全程使用键盘可以完成10个药材的录入
- [ ] 按Enter键自动跳转到下一个输入框
- [ ] 自动创建的新卡片焦点在药材名称输入框

**价格计算验收**:
- [ ] 修改用量后小计立即更新
- [ ] 修改剂数后总价立即更新
- [ ] 价格显示保留2位小数
- [ ] 保存后再次打开,价格与保存时一致(快照)

**经验方导入验收**:
- [ ] 导入后药材名称、用量、单位正确
- [ ] 单价自动从Herbs表查询并填充
- [ ] 导入后可以编辑和删除药材

**处方保存验证**:
- [ ] 新建处方调用POST端点
- [ ] 更新处方调用PUT端点
- [ ] 保存失败时显示详细错误信息
- [ ] 验证错误时无法保存

### 9.2 性能验收

- [ ] 拼音过滤响应时间 < 100ms
- [ ] 药材列表加载时间 < 500ms
- [ ] UI渲染帧率 ≥ 60fps
- [ ] 价格实时计算延迟 < 50ms

### 9.3 安全验证

- [ ] 非Doctor角色无法创建处方
- [ ] 价格计算在后端验证,前端无法篡改
- [ ] 后端重新计算TotalAmount并与前端对比

### 9.4 代码质量验收

- [ ] 单元测试覆盖率 ≥ 80%
- [ ] 所有公共方法添加XML注释
- [ ] 遵循C#编码规范和命名约定
- [ ] 通过Code Review (lybtzyzs-code-review skill)

---

## 10. 附录

### 10.1 相关文档

- `docs/explanation/architecture/server/README.md` - 后端三层架构
- `docs/explanation/architecture/client/README.md` - 前端MVVM架构
- `docs/explanation/architecture/shared/README.md` - 共享层设计
- `docs/adr/ADR-006-aggregate-root-pattern.md` - 聚合根模式
- `docs/reference/mvp-constraints.md` - MVP技术约束

### 10.2 相关Issues/Epics

- Epic #1736: InputDto统一模式
- Epic #1773: 组件化架构(DataManager + CommandHandler)
- Epic #1600: Repository可见性约束
- Issue #2149: Formula模块拼音过滤Bug修复

### 10.3 Git Commit规范

```
feat(Prescription): 实现7级智能拼音药材搜索 - FR-001
feat(Prescription): 添加HerbCardControl组件 - FR-002
feat(Prescription): 实现键盘自动焦点管理 - FR-003
feat(Prescription): 集成价格计算功能 - FR-004
feat(Prescription): 支持经验方模板导入 - FR-005

关联Epic: 医案模块完善
参考实现: Formula模块
```

---

**文档状态**: ✅ 开放问题已解决,待生成设计文档
**下一步**: 生成技术设计文档 (调用lybtzyzs-design-generator)
**责任人**: Claude Code (AI Assistant)
**审核人**: TonyShou

---

## 11. 变更记录

| 日期 | 版本 | 变更内容 | 变更人 |
|------|------|----------|--------|
| 2025-11-20 | v1.0 | 初始版本,完成10个FR、4个NFR、5个BR、4个开放问题 | Claude Code |
| 2025-11-20 | v1.1 | 解决OQ-001至OQ-004,添加FR-011、FR-012、FR-013、BR-006 | Claude Code |

**v1.1 主要变更**:
- ✅ OQ-001已解决: 共享HerbCardControl,Formula返回0价格
- ✅ OQ-002已解决: 诊断+处方一体化页面,1920×1080布局
- ✅ OQ-003已解决: 数据库草稿存储,IsConfirmed字段区分
- ✅ OQ-004已解决: 双导入方式(经验方+历史处方),一次性聚合提醒重复药材
- 新增FR-011: 经验方导入对话框
- 新增FR-012: 历史处方导入对话框
- 新增FR-013: 重复药材检测与聚合提醒
- 新增BR-006: 导入时价格来源规则 (使用当前Herbs表价格)
