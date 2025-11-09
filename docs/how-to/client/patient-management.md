# 患者管理CRUD操作指南

> **文档类型**: How-to Guide
> **目标读者**: 用户、开发人员
> **前置阅读**: [患者模块架构设计](../../explanation/architecture/client/README.md)
> **完成日期**: 2025-11-09（Epic #1934）

---

## 1. 概述

本指南介绍患者管理模块的CRUD操作功能，包括患者的新建、编辑、查看和删除。

**核心功能**：
- ✅ 新建患者（自动生成拼音码、身份证号验证）
- ✅ 编辑患者（更新信息、同步拼音码）
- ✅ 查看患者详情（只读模式）
- ✅ 删除患者
- ✅ 年龄自动计算（基于出生日期）

**技术亮点**：
- 拼音码自动生成（使用hyjiacan.pinyin4net库）
- 年龄实时计算（前端+后端）
- FluentValidation条件验证（创建/更新统一DTO）
- Prism MVVM模式（Region Navigation + 事件驱动）

---

## 2. 新建患者

### 2.1 操作步骤

1. **进入新建页面**
   - 在患者列表页面，点击"新建"按钮
   - 系统导航到患者新建页面（`PatientCreateView`）

2. **填写基本信息**

   **必填字段**：
   - **患者姓名**（最大50字符）
     - 输入姓名后，拼音码会自动生成并显示
     - 示例：输入"张三" → 拼音码自动生成"ZS"

   - **身份证号**（18位）
     - 必填，最大18字符
     - 系统会进行格式验证

   **可选字段**：
   - **性别**（下拉选择：未知/男/女）
   - **出生日期**（日期选择器）
     - 选择后，年龄会自动计算并显示
   - **手机号码**（最大20字符）
   - **地址**（最大200字符）

3. **提交创建**
   - 点击"保存"按钮
   - 系统验证必填字段
   - 创建成功后，自动返回患者列表
   - 列表会实时刷新，显示新创建的患者

### 2.2 字段说明

#### 拼音码自动生成

**工作原理**：
- 前端：当用户输入姓名时，`Name`属性的setter会自动调用`PinYinHelper.GetPinYinCode()`
- 后端：Server端在创建时也会同步生成拼音码，确保数据一致性

**代码位置**：
- 前端：`PatientCreateViewModel.cs:51`
- 后端：`PatientService.cs:109-116`

**示例**：
```
输入姓名：李明
自动生成拼音码：LM

输入姓名：王小红
自动生成拼音码：WXH
```

#### 年龄自动计算

**计算规则**：
```csharp
if (BirthDate.HasValue)
{
    var today = DateTime.Today;
    var age = today.Year - BirthDate.Value.Year;
    if (BirthDate.Value.Date > today.AddYears(-age))
    {
        age--;
    }
    return age;
}
```

**示例**：
- 出生日期：1990-05-15
- 今天：2025-11-09
- 计算年龄：35岁

**显示位置**：
- 列表页面：年龄列
- 详情页面：年龄字段
- 新建/编辑页面：年龄字段（只读，自动更新）

### 2.3 验证规则

| 字段 | 验证规则 | 错误提示 |
|-----|---------|---------|
| 患者姓名 | 必填，最大50字符 | "患者姓名不能为空" / "患者姓名长度不能超过50个字符" |
| 身份证号 | 必填，最大18字符 | "身份证号不能为空" / "身份证号长度不能超过18个字符" |
| 手机号码 | 可选，最大20字符 | "手机号码长度不能超过20个字符" |
| 地址 | 可选，最大200字符 | "地址长度不能超过200个字符" |

**提交条件**：
- 所有必填字段已填写
- 所有字段验证通过（无错误）
- 系统未处于加载状态

---

## 3. 编辑患者

### 3.1 操作步骤

1. **进入编辑页面**
   - 在患者列表页面，点击某个患者的"编辑"按钮
   - 系统导航到患者编辑页面（`PatientEditView`）
   - 页面自动加载患者数据

2. **修改信息**

   **可编辑字段**：
   - 患者姓名（修改后拼音码自动更新）
   - 性别
   - 出生日期（修改后年龄自动更新）
   - 身份证号
   - 手机号码
   - 地址
   - 状态（启用/禁用）

   **只读字段**：
   - 拼音码（自动生成，不可手动编辑）
   - 年龄（自动计算，不可手动编辑）

3. **提交更新**
   - 点击"保存"按钮
   - 系统验证必填字段
   - 更新成功后，自动返回患者列表
   - 列表会实时刷新，显示更新后的患者信息

### 3.2 拼音码同步更新

**更新逻辑**：
- 当用户修改患者姓名时，拼音码会自动更新
- Server端会检测姓名是否变化，只在变化时更新拼音码

**代码位置**：
- 前端：`PatientEditViewModel.cs:67`
- 后端：`PatientService.cs:136-152`

**示例**：
```
原姓名：张三 → 拼音码：ZS
修改为：张三丰 → 拼音码自动更新为：ZSF
```

### 3.3 状态管理

**状态选项**：
- **启用**（Enabled）：患者可正常使用
- **禁用**（Disabled）：患者被禁用，但数据保留

**注意事项**：
- 编辑页面新增了"状态"字段（新建时默认为"启用"）
- 禁用患者不会删除数据，只是标记为不可用状态

---

## 4. 查看患者详情

### 4.1 操作步骤

1. **进入详情页面**
   - 在患者列表页面，点击某个患者的"详情"按钮或患者姓名
   - 系统导航到患者详情页面（`PatientDetailView`）

2. **查看信息**

   **显示字段**：
   - 患者姓名
   - 拼音码
   - 性别
   - 出生日期
   - 年龄（自动计算）
   - 身份证号
   - 手机号码
   - 地址
   - 状态

3. **操作选项**
   - **返回列表**：点击"返回"按钮
   - **编辑患者**：点击"编辑"按钮（跳转到编辑页面）
   - **删除患者**：点击"删除"按钮

### 4.2 只读模式

**设计理念**：
- 详情页面采用只读模式，不提供直接编辑功能
- 用户需要点击"编辑"按钮进入编辑页面才能修改信息
- 这样可以避免误操作，并保持UI一致性

**实施变更**（Epic #1934）：
- ✅ 移除了详情页面的编辑功能
- ✅ 扩大了详情区域显示框，提升阅读体验

---

## 5. 删除患者

### 5.1 操作步骤

1. **触发删除操作**
   - 在患者列表页面，点击某个患者的"删除"按钮
   - 或在患者详情页面，点击"删除"按钮

2. **确认删除**
   - 系统弹出确认对话框："确定要删除该患者吗？"
   - 点击"确定"继续删除
   - 点击"取消"放弃删除

3. **删除完成**
   - 删除成功后，系统显示成功提示
   - 患者列表自动刷新，被删除的患者不再显示

### 5.2 删除策略

**当前实现**：
- 硬删除：直接从数据库中删除患者记录
- 不支持恢复：删除后无法恢复（请谨慎操作）

**注意事项**：
- ⚠️ 删除操作不可逆，请确认后再执行
- 如果患者有关联的病案、处方等数据，删除可能会失败（取决于外键约束）
- 建议：对于需要保留历史记录的患者，使用"禁用"状态而非删除

---

## 6. 特殊功能说明

### 6.1 拼音码生成

**技术实现**：
- 使用库：`hyjiacan.pinyin4net`
- 工具类：`LYBT.Shared.Utilities.Text.PinYinHelper`
- 方法：`PinYinHelper.GetPinYinCode(string name)`

**生成规则**：
- 提取每个汉字的拼音首字母
- 示例：
  - "张三" → "ZS"
  - "王小红" → "WXH"
  - "李明" → "LM"

**生成时机**：
- 新建：用户输入姓名时自动生成（前端） + 提交时生成（后端）
- 编辑：用户修改姓名时自动更新（前端） + 提交时同步（后端）
- 批量导入：导入时自动生成（后端）

**代码位置**：
```
前端：
- PatientCreateViewModel.cs:51
- PatientEditViewModel.cs:67

后端：
- PatientService.cs:109-116 (CreateAsync)
- PatientService.cs:136-152 (UpdateAsync)
- PatientService.cs:254 (BatchImportAsync)
```

### 6.2 年龄计算

**计算位置**：
- **前端**：ViewModel的`Age`属性（getter）
- **后端**：Entity的`Age`属性（`[NotMapped]`标记）

**数据流**：
1. 用户选择出生日期
2. 前端ViewModel立即计算年龄并显示
3. 提交到后端时，只传输`BirthDate`字段
4. 后端Entity在查询时计算`Age`属性
5. Service层手动复制`Age`到DTO（因为AutoMapper不支持computed属性）
6. 返回前端显示

**代码位置**：
```
前端计算：
- PatientCreateViewModel.cs:96-112
- PatientEditViewModel.cs:112-128

后端计算：
- Patient.cs (Entity的Age属性)

后端复制：
- PatientService.cs:53-63 (GetPagedAsync)
- PatientService.cs:91-92 (GetByIdAsync)
```

### 6.3 FluentValidation条件验证

**应用场景**：
- 统一使用`PatientInputDto`进行创建和更新
- 通过`Id`字段判断是创建（Id为null）还是更新（Id有值）

**验证规则示例**（参考UserInputDtoValidator）：
```csharp
public class UserInputDtoValidator : AbstractValidator<UserInputDto>
{
    public UserInputDtoValidator()
    {
        // 用户名：创建时必填（Id为null），更新时可选
        RuleFor(x => x.UserName)
            .NotEmpty()
            .When(x => x.Id == null || x.Id == Guid.Empty);
    }
}
```

**关键点**：
- 使用`.When()`谓词进行条件验证
- 创建时：`Id == null || Id == Guid.Empty` → 必填
- 更新时：`Id`有值 → 可选

**代码位置**：
- `UserInputDtoValidator.cs:10-13`（参考示例）

---

## 7. 常见问题

### Q1: 拼音码显示为空或不正确？

**可能原因**：
1. 姓名中包含非汉字字符（英文、数字、符号）
2. 后端未正确生成拼音码

**解决方法**：
1. 确保使用正确的工具类：`LYBT.Shared.Utilities.Text.PinYinHelper`
2. 检查姓名是否为纯中文
3. 查看后端日志确认生成逻辑是否执行

### Q2: 年龄显示不正确或为空？

**可能原因**：
1. 出生日期未填写
2. 后端未正确复制Age属性到DTO

**解决方法**：
1. 确保已选择出生日期
2. 检查`PatientService`中的Age复制逻辑（4个方法：GetPagedAsync, GetByIdAsync, CreateAsync, UpdateAsync）
3. 查看后端日志确认Age计算是否正确

### Q3: 提交时显示验证错误？

**可能原因**：
1. 必填字段未填写（姓名、身份证号）
2. 字段长度超过限制

**解决方法**：
1. 检查所有必填字段是否已填写
2. 检查字段长度是否符合要求
3. 查看错误提示信息，按提示修改

### Q4: 编辑时拼音码未更新？

**可能原因**：
1. 姓名未实际发生变化
2. 后端更新逻辑未正确检测姓名变化

**解决方法**：
1. 确认姓名确实发生了变化
2. 检查`PatientService.UpdateAsync`中的姓名变化检测逻辑（`existingPatient.Name != input.Name`）

---

## 8. 技术参考

### 相关文件

**前端ViewModel**：
- `PatientCreateViewModel.cs` - 患者新建视图模型
- `PatientEditViewModel.cs` - 患者编辑视图模型
- `PatientDetailViewModel.cs` - 患者详情视图模型（如存在）
- `PatientManagementViewModel.cs` - 患者列表视图模型

**后端Service**：
- `PatientService.cs` - 患者业务逻辑
- `PatientRepository.cs` - 患者数据访问

**验证器**：
- `UserInputDtoValidator.cs` - 条件验证示例（参考）

**工具类**：
- `PinYinHelper.cs` - 拼音码生成工具

### 架构模式

**MVVM模式**：
- Model：`Patient` Entity，`PatientDto`
- ViewModel：`PatientCreateViewModel`，`PatientEditViewModel`
- View：`PatientCreateView.xaml`，`PatientEditView.xaml`

**Prism集成**：
- Region Navigation：`NavigateTo("ContentRegion", "PatientCreateView")`
- Event Aggregator：`PatientCreatedEvent`，`PatientUpdatedEvent`
- DelegateCommand：`SubmitCommand`，`CancelCommand`

**依赖注入**：
- 所有服务通过构造函数注入
- 示例：`IPatientRepository`, `IEventAggregator`, `ILoggerFactory`

---

## 9. 相关文档

- [Patient模块架构设计](../../explanation/architecture/client/README.md)
- [FluentValidation条件验证模式](../../explanation/validation-patterns.md)
- [Prism MVVM最佳实践](../development/prism-mvvm-best-practices.md)
- [Server端三层架构](../../explanation/architecture/server/README.md)

---

**文档版本**: v1.0
**最后更新**: 2025-11-09
**相关Issue**: Epic #1934
**提交哈希**: 3864741dc
