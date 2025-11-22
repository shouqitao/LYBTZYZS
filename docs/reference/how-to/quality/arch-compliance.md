---
name: lybtzyzs-arch-compliance
description: 检查LYBTZYZS项目是否符合三层对齐架构规范，验证依赖方向、DDD聚合根边界和Repository模式
version: v1.0
last_updated: 2025-10-21
---

# LYBTZYZS 架构合规检查

## 变更记录
- v1.0 (2025-10-21): 初始版本

---

## 检查目标

本Skill用于验证代码是否遵循LYBTZYZS项目的三层对齐架构规范。

**检查范围**：
- Server端三层架构（Controller → Service → Repository → DB）
- Client端MVVM五层架构（View → ViewModel → Service → ApiClient → Model）
- DDD聚合根边界和一致性规则
- Repository模式正确性
- 依赖方向验证（自动检测）

**参考文档**：
- Server架构：`docs/architecture/server/README.md`
- Client架构：`docs/architecture/client/README.md`
- Shared架构：`docs/architecture/shared/README.md`

---

## 检查流程

### 第一步：验证Server端三层架构

使用`serena`工具分析项目依赖关系，检查是否违反依赖方向。

**架构规范**：
```
src/Server/
├── Presentation/           # API Controllers（端点）
│   └─ 依赖 → Application
├── Application/            # Services（业务逻辑）
│   └─ 依赖 → Domain + Infrastructure
└── Infrastructure/         # Repository（数据访问）
    └─ 依赖 → Domain
```

**依赖方向规则**：
- ✅ Presentation → Application ✓
- ✅ Application → Domain ✓
- ✅ Infrastructure → Domain ✓
- ❌ Application → Presentation ✗（违规）
- ❌ Domain → Application ✗（违规）
- ❌ Domain → Infrastructure ✗（违规）

**检测命令**（使用serena工具）：
```
1. 分析Application项目引用
   → 如引用Presentation项目 → 违规

2. 分析Domain项目引用
   → 如引用Application或Infrastructure → 违规
```

**如发现违规**：
- 直接报告违规的项目引用
- 说明违反的架构原则
- 提供重构建议

**示例报告**：
```
❌ 架构违规：依赖方向错误

项目：LYBT.Server.Application
违规：引用了LYBT.Server.Presentation
位置：Application.csproj:12

违反原则：Application层不应依赖Presentation层
说明：业务逻辑层不应依赖API控制器

建议修复：
1. 移除对Presentation的引用
2. 将共享的DTO移动到Shared项目
3. 使用依赖注入接口而非直接引用
```

---

### 第二步：验证Client端MVVM架构

使用`serena`工具分析Client端依赖关系。

**架构规范**：
```
src/Client/Desktop/
├── Views/                  # WPF视图
│   └─ 依赖 → ViewModels
├── ViewModels/             # MVVM ViewModel
│   └─ 依赖 → Services
├── Services/               # 业务服务层
│   └─ 依赖 → ApiClient
├── ApiClient/              # HTTP客户端
│   └─ 依赖 → Shared.Contracts
└── Models/                 # 客户端模型
```

**依赖方向规则**：
- ✅ View → ViewModel ✓
- ✅ ViewModel → Service ✓
- ✅ Service → ApiClient ✓
- ❌ ViewModel → ApiClient ✗（跳层）
- ❌ Service → View ✗（反向依赖）

**检测重点**：
1. ViewModel是否直接调用HttpClient（应通过ApiClient）
2. View是否包含业务逻辑（应在ViewModel）
3. Service是否直接访问UI控件（应通过数据绑定）

---

### 第三步：验证DDD聚合根边界

使用`grep`工具检查聚合根使用是否符合DDD规范。

**聚合根规范**：
1. **1:1:1原则** - 一次就诊=1个就诊记录+1个医案+1个处方
2. **边界一致性** - 聚合根内部强一致性，跨聚合最终一致性
3. **Repository粒度** - 每个聚合根一个Repository

**检测场景**：

**场景1：检测跨聚合直接修改**
```bash
grep -r "medicalCase\.Prescription\." --include="*.cs" src/Server/Application/
```

如果Service层直接修改`medicalCase.Prescription`内部状态 → 违规
建议：通过MedicalCase的公共方法修改，保持边界

**场景2：检测Repository粒度**
```bash
grep -r "class.*Repository" --include="*.cs" src/Server/Infrastructure/
```

检查是否存在：
- 细粒度Repository（如PrescriptionItemRepository）→ 建议合并到PrescriptionRepository
- 跨聚合Repository（如ConsultationMedicalCaseRepository）→ 建议拆分

**示例报告**：
```
⚠️ 聚合根边界建议

文件：ConsultationService.cs:45
代码：medicalCase.Prescription.Items.Add(item);

分析：Service层直接修改Prescription内部集合
风险：破坏聚合根封装，绕过业务规则验证

建议修复：
// ❌ 当前代码
medicalCase.Prescription.Items.Add(item);

// ✅ 建议代码
medicalCase.AddPrescriptionItem(item);  // 通过MedicalCase公共方法

理由：保持聚合根边界，确保业务规则在聚合内执行

❓ 请确认是否接受此建议？
```

---

### 第四步：验证Repository模式

使用`serena`工具检查Repository实现是否符合规范。

**Repository规范**：
1. **接口驱动** - Repository必须有接口定义
2. **单一职责** - 每个聚合根一个Repository
3. **仅数据访问** - 不包含业务逻辑

**检测重点**：

**1. 检查Repository是否包含业务逻辑**
```csharp
// ❌ 违规：Repository包含业务计算
public class PatientRepository
{
    public decimal CalculateAge(Patient patient)  // 业务逻辑
    {
        return DateTime.Now.Year - patient.BirthDate.Year;
    }
}

// ✅ 正确：业务逻辑在Service或Domain
public class PatientService
{
    public decimal CalculateAge(Patient patient)
    {
        return DateTime.Now.Year - patient.BirthDate.Year;
    }
}
```

**2. 检查是否有细粒度Repository**
```
❌ PrescriptionItemRepository   → 应合并到PrescriptionRepository
❌ MedicalCaseSymptomRepository → 应合并到MedicalCaseRepository
✅ PatientRepository            → 正确（Patient是聚合根）
```

**检测命令**（使用grep）：
```bash
grep -r "public.*Calculate\|public.*Validate" --include="*Repository.cs" src/Server/Infrastructure/
```

如发现Repository包含Calculate/Validate等业务方法 → 违规

---

### 第五步：生成架构合规报告

汇总所有检查结果，生成完整的架构合规报告。

**报告结构**：
```markdown
# 架构合规检查报告

生成时间：[时间戳]
检查范围：[项目/模块]

## ❌ 自动检测违规（需立即修复）

### 1. 依赖方向违规
- 项目：Application
- 违规：引用Presentation
- 修复：移除引用，使用接口

### 2. Repository业务逻辑
- 文件：PatientRepository.cs
- 违规：包含CalculateAge业务方法
- 修复：移至Service层

## ⚠️ 建议确认项（需人工决策）

### 1. 聚合根边界
- 文件：ConsultationService.cs:45
- 分析：直接修改聚合内部集合
- 建议：通过聚合根公共方法修改
- 状态：等待确认

### 2. Repository粒度
- 发现：PrescriptionItemRepository
- 分析：细粒度Repository
- 建议：合并到PrescriptionRepository
- 状态：等待确认

## ✅ 通过检查

- Server端依赖方向正确
- Client端MVVM架构清晰
- Repository模式规范
```

---

## 工具协同

本Skill调用以下MCP工具：

1. **serena** - 分析项目依赖关系和代码结构
2. **grep** - 检测聚合根边界和Repository模式
3. **sequential-thinking** - 深度分析架构设计合理性
4. **filesystem** - 读取架构文档

**执行顺序**：
```
serena（依赖分析）→ grep（模式检测）→ sequential-thinking（架构评估）→ 生成报告
```

---

## 测试场景

### 场景1：检测依赖方向违规

**测试代码**：
```xml
<!-- Application.csproj -->
<ProjectReference Include="..\Presentation\Presentation.csproj" />
```

**预期输出**：
```
❌ 架构违规：依赖方向错误

项目：LYBT.Server.Application
违规：引用了LYBT.Server.Presentation
说明：Application层不应依赖Presentation层

修复建议：
1. 移除对Presentation的引用
2. 将共享DTO移至Shared项目
```

---

### 场景2：检测聚合根边界违规

**测试代码**：
```csharp
public class ConsultationService
{
    public void AddPrescriptionItem(int medicalCaseId, PrescriptionItem item)
    {
        var medicalCase = _repository.GetById(medicalCaseId);

        // ❌ 直接修改Prescription内部集合
        medicalCase.Prescription.Items.Add(item);

        _repository.Update(medicalCase);
    }
}
```

**预期输出**：
```
⚠️ 聚合根边界建议

文件：ConsultationService.cs
代码：medicalCase.Prescription.Items.Add(item)
分析：直接修改聚合内部集合，破坏封装

建议修复：
medicalCase.AddPrescriptionItem(item);  // 通过聚合根公共方法

❓ 请确认是否接受此建议？
```

---

## 使用指南

### 触发时机

当用户提出以下请求时，自动触发本Skill：
- "检查架构是否符合规范"
- "验证依赖方向"
- "分析DDD聚合根边界"
- "检查Repository模式"

### 执行步骤

1. **明确检查范围**：询问检查Server端还是Client端
2. **依赖分析**：使用serena工具分析项目引用
3. **模式检测**：使用grep检查聚合根和Repository
4. **生成报告**：汇总违规和建议项
5. **等待确认**：对于边界判断，等待用户决策

### 注意事项

- 依赖方向违规 → 直接报告，无需确认
- 聚合根边界 → 提供分析，等待确认
- Repository粒度 → 提供建议，等待确认
- 报告中包含具体修复步骤

---

## 限制和免责

- 本Skill基于静态分析，无法检测运行时架构违规
- 聚合根边界判断依赖启发式分析，可能存在误判
- 最终决策权在用户，本Skill仅提供建议
- 建议结合Code Review进行人工验证

---

## 相关资源

- Server架构文档：`docs/architecture/server/README.md`
- Client架构文档：`docs/architecture/client/README.md`
- DDD最佳实践：`docs/deep/advanced-patterns.md`
- Repository模式：`docs/quick-reference/code-patterns.md`
