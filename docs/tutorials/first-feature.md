# 开发第一个功能

**目标**：通过完整的端到端示例，掌握系统的开发流程

**创建日期**：2025-10-29
**状态**：🚧 占位文档（待补充详细内容）

---

## 📋 你将学到

完成本教程后，你将能够：
- ✅ Server端三层架构开发（Entity → DTO → Service → Controller）
- ✅ Client端MVVM架构开发（Model → ViewModel → View）
- ✅ 完整的测试和提交流程

**预计时间**：1小时
**难度**：⭐⭐（初级）

---

## 🎯 示例功能说明

我们将开发一个简单的功能：**为病案添加备注**

**业务需求**：
- 医生在查看病案时，可以添加简短备注（如"需要复诊"）
- 备注显示在病案详情页面
- 备注可以修改和删除

**技术范围**：
- Server端：MedicalCase模块新增Remark端点
- Client端：PatientSelection视图新增备注输入框
- 数据库：MedicalCase表新增Remark字段

⚠️ **TODO**：确认示例功能选择，可能需要调整为更合适的教学案例

---

## 📝 第一部分：理解架构（10分钟）

### 1.1 Server端三层架构

⚠️ **TODO**：补充架构图和详细说明

```
┌─────────────────────────────────────────┐
│ Presentation Layer (Controllers)         │
│  - MedicalCaseController                 │
│  - 接收HTTP请求，返回响应                │
└─────────────────────────────────────────┘
              ↓ 调用
┌─────────────────────────────────────────┐
│ Application Layer (Services)             │
│  - MedicalCaseService                    │
│  - 业务逻辑、验证、协调                  │
└─────────────────────────────────────────┘
              ↓ 调用
┌─────────────────────────────────────────┐
│ Domain Layer (Entities + Repository)     │
│  - MedicalCaseEntity (DDD聚合根)         │
│  - IMedicalCaseRepository                │
└─────────────────────────────────────────┘
```

**关键概念**：
- ⚠️ **TODO**：解释每层职责
- ⚠️ **TODO**：说明依赖方向（Presentation → Application → Domain）

### 1.2 Client端MVVM架构

⚠️ **TODO**：补充架构图和详细说明

```
┌─────────────────────────────────────────┐
│ View (XAML + Code-Behind)                │
│  - PatientSelectionView.xaml             │
│  - 用户界面、数据绑定                    │
└─────────────────────────────────────────┘
              ↓ 绑定到
┌─────────────────────────────────────────┐
│ ViewModel (Prism ViewModelBase)          │
│  - PatientSelectionViewModel             │
│  - 属性、命令、UI逻辑                    │
└─────────────────────────────────────────┘
              ↓ 调用
┌─────────────────────────────────────────┐
│ Model (API Client + Repository)          │
│  - IMedicalCaseRepository                │
│  - IMedicalCaseApi (Refit)               │
└─────────────────────────────────────────┘
```

**关键概念**：
- ⚠️ **TODO**：解释MVVM模式的优势
- ⚠️ **TODO**：说明数据绑定和命令绑定

---

## 📝 第二部分：Server端开发（30分钟）

### 2.1 修改Entity（Domain层）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Server/Modules/LYBT.Server.MedicalCase.Domain/Entities/MedicalCaseEntity.cs`

```csharp
// TODO: 补充完整代码示例
public class MedicalCaseEntity
{
    // 新增字段
    public string? Remark { get; set; }  // 备注
    public DateTime? RemarkUpdatedAt { get; set; }  // 备注更新时间
}
```

**验证步骤**：
1. ⚠️ **TODO**：编译检查
2. ⚠️ **TODO**：创建EF Core迁移

### 2.2 创建DTO（Application层）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Server/Modules/LYBT.Server.MedicalCase.Application/DTOs/UpdateMedicalCaseRemarkDto.cs`

```csharp
// TODO: 补充完整代码示例
public class UpdateMedicalCaseRemarkDto
{
    public int MedicalCaseId { get; set; }
    public string? Remark { get; set; }
}
```

**验证步骤**：
1. ⚠️ **TODO**：DTO验证规则
2. ⚠️ **TODO**：AutoMapper配置

### 2.3 实现Service（Application层）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Server/Modules/LYBT.Server.MedicalCase.Application/Services/MedicalCaseService.cs`

```csharp
// TODO: 补充完整代码示例
public async Task UpdateRemarkAsync(UpdateMedicalCaseRemarkDto dto)
{
    // 业务规则验证
    // 调用Repository
    // 返回结果
}
```

**验证步骤**：
1. ⚠️ **TODO**：业务规则检查
2. ⚠️ **TODO**：异常处理

### 2.4 创建Controller（Presentation层）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Server/Services/LYBT.WebAPI/Controllers/v1/MedicalCaseController.cs`

```csharp
// TODO: 补充完整代码示例
[HttpPatch("{id}/remark")]
public async Task<ActionResult<MedicalCaseDto>> UpdateRemark(
    int id,
    [FromBody] UpdateMedicalCaseRemarkDto dto)
{
    // 调用Service
    // 返回结果
}
```

**验证步骤**：
1. ⚠️ **TODO**：Swagger测试
2. ⚠️ **TODO**：Postman测试

---

## 📝 第三部分：Client端开发（30分钟）

### 3.1 更新API接口（Refit）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`

```csharp
// TODO: 补充完整代码示例
[Patch("/api/v1/medicalcases/{id}/remark")]
Task<ApiResponse<MedicalCaseDto>> UpdateRemarkAsync(
    int id,
    [Body] UpdateMedicalCaseRemarkDto dto);
```

**验证步骤**：
1. ⚠️ **TODO**：编译检查
2. ⚠️ **TODO**：API调用测试

### 3.2 更新ViewModel（MVVM）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`

```csharp
// TODO: 补充完整代码示例
// 新增属性
private string? _remark;
public string? Remark
{
    get => _remark;
    set => SetProperty(ref _remark, value);
}

// 新增命令
public DelegateCommand UpdateRemarkCommand { get; }

private async void OnUpdateRemark()
{
    // 调用API
    // 更新UI
}
```

**验证步骤**：
1. ⚠️ **TODO**：属性通知检查
2. ⚠️ **TODO**：命令绑定检查

### 3.3 更新View（XAML）

⚠️ **TODO**：补充详细代码示例

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`

```xml
<!-- TODO: 补充完整代码示例 -->
<TextBox Text="{Binding Remark, UpdateSourceTrigger=PropertyChanged}"
         Watermark="添加备注..." />

<Button Content="保存备注"
        Command="{Binding UpdateRemarkCommand}" />
```

**验证步骤**：
1. ⚠️ **TODO**：UI显示检查
2. ⚠️ **TODO**：数据绑定检查

---

## 📝 第四部分：测试与提交（20分钟）

### 4.1 编译验证

⚠️ **TODO**：补充详细步骤

```bash
# 编译整个解决方案
dotnet build LYBT.All.sln -c Release --no-restore

# 要求：0 errors, 0 warnings
```

**验证步骤**：
1. ⚠️ **TODO**：检查编译输出
2. ⚠️ **TODO**：修复任何警告

### 4.2 运行时验证（⚠️ 强制）

⚠️ **TODO**：补充详细步骤

**Step 1：启动Server端**
```bash
# TODO: 补充启动命令
```

**Step 2：启动Client端**
```bash
# TODO: 补充启动命令
```

**Step 3：功能验证清单**
- [ ] 在病案详情页看到备注输入框
- [ ] 输入备注并保存
- [ ] 刷新页面，备注正确显示
- [ ] 修改备注，再次保存
- [ ] 验证数据库中Remark字段正确更新

### 4.3 创建Issue并提交

⚠️ **TODO**：补充详细步骤

**Step 1：创建GitHub Issue**
```bash
gh issue create --title "功能：为病案添加备注" --body "..."
```

**Step 2：提交代码**
```bash
git add .
git commit -m "feat(medicalcase): 为病案添加备注功能

Fixes #XXXX

- Server端：新增Remark字段和UpdateRemark端点
- Client端：新增备注输入框和保存命令
- 验证：功能已正常工作

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"

git push origin master
```

**Step 3：关闭Issue**
- Issue会通过`Fixes #XXXX`自动关闭

---

## ✅ 成功标志

完成以上步骤后，你应该能够：
- [x] 理解Server端三层架构的完整开发流程
- [x] 理解Client端MVVM架构的完整开发流程
- [x] 独立完成从需求到交付的完整流程
- [x] 掌握编译验证和运行时验证的标准

---

## 🐛 常见问题

### 问题1：编译错误 - 找不到类型

⚠️ **TODO**：补充常见编译错误

**错误信息**：
```
error CS0246: The type or namespace name 'UpdateMedicalCaseRemarkDto' could not be found
```

**解决方案**：
1. TODO: 检查命名空间
2. TODO: 检查项目引用

### 问题2：数据绑定不生效

⚠️ **TODO**：补充数据绑定问题

**症状**：UI不更新

**解决方案**：
1. TODO: 检查INotifyPropertyChanged实现
2. TODO: 检查UpdateSourceTrigger设置

### 问题3：API调用失败

⚠️ **TODO**：补充API调用问题

**症状**：404或500错误

**解决方案**：
1. TODO: 检查路由配置
2. TODO: 检查Swagger文档
3. TODO: 查看Server端日志

---

## 📚 下一步

恭喜完成第一个功能开发！接下来推荐：

1. **深入Server端**：阅读[Server端开发指南](../how-to-guides/server/README.md)
2. **深入Client端**：阅读[Client端开发指南](../how-to-guides/client/README.md)
3. **学习设计模式**：
   - [Repository模式](../explanation/architecture/patterns/repository-pattern.md)
   - [MVVM模式](../explanation/architecture/patterns/mvvm-pattern.md)
   - [聚合根模式](../explanation/architecture/patterns/aggregate-root-pattern.md)
4. **学习测试**：阅读[测试指南](../how-to-guides/shared/testing-guide.md)

---

## 🔗 相关资源

- [架构总览](../explanation/architecture/README.md) - 三层对齐架构
- [API快速参考](../reference/quick-reference/api-reference.md) - API文档
- [代码模式](../reference/quick-reference/code-patterns.md) - 常用代码模式
- [常见问题解决](../reference/quick-reference/troubleshooting.md) - 问题排查

---

⚠️ **编辑者注意**：本文档为占位版本，需要补充详细步骤、完整代码示例和验证命令。请参考Issue #1715完成内容填充。

**最后更新**：2025-10-29
**状态**：占位文档（待补充）
