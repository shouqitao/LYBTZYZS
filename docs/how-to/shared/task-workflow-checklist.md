# 任务执行流程检查清单

**版本**: v6.0（Constitution + Checklists增强版）
**创建日期**: 2025-10-16
**适用范围**: 所有新功能开发、重大重构、复杂Bug修复
**使用方法**: 每个任务开始前打印此清单，逐项检查并勾选，完成后归档

---

## 📋 使用说明

### 适用场景
- ✅ **新功能开发** - 完整流程（Spec-Driven + Issue-Driven）
- ✅ **重大重构** - 完整流程（Spec-Driven + Issue-Driven）
- ✅ **复杂Bug修复** - 简化流程（Constitution检查 + Issue-Driven）
- ⚠️ **简单Bug修复** - 最小流程（Issue创建 + 代码修复 + PR）
- ⚠️ **文档更新** - 最小流程（Issue创建 + 文档修改 + PR）

### 检查方式
- 📝 打印此文档或复制到Markdown编辑器
- ✅ 完成每个步骤后，在复选框中打勾 `- [x]`
- 🚫 不适用的步骤标记为 `- [N/A]`
- ⚠️ 遇到问题时记录在"问题记录"区域
- 📊 任务完成后归档到 `.spec-workflow/specs/{spec-name}/workflow-record.md`

---

## 🎯 阶段0：任务启动前检查（所有任务必做）

### 0.1 环境准备
- [ ] 执行 `git pull` 获取最新代码
- [ ] 执行 `dotnet restore LYBT.All.sln` 恢复依赖
- [ ] 执行 `dotnet build LYBT.All.sln -c Release --no-restore` 确认编译通过
- [ ] 执行 `dotnet test LYBT.All.sln -c Release` 记录基线测试结果
- [ ] 确认开发环境正常（VS2022/Rider、数据库、工具）

**问题记录**:
```
编译错误：
测试失败：
环境问题：
```

---

### 0.2 Constitution合规性检查（新功能/重构必做）

**检查依据**: `.spec-workflow/steering/constitution.md`

#### 架构原则检查
- [ ] **三层对齐架构** - Server/Client/Shared层次清晰，无跨层直接调用
- [ ] **依赖注入规范** - 仅使用构造函数注入，无ServiceLocator
- [ ] **技术黑名单** - 未使用Redis/CQRS/MediatR/Docker/GraphQL等禁用技术

#### MVP优先原则检查
- [ ] **MVP必需性判断** - 问："这个功能MVP需要吗？"答案明确为"是"
- [ ] **够用即好** - 避免过度抽象（单一使用场景不需要抽象）
- [ ] **增量优化** - 小步快跑，无大规模重构
- [ ] **无投机性优化** - 无性能瓶颈不需要预先优化

#### 安全合规检查
- [ ] **双轨认证系统** - Users表 与 AdminSecrets表物理隔离
- [ ] **敏感数据保护** - 密码使用BCrypt/PBKDF2哈希
- [ ] **HTTPS传输** - 生产环境强制HTTPS

**不合规项记录**:
```
违规项：
计划处理：
豁免理由（如适用）：
```

---

### 0.3 GitHub Issue检查（所有任务必做）

- [ ] **Issue已创建** - GitHub Issue已存在，编号: #_____
- [ ] **Issue描述完整** - 包含任务描述、目标、验收标准、参考资料
- [ ] **Epic关联** - Issue已关联到Epic（如适用）
- [ ] **标签正确** - `type:*`、`module:*`、`priority:*`、`status:todo` 标签已添加
- [ ] **工作量估算** - 已估算工作量（小时/天）

**Issue信息**:
```
Issue编号: #_____
Issue标题:
Epic关联: #_____ (如适用)
工作量估算: _____小时
```

---

## 📝 阶段1：Spec-Driven 需求分析（新功能/重构必做）

### 1.1 创建Spec目录结构

- [ ] 创建Spec目录: `.spec-workflow/specs/{spec-name}/`
- [ ] 创建Checklist目录: `.spec-workflow/specs/{spec-name}/checklists/`
- [ ] 复制必选Checklist模板:
  - [ ] `requirements-checklist.md`
  - [ ] `security-checklist.md`
- [ ] 根据功能类型复制可选Checklist（选择适用的）:
  - [ ] `ux-checklist.md` - 面向用户的功能
  - [ ] `performance-checklist.md` - 数据密集型/核心功能
  - [ ] `accessibility-checklist.md` - 公共功能（可选）

**Spec信息**:
```
Spec名称: {spec-name}
Spec路径: .spec-workflow/specs/{spec-name}/
复制的Checklist: requirements, security, _____
```

---

### 1.2 创建requirements.md

**参考**: Steering Documents (`.spec-workflow/steering/product.md`, `tech.md`, `structure.md`, `constitution.md`)

#### 必需章节检查
- [ ] **问题陈述** - 清晰定义要解决的问题
- [ ] **用户价值** - 说明用户受益和优先级（P0/P1/P2）
- [ ] **验收标准** - 可测试的验收标准（正常路径+异常路径）
- [ ] **范围边界** - 明确包含和排除的功能
- [ ] **依赖关系** - 列出依赖的现有功能/数据模型/外部服务
- [ ] **影响范围** - 列出影响的模块和需要更新的文档
- [ ] **技术可行性** - 初步评估技术方案和风险

#### Constitution合规性
- [ ] 在requirements.md中引用Constitution相关原则
- [ ] 说明如何符合MVP优先原则
- [ ] 说明如何符合三层对齐架构

**文档路径**: `.spec-workflow/specs/{spec-name}/requirements.md`

---

### 1.3 填写requirements-checklist.md（第一轮）

**文件**: `.spec-workflow/specs/{spec-name}/checklists/requirements-checklist.md`

- [ ] 完成"1. 需求定义清晰性"（问题陈述、用户价值、验收标准）
- [ ] 完成"2. 范围管理"（范围边界、依赖关系、影响范围）
- [ ] 完成"3. Constitution合规性"（架构原则、MVP原则、开发流程）
- [ ] 完成"4. 技术可行性"（技术方案、数据模型、依赖评估）
- [ ] 记录高风险项和中风险项

**通过标准**:
- 通过率 ≥ _____% (目标≥90%)
- 所有MUST项通过: ☐ 是 / ☐ 否

---

### 1.4 Dashboard审批requirements.md

- [ ] 访问Dashboard: http://localhost:3000
- [ ] 提交requirements.md审批请求
- [ ] 等待审批通过（或根据反馈修订）
- [ ] 审批通过后继续下一阶段

**审批记录**:
```
提交时间:
审批结果: ☐ 通过 / ☐ 需修订
修订内容（如适用）:
```

---

## 🏗️ 阶段2：Spec-Driven 设计方案（新功能/重构必做）

### 2.1 创建design.md

**参考**: requirements.md、Architecture文档 (`docs/explanation/architecture/`)

#### 必需章节检查
- [ ] **架构设计** - Server/Client/Shared层次设计
- [ ] **数据模型** - 实体关系、字段定义、约束
- [ ] **API设计** - 端点定义、请求/响应格式
- [ ] **UI设计** - 页面结构、交互流程（Client功能）
- [ ] **技术方案** - 具体实现技术选型
- [ ] **安全设计** - 身份认证、授权、数据保护
- [ ] **测试策略** - 单元测试、集成测试、E2E测试
- [ ] **部署方案** - 数据库迁移、配置变更

#### 架构合规性
- [ ] 符合Server端三层架构（Controller → Service → Repository）
- [ ] 符合Client端MVVM五层架构（View → ViewModel → Service → ApiClient → Model）
- [ ] 依赖方向正确（无逆向依赖）
- [ ] 使用构造函数注入

**文档路径**: `.spec-workflow/specs/{spec-name}/design.md`

---

### 2.2 填写security-checklist.md（设计阶段）

**文件**: `.spec-workflow/specs/{spec-name}/checklists/security-checklist.md`

- [ ] 完成"1. 身份认证与授权"（双轨认证、JWT、权限控制）
- [ ] 完成"2. 数据保护"（敏感数据加密、数据脱敏、数据完整性）
- [ ] 完成"3. 输入验证与防护"（输入验证、注入攻击防护）
- [ ] 完成"8. 医疗数据特定要求"（患者隐私、医案完整性）
- [ ] 识别严重风险项并制定缓解措施

**通过标准**:
- 通过率 ≥ _____% (目标≥90%)
- 严重风险项已解决: ☐ 是 / ☐ 否

---

### 2.3 更新requirements-checklist.md（第二轮）

- [ ] 完成"5. 文档质量"（文档结构、语言、格式、可读性）
- [ ] 完成"6. 质量检查总结"（检查结果、风险评估、审批决策）
- [ ] 计算总通过率

**最终通过率**: _____% (必须≥90%)

---

### 2.4 Dashboard审批design.md

- [ ] 提交design.md审批请求
- [ ] 提交Checklist验证结果摘要
- [ ] 等待审批通过（或根据反馈修订）
- [ ] 审批通过后继续下一阶段

**审批记录**:
```
提交时间:
审批结果: ☐ 通过 / ☐ 需修订
Checklist通过率: _____%
修订内容（如适用）:
```

---

## ✅ 阶段3：Spec-Driven 任务分解（新功能/重构必做）

### 3.1 创建tasks.md

**参考**: design.md、MVP任务清单（如适用）

#### 任务分解原则
- [ ] 任务粒度合理（2-8小时/任务）
- [ ] 任务可并行标注（标注依赖关系）
- [ ] 任务优先级明确（P0/P1/P2）
- [ ] 任务验收标准清晰
- [ ] 任务工作量估算合理

#### 任务分类
- [ ] Server端任务（SRV-1, SRV-2...）
- [ ] Client端任务（CLI-1, CLI-2...）
- [ ] 共享层任务（SHR-1, SHR-2...）
- [ ] 测试任务（TEST-1, TEST-2...）
- [ ] 文档任务（DOC-1, DOC-2...）

**文档路径**: `.spec-workflow/specs/{spec-name}/tasks.md`
**任务总数**: _____个
**总工作量**: _____小时

---

### 3.2 填写可选Checklist（根据功能类型）

#### UX Checklist（面向用户功能）
**文件**: `.spec-workflow/specs/{spec-name}/checklists/ux-checklist.md`

- [ ] 完成"1. 交互设计"（响应速度、反馈机制、错误处理）
- [ ] 完成"4. 表单设计"（布局、控件选择、验证）
- [ ] 完成"5. 数据展示"（列表/表格、数据可读性、数据操作）

#### Performance Checklist（数据密集型/核心功能）
**文件**: `.spec-workflow/specs/{spec-name}/checklists/performance-checklist.md`

- [ ] 完成"1. 数据库性能"（查询优化、事务优化、数据访问模式）
- [ ] 完成"2. API性能"（响应时间、并发处理、有效载荷优化）
- [ ] 完成"11. 性能基线与目标"（响应时间目标、吞吐量目标、资源使用目标）

---

### 3.3 Dashboard审批tasks.md

- [ ] 提交tasks.md审批请求
- [ ] 提交可选Checklist验证结果摘要（如适用）
- [ ] 等待审批通过
- [ ] 审批通过后生成GitHub Issues

**审批记录**:
```
提交时间:
审批结果: ☐ 通过 / ☐ 需修订
任务总数: _____个
修订内容（如适用）:
```

---

### 3.4 生成GitHub Issues

- [ ] 创建Epic Issue: `[Epic] {功能名称} (SPEC-{编号})`
- [ ] 为每个Task创建子Issue: `[Spec: {spec-name}] [{类型}-N] {任务描述}`
- [ ] 所有Issue关联到Epic
- [ ] 所有Issue添加正确标签（`type:*`, `module:*`, `priority:*`, `epic:*`, `status:todo`）
- [ ] 更新tasks.md添加Issue链接

**Epic Issue**: #_____
**子Issue范围**: #_____ - #_____
**总Issue数**: _____个

---

## 🚀 阶段4：Issue-Driven 开发实施（所有任务必做）

### 4.1 选择待实施Issue

- [ ] 从GitHub Issues中选择一个`status:todo`的Issue
- [ ] 检查Issue依赖是否已完成
- [ ] 更新Issue标签为`status:in-progress`
- [ ] 记录开始时间

**当前Issue**: #_____
**Issue标题**:
**开始时间**:

---

### 4.2 创建功能分支

**分支命名规范**: `feature/{issue-id}-{description}`

- [ ] 切换到master分支: `git checkout master`
- [ ] 拉取最新代码: `git pull`
- [ ] 创建功能分支: `git checkout -b feature/{issue-id}-{description}`
- [ ] 推送远程分支: `git push -u origin feature/{issue-id}-{description}`

**分支名称**: feature/{issue-id}-{description}

---

### 4.3 代码实施

#### 编码规范检查
- [ ] **语言统一** - 代码注释使用中文
- [ ] **文件编码** - UTF-8 with BOM
- [ ] **命名规范** - PascalCase（类）、_camelCase（私有字段）、UPPER_SNAKE_CASE（常量）
- [ ] **依赖注入** - 仅使用构造函数注入
- [ ] **异步约定** - I/O操作使用async/await
- [ ] **文件体量** - 单文件≤500行（建议）

#### Constitution合规检查（开发过程）
- [ ] 参考Checklist要求实施代码
- [ ] 无跨层直接调用
- [ ] 无ServiceLocator使用
- [ ] 无技术黑名单违规

#### 提交规范
- [ ] 提交信息使用中文
- [ ] 提交信息格式: `类型(范围): 描述 #Issue号`
- [ ] 示例: `feat(formula): 添加延迟绑定字段 #1344`

**提交记录**:
```
提交1:
提交2:
提交3:
```

---

### 4.4 单元测试（核心逻辑必做）

- [ ] 为新增核心逻辑补充单元测试
- [ ] 使用AAA模式（Arrange - Act - Assert）
- [ ] 测试覆盖率符合Constitution要求：
  - [ ] 核心业务逻辑 ≥ 80%
  - [ ] Service层 ≥ 75%
  - [ ] Repository层 ≥ 70%
- [ ] 执行测试: `dotnet test LYBT.All.sln -c Release`
- [ ] 所有测试通过

**测试覆盖率**:
```
核心业务逻辑: _____%
Service层: _____%
Repository层: _____%
```

---

### 4.5 本地验证

- [ ] 编译通过: `dotnet build LYBT.All.sln -c Release --no-restore`
- [ ] 测试通过: `dotnet test LYBT.All.sln -c Release`
- [ ] 代码格式化: `dotnet format LYBT.All.sln`
- [ ] 功能验证通过（手动测试核心流程）

**验证结果**:
```
编译: ☐ 通过 / ☐ 失败
测试: ☐ 通过 / ☐ 失败 (失败项: _____)
功能: ☐ 通过 / ☐ 失败
```

---

## ✅ 阶段5：Issue-Driven 质量验证（新功能/重构必做）

### 5.1 填写Checklist实施阶段检查项

#### Requirements Checklist最终验证
**文件**: `.spec-workflow/specs/{spec-name}/checklists/requirements-checklist.md`

- [ ] 验证所有验收标准已满足
- [ ] 验证范围边界未超出
- [ ] 验证Constitution合规性
- [ ] 更新"质量检查总结"

#### Security Checklist最终验证
**文件**: `.spec-workflow/specs/{spec-name}/checklists/security-checklist.md`

- [ ] 完成"4. 会话管理"检查项
- [ ] 完成"5. 日志与审计"检查项
- [ ] 完成"10. 安全测试"检查项（渗透测试、代码扫描）
- [ ] 验证所有严重/高风险项已解决
- [ ] 更新"质量检查总结"

#### UX Checklist最终验证（如适用）
**文件**: `.spec-workflow/specs/{spec-name}/checklists/ux-checklist.md`

- [ ] 完成"6. 对话框与通知"检查项
- [ ] 完成"7. 性能感知"检查项
- [ ] 完成"10. 一致性检查"检查项
- [ ] 更新"质量检查总结"

#### Performance Checklist最终验证（如适用）
**文件**: `.spec-workflow/specs/{spec-name}/checklists/performance-checklist.md`

- [ ] 完成"10. 性能测试"检查项（负载测试、性能监控、性能分析）
- [ ] 验证性能目标已达成（API响应≤500ms、页面加载≤2s等）
- [ ] 更新"质量检查总结"

---

### 5.2 计算Checklist通过率

**必选清单通过率**（必须≥90%）:
```
requirements-checklist.md:
  - 总检查项: _____项
  - 通过项: _____项
  - 通过率: _____%

security-checklist.md:
  - 总检查项: _____项
  - 通过项: _____项
  - 通过率: _____%

必选清单综合通过率: _____%（必须≥90%）
```

**可选清单通过率**（建议≥80%）:
```
ux-checklist.md（如适用）:
  - 总检查项: _____项
  - 通过项: _____项
  - 通过率: _____%

performance-checklist.md（如适用）:
  - 总检查项: _____项
  - 通过项: _____项
  - 通过率: _____%
```

**质量验证结论**:
- [ ] ✅ 通过 - 必选清单通过率≥90%，可提交PR
- [ ] ⚠️ 有条件通过 - 部分改进项可后续优化，需记录技术债务
- [ ] ❌ 不通过 - 存在严重问题，必须修复后重新验证

---

### 5.3 文档同步（影响文档时必做）

**参考**: CLAUDE.md "2.4 完成后的文档系统更新"

#### 文档影响评估
- [ ] 评估影响的文档范围（架构/API/快速参考/模块文档）
- [ ] 列出需要更新的文档清单

**需要更新的文档**:
```
架构文档:
  - docs/architecture/server/README.md: ☐ 是 / ☐ 否
  - docs/architecture/client/README.md: ☐ 是 / ☐ 否
  - docs/architecture/shared/README.md: ☐ 是 / ☐ 否

开发指南:
  - docs/development/server/README.md: ☐ 是 / ☐ 否
  - docs/development/client/README.md: ☐ 是 / ☐ 否
  - docs/development/shared/README.md: ☐ 是 / ☐ 否

API文档:
  - docs/api/README.md: ☐ 是 / ☐ 否

快速参考:
  - docs/quick-reference/api-reference.md: ☐ 是 / ☐ 否
  - docs/quick-reference/code-patterns.md: ☐ 是 / ☐ 否
  - docs/quick-reference/config-templates.md: ☐ 是 / ☐ 否

导航索引:
  - docs/index.md: ☐ 是 / ☐ 否
  - 相关README: ☐ 是 / ☐ 否
```

#### 文档更新执行
- [ ] 更新所有标记为"是"的文档
- [ ] 验证所有文档链接有效
- [ ] 提交文档变更到功能分支

**文档提交记录**:
```
提交信息: docs: 更新XXX文档同步代码变更 #Issue号
提交SHA:
```

---

## 📤 阶段6：Issue-Driven PR提交（所有任务必做）

### 6.1 准备PR描述

#### PR标题格式
`[{类型}] {简要描述} (#{Issue号})`
- 类型: feat/fix/refactor/docs/test/chore
- 示例: `[feat] 添加验方延迟绑定功能 (#1344)`

**PR标题**:

---

#### PR描述模板

```markdown
## 📋 关联Issue
Closes #{Issue号}
Epic: #{Epic号}（如适用）

## 📝 变更摘要
[简要描述此PR的变更内容]

## ✅ 验收标准
- [x] 标准1
- [x] 标准2
- [x] 标准3

## 🔍 代码变更
### Server端变更
- 新增文件:
- 修改文件:
- 删除文件:

### Client端变更
- 新增文件:
- 修改文件:
- 删除文件:

### 测试变更
- 新增测试:
- 测试覆盖率: _____%

## 📊 Checklist验证结果
### 必选清单（通过率≥90%）
- requirements-checklist.md: _____%
- security-checklist.md: _____%
- **综合通过率**: _____%

### 可选清单
- ux-checklist.md: _____%（如适用）
- performance-checklist.md: _____%（如适用）

### 未通过项说明
[列出未通过的检查项及原因]

## 📚 文档变更
- [x] 架构文档已更新
- [x] API文档已更新
- [x] 快速参考已更新
- [x] 导航索引已更新

## 🧪 测试结果
- 编译: ✅ 通过
- 单元测试: ✅ 通过 (____个测试)
- 集成测试: ✅ 通过 (____个测试)（如适用）
- 手动测试: ✅ 通过

## 📸 截图/演示
[如有UI变更，附上截图或演示视频]

## ⚠️ 注意事项
[如有特殊注意事项，列在此处]

## 🔄 后续工作
[如有后续优化计划，列在此处]

---

🤖 Generated with Claude Code
```

---

### 6.2 创建Pull Request

- [ ] 推送所有提交到远程分支: `git push`
- [ ] 访问GitHub创建PR
- [ ] 粘贴PR描述模板并填写完整
- [ ] 设置PR标签（与Issue一致）
- [ ] 请求审查者（如需要）
- [ ] 等待CI/CD检查通过

**PR链接**: https://github.com/shouqitao/LYBTZYZS/pull/_____

---

### 6.3 代码审查

- [ ] GitHub Copilot自动审查通过（如配置）
- [ ] Claude Code二审通过（如需要）
- [ ] 人工审查通过
- [ ] 解决所有审查意见

**审查记录**:
```
审查者1:
  - 意见:
  - 状态: ☐ 通过 / ☐ 需修改

审查者2:
  - 意见:
  - 状态: ☐ 通过 / ☐ 需修改
```

---

### 6.4 合并PR

- [ ] 所有CI/CD检查通过
- [ ] 所有审查者批准
- [ ] 合并PR到master分支
- [ ] 删除功能分支（可选）
- [ ] 验证Issue自动关闭

**合并信息**:
```
合并时间:
合并SHA:
Issue状态: ☐ 已关闭 / ☐ 需手动关闭
```

---

## 🧹 阶段7：后台清理（所有任务必做）

### 7.1 资源清理

**参考**: CLAUDE.md "9. 代码修复后的后台清理"

- [ ] **终止临时进程** - 停止dotnet run/测试进程
- [ ] **释放资源与缓存** - 清理BIN/、logs/、TestResults/
- [ ] **还原配置** - 移除临时环境变量/测试密钥
- [ ] **关闭外部连接** - 断开数据库连接/HTTP代理
- [ ] **端口检查** - 确认5001等端口未被占用

---

### 7.2 文档归档

- [ ] 归档Workflow Record到 `.spec-workflow/specs/{spec-name}/workflow-record.md`
- [ ] 归档Checklist验证结果
- [ ] 更新任务进度（tasks.md或MVP清单）

**归档记录**:
```
Workflow Record: .spec-workflow/specs/{spec-name}/workflow-record.md
完成时间:
实际工作量: _____小时（估算: _____小时）
```

---

### 7.3 经验总结（可选）

**遇到的问题**:
```
问题1:
解决方案:

问题2:
解决方案:
```

**改进建议**:
```
流程改进:

工具改进:

文档改进:
```

---

## 📊 任务完成检查总结

### 任务信息
- **Issue编号**: #_____
- **Issue标题**:
- **开始时间**:
- **完成时间**:
- **实际工作量**: _____小时
- **估算工作量**: _____小时
- **工作量偏差**: ±_____%

### 质量指标
- **编译**: ☐ 通过 / ☐ 失败
- **测试**: ☐ 通过 / ☐ 失败
- **测试覆盖率**: _____%
- **Checklist通过率**: _____%（必选）
- **Constitution合规**: ☐ 是 / ☐ 否
- **文档同步**: ☐ 完成 / ☐ 不适用

### 流程遵循度
- **Constitution检查**: ☐ 完成 / ☐ 跳过（简单任务）
- **Spec-Driven流程**: ☐ 完成 / ☐ 跳过（简单任务）
- **Checklist验证**: ☐ 完成 / ☐ 跳过（简单任务）
- **文档同步**: ☐ 完成 / ☐ 不适用
- **PR规范**: ☐ 符合 / ☐ 不符合

### 总体评价
- [ ] ✅ 优秀 - 严格遵循流程，质量指标全部达标
- [ ] ⚠️ 良好 - 基本遵循流程，部分指标有待改进
- [ ] ❌ 需改进 - 流程遵循度低，质量指标未达标

---

## 📎 附录：快速参考

### Constitution路径
`.spec-workflow/steering/constitution.md`

### Checklist模板路径
`.spec-workflow/templates/checklists/`
- requirements-checklist.md
- security-checklist.md
- ux-checklist.md
- performance-checklist.md
- accessibility-checklist.md

### 核心文档路径
- MVP任务清单: `docs/tasks/mvp-task-checklist-2025-10-16.md`
- 工作流定义: `CLAUDE.md`
- Server架构: `docs/explanation/architecture/server/README.md`
- Client架构: `docs/explanation/architecture/client/README.md`
- Shared架构: `docs/explanation/architecture/shared/README.md`

### 常用命令
```bash
# 环境检查
git pull
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
dotnet test LYBT.All.sln -c Release

# 代码格式化
dotnet format LYBT.All.sln

# Dashboard启动
npx -y @pimzino/spec-workflow-mcp@latest D:\source\repos\LYBTZYZS --dashboard
```

---

**文档版本**: v6.0
**最后更新**: 2025-10-16
**适用范围**: 所有新功能、重构、复杂Bug修复
**维护者**: Claude Code

---

## 💡 使用提示

1. **打印此清单** - 每个任务开始前打印或复制到笔记本
2. **逐项检查** - 完成一项勾选一项，不要跳过
3. **记录问题** - 遇到问题立即记录在对应区域
4. **归档保存** - 任务完成后归档到Spec目录
5. **定期回顾** - 每周回顾清单，总结经验教训

**严格遵循此流程，确保代码质量和项目一致性！** 🎯
