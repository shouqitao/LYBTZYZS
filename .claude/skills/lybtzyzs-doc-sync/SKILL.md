---
name: lybtzyzs-doc-sync
description: 检测LYBTZYZS项目代码变更并生成文档更新清单，确保文档与代码100%同步
version: v1.0
last_updated: 2025-10-21
---

# LYBTZYZS 文档同步检查

## 变更记录
- v1.0 (2025-10-21): 初始版本

---

## 检查目标

本Skill用于自动检测代码变更对文档的影响，生成文档更新清单，确保文档与代码保持同步。

**检查范围**：
- API端点变更（新增/修改/删除）
- 架构调整（模块/服务变更）
- 数据模型变更（实体/DTO修改）
- 配置文件变更
- 文档链接有效性
- **需求分析和设计文档前置检查**（⭐ 新增）
- **架构调整文档同步强制验证**（⭐ 新增）

**核心文档体系**（必须覆盖）：
- **Level 0 - 导航中心**：`docs/index.md` - 文档体系总入口 ⭐⭐⭐
- **Level 1 - 快速参考**：`docs/quick-reference/` - API参考、配置模板、代码模式、问题解决、开发清单
- **Level 2 - 架构指南**：`docs/architecture/{server|client|shared}/` - 三层对齐架构文档
  - `docs/architecture/server/README.md` - Server端三层架构
  - `docs/architecture/client/README.md` - Client端MVVM架构
  - `docs/architecture/shared/README.md` - 共享架构
- **Level 3 - 深度参考**：`docs/deep/`, `docs/api/`, `docs/modules/` - 完整技术文档
- **业务规则**：`docs/business-rules.md` - 14条核心业务规则 ⭐⭐⭐
- **技术决策**：`docs/architecture/decisions/` - ADR架构决策记录（计划中）

---

## 检查流程

### 🚨 前置检查（强制优先级）

#### 检查0：需求分析和设计文档前置验证

**触发时机**：用户要求生成需求文档或设计文档时

**强制流程**：
1. **拒绝未读文档的请求**：
   - 如用户直接要求"生成需求文档"或"写设计文档"，必须先拒绝
   - 提示："⚠️ 需求分析前必须先阅读文档体系，请确认是否已阅读相关文档？"

2. **强制文档阅读清单**：
   ```markdown
   ## 📚 需求分析前必读文档

   ### 核心必读（100%必须）
   - [ ] `docs/index.md` - 文档导航中心，了解文档体系结构
   - [ ] `docs/business-rules.md` - 14条核心业务规则
   - [ ] `docs/architecture/{server|client|shared}/README.md` - 对应层的架构指南

   ### 模块相关（根据需求选择）
   - [ ] `docs/modules/{module-name}/README.md` - 相关模块文档
   - [ ] `docs/api/{module-name}-api.md` - 相关API文档
   - [ ] `docs/quick-reference/code-patterns.md` - 代码模式参考

   ### 设计参考（可选）
   - [ ] `docs/deep/advanced-patterns.md` - 高级设计模式
   - [ ] `docs/deep/api-design-best-practices.md` - API设计最佳实践
   ```

3. **验证文档阅读**：
   - 使用 `Read` 工具读取核心文档（docs/index.md、business-rules.md、对应架构文档）
   - 生成文档要点摘要，证明已理解
   - 用户确认后才继续需求分析

4. **生成需求文档**：
   - 基于阅读的文档体系，生成符合架构标准的需求文档
   - 需求文档必须引用相关架构文档和业务规则

**示例对话**：
```
用户："帮我生成处方管理功能的需求文档"

Skill自动触发：
⚠️ 需求分析前必须先阅读文档体系！

📚 即将阅读以下核心文档：
1. docs/index.md - 文档导航
2. docs/business-rules.md - 业务规则
3. docs/architecture/client/README.md - Client架构
4. docs/modules/prescriptions/README.md - 处方模块

正在阅读文档...

✅ 文档阅读完成，关键要点：
- 处方必须通过MedicalCase聚合根操作（业务规则#3）
- Desktop采用MVVM架构，View→ViewModel→Repository→ApiClient（架构规范）
- 处方支持四种录入方式：手工/模板/历史/验方（模块文档）

请确认是否开始需求分析？
```

---

#### 检查0.5：架构调整文档同步强制验证

**触发时机**：用户要求进行架构调整时（新增模块、修改架构、重构）

**强制流程**：
1. **拒绝未同步文档的架构变更**：
   - 如用户直接要求"重构XXX"或"新增YYY模块"，必须先拒绝
   - 提示："⚠️ 架构调整前必须先更新文档，请确认是否已更新ADR和架构文档？"

2. **强制文档更新流程**：
   ```markdown
   ## 🏗️ 架构调整文档同步流程

   ### Step 1: 创建ADR（Architecture Decision Record）
   - [ ] 在 `docs/architecture/decisions/` 创建 ADR-XXX.md
   - [ ] 记录架构决策背景、方案对比、后果分析
   - [ ] 状态标记：Proposed → Accepted → Implemented

   ### Step 2: 更新架构文档
   - [ ] 更新 `docs/architecture/{server|client|shared}/README.md`
   - [ ] 如新增模块，创建 `docs/modules/{module-name}/README.md`
   - [ ] 更新 `docs/index.md` 导航链接

   ### Step 3: 更新架构例外清单（如有违反）
   - [ ] 在 `docs/architecture/exceptions.md` 记录例外
   - [ ] 说明批准理由和补救措施

   ### Step 4: 确认后开始代码变更
   - [ ] 用户审查ADR和文档更新
   - [ ] 批准后创建Issue并开始实施
   ```

3. **验证文档更新**：
   - 检查是否已创建ADR文档
   - 检查相关架构文档是否已更新
   - 生成文档更新清单，等待用户确认

4. **开始代码变更**：
   - 确认文档更新后，才允许开始代码修改
   - 代码变更必须引用对应的ADR编号

**示例对话**：
```
用户："重构Prescriptions模块，删除IPrescriptionRepository"

Skill自动触发：
⚠️ 架构调整前必须先更新文档！

🏗️ 检测到架构调整：删除IPrescriptionRepository

📝 必须先完成以下文档：
1. 创建 ADR-003-repository-simplification.md
   - 记录删除Repository的决策背景
   - 分析Read/Write分离方案
   - 记录架构例外（Desktop三层架构违反）

2. 更新 docs/architecture/client/README.md
   - 补充Repository层简化说明
   - 更新依赖关系图

3. 更新 docs/architecture/exceptions.md
   - 记录Desktop三层架构违反
   - 批准理由：DDD聚合根优先

是否现在创建这些文档？
```

---

### 第一步：检测API端点变更

使用`serena`工具分析Controller文件，对比Git历史，检测API变更。

**检测内容**：
1. **新增端点** - 新增的`[HttpGet/Post/Put/Delete]`方法
2. **修改端点** - 路由、参数、返回类型变更
3. **删除端点** - 移除的API方法

**检测命令**（使用serena + git）：
```bash
# 1. 获取修改的Controller文件
git diff --name-only HEAD~1 src/Server/Presentation/Controllers/

# 2. 使用serena分析Controller变更
serena find_symbol "Controller" --include-body

# 3. 对比API端点差异
```

**如发现API变更**：
- 自动记录变更的端点
- 生成API文档更新清单
- 检查影响的文档范围

**示例报告**：
```
📝 API变更检测

新增端点：
- POST /api/patients/{id}/prescriptions
  Controller: PatientController.cs
  方法: CreatePrescription
  影响文档：
    - docs/api/patients-api.md
    - docs/quick-reference/api-reference.md

修改端点：
- GET /api/consultations/{id}
  变更: 返回类型从ConsultationDto改为ConsultationDetailDto
  影响文档：
    - docs/api/consultations-api.md
    - docs/quick-reference/api-reference.md

删除端点：
- DELETE /api/temp-endpoint
  影响文档：
    - docs/api/deprecated-apis.md（需移动至已废弃）
```

---

### 第二步：检测架构调整

使用`serena`工具检测模块结构变更、新增服务、Repository变更。

**检测内容**：
1. **新增模块** - 新增的业务模块目录
2. **服务变更** - Service接口或实现变更
3. **Repository变更** - 新增或修改Repository

**检测命令**：
```bash
# 1. 检测模块目录结构变更
git diff --name-status HEAD~1 src/Server/Application/

# 2. 检测Service变更
serena search_for_pattern "interface.*Service" --paths_include_glob="*.cs"

# 3. 检测Repository变更
serena search_for_pattern "class.*Repository" --paths_include_glob="*.cs"
```

**影响范围分析**（需人工确认）：

**新增模块**：
- 影响：`docs/architecture/server/README.md`（8个模块列表）
- 影响：`docs/modules/README.md`（模块索引）
- 影响：`docs/index.md`（导航链接）
- 建议：创建新模块文档`docs/modules/{module-name}/README.md`

**Service变更**：
- 影响：`docs/architecture/server/services.md`
- 影响：`docs/quick-reference/code-patterns.md`（Service模式示例）

**Repository变更**：
- 影响：`docs/architecture/server/repositories.md`
- 影响：`docs/quick-reference/code-patterns.md`（Repository模式示例）

**示例报告**：
```
🏗️ 架构调整检测

新增模块：
- 模块: LYBT.Server.Application.Reports
  路径: src/Server/Application/Reports/
  影响文档：
    - docs/architecture/server/README.md（需添加第9个模块）
    - docs/modules/README.md（需创建Reports模块文档）
    - docs/index.md（需添加导航链接）

Service变更：
- Service: IReportService
  变更: 新增接口
  影响文档：
    - docs/architecture/server/services.md

❓ 请确认是否需要创建完整的Reports模块文档？
```

---

### 第三步：检测数据模型变更

使用`grep`工具检测实体和DTO变更。

**检测内容**：
1. **实体变更** - Domain/Entities目录下的类
2. **DTO变更** - Shared/Contracts/DTOs目录下的类
3. **Enum变更** - Shared/Enums目录下的枚举

**检测命令**：
```bash
# 检测实体变更
git diff HEAD~1 src/Server/Domain/Entities/

# 检测DTO变更
git diff HEAD~1 src/Shared/Contracts/DTOs/

# 检测Enum变更
git diff HEAD~1 src/Shared/Enums/
```

**影响文档**：
- 实体变更 → `docs/architecture/server/domain-model.md`
- DTO变更 → `docs/api/{module}-api.md`（请求/响应示例）
- Enum变更 → `docs/quick-reference/api-reference.md`

---

### 第四步：检测配置文件变更

使用`git diff`检测配置文件变更。

**检测文件**：
- `appsettings.json` / `appsettings.Development.json`
- `launchSettings.json`
- `.runsettings`
- `Directory.Build.props`

**影响文档**：
- `docs/quick-reference/config-templates.md`
- `docs/development/server/environment-setup.md`

---

### 第五步：验证文档链接有效性

使用`grep`工具检查文档中的内部链接是否有效。

**检测命令**：
```bash
# 检查Markdown文件中的内部链接
grep -r "\[.*\](docs/" docs/ --include="*.md"
```

**验证逻辑**：
1. 提取所有内部链接（`docs/`开头的链接）
2. 使用`filesystem`工具检查文件是否存在
3. 报告失效链接

**示例报告**：
```
🔗 文档链接验证

失效链接：
- 文档: docs/architecture/server/README.md:45
  链接: docs/architecture/server/old-services.md
  状态: 文件不存在
  建议: 更新为docs/architecture/server/services.md

- 文档: docs/index.md:12
  链接: docs/modules/auth/README.md
  状态: 文件不存在
  建议: 检查模块名称是否正确
```

---

### 第六步：生成文档更新清单

汇总所有检测结果，生成文档更新待办清单。

**清单格式**：
```markdown
# 文档更新清单

生成时间：[时间戳]
代码变更范围：[Git commit range]

## 🔴 必须更新（自动检测到的变更）

### API文档
- [ ] 更新`docs/api/patients-api.md`
  - 新增端点：POST /api/patients/{id}/prescriptions
  - 添加请求/响应示例

- [ ] 更新`docs/quick-reference/api-reference.md`
  - 添加新端点到快速参考

### 架构文档
- [ ] 更新`docs/architecture/server/README.md`
  - 添加第9个模块：Reports

## 🟡 建议更新（需人工确认）

### 模块文档
- [ ] 创建`docs/modules/reports/README.md`
  - 分析：新增Reports模块
  - 建议：创建完整模块文档（包含API、架构、使用指南）
  - 状态：等待确认

### 示例代码
- [ ] 更新`docs/quick-reference/code-patterns.md`
  - 分析：新增ReportService
  - 建议：添加Service模式示例
  - 状态：等待确认

## ✅ 链接验证
- [ ] 修复`docs/architecture/server/README.md:45`
  - 失效链接：docs/architecture/server/old-services.md
  - 修复为：docs/architecture/server/services.md
```

---

## 工具协同

本Skill调用以下MCP工具：

1. **git** - 检测文件变更（git diff）
2. **serena** - 分析代码结构（API、Service、Repository）
3. **grep** - 检测数据模型和链接
4. **filesystem** - 验证文件存在性

**执行顺序**：
```
git diff（变更检测）→ serena（代码分析）→ grep（模式匹配）→ filesystem（链接验证）→ 生成清单
```

---

## 测试场景

### 场景1：检测API新增端点

**测试代码**：
```csharp
// PatientController.cs（新增）
[HttpPost("{id}/prescriptions")]
public async Task<ActionResult<PrescriptionDto>> CreatePrescription(
    int id,
    [FromBody] CreatePrescriptionRequest request)
{
    // ...
}
```

**预期输出**：
```
📝 API变更检测

新增端点：
- POST /api/patients/{id}/prescriptions
  Controller: PatientController
  方法: CreatePrescription
  影响文档：
    - docs/api/patients-api.md
    - docs/quick-reference/api-reference.md

文档更新清单：
- [ ] 在patients-api.md中添加新端点文档
- [ ] 在api-reference.md中添加快速参考
```

---

### 场景2：检测架构调整（新增模块）

**测试代码**：
```
新增目录: src/Server/Application/Reports/
新增文件: ReportService.cs, IReportService.cs
```

**预期输出**：
```
🏗️ 架构调整检测

新增模块：
- 模块: Reports
  路径: src/Server/Application/Reports/
  影响文档：
    - docs/architecture/server/README.md
    - docs/modules/README.md
    - docs/index.md

建议操作：
1. 在server/README.md中添加Reports模块描述
2. 创建docs/modules/reports/README.md
3. 在index.md中添加Reports导航链接

❓ 请确认是否需要创建完整的Reports模块文档？
```

---

## 使用指南

### 触发时机

当用户提出以下请求时，自动触发本Skill：
- "检查文档是否需要更新"
- "生成文档更新清单"
- "验证文档同步"
- "检测文档链接"
- 完成代码变更后

### 执行步骤

1. **确定检查范围**：询问检查哪些变更（当前commit/PR/特定范围）
2. **检测变更**：分析API、架构、数据模型变更
3. **影响分析**：评估影响的文档范围（自动 + 建议）
4. **验证链接**：检查文档内部链接有效性
5. **生成清单**：生成文档更新待办清单
6. **等待确认**：对于建议更新项，等待用户决策

### 注意事项

- API变更 → 自动检测，生成清单
- 架构调整 → 分析影响，等待确认
- 链接失效 → 自动检测，直接报告
- 清单分为"必须更新"和"建议更新"两类

---

## 限制和免责

- 本Skill基于静态分析和Git差异，无法检测未提交的变更
- 文档影响范围基于启发式规则，可能遗漏边界情况
- 建议结合人工审查确认文档更新完整性
- 新增功能可能需要创建全新文档，需人工判断

---

## 相关资源

- 文档导航：`docs/index.md`
- 文档维护指南：`docs/support/documentation-maintenance.md`
- 快速参考：`docs/quick-reference/`
- 架构文档：`docs/architecture/`
