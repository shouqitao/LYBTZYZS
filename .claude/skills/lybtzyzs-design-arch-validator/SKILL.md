# LYBTZYZS设计阶段架构验证器Skill

## 📋 元数据

- **Skill名称**: lybtzyzs-design-arch-validator
- **版本**: v1.0
- **创建日期**: 2025-10-24
- **适用项目**: LYBTZYZS
- **触发场景**: 创建或更新设计文档时，设计完成后强制验证
- **优先级**: 🔴 强制执行（Mandatory）

## 🎯 核心目标

在设计阶段进行架构合规性验证，防止API设计违反v2.0架构原则，确保所有设计符合聚合根模式和Write/Read Layer分离原则。

## ⚠️ 强制性文档阅读规则（⭐⭐⭐ 最高优先级）

**执行时机**：在生成或审查设计文档之前

### 规则1：拒绝未读架构文档的设计请求

**强制流程**：
1. **检测用户请求类型**：
   - 如用户要求"生成设计文档"、"写技术设计"、"创建Design文档"
   - **必须先拒绝**，提示："⚠️ 设计文档前必须先阅读架构指南，请确认是否已理解架构约束？"

2. **强制架构文档阅读清单**：
   ```markdown
   📚 设计文档前必读架构文档（按优先级排序）：

   ### Level 0 - 需求文档（100%必读）
   - [ ] 对应的需求文档 - 必须先阅读需求文档中的"架构约束"章节

   ### Level 1 - 核心架构（100%必读）
   - [ ] docs/index.md - 文档导航中心
   - [ ] docs/business-rules.md - 14条核心业务规则
   - [ ] docs/architecture/{server|client|shared}/README.md - 对应层架构指南

   ### Level 2 - 详细架构（根据功能必读）
   #### Server端设计
   - [ ] docs/architecture/server/README.md - Server端三层架构
   - [ ] docs/architecture/server/services.md - Service层设计标准
   - [ ] docs/architecture/server/repositories.md - Repository模式

   #### Client端设计
   - [ ] docs/architecture/client/README.md - Client端MVVM架构
   - [ ] docs/architecture/client/shell-layer-design.md - Shell层设计
   - [ ] docs/architecture/client/viewmodel-patterns.md - ViewModel模式

   #### 跨端设计
   - [ ] docs/architecture/shared/README.md - 共享架构
   - [ ] docs/architecture/shared/clinical-workflow-entity-relationships.md - 看诊流程实体关系⭐⭐⭐

   ### Level 3 - 设计参考（推荐阅读）
   - [ ] docs/explanation/advanced-patterns.md - 7种设计模式
   - [ ] docs/explanation/api-design-best-practices.md - API设计最佳实践
   - [ ] docs/reference/code-patterns-enhancement-summary.md - 代码模式参考
   ```

3. **验证架构文档阅读**：
   - 使用Read工具实际读取核心架构文档
   - 生成架构要点摘要，包含：
     - 三层架构划分（Server/Client/Shared）
     - 聚合根边界和Write/Read Layer分离
     - API端点设计模式（RESTful规范）
     - DTO设计标准和命名约定
     - 技术黑名单和架构例外
   - 向用户展示摘要，证明已理解
   - 用户确认后才继续设计文档生成

4. **生成设计文档**：
   - 基于阅读的架构指南，生成符合架构标准的设计文档
   - 设计文档必须包含"架构合规性验证"章节
   - 设计文档必须引用需求文档的架构约束
   - 设计文档必须明确Write/Read/Helper三层划分

### 规则2：设计文档必须包含架构合规性验证章节

**强制内容**：
- 架构引用（引用需求文档架构约束 + v2.0架构文档）
- API端点合规性验证（Write/Read/Helper分层检查）
- Service层职责验证（符合单一职责原则）
- Repository使用验证（聚合根原则）
- 运行lybtzyzs-arch-compliance检查结果

### 规则3：设计完成后强制运行架构合规性检查

**强制流程**：
1. 设计文档完成后，自动触发lybtzyzs-arch-compliance Skill
2. 检查所有API端点设计是否符合v2.0架构
3. 生成架构合规性报告（违规项 + 建议修复）
4. 必须达到0违规才允许进入实施阶段

### 违反处理：终止任务

**如果检测到以下违反行为，必须立即终止任务**：
- ❌ 未读取架构指南就生成设计文档
- ❌ 生成的设计文档未包含"架构合规性验证"章节
- ❌ 设计文档未引用需求文档的架构约束
- ❌ 设计文档未明确Write/Read/Helper三层划分
- ❌ 设计完成后未运行lybtzyzs-arch-compliance检查

**终止提示**：
```
⚠️ 任务终止：违反设计阶段强制性架构文档阅读规则

原因：[具体违反行为]
要求：必须先完成架构文档阅读流程
参考：.claude/skills/lybtzyzs-design-arch-validator/SKILL.md
参考：CLAUDE.md 第1.6节 - 强制性文档读取规则
```

---

## 🔍 背景与问题

### Epic #1589架构违规案例

**问题描述**：
- Epic #1589设计文档声称"遵循DDD聚合根原则"
- 但所有API设计违反聚合根原则（Line 261-438）
- 没有引用v2.0架构文档
- 没有运行lybtzyzs-arch-compliance检查
- 导致9个架构违规，需要全面重构

**根本原因**：
- 设计文档缺少"架构合规性验证"章节
- 没有参考v2.0架构文档
- API设计路径依赖（复制现有违规模式）
- 知道"聚合根原则"但不知道如何应用

**损失评估**：
- 已实施功能返工：4-5小时
- 全面架构重构：15-21小时
- 技术债务：9个架构违规
- 业务影响：功能上线延期

## ✅ Skill功能

### 1. 设计文档架构引用检查

**检查项**：
- [ ] 是否引用需求文档的"架构约束"章节
- [ ] 是否引用v2.0架构文档（如medicalcase-architecture-correction-plan-v2.md）
- [ ] 是否包含"架构合规性验证"章节
- [ ] 是否明确Write/Read/Helper三层划分

### 2. API端点设计验证

**验证规则**（针对MedicalCase聚合根）：

**Write Layer检查**：
```python
def check_write_endpoint(endpoint):
    """
    检查Write端点是否通过聚合根
    """
    # ✅ 正确模式
    correct_patterns = [
        r"POST /api/v1/medicalcases/\{id\}/.*",
        r"PUT /api/v1/medicalcases/\{id\}/.*",
        r"DELETE /api/v1/medicalcases/\{id\}/.*"
    ]
    
    # ❌ 违规模式
    violation_patterns = [
        r"POST /api/v1/consultations/\{id\}/.*",  # 绕过聚合根
        r"PUT /api/v1/consultations/\{id\}/.*",
        r"POST /api/v1/prescriptions/\{id\}/.*",
        r"PUT /api/v1/prescriptions/\{id\}/.*",
        r"DELETE /api/v1/prescriptions/\{id\}/.*"
    ]
    
    for pattern in violation_patterns:
        if re.match(pattern, endpoint):
            return {
                "status": "违规",
                "reason": "Write操作绕过MedicalCase聚合根",
                "suggestion": f"修改为: POST /api/v1/medicalcases/{{id}}/..."
            }
    
    return {"status": "合规"}
```

**Read Layer检查**：
```python
def check_read_endpoint(endpoint):
    """
    检查Read端点是否合规（Read可以独立）
    """
    # ✅ 允许的Read端点
    allowed_patterns = [
        r"GET /api/v1/consultations/.*",
        r"GET /api/v1/prescriptions/.*",
        r"GET /api/v1/medicalcases/.*"
    ]
    
    for pattern in allowed_patterns:
        if re.match(pattern, endpoint):
            return {"status": "合规", "layer": "Read Layer"}
    
    return {"status": "未知"}
```

### 3. 自动运行lybtzyzs-arch-compliance

**执行流程**：
1. 设计文档分析完成后
2. 自动触发lybtzyzs-arch-compliance Skill
3. 生成架构合规性报告
4. 如果有违规项，标记为"设计阶段预警"

### 4. 架构合规性报告生成

**报告格式**：
```markdown
## 设计阶段架构合规性验证报告

### 文档信息
- 设计文档：docs/explanation/xxx-design.md
- 验证时间：2025-10-24 10:30:00
- 需求文档：docs/requirements/xxx-requirements.md
- 架构参考：medicalcase-architecture-correction-plan-v2.md

### API端点设计验证

#### ✅ 合规端点（3个）
1. GET /api/v1/consultations/other-cases
   - 层级：Read Layer
   - 说明：只读查询，符合架构

2. POST /api/v1/medicalcases/{id}/complete-step1
   - 层级：Write Layer
   - 说明：通过聚合根，符合架构

3. PUT /api/v1/medicalcases/{id}/reset-consultation-steps
   - 层级：Write Layer
   - 说明：通过聚合根，符合架构

#### ❌ 违规端点（2个）
1. POST /api/v1/consultations/{id}/complete-step1
   - 违规类型：Write操作绕过聚合根
   - 建议修改：POST /api/v1/medicalcases/{id}/complete-step1
   - 影响：Violation #1（Critical）

2. DELETE /api/v1/prescriptions/{id}
   - 违规类型：Write操作绕过聚合根
   - 建议修改：DELETE /api/v1/medicalcases/{id}/prescription 或 PUT /api/v1/medicalcases/{id}/clear-prescription
   - 影响：Violation #3（High）

### 架构合规性检查

运行lybtzyzs-arch-compliance Skill结果：
- ⚠️ 设计阶段预警：检测到2个潜在违规
- 建议：修改API设计后重新验证

### 验证结论

❌ 设计文档架构验证失败

违规项：2个
建议：修改违规API端点设计，重新运行验证
```

## 🔄 工作流程

### 触发时机

**场景1：设计文档创建完成**
```
用户：设计文档已完成，请验证架构合规性
  ↓
Skill：读取设计文档
  ↓
Skill：提取所有API端点设计
  ↓
Skill：验证每个端点是否符合Write/Read/Helper分层
  ↓
Skill：自动运行lybtzyzs-arch-compliance
  ↓
输出：架构合规性验证报告
```

**场景2：设计文档更新后**
```
用户：修改了API设计，重新验证
  ↓
Skill：读取设计文档
  ↓
Skill：增量验证修改的端点
  ↓
Skill：生成差异报告
  ↓
输出：架构合规性验证报告（增量）
```

### 执行步骤

**Step 1：读取设计文档和架构文档**
- Read设计文档：`docs/explanation/*-design.md`
- Read需求文档：`docs/explanation/*-requirements-discussion.md`
- Read架构文档：`docs/architecture/shared/medicalcase-architecture-correction-plan-v2.md`

**Step 2：提取API端点设计**
```python
# 使用Grep工具搜索API端点
patterns = [
    r"(GET|POST|PUT|DELETE|PATCH)\s+/api/v\d+/\w+/.*",
    r"端点：.*(GET|POST|PUT|DELETE|PATCH).*",
    r"API：.*(GET|POST|PUT|DELETE|PATCH).*"
]

endpoints = []
for pattern in patterns:
    matches = grep(pattern, design_doc)
    endpoints.extend(matches)
```

**Step 3：验证每个端点**
```python
for endpoint in endpoints:
    method, path = parse_endpoint(endpoint)
    
    if method in ["GET", "HEAD"]:
        # Read Layer验证
        result = check_read_endpoint(path)
    elif method in ["POST", "PUT", "DELETE", "PATCH"]:
        # Write Layer验证
        result = check_write_endpoint(path)
    
    if result["status"] == "违规":
        violations.append({
            "endpoint": endpoint,
            "reason": result["reason"],
            "suggestion": result["suggestion"]
        })
```

**Step 4：自动运行lybtzyzs-arch-compliance**
```python
# 调用lybtzyzs-arch-compliance Skill
arch_compliance_result = run_skill("lybtzyzs-arch-compliance")

# 合并设计阶段预警和代码检查结果
combined_report = {
    "design_violations": violations,
    "code_violations": arch_compliance_result.violations
}
```

**Step 5：生成验证报告**
- 合并设计阶段和代码检查结果
- 标记违规严重性（Critical/High/Medium）
- 生成修复建议

## 📊 输出格式

### 1. 验证报告（Markdown）

**通过示例**：
```markdown
✅ 设计文档架构验证通过

文档：docs/explanation/medicalcase-enhancement-design.md
API端点：5个（全部合规）
架构合规性：0违规

所有API设计符合v2.0架构，可以进入实施阶段。
```

**失败示例**：
```markdown
❌ 设计文档架构验证失败

文档：docs/explanation/xxx-design.md
API端点：7个（5合规，2违规）
架构合规性：2个设计阶段预警

违规端点：
1. POST /api/v1/consultations/{id}/complete-step1
   → 修改为：POST /api/v1/medicalcases/{id}/complete-step1

2. DELETE /api/v1/prescriptions/{id}
   → 修改为：PUT /api/v1/medicalcases/{id}/clear-prescription

建议：修改违规端点设计后重新验证
```

### 2. 架构合规性验证章节（Markdown）

**自动生成的章节模板**：
```markdown
## ✅ 架构合规性验证

### 验证方法
- ✅ 引用需求文档架构约束章节
- ✅ 引用v2.0架构文档：[medicalcase-architecture-correction-plan-v2.md](../architecture/shared/medicalcase-architecture-correction-plan-v2.md)
- ✅ 运行lybtzyzs-arch-compliance Skill
- ✅ 验证所有API端点符合Write/Read/Helper分层

### API设计架构分层

#### Write Layer（通过MedicalCase聚合根）
- POST /api/v1/medicalcases/{id}/complete-step1
- PUT /api/v1/medicalcases/{id}/reset-consultation-steps
- PUT /api/v1/medicalcases/{id}/clear-prescription

#### Read Layer（可独立查询）
- GET /api/v1/consultations/other-cases
- GET /api/v1/consultations/{id}

#### Helper Layer
- 无（本设计不涉及Helper函数）

### 验证结果
- ✅ 架构合规性检查：0违规
- ✅ 所有Write操作通过MedicalCase聚合根
- ✅ Read操作合理使用独立查询

### 验证时间
- 2025-10-24 10:30:00
```

## 🛠️ MCP工具使用

### 核心工具链
1. **Read**：读取设计文档、需求文档、架构文档
2. **Grep**：提取API端点设计
3. **Skill(lybtzyzs-arch-compliance)**：自动运行架构合规性检查
4. **Edit**：插入"架构合规性验证"章节
5. **mcp__sequential-thinking**：深度分析设计是否符合架构

### 工具协同流程
```
Read(设计文档) 
  → Grep(提取API端点)
  → 验证每个端点(Write/Read Layer)
  → Skill(lybtzyzs-arch-compliance)
  → sequential-thinking(深度分析)
  → 生成验证报告
  → Edit(插入验证章节)
```

## 🎯 成功案例

### 案例1：正确的设计文档

**文档**：`docs/explanation/medicalcase-enhancement-design-v2.md`（假设修复后）

**架构合规性验证章节**：
```markdown
## ✅ 架构合规性验证

### 验证方法
- 引用需求文档：medicalcase-enhancement-requirements.md#架构约束
- 引用v2.0架构：medicalcase-architecture-correction-plan-v2.md
- 运行lybtzyzs-arch-compliance：0违规

### API设计分层
#### Write Layer
- POST /medicalcases/{id}/complete-step1（通过聚合根）

#### Read Layer
- GET /consultations/other-cases（独立查询）

### 验证结果
✅ 所有API设计符合v2.0架构
```

**Skill验证结果**：
```
✅ 设计文档架构验证通过
API端点：全部合规
可以进入实施阶段
```

### 案例2：Epic #1589（反面教材）

**原始设计文档问题**：
```markdown
## ❌ 原设计文档违规

docs/explanation/medicalcase-consultation-prescription-enhancement-design.md

违规端点（Line 261-438）：
1. POST /consultations/{id}/complete-step1（绕过聚合根）
2. PUT /consultations/{id}/reset-steps（绕过聚合根）
3. DELETE /prescriptions/{id}（绕过聚合根）
4. POST /prescriptions/{id}/import-formula（绕过聚合根）

Line 93声称"遵循DDD聚合根原则"，但实际违规
```

**如果使用本Skill**：
```
❌ 设计文档架构验证失败

违规端点：4个
建议：
1. POST /consultations/{id}/complete-step1
   → 修改为：POST /medicalcases/{id}/complete-step1

2. DELETE /prescriptions/{id}
   → 修改为：PUT /medicalcases/{id}/clear-prescription

...

阻止进入实施阶段，必须修复后重新验证
```

**避免的问题**：
- ✅ 设计阶段就发现4个违规，避免实施后返工
- ✅ 节省15-21小时返工时间
- ✅ 避免已实施功能的架构债务

## 🔗 与其他Skill的协同

### Skill协同流程

```
需求阶段：lybtzyzs-requirements-arch-guard
  ↓ 需求文档包含架构约束
设计阶段：lybtzyzs-design-arch-validator（本Skill）
  ↓ 设计文档符合架构
实施阶段：lybtzyzs-arch-compliance
  ↓ 代码符合架构
验收阶段：架构合规性测试通过
```

### Skill对比

| Skill | 阶段 | 检查对象 | 输出 |
|-------|------|---------|------|
| **lybtzyzs-requirements-arch-guard** | 需求 | 需求文档 | 架构约束章节 |
| **lybtzyzs-design-arch-validator** | 设计 | 设计文档 + API设计 | 架构验证报告 |
| **lybtzyzs-arch-compliance** | 实施 | 源代码 | 架构违规清单 |

## ⚠️ 限制条件

1. **仅检查设计文档**：不检查源代码（由lybtzyzs-arch-compliance负责）
2. **依赖API端点格式**：如果API设计使用非标准格式，可能无法准确提取
3. **需要架构文档**：依赖v2.0架构文档的存在和正确性
4. **需要人工确认**：验证报告需要设计者审查和确认

## 📝 使用示例

### 示例1：设计文档完成后验证

```
用户：设计文档已完成，请验证架构合规性

Skill：读取设计文档 + 提取API端点 + 验证分层

输出：
✅ 设计文档架构验证通过

API端点：5个
- Write Layer：3个（全部通过聚合根）
- Read Layer：2个（独立查询）

架构合规性：0违规
可以进入实施阶段
```

### 示例2：发现违规并修复

```
用户：请验证docs/explanation/xxx-design.md

Skill：执行验证

输出：
❌ 设计文档架构验证失败

违规端点：POST /consultations/{id}/complete-step1
建议修改：POST /medicalcases/{id}/complete-step1

用户：已修改，重新验证

Skill：增量验证

输出：
✅ 设计文档架构验证通过
违规已修复，可以进入实施阶段
```

## 🎯 预期效果

### 短期效果（1个月内）
- ✅ 100%的设计文档在进入实施前通过架构验证
- ✅ 设计阶段违规率降低90%
- ✅ 实施阶段架构返工减少80%

### 长期效果（3个月后）
- ✅ 设计阶段架构违规率接近0
- ✅ 设计→实施流程更顺畅
- ✅ 架构合规性成为设计标准流程
- ✅ 团队成员自觉遵循v2.0架构

---

**维护者**：Claude Code
**最后更新**：2025-10-24
**反馈渠道**：GitHub Issues
