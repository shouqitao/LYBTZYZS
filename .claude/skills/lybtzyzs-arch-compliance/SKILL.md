---
name: lybtzyzs-arch-compliance
description: 检查LYBTZYZS项目是否符合三层对齐架构规范，验证依赖方向、DDD聚合根边界和Repository模式。支持三个阶段：需求文档检查、设计文档验证、代码实现检查。触发关键词：架构检查、检查架构、验证架构、arch-compliance、architecture check
version: v2.0
last_updated: 2025-11-29
---

# LYBTZYZS 架构合规检查

## 变更记录
- v2.0 (2025-11-29): 合并需求/设计阶段检查，支持全生命周期架构验证
- v1.0 (2025-10-21): 初始版本

---

## 核心能力

本Skill支持三个阶段的架构合规检查:

| 阶段 | 检查对象 | 主要检查项 |
|------|---------|-----------|
| **需求阶段** | 需求文档 | 架构约束章节、架构文档引用 |
| **设计阶段** | 设计文档 | API端点设计、Write/Read分层 |
| **实施阶段** | 源代码 | 依赖方向、聚合根边界、Repository模式 |

---

## 使用方式

### 1. 需求阶段检查

**触发词**: "检查需求文档架构约束"、"需求阶段架构检查"

**检查项**:
- [ ] 是否有"架构约束"章节
- [ ] 是否引用相关架构文档
- [ ] 是否明确Write/Read Layer要求
- [ ] 是否列出技术黑名单

**输出**: 需求文档架构约束检查报告

### 2. 设计阶段检查

**触发词**: "验证设计文档架构"、"设计阶段架构检查"

**检查项**:
- [ ] API端点是否符合Write/Read/Helper分层
- [ ] 是否引用需求文档的架构约束
- [ ] 是否包含"架构合规性验证"章节

**输出**: 设计文档架构验证报告

### 3. 实施阶段检查

**触发词**: "检查代码架构"、"实施阶段架构检查"、"arch-compliance"

**检查项**:
- [ ] Server端三层依赖方向
- [ ] Client端MVVM依赖方向
- [ ] DDD聚合根边界
- [ ] Repository模式正确性

**输出**: 代码架构合规报告

---

## 实施阶段详细检查

### 第一步：验证Server端三层架构

**架构规范**:
```
src/Server/
├── Presentation/           # API Controllers
│   └─ 依赖 → Application
├── Application/            # Services
│   └─ 依赖 → Domain + Infrastructure
└── Infrastructure/         # Repository
    └─ 依赖 → Domain
```

**依赖方向规则**:
- ✅ Presentation → Application
- ✅ Application → Domain
- ✅ Infrastructure → Domain
- ❌ Application → Presentation（违规）
- ❌ Domain → Application（违规）

### 第二步：验证Client端MVVM架构

**架构规范**:
```
src/Client/Desktop/
├── Views/          → 依赖 ViewModels
├── ViewModels/     → 依赖 Services
├── Services/       → 依赖 ApiClient
└── ApiClient/      → 依赖 Shared.Contracts
```

### 第三步：验证DDD聚合根边界

**聚合根规范**:
1. **1:1:1原则** - 一次就诊=1个就诊记录+1个医案+1个处方
2. **边界一致性** - 聚合根内部强一致性
3. **Repository粒度** - 每个聚合根一个Repository

**检测场景**:
```csharp
// ❌ 违规：直接修改聚合内部
medicalCase.Prescription.Items.Add(item);

// ✅ 正确：通过聚合根公共方法
medicalCase.AddPrescriptionItem(item);
```

### 第四步：验证Repository模式

**Repository规范**:
1. **接口驱动** - Repository必须有接口定义
2. **单一职责** - 每个聚合根一个Repository
3. **仅数据访问** - 不包含业务逻辑

---

## 输出格式

### 架构合规报告

```markdown
# 架构合规检查报告

生成时间：[时间戳]
检查阶段：[需求/设计/实施]
检查范围：[项目/模块]

## ❌ 违规项（需立即修复）

### 1. [违规类型]
- 位置：[文件:行号]
- 违规：[具体描述]
- 修复：[修复建议]

## ⚠️ 建议项（需人工确认）

### 1. [建议类型]
- 位置：[文件:行号]
- 分析：[分析内容]
- 建议：[改进建议]

## ✅ 通过项

- [通过项1]
- [通过项2]
```

---

## 工具协同

本Skill调用以下MCP工具:

1. **serena** - 分析项目依赖关系和代码结构
2. **grep** - 检测聚合根边界和Repository模式
3. **sequential-thinking** - 深度分析架构设计合理性
4. **filesystem** - 读取架构文档

---

## 参考文档

- Server架构：`docs/architecture/server/README.md`
- Client架构：`docs/architecture/client/README.md`
- Shared架构：`docs/architecture/shared/README.md`
- DDD最佳实践：`docs/explanation/advanced-patterns.md`

---

## 限制条件

- 本Skill基于静态分析，无法检测运行时架构违规
- 聚合根边界判断依赖启发式分析，可能存在误判
- 最终决策权在用户，本Skill仅提供建议
